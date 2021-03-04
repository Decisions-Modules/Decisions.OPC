using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities;
using DecisionsFramework.Design.Flow.SystemConstants;
using DecisionsFramework.ServiceLayer.Agent;
using DecisionsFramework.ServiceLayer.Services.Agent;
using DecisionsFramework.ServiceLayer.Services.ClientEvents;

namespace Decisions.OPC
{
    public class OPCEngine
    {
        static ConcurrentDictionary<string, string> pingInstructionIdPerAgent = new ConcurrentDictionary<string, string>();
        internal static ConcurrentDictionary<string, DateTime?> lastPingTimePerInstruction = new ConcurrentDictionary<string, DateTime?>();

        internal static ConcurrentDictionary<string, BaseTagValue> mostRecentValues = new ConcurrentDictionary<string, BaseTagValue>();
        internal static ConcurrentDictionary<string, BaseTagValue> priorValues = new ConcurrentDictionary<string, BaseTagValue>();

        static Log log = new Log("OPCEngine");

        public static void StartListening(string url, string agentId, OpcEventGroup[] eventGroups)
        {
            AgentInstructions instr = new AgentInstructions()
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = "Start Listening on " + url,
                MustBeCompletedBy = DateUtilities.Now(),
                AgentInstructionHanlderType = "Decisions.OPC.Agent.StartListeningInstructionHandler",
                AgentResultsHandlerType = typeof(StartListeningResultHandler).FullName,
                Data = new[]
                {
                    new DataPair("opcServerUrl", url),
                    new DataPair("eventGroups", eventGroups),
                }
            };
            AgentService.Instance.InstructAgent(new SystemUserContext(), agentId, instr);

            string ignored;
            pingInstructionIdPerAgent.TryRemove(url + agentId, out ignored);
            pingInstructionIdPerAgent.TryAdd(url + agentId, instr.Id);
        }
        public static void StopListening(string url, string agentId)
        {
            AgentInstructions instr = new AgentInstructions()
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = "Stop Listening on " + url,
                MustBeCompletedBy = DateUtilities.Now(),
                AgentInstructionHanlderType = "Decisions.OPC.Agent.StopListeningInstructionHandler",
                Data = new[]
                {
                    new DataPair("opcServerUrl", url),
                }
            };
            AgentService.Instance.InstructAgent(new SystemUserContext(), agentId, instr);

            string ignored; // Forget this instruction & refresh folder, so IsListening will immediately switch to false:
            pingInstructionIdPerAgent.TryRemove(url + agentId, out ignored);

