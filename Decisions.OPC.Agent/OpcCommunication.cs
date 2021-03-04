using System;
using System.Collections.Generic;
using System.Linq;
using TitaniumAS.Opc.Client.Da;
using System.Collections.Concurrent;
using TitaniumAS.Opc.Client.Common;
using TitaniumAS.Opc.Client.Da.Browsing;
using System.Threading;

namespace Decisions.OPC.Agent
{
    public static class OpcCommunication
    {
        internal static ConcurrentDictionary<string, OpcDaServer> servers = new ConcurrentDictionary<string, OpcDaServer>();

        static ConcurrentDictionary<string, object> lockObjects = new ConcurrentDictionary<string, object>();

        static ConcurrentDictionary<string, PingThread> pingThreads = new ConcurrentDictionary<string, PingThread>();

        internal static object GetLockObject(string serverUrl)
        {
            return lockObjects.GetOrAdd(serverUrl, new object());
        }

        internal static bool ServerIsConnected(string serverUrl)
        {
            lock (GetLockObject(serverUrl))
            {
                OpcDaServer server;
                if (!servers.TryGetValue(serverUrl, out server))
                    return false;

                if (server.IsConnected)
                    return true;
                else // If connection failed, forget about this one. Reconnections could cause unpredictable behavior.
                    DisposeServer(serverUrl);

                return false;
            }
        }

        private static void DisposeServer(string serverUrl)
        {
            lock (GetLockObject(serverUrl))
            {
                OpcDaServer server;
                if (!servers.TryGetValue(serverUrl, out server))
                    return;

                if (server.IsConnected)
                    server.Disconnect();

                server.Dispose();
                servers.TryRemove(serverUrl, out server);
            }
        }

        public static void StopListening(string opcServerUrl)
        {
            lock (GetLockObject(opcServerUrl))
            {
                PingThread pingThread;
                if (pingThreads.TryGetValue(opcServerUrl, out pingThread))
                {
                    pingThread.IsRunning = false;
                    pingThreads.TryRemove(opcServerUrl, out pingThread);
                }

                DisposeServer(opcServerUrl);
            }
        }

        public static void StartListening(string opcServerUrl, string instructionId, OpcEventGroup[] eventGroups)
        {
            lock (GetLockObject(opcServerUrl))
            {
                if (eventGroups == null || eventGroups.Length == 0)
                    throw new Exception("No event groups specified");

                // Always recreate the server here, but keep the thread if it exists:
                DisposeServer(opcServerUrl);
                GetOrCreateServer(opcServerUrl);

                PingThread pingThread;
                if (pingThreads.TryGetValue(opcServerUrl, out pingThread) && pingThread.IsRunning)
                { // Make sure the thread is responding to the latest instruction:
                    pingThread.InstructionId = instructionId;
                }
                else
                {
                    if(pingThread != null) // If thread exists but encountered an error, replace it here:
                        pingThreads.TryRemove(opcServerUrl, out pingThread);

                    pingThread = new PingThread(opcServerUrl, instructionId) { IsRunning = true };
                    pingThreads.TryAdd(opcServerUrl, pingThread);
                    new Thread(pingThread.Run) { IsBackground = true }.Start();
                }

                foreach (OpcEventGroup eventGroup in eventGroups)
                {
                    AddOrUpdateEventGroup(opcServerUrl, eventGroup);
                }

            }
        }

        private static OpcDaServer GetOrCreateServer(string opcServerUrl)
        {
            if (string.IsNullOrEmpty(opcServerUrl))
                throw new Exception("URL is null or empty");
            OpcDaServer server;
            if (servers.TryGetValue(opcServerUrl, out server))
                return server;

            server = new OpcDaServer(UrlBuilder.Build(opcServerUrl));
            server.Connect();
            servers[opcServerUrl] = server;
            return server;
        }

        private static OpcDaServer GetServer(string opcServerUrl)
        {
            if (string.IsNullOrEmpty(opcServerUrl))
                throw new Exception("URL is null or empty");
            OpcDaServer server;
            servers.TryGetValue(opcServerUrl, out server);
            return server;
        }

