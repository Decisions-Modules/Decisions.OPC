using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.ToolboxFilter;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using DecisionsFramework.ServiceLayer.Services.Folder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    public class OPCLookupListProvider : ILookupListProvider
    {
        public Dictionary<string, object> GetLookupItems(Type t)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (OPCServerFolderBehaviorData ext in new ORM<OPCServerFolderBehaviorData>().Fetch())
            {
                string serverName = ext.GetEntity()?.EntityName;
                if (string.IsNullOrEmpty(serverName))
                    continue;
                result.Add(serverName, new OPCDataProvider { Path = serverName });
            }
            return result;
        }
    }

    [Writable]
    public class OPCDataProvider : IDataProviderObject
    {
        [WritableValue]
        public string Path { get; set; }

        [WritableValue]
        private DataPair[] snapshotPairs;

        private Dictionary<string, BaseTagValue> snapshot;
        public Dictionary<string, BaseTagValue> Snapshot
        {
            get
            {
                if (snapshot == null && snapshotPairs != null)
                {
                    snapshot = new Dictionary<string, BaseTagValue>();
                    foreach (DataPair pair in snapshotPairs)
                        snapshot.Add(pair.Name, (BaseTagValue)pair.OutputValue);
                }
                return snapshot;
            }
            set
            {
                snapshot = value;

                if (value == null)
                    snapshotPairs = null;
                else
                    snapshotPairs = value.Select(pair => new DataPair(pair.Key, pair.Value)).ToArray();
            }
        }

        public void SetTag(string tag)
        {
           
        }

        public void SetPath(string name)
        {
            Path = name;
        }
        static DecisionsFramework.Log Log = new DecisionsFramework.Log("OPCDataProviderLog");
        private ORM<OPCServerFolderBehaviorData> opcServerOrm = new ORM<OPCServerFolderBehaviorData>();
        public DataDescription[] GetSubItems()
        {
            Log.Debug($"GetSubItems called on path {Path}");

            if (Path == null || Path == OPCEventFlowBehavior.SNAPSHOT_DATA) // If we're at the top, just return all server names:
                return opcServerOrm.Fetch()
                    .Select(x => new DataDescription(typeof(OPCDataProvider), x.GetEntity().EntityName, false)
                    { NestedVariableName = x.GetEntity().EntityName }).ToArray();

            string[] pathSplit = Path.Split(new char[] { '.' }, 2); // {Server Name}.{remaining.tag.path}

            DynamicORM orm = new DynamicORM();

            HashSet<string> nextPaths = new HashSet<string>();
            List<DataDescription> dds = new List<DataDescription>();

            foreach (string key in OPCEngine.mostRecentValues.Keys) // keys are like "<guid>|Channel1.Device1.Tag1"
            {
                string[] keySplit = key.Split('|');
                string eventId = keySplit[0];
                string keyTagPath = keySplit[1];
                OPCEvent ev = orm.Fetch(typeof(OPCEvent), eventId) as OPCEvent;
                if (ev == null) throw new Exception("OPCEvent not found with id " + eventId);
                string serverName = (orm.Fetch(typeof(Folder), ev.EntityFolderID) as Folder)?.EntityName;
                if (string.IsNullOrEmpty(serverName)) throw new Exception("Server name not found");

                if (serverName != pathSplit[0])
                    continue;

                if (pathSplit.Length == 1) // This is the node for the server, so include everything under it:
                {
                    nextPaths.Add(keyTagPath.Split('.')[0]);
                }
                else
                {
                    // If "Channel1.Device1.Tag1" starts with "Channel1.Device1", for example:
                    if (keyTagPath.StartsWith(pathSplit[1]))
                    {
                        string remainingPath = keyTagPath.Substring(pathSplit[1].Length + 1); // "Tag1"
                        string[] splitRemaining = remainingPath.Split('.');
                        if(splitRemaining.Length == 1) // This is a tag, so find its type
                        {
                            BaseTagValue tagValue;
                            if (!OPCEngine.mostRecentValues.TryGetValue(key, out tagValue))
                                throw new Exception("Could not find type for tag " + key);

                            if (dds.Any(d => d.Name == splitRemaining[0])) // Don't create duplicates - most recent value will be used.
                                continue;

                            DataDescription dd = TagValueUtils.GetDataDescriptionFromTagType(tagValue.GetType(), splitRemaining[0]);
                            dd.NestedVariableName = $"{this.Path}.{splitRemaining[0]}";
                            dds.Add(dd);
                        }
                        else
                        {
                            nextPaths.Add(splitRemaining[0]);
                        }
                    }
                }

            }

            foreach(string nextPath in nextPaths)
            {
                dds.Add(new DataDescription(typeof(OPCDataProvider), nextPath, false) { NestedVariableName = $"{this.Path}.{nextPath}" });
            }

            return dds.ToArray();
        }

        public object GetValue(string name)
        {
            Log.Debug($"GetValue called on path {Path}");

            if (Path == null || !Path.Contains("."))
            {
                // If at the top, or at a server, the next part can't be a tag:
                string nextPath = (Path == null) ? name : $"{Path}.{name}";
                return new OPCDataProvider { Path = nextPath, Snapshot = this.Snapshot };
            }
            string[] pathSplit = Path.Split(new char[] { '.' }, 2); // {Server Name}.{remaining.tag.path}

            Folder folder = opcServerOrm.Fetch().Select(ext => ext.GetEntity()).FirstOrDefault(f => f.EntityName == pathSplit[0]) as Folder;
            if (folder == null) throw new Exception($"Could not find server named {pathSplit[0]}");

            string[] eventIds = FolderService.GetFolderEntities<OPCEvent>(folder.FolderID).Select(e => e.Id).ToArray();
            BaseTagValue mostRecentTag = null;

            IDictionary<string, BaseTagValue> sourceDict = ((Snapshot as IDictionary<string, BaseTagValue>) ?? OPCEngine.mostRecentValues);

            foreach(string eventId in eventIds)
            {
                string key = $"{eventId}|{pathSplit[1]}.{name}";
                BaseTagValue tag;
                if(sourceDict.TryGetValue(key, out tag))
                {
                    if (mostRecentTag == null || mostRecentTag.TimeStamp < tag.TimeStamp)
                        mostRecentTag = tag;
                }
            }

            if(mostRecentTag != null)
                return TagValueUtils.GetObjectValueFromTag(mostRecentTag);

            // No current value found, so check folders to see whether this is a folder or a tag:

            Folder tagDataFolder = folder.GetSubFolder().FirstOrDefault(x => x.EntityName == "Tag Data");
            if (tagDataFolder == null)
                throw new Exception("Tag data folder not found");

            string[] parts = pathSplit[1].Split('.');
            Folder currentFolder = tagDataFolder;
            for (int i = 0; i < parts.Length; ++i)
            { // descend for each part, arriving at the folder which should contain the named entity
                Folder nextFolder = currentFolder.GetSubFolder().FirstOrDefault(x => x.EntityName == parts[i]);
                if (nextFolder == null)
                    throw new Exception($"Folder '{parts[i]}' not found in folder '{currentFolder?.EntityName}'.");
                currentFolder = nextFolder;
            }

            Folder namedFolder = FolderService.GetFolderEntities<Folder>(currentFolder.FolderID).FirstOrDefault(x => x.EntityName == name);
            if (namedFolder != null)
                return new OPCDataProvider { Path = $"{this.Path}.{name}", Snapshot = this.Snapshot };

            OPCTag namedTag = FolderService.GetFolderEntities<OPCTag>(currentFolder.FolderID).FirstOrDefault(x => x.EntityName == name);
            if (namedTag != null)
                throw new Exception($"Tag '{name}' has no known value");
            else
                throw new Exception($"No tag or folder '{name}' found in '{currentFolder.EntityName}'");
        }

        public void SetValue(string name, object value) { }
    }
}
