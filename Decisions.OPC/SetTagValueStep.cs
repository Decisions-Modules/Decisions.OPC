using DecisionsFramework;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.Properties.Attributes;
using DecisionsFramework.ServiceLayer.Services.Folder;
using DecisionsFramework.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    [Writable]
    [AutoRegisterStep("Set Tag Value", "Integration", "OPC")]
    public class SetTagValueStep : ISyncStep, IDataConsumer, IValidationSource, INotifyPropertyChanged
    {
        private const string PATH_DONE = "Done";
        private const string VALUE_INPUT = "Value";
        private const string TAG_INPUT = "Tag Path";
        private const string SERVER_URL_INPUT = "Server URL";

        [WritableValue]
        private bool chooseTagAtRuntime;

        [PropertyClassification(0, "Choose Tag At Runtime", "Settings")]
        public bool ChooseTagAtRuntime
        {
            get { return chooseTagAtRuntime; }
            set
            {
                chooseTagAtRuntime = value;
                OnPropertyChanged();
                OnPropertyChanged("InputData");
            }
        }

        [WritableValue]
        private string tagId;

        [PropertyClassification(10, "Tag", "Settings")]
        [EntityPickerEditor(new Type[] { typeof(OPCTag) }, "OPCBaseFolder", "Select Tag")]
        [BooleanPropertyHidden(nameof(ChooseTagAtRuntime), true)]
        public string TagId
        {
            get { return tagId; }
            set
            {
                tagId = value;
                OnPropertyChanged();
                OnPropertyChanged("InputData");
            }
        }

        public DataDescription[] InputData
        {
            get
            {
                List<DataDescription> inputs = new List<DataDescription>();

                if (ChooseTagAtRuntime)
                {
                    inputs.Add(new DataDescription(new DecisionsNativeType(typeof(string)), TAG_INPUT, false, false, false));
                    inputs.Add(new DataDescription(new DecisionsNativeType(typeof(string)), SERVER_URL_INPUT, false, false, false));
                    inputs.Add(new DataDescription(new DecisionsNativeType(typeof(object)), VALUE_INPUT, false, false, false));
                }
                else
                {
                    if (!string.IsNullOrEmpty(TagId))
                    {
                        OPCTag tag = EntityCache<OPCTag>.GetCache().GetById(TagId);
                        if (tag != null)
                        {
                            Type valueType = TypeUtilities.FindTypeByFullName(tag.TypeName);
                            if(valueType.IsArray)
                                inputs.Add(new DataDescription(new DecisionsNativeType(valueType.GetElementType()), VALUE_INPUT, true, false, false));
                            else
                                inputs.Add(new DataDescription(new DecisionsNativeType(valueType), VALUE_INPUT, false, false, false));

                        }
                    }
                }

                return inputs.ToArray();
            }
        }

        public OutcomeScenarioData[] OutcomeScenarios => new OutcomeScenarioData[]
        {
            new OutcomeScenarioData(PATH_DONE, new DataDescription[] { }),
        };

        public ResultData Run(StepStartData data)
        {
            object value = data.Data[VALUE_INPUT];

            OPCTag tag;
            string serverUrl;
            if (chooseTagAtRuntime)
            {
                string tagPath = data.Data[TAG_INPUT] as string;
                serverUrl = data.Data[SERVER_URL_INPUT] as string;
                if (string.IsNullOrEmpty(tagPath))
                    throw new Exception("No tag specified");
                if (string.IsNullOrEmpty(serverUrl))
                    throw new Exception("No server URL specified");
                tag = EntityCache<OPCTag>.GetCache().AllEntities.FirstOrDefault(x => x.Path == tagPath && x.ServerUrl == serverUrl);
                if (tag == null)
                    throw new Exception($"No tag found at '{tagPath}' on server '{serverUrl}'");
            }
            else
            {
                tag = EntityCache<OPCTag>.GetCache().GetById(tagId);
                if (tag == null)
                    throw new Exception($"Tag '{tagId}' not found");
                serverUrl = tag.ServerUrl;
            }

            BaseTagValue tagValue = TagValueUtils.GetTagWithValue(TypeUtilities.FindTypeByFullName(tag.TypeName), value);

            tagValue.Path = tag.Path;

            OPCServerFolderBehaviorData folderExt = EntityCache<OPCServerFolderBehaviorData>.GetCache().AllEntities.FirstOrDefault(s => s.Url == serverUrl);
            Folder folder = folderExt?.GetEntity() as Folder;
            if (folder == null)
                throw new Exception($"No server folder configured for URL '{serverUrl}'.");

            OPCEngine.SetTagValues(serverUrl, folderExt.AgentId, new BaseTagValue[] { tagValue });

            return new ResultData(PATH_DONE, new KeyValuePair<string, object>[] { });
        }
        public ValidationIssue[] GetValidationIssues()
        {
            List<ValidationIssue> issues = new List<ValidationIssue>();

            if (!ChooseTagAtRuntime && string.IsNullOrEmpty(TagId))
                issues.Add(new ValidationIssue("No tag chosen", "", BreakLevel.Fatal));

            return issues.ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