            Folder configFolder = EntityCache<OPCServerFolderBehaviorData>.GetCache().AllEntities.FirstOrDefault(s => s.Url == url)?.GetEntity() as Folder;
            if (configFolder == null)
                return;
            ClientEventsService.SendEvent(FolderMessage.FolderChangeEventId, new FolderChangedMessage(configFolder.FolderID));
        }

        static ORM<AgentInstructions> agentInstructionsOrm = new ORM<AgentInstructions>();
        internal const int LATE_PING_VALUE = 15;
        public static bool IsListening(string url, string agentId)
        {
            try
            {
                log.Debug("IsListening is being checked...");
                string instrId;
                if (!pingInstructionIdPerAgent.TryGetValue(url + agentId, out instrId))
                    return false;
                log.Debug("Found ping/startlistening instruction");

                DateTime? time;
                lastPingTimePerInstruction.TryGetValue(instrId, out time);

                if (time == null)
                    return false;

                DateTime now = DateTime.UtcNow;

                if (time.Value.AddSeconds(LATE_PING_VALUE) > now)
                    log.Debug($"Found a RECENT time (within {LATE_PING_VALUE} seconds). Ping time [{time.Value}] - Current time [{now}].");
                else
                    log.Debug($"Found an OLD time (not within {LATE_PING_VALUE} seconds). Ping time [{time.Value}] - Current time [{now}].");
                return (time.Value.AddSeconds(LATE_PING_VALUE) > now);
            }
            catch
            {
                log.Debug("Error in IsListening, returning false");
                return false;
            }
        }

        public static void AddOrUpdateEventGroup(string url, string agentId, OpcEventGroup eventGroup)
        {
            AgentInstructions instr = new AgentInstructions()
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = "Update Event Group on " + url,
                MustBeCompletedBy = DateUtilities.Now(),
                AgentInstructionHanlderType = "Decisions.OPC.Agent.AddOrUpdateEventGroupInstructionHandler",
                Data = new[]
                {
                    new DataPair("opcServerUrl", url),
                    new DataPair("eventGroup", eventGroup),
                }
            };
            AgentService.Instance.InstructAgent(new SystemUserContext(), agentId, instr);
        }

        public static void RemoveEventGroup(string url, string agentId, string eventId)
        {
            AgentInstructions instr = new AgentInstructions()
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = "Remove Event Group on " + url,
                MustBeCompletedBy = DateUtilities.Now(),
                AgentInstructionHanlderType = "Decisions.OPC.Agent.RemoveEventGroupInstructionHandler",
                Data = new[]
                {
                    new DataPair("opcServerUrl", url),
                    new DataPair("eventId", eventId),
                }
            };
            AgentService.Instance.InstructAgent(new SystemUserContext(), agentId, instr);
        }


        public static void GetInitialData(string url, string agentId, bool valuesOnly)
        {
            AgentInstructions instr = new AgentInstructions()
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = "Get Initial Data on " + url,
                MustBeCompletedBy = DateUtilities.Now(),
                AgentInstructionHanlderType = "Decisions.OPC.Agent.GetInitialDataInstructionHandler",
                AgentResultsHandlerType = typeof(GetInitialDataResultHandler).FullName,
                Data = new[]
                {
                    new DataPair("opcServerUrl", url),
                    new DataPair("valuesOnly", valuesOnly),
                }
            };
            AgentService.Instance.InstructAgent(new SystemUserContext(), agentId, instr);
        }

        internal static void HandleInitialData(string url, OpcInitialData initialData)
        {
            log.Debug("HandleInitialData entered");
            OPCServerFolderBehaviorData folderExt = EntityCache<OPCServerFolderBehaviorData>.GetCache().AllEntities.FirstOrDefault(s => s.Url == url);
            Folder f = folderExt?.GetEntity() as Folder;
            if (f == null)
                throw new Exception("No OPC server found with this URL");

            if(initialData.Nodes != null)
                UpdateTagsInFolder(f.FolderID + ".tagdata", f.FolderID, url, initialData.Nodes);

            string[] eventIds = FolderService.GetFolderEntities<OPCEvent>(f.FolderID).Select(e => e.Id).ToArray();

            foreach (BaseTagValue value in initialData.Values)
            {
                // Record the value separately for each event, because they might update at different rates:
                foreach(string eventId in eventIds)
                {
                    string key = eventId + "|" + value.Path;
                    OPCEngine.mostRecentValues[key] = value;
                }
            }

            log.Debug("HandleInitialData complete");
        }

        private static void UpdateTagsInFolder(string folderId, string baseFolderId, string serverUrl, OpcNode[] opcNodes)
        {
            // get the set of tags + folders within this folder, so that missing ones can be removed:
            HashSet<string> oldTagNames = new HashSet<string>(FolderService.GetFolderEntities<OPCTag>(folderId).Select(x => x.EntityName));
            HashSet<string> oldFolderNames = new HashSet<string>(FolderService.GetFolderEntities<Folder>(folderId).Select(x => x.EntityName));

            foreach (OpcNode node in opcNodes)
            {
                if (ArrayUtilities.IsEmpty(node.Children))
                { // create or update tag
                    oldTagNames.Remove(node.Name);
                    OPCTag tag = FolderService.GetFolderEntities<OPCTag>(folderId).FirstOrDefault(x => x.EntityName == node.Name)
                        ?? new OPCTag();

                    tag.EntityName = node.Name;
                    tag.EntityFolderID = folderId;
                    tag.Path = node.FullPath;
                    tag.TypeName = node.TypeName;
                    tag.ServerUrl = serverUrl;
                    tag.Store();
                }
                else
                {
                    oldFolderNames.Remove(node.Name);
                    Folder sf = FolderService.GetFolderEntities<Folder>(folderId).FirstOrDefault(x => x.EntityName == node.Name);
                    if (sf == null)
                    {
                        new Folder
                        {
                            EntityName = node.Name,
                            EntityFolderID = folderId,
                            FolderBehaviorType = typeof(DefaultFolderBehavior).FullName
                        }.Store();

                        // create, then fetch to make sure id is present.
                        sf = FolderService.GetFolderEntities<Folder>(folderId).FirstOrDefault(x => x.EntityName == node.Name);
                        if (sf == null) throw new Exception($"Folder {node.Name} could not be created in {folderId}.");
                    }
                    UpdateTagsInFolder(sf.FolderID, baseFolderId, serverUrl, node.Children);
                }
            }

            foreach (string removedTag in oldTagNames)
            {
                OPCTag tag = FolderService.GetFolderEntities<OPCTag>(folderId).FirstOrDefault(x => x.EntityName == removedTag);
                if (tag != null)
                { // delete tag & matching flow constant:
                    tag.Delete();
                    string flowConstantId = $"OPCTAG__{baseFolderId}__{tag.Path}";
                    new ORM<FlowConstant>().Delete(flowConstantId);
                }
            }
            foreach (string removedFolder in oldFolderNames)
            {
                Folder folder = FolderService.GetFolderEntities<Folder>(folderId).FirstOrDefault(x => x.EntityName == removedFolder);
                if (folder != null)
                    folder.Delete();
            }

        }

        public static void SetTagValues(string url, string agentId, BaseTagValue[] values)
        {
            AgentInstructions instr = new AgentInstructions()
            {
                Id = Guid.NewGuid().ToString(),
                EntityName = "Set Tag Values on " + url,
                MustBeCompletedBy = DateUtilities.Now(),
                AgentInstructionHanlderType = "Decisions.OPC.Agent.SetTagValuesInstructionHandler",
                Data = new[]
                {
                    new DataPair("opcServerUrl", url),
                    new DataPair("valuesWrapper", new BaseTagValueWrapper { Values = values }),
                }
            };
            AgentService.Instance.InstructAgent(new SystemUserContext(), agentId, instr);
        }
    }

    [ServiceContract]
    public interface IOPCCallbackService
    {
        [OperationContract]
        void ValuesChanged(string opcServerUrl, string eventId, BaseTagValueWrapper valuesWrapper);

        [OperationContract]
        void Ping();
    }

    [AutoRegisterService("OPCCallbackService", typeof(IOPCCallbackService))]
    public class OPCCallbackService : IOPCCallbackService
    {
        public void ValuesChanged(string opcServerUrl, string eventId, BaseTagValueWrapper valuesWrapper)
        {
            SetValues(opcServerUrl, eventId, valuesWrapper);
        }

        private static Log LOG = new Log("OPC");
        private ORM<OPCEvent> opcEventOrm = new ORM<OPCEvent>();

        private void SetValues(string opcServerUrl, string eventId, BaseTagValueWrapper valuesWrapper)
        {
            BaseTagValue[] values = valuesWrapper.Values;
            using (UserContextHolder.Register(new SystemUserContext()))
            {
                Folder configFolder = EntityCache<OPCServerFolderBehaviorData>.GetCache().AllEntities.FirstOrDefault(s => s.Url == opcServerUrl)?.GetEntity() as Folder;
                if (configFolder == null)
                    return;

                Array.ForEach(values, tagValue => { LOG.Debug($"Value Change: {tagValue.Path} - {TagValueUtils.GetObjectValueFromTag(tagValue)}"); });

                // put values in last cache
                foreach (var v in values)
                {
                    string key = eventId + "|" + v.Path;
                    BaseTagValue priorValue;
                    OPCEngine.mostRecentValues.TryGetValue(key, out priorValue);

                    OPCEngine.mostRecentValues[key] = v;

                    if (priorValue == null)
                    {
                        OPCEngine.priorValues[key] = v;
                    }
                    else
                    {
                        OPCEngine.priorValues[key] = priorValue;
                    }

                }

                OPCEvent opcEvent = opcEventOrm.Fetch(eventId);
                if (opcEvent == null || opcEvent.Disabled)
                    return;

                bool runIt = false;
                // see if this event is interested in this change
                foreach (var v in opcEvent.EventValues)
                {
                    if (values.FirstOrDefault(changedValue => changedValue.Path == v.PathToValue) != null)
                    {
                        runIt = true;
                        break;
                    }
                }

                if (runIt)
                {
                    try
                    {
                        List<DataPair> inputs = new List<DataPair>();

                        foreach (var v in opcEvent.EventValues)
                        {
                            string key = eventId + "|" + v.PathToValue;
                            BaseTagValue value = null;

                            OPCEngine.mostRecentValues.TryGetValue(key, out value);

                            inputs.Add(new DataPair(v.Name, value));

                            BaseTagValue priorvalue = null;

                            OPCEngine.priorValues.TryGetValue(key, out priorvalue);

                            inputs.Add(new DataPair("Last " + v.Name, priorvalue));
                        }

                        inputs.Add(new DataPair("LastWorkflowRun", opcEvent.LastRun));

                        // check rule to see if it matches
                        var ruleResult = RuleEngine.RunRule(opcEvent.Rule, inputs.ToArray());

                        if (ruleResult != null && ruleResult is bool)
                        {
                            if (((bool)ruleResult) == true)
                            {
                                new Log("OPC").Error("Value Changed And Rule Returned True - running flow");
                                FlowEngine.Start(FlowEngine.LoadFlowByID(opcEvent.Flow, false, true),
                                    new FlowStateData(inputs.ToArray()));
                            }
                            else
                            {
                                new Log("OPC").Error("Value Changed But Rule Returned False");
                            }

                        }
                        else
                        {
                            new Log("OPC").Error("Value Changed But Rule Returned False");
                        }
                    }
                    catch (Exception except)
                    {
                        new Log("OPC").Error(except, "Error running flow from event");

                    }
                }
            }
        }

        public void Ping()
        {

        }
    }

}
