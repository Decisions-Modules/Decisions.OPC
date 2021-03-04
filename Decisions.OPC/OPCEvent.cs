using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
#if !NETCOREAPP
using System.ServiceModel.Configuration;
#endif
using System.Text;
using System.Threading.Tasks;
using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.StepImplementations;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Actions.Common;
using DecisionsFramework.ServiceLayer.Services.ConfigurationStorage;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.ServiceLayer.Utilities;
using DecisionsFramework.Utilities.validation.Rules;

namespace Decisions.OPC
{
    [DataContract]
    public class OpcEventGroup // OpcEventGroup is the minimal version sent to the agent
    {
        [DataMember]
        public string EventId { get; set; }

        [DataMember]
        public string[] Paths { get; set; }

        [DataMember]
        public float Deadband { get; set; }

        [DataMember]
        public int UpdateRate { get; set; }
    }

    [DataContract]
    [ORMEntity]
    public class OPCEvent : AbstractFolderEntity
    {
        [ORMPrimaryKeyField]
        private string id;
        [ORMField]
        private string flowId;
        [ORMField]
        private string ruleId;
        [ORMField]
        private float deadband;
        [ORMField]
        private int updateRate = 100;

        private ORMOneToManyRelationship<OPCEventValue> eventValues = new ORMOneToManyRelationship<OPCEventValue>("event_id", false);

        [ORMField("last_run")]
        private DateTime _lastRun;


        [ORMField]
        private bool disabled;

        public OPCEvent()
        {
            EntityName = "New Event";
        }

        [DataMember]
        [PropertyHidden]
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        [PropertyHidden]
        public override string EntityDescription
        {
            get { return base.EntityDescription; }
            set { base.EntityDescription = value; }
        }

        [DataMember]
        [PropertyClassification(1, "Event Data", "Event Configuration")]
        public OPCEventValue[] EventValues
        {
            get { return eventValues.Items; }
            set { eventValues.Items = value; }
        }

        [DataMember]
        [PropertyClassification(2, "Event Flow", "Event Configuration")]
        [ElementRegistrationPickerEditor(new ElementType[] { ElementType.Flow },
            typeof(OPCEventFlowBehavior), Actions = PickerActions.Edit)]
        public string Flow
        {
            get
            {
                // create flow if does not exist
                if (this.IsNew())
                    Store();

                if (string.IsNullOrEmpty(flowId) && !string.IsNullOrEmpty(this.EntityFolderID))
                {
                    Flow f = new Flow(id + "Flow");

                    f.AddStep(new FlowStep(new StartStep()) { X = 100, Y = 100 });
                    f.AddStep(new FlowStep(new EndStep()) { X = 300, Y = 300 });

                    f.SetFlowBehaviorType(typeof(OPCEventFlowBehavior));

                    ElementRegistration er = new ElementRegistration();
                    er.EntityFolderID = EntityFolderID;
                    er.Name = f.Name;
                    er.Hidden = true;
                    er.ComponentRegistrationId = id + ".flow";
                    er.ElementAttribute = new ElementType[] { ElementType.Flow, };

                    ConfigurationStorageService.Instance.SaveWithConfigurationData(UserContextHolder.GetCurrent(), er, WritableHelper.Write(f));

                    flowId = er.ComponentRegistrationId;
                }

                return flowId;
            }
            set { flowId = value; }
        }

        [DataMember]
        [PropertyClassification(3, "Event Rule", "Event Configuration")]
        [ElementRegistrationPickerEditor(new ElementType[] { ElementType.Rule},
            typeof(OPCEventRuleBehavior), Actions = PickerActions.Edit)]
        public string Rule
        {
            get
            {
                // create rule if does not exist
                if (this.IsNew())
                    Store();

                if (string.IsNullOrEmpty(ruleId) && !string.IsNullOrEmpty(this.EntityFolderID))
                {
                    Rule f = new Rule();
                    f.Name = id + "Rule";

                    f.SetRuleBehaviorType(typeof(OPCEventRuleBehavior));

                    ElementRegistration er = new ElementRegistration();
                    er.EntityFolderID = EntityFolderID;
                    er.Hidden = true;
                    er.Name = f.Name;
                    er.ComponentRegistrationId = id + ".rule";
                    er.ElementAttribute = new ElementType[] { ElementType.Rule, };

                    ConfigurationStorageService.Instance.SaveWithConfigurationData(
                        UserContextHolder.GetCurrent(), er,
                        WritableHelper.Write(f));

                    ruleId = er.ComponentRegistrationId;
                }

                return ruleId;
            }
            set { ruleId = value; }
        }

        [PropertyHidden]
        public DateTime LastRun
        {
            get { return _lastRun; }
            set { _lastRun = value; }
        }

        [PropertyClassification(4, "Disable Event", "Event Configuration")]
        public bool Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }

        [DataMember]
        [PropertyClassification(10, "Deadband (percent)", "Event Group Configuration")]
        public float Deadband
        {
            get { return deadband; }
            set { deadband = value; }
        }

