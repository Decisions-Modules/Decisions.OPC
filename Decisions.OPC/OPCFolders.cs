using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.Design.ToolboxFilter;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities.validation.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    public class OPCServersFolderBehavior : DefaultFolderBehavior
    {
        public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        {
            return new BaseActionType[]
            {
                new EditObjectAction(typeof(AddOPCServer), "Add Server", null, null, "Add Server", new AddOPCServer(folder.FolderID), AddServer) { DisplayType = ActionDisplayType.Primary },
            };
        }

        private void AddServer(AbstractUserContext usercontext, object addServerObject)
        {
            AddOPCServer addServer = (AddOPCServer)addServerObject;
            if (string.IsNullOrEmpty(addServer.Name))
                throw new Exception("Name is required");
            if (string.IsNullOrEmpty(addServer.Url))
                throw new Exception("URL is required");
            if (string.IsNullOrEmpty(addServer.AgentId))
                throw new Exception("Agent is required");

            Folder[] serverFolders = FolderService.Instance.GetSubFolders(usercontext, addServer.FolderId);
            foreach(Folder serverFolder in serverFolders)
            {
                OPCServerFolderBehaviorData extData = serverFolder.ExtensionData as OPCServerFolderBehaviorData;
                if (extData?.Url == addServer.Url)
                    throw new Exception($"A server ({serverFolder.EntityName}) already exists with this URL ({addServer.Url}).");
            }

            Folder f = new Folder(addServer.Name, addServer.FolderId);
            f.FolderBehaviorType = typeof(OPCServerFolderBehavior).FullName;
            f.ExtensionData = new OPCServerFolderBehaviorData { Url = addServer.Url, AgentId = addServer.AgentId };
            f.Store();
            f.ExtensionData.Store();
        }
    }

    [ValidationRules]
    public class AddOPCServer : IValidationSource
    {
        [PropertyHidden]
        public string FolderId { get; set; }
        [EmptyStringRule]
        public string Name { get; set; }
        [EmptyStringRule]
        public string Url { get; set; }
        [PropertyClassification("Agent", 3)]
        [EntityPickerEditor(new Type[] { typeof(Folder) }, Constants.AGENT_FOLDER_ID, "Select Agent", true, false, true)]
        [EmptyStringRule]
        public string AgentId { get; set; }
        public AddOPCServer(string folderId) { FolderId = folderId; }

        public ValidationIssue[] GetValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();
            if (Name != null && Name.Contains("."))
                issues.Add(new ValidationIssue("Server name can't contain '.' character", "", BreakLevel.Fatal));

            return issues.ToArray();
        }
    }

    public class OPCServerFolderBehavior : DefaultFolderBehavior
    {
        public override BaseActionType[] GetFolderActions(Folder folder, BaseActionType[] proposedActions, EntityActionType[] types)
        {

            BaseActionType action = null;

            OPCServerFolderBehaviorData ext = folder.GetExtensionData<OPCServerFolderBehaviorData>();
            if (!OPCEngine.IsListening(ext.Url, ext.AgentId))
            {
                action = new ConfirmAction("Start Listening", null, "Start Listening", "Are You Sure?", new SimpleDelegate(() =>
                {
                    OPCServerFolderBehaviorData folderExt = folder.GetExtensionData<OPCServerFolderBehaviorData>();

                    var allEvents = Folder.GetEntitiesInFolder<OPCEvent>(folder.FolderID);

                    OpcEventGroup[] eventGroups = allEvents.Where(x => !x.Disabled).Select(x => new OpcEventGroup
                    {
                        Paths = x.EventValues?.Select(y => y.PathToValue).ToArray() ?? new string[0],
                        EventId = x.Id,
                        Deadband = x.Deadband,
                        UpdateRate = x.UpdateRate
                    }).ToArray();

                    OPCEngine.StartListening(folderExt.Url, folderExt.AgentId, eventGroups);
                    OPCEngine.GetInitialData(folderExt.Url, folderExt.AgentId, true);
                }));
            }
            else
            {
                action = new ConfirmAction("Stop Listening", null, "Stop Listening", "Are You Sure?", new SimpleDelegate(() =>
                {
                    OPCServerFolderBehaviorData folderExt = folder.GetExtensionData<OPCServerFolderBehaviorData>();
                    OPCEngine.StopListening(folderExt.Url, folderExt.AgentId);
                }));
            }


            return new BaseActionType[]
            {
                new AddEntityAction(typeof(OPCEvent), "Add Event", null, "Add OPC Event"),
                new SimpleAction("Rebuild Tag Cache", null, new SimpleDelegate(() =>
                {
                    RebuildTagCache(folder);
                })) {DisplayType = ActionDisplayType.Secondary},
                action
            }.Concat(proposedActions.Where(a => a is NavigateToFolderAction || a.Name == "Delete Folder")).ToArray();
        }

        private void RebuildTagCache(Folder f)
        {
            ORM<Folder> orm = new ORM<Folder>();
            if (orm.Fetch(f.FolderID + ".tagdata") == null)
            {
                Folder tagf = new Folder();
                tagf.EntityFolderID = f.FolderID;
                tagf.EntityName = "Tag Data";
                tagf.FolderBehaviorType = typeof(DefaultFolderBehavior).FullName;
                tagf.FolderID = f.FolderID + ".tagdata";
                tagf.Store();
            }

            OPCServerFolderBehaviorData folderExt = f.GetExtensionData<OPCServerFolderBehaviorData>();
            var url = folderExt.Url;

            OPCEngine.GetInitialData(url, folderExt.AgentId, false);
        }

        public override Type GetFolderExtensionDataType()
        {
            return typeof(OPCServerFolderBehaviorData);
        }


    }

    [ORMEntity]
    [ExtensionForType(typeof(Folder))]
    public class OPCServerFolderBehaviorData : AbstractEntityExtensionData
    {
        [ORMField]
        private string url;
        [ORMField]
        private string agentId;
        [ORMField]
        private string agentName;

        public string Url
        {
            get { return url; }
            set { url = value; }
        }

        public string AgentId
        {
            get { return agentId; }
            set { agentId = value; }
        }

        public string AgentName
        {
            get { return agentName; }
            set { agentName = value; }
        }

        public override bool IsUserSettable
        {
            get { return false; }
        }

        public override void AfterRead()
        {
            base.AfterRead();
            if (!string.IsNullOrEmpty(agentId))
            {
                string agentName = new ORM<Folder>().Fetch(agentId)?.EntityName;
                if(agentName != this.agentName)
                {
                    this.agentName = agentName;
                    Store();
                }
            }
        }
    }

    public class OPCInitializer : IInitializable
    {
        const string SERVERS_PAGE_ID = "7a73a010-2bb1-11e8-8622-9cb6d0d53f40";

        public void Initialize()
        {
            ORM<Folder> orm = new ORM<Folder>();

            if (orm.Fetch("OPCBaseFolder") == null)
            {
                Folder f = new Folder();
                f.FolderID = "OPCBaseFolder";
                f.CanBeRoot = true;
                f.EntityName = "OPC Servers";
                f.FolderBehaviorType = typeof(OPCServersFolderBehavior).FullName;
                f.Store();
            }

            ORM<PageData> pageDataOrm = new ORM<PageData>();
            PageData pageData = pageDataOrm.Fetch(new WhereCondition[] {
                new FieldWhereCondition("configuration_storage_id", QueryMatchType.Equals, SERVERS_PAGE_ID),
                new FieldWhereCondition("entity_folder_id", QueryMatchType.Equals, "OPCBaseFolder")
            }).FirstOrDefault();

            if (pageData == null)
            {
                pageDataOrm.Store(new PageData
                {
                    EntityFolderID = "OPCBaseFolder",
                    ConfigurationStorageID = SERVERS_PAGE_ID,
                    EntityName = "Servers",
                    Order = -1
                });
            }

            LookupListRegistration.Instance.RegisterObjectForType(typeof(OPCDataProvider), new OPCLookupListProvider());
        }
    }

}