        public static void AddOrUpdateEventGroup(string opcServerUrl, OpcEventGroup eventGroup)
        {
            lock (GetLockObject(opcServerUrl))
            {
                OpcDaServer server = GetServer(opcServerUrl);
                if (server == null)
                    return;

                if (eventGroup?.Paths == null || eventGroup.Paths.Length == 0)
                {
                    RemoveEventGroup(opcServerUrl, eventGroup.EventId);
                    return;
                }

                // If server isn't connected or there is no ping thread running, then we aren't listening, and can't do anything here:
                PingThread pingThread;
                if (!ServerIsConnected(opcServerUrl) || !pingThreads.TryGetValue(opcServerUrl, out pingThread) || !pingThread.IsRunning)
                    return;

                string groupName = "DecisionsGroup_" + eventGroup.EventId;
                OpcDaGroup group = server.Groups.FirstOrDefault(x => x.Name == groupName);

                if (group == null)
                {
                    group = server.AddGroup(groupName);
                    group.IsActive = true;
                    AddItemIdsToGroup(group, eventGroup.Paths);
                    OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
                    Callback(opcServerUrl, eventGroup.EventId, ConvertToBaseTagValues(values)); // Send all current values in this group before subscribing to further changes
                    group.ValuesChanged += (object sender, OpcDaItemValuesChangedEventArgs e) =>
                    {
                        Callback(opcServerUrl, eventGroup.EventId, ConvertToBaseTagValues(e.Values));
                    };
                }
                else
                {
                    string[] addedPaths = eventGroup.Paths.Except(group.Items.Select(x => x.ItemId)).ToArray();
                    OpcDaItem[] removedItems = group.Items.Where(x => !eventGroup.Paths.Contains(x.ItemId)).ToArray();

                    AddItemIdsToGroup(group, addedPaths);
                    if(removedItems.Length > 0)
                        group.RemoveItems(removedItems); // could check return value for errors here
                }

                group.PercentDeadband = eventGroup.Deadband;
                group.UpdateRate = TimeSpan.FromMilliseconds(eventGroup.UpdateRate > 0 ? eventGroup.UpdateRate : 100); // ValuesChanged won't be triggered if zero
            }
        }

        private static void AddItemIdsToGroup(OpcDaGroup group, string[] paths)
        {
            if (paths.Length == 0)
                return;
            OpcDaItemDefinition[] definitions = paths.Select(x => new OpcDaItemDefinition { ItemId = x, IsActive = true }).ToArray();
            OpcDaItemResult[] results = group.AddItems(definitions);

            foreach (OpcDaItemResult result in results)
            {
                if (result.Error.Failed)
                    ; //todo, error handling?
            }
        }

        private static BaseTagValue[] ConvertToBaseTagValues(OpcDaItemValue[] opcDaValues)
        {
            List<BaseTagValue> values = new List<BaseTagValue>();
            foreach (OpcDaItemValue valueChange in opcDaValues)
            {
                BaseTagValue value = OPCAgentUtils.GetTagWithValue(valueChange.Item.CanonicalDataType, valueChange.Value);
                value.Path = valueChange.Item.ItemId;
                value.Quality = new OpcQuality
                {
                    Status = (OpcQualityStatus)valueChange.Quality.Master,
                    SubStatus = (OpcQualitySubStatus)valueChange.Quality.Status,
                    Limit = (OpcQualityLimit)valueChange.Quality.Limit
                };
                value.TimeStamp = valueChange.Timestamp.DateTime; //todo, make sure this is utc
                values.Add(value);
            }
            return values.ToArray();
        }

        public static void RemoveEventGroup(string opcServerUrl, string eventId)
        {
            lock (GetLockObject(opcServerUrl))
            {
                OpcDaServer server = GetServer(opcServerUrl);
                if (server == null)
                    return;
                string groupName = "DecisionsGroup_" + eventId;
                OpcDaGroup group = server.Groups.FirstOrDefault(x => x.Name == groupName);
                if (group != null)
                    server.RemoveGroup(group);
            }
        }