        [DataMember]
        [PropertyClassification(20, "Update Rate (ms)", "Event Group Configuration")]
        public int UpdateRate
        {
            get { return updateRate; }
            set { updateRate = value; }
        }

        public void SetLastRun()
        {
            _lastRun = DateTime.Now;
            new ORM<OPCEvent>().Store(this, false, false, "last_run");
        }

        public override BaseActionType[] GetActions(AbstractUserContext userContext, EntityActionType[] types)
        {
            return new BaseActionType[]
            {
                new EditEntityAction(GetType(), "Edit Event", null), 
            };
        }

        public override void BeforeDelete()
        {
            base.BeforeDelete();

            if (string.IsNullOrEmpty(EntityFolderID))
                return;

            Folder folder = new ORM<Folder>().Fetch(EntityFolderID);
            OPCServerFolderBehaviorData ext = folder?.GetExtensionData<OPCServerFolderBehaviorData>();
            if (ext != null)
                OPCEngine.RemoveEventGroup(ext.Url, ext.AgentId, Id);
        }

        public override void AfterDelete()
        {
            var orm = new DynamicORM();
            orm.Delete(typeof(OPCEventValue), new WhereCondition[]
            {
                new FieldWhereCondition("event_id", QueryMatchType.Equals, this.id)
            });

            orm.Delete(typeof(ElementRegistration), this.flowId);
            orm.Delete(typeof(ElementRegistration), this.ruleId);

            base.AfterDelete();
        }

        public override void AfterSave()
        {
            base.AfterSave();

            if (string.IsNullOrEmpty(EntityFolderID))
                throw new Exception("EntityFolderID not found");

            Folder folder = new ORM<Folder>().Fetch(EntityFolderID);
            OPCServerFolderBehaviorData ext = folder?.GetExtensionData<OPCServerFolderBehaviorData>();
            if (ext != null)
            {
                if (Disabled)
                {
                    OPCEngine.RemoveEventGroup(ext.Url, ext.AgentId, Id);
                }
                else
                {
                    OpcEventGroup eventGroup = new OpcEventGroup
                    {
                        Paths = EventValues?.Select(y => y.PathToValue).ToArray() ?? new string[0],
                        EventId = Id,
                        Deadband = Deadband,
                        UpdateRate = UpdateRate
                    };
                    OPCEngine.AddOrUpdateEventGroup(ext.Url, ext.AgentId, eventGroup);
                }
            }
        }
    }

    [DataContract]
    [ValidationRules]
    [ORMEntity]
    public class OPCEventValue : BaseORMEntity, INotifyPropertyChanged
    {
        [ORMPrimaryKeyField]
        private string id;
        [ORMField]
        private string pathToValue;
        [ORMField]
        private string name;
        [ORMField]
        private string typeName;

        [DataMember]
        [PropertyClassification(3, "Path", "Event Configuration")]
        [EmptyStringRule]
        public string PathToValue
        {
            get { return pathToValue; }
            set
            {
                pathToValue = value;
                if(string.IsNullOrEmpty(name) && pathToValue != null)
                {
                    string[] parts = pathToValue.Split('.');
                    Name = parts[parts.Length - 1];
                }
                OnPropertyChanged();
                OnPropertyChanged("SelectTag");
            }
        }

        [DataMember]
        [PropertyClassification(2, "Pick Tag", "Event Configuration")]
        [EntityPickerEditor(new Type[] {typeof(OPCTag)}, "OPCBaseFolder", "Select Tag")]
        public string SelectTag {
            get
            {
                return EntityCache<OPCTag>.GetCache().AllEntities.FirstOrDefault(t => t.Path == PathToValue)?.Id;
            }
            set
            {
                if (value == null)
                    return;
                OPCTag tag = EntityCache<OPCTag>.GetCache().GetById(value);
                PathToValue = tag?.Path;
                TypeName = tag?.TypeName;
                OnPropertyChanged();
                OnPropertyChanged("PathToValue");
            }
        }

        [DataMember]
        [PropertyClassification(1, "Name", "Event Configuration")]
        [EmptyStringRule]
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        [DataMember]
        [PropertyClassification(4, "Type", "Event Configuration")]
        [ReadonlyEditor]
        public string TypeName
        {
            get { return typeName; }
            set
            {
                typeName = value;
                OnPropertyChanged();
            }
        }

        public override string ToString()
        {
            return $"{Name} - [{PathToValue}]";

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [DataContract]
    [ORMEntity]
    public class OPCTag : AbstractFolderEntity
    {
        [ORMPrimaryKeyField]
        private string id;
        [ORMField]
        private string path;
        [ORMField]
        private string typeName;
        [ORMField]
        private string serverUrl;

        [DataMember]
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        [DataMember]
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        [DataMember]
        public string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        [DataMember]
        [ExcludeInDescription]
        public string ServerUrl
        {
            get { return serverUrl; }
            set { serverUrl = value; }
        }

    }

}