        public static OpcInitialData GetInitialData(string opcServerUrl, bool valuesOnly)
        {
            lock (GetLockObject(opcServerUrl))
            {
                OpcDaServer server = GetOrCreateServer(opcServerUrl);
                var browser = new OpcDaBrowserAuto(server);
                OpcDaGroup group = server.AddGroup("DecisionsDataTypeGroup");
                OpcNode[] nodes = GetNodesRecursive(browser, group, null);
                //now that group has all tags, read all initial values at once:
                OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
                server.RemoveGroup(group);
                return new OpcInitialData { Nodes = valuesOnly ? null : nodes, Values = ConvertToBaseTagValues(values) };
            }
        }

        private static OpcNode[] GetNodesRecursive(OpcDaBrowserAuto browser, OpcDaGroup group, OpcNode parentNode)
        {
            OpcDaBrowseElement[] elements = browser.GetElements(parentNode?.FullPath); // fetches from root if null

            OpcNode[] results = elements.Select(x => new OpcNode
            {
                Name = x.Name,
                FullPath = x.ItemId,
                ItemId = x.IsItem ? x.ItemId : null
            }).ToArray();

            foreach (OpcNode node in results)
            {
                GetNodesRecursive(browser, group, node);
            }

            if (parentNode != null)
            {
                parentNode.Children = results;

                if (!string.IsNullOrEmpty(parentNode.ItemId)) // fetch data types for tags
                {
                    var def = new OpcDaItemDefinition
                    {
                        ItemId = parentNode.ItemId
                    };
                    OpcDaItemResult[] groupAddResults = group.AddItems(new OpcDaItemDefinition[] { def });

                    foreach (OpcDaItemResult addResult in groupAddResults)
                    {
                        if (addResult.Error.Failed)
                            throw new Exception($"Could not fetch type data for {parentNode.ItemId}");
                    }

                    OpcDaItem item = group.Items.FirstOrDefault(x => x.ItemId == parentNode.ItemId);
                    if (item != null)
                        parentNode.TypeName = item.CanonicalDataType.FullName;
                }
            }

            return results;
        }

        public static void SetTagValues(string opcServerUrl, BaseTagValueWrapper valuesWrapper)
        {
            lock (GetLockObject(opcServerUrl))
            {
                OpcDaServer server = GetOrCreateServer(opcServerUrl);
                OpcDaGroup group = server.Groups.FirstOrDefault(x => x.Name == "DecisionsWriteGroup");
                if (group == null)
                    group = server.AddGroup("DecisionsWriteGroup");

                BaseTagValue[] values = valuesWrapper.Values;
                if (values == null || values.Length == 0)
                    return;

                List<OpcDaItemDefinition> missing = new List<OpcDaItemDefinition>();
                foreach (BaseTagValue value in values)
                {
                    OpcDaItem item = group.Items.FirstOrDefault(x => x.ItemId == value.Path);
                    if (item == null)
                        missing.Add(new OpcDaItemDefinition { ItemId = value.Path });
                } // ensure that tags are in group

                if (missing.Count > 0)
                {
                    OpcDaItemResult[] addResults = group.AddItems(missing);
                    foreach (OpcDaItemResult result in addResults)
                    {
                        if (result.Error.Failed)
                            throw new Exception($"Set tag value failed: could not add tag to group");
                    }
                }

                List<OpcDaItem> items = new List<OpcDaItem>();
                List<object> itemValues = new List<object>();
                foreach (BaseTagValue value in values)
                {
                    OpcDaItem item = group.Items.First(x => x.ItemId == value.Path); //throw if missing
                    items.Add(item);
                    itemValues.Add(OPCAgentUtils.GetObjectValueFromTag(value));
                }
                HRESULT[] writeResults = group.Write(items, itemValues.ToArray());
                foreach (HRESULT writeResult in writeResults)
                {
                    if (writeResult.Failed)
                        throw new Exception($"Set tag value failed: Error while writing values: {writeResult.ToString()}");
                }
            }
        }

        public static void Callback(string opcServerUrl, string eventId, BaseTagValue[] values)
        {
            OPCCallbackServiceClient client = new OPCCallbackServiceClient();
            client.ValuesChanged(opcServerUrl, eventId, new BaseTagValueWrapper { Values = values });
        }

    }
}
