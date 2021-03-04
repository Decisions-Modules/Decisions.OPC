using System;
using System.Collections.Generic;
using System.Linq;
using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Utilities;

namespace Decisions.OPC
{
    public class OPCEventRuleBehavior : DefaultRuleBehavior
    {
        public override DataDescription[] ProcessInputDeclaration(Rule flow, DataDescription[] inputData)
        {
            var id = flow.Source?.ComponentRegistrationId?.Split('.').FirstOrDefault();

            if (id != null)
            {
                ORM<OPCEvent> orm = new ORM<OPCEvent>();
                var e = orm.Fetch(id);

                if (e != null)
                {
                    List<DataDescription> output = new List<DataDescription>();

                    foreach (var ev in e.EventValues)
                    {
                        Type tagType = TagValueUtils.GetTagValueType(TypeUtilities.FindTypeByFullName(ev.TypeName));
                        output.Add(new DataDescription(tagType, ev.Name));
                        output.Add(new DataDescription(tagType, "Last " + ev.Name));
                    }

                    output.Add(new DataDescription(typeof(DateTime), "LastWorkflowRun"));

                    output.AddRange(base.ProcessInputDeclaration(flow, inputData));

                    return output.ToArray();
                }
            }

            return base.ProcessInputDeclaration(flow, inputData);
        }

        public override bool AllowUserToEditRuleInputData
        {
            get { return false; }
        }

        public override string Name
        {
            get { return "OPC Event Rule"; }
        }

        public override bool IsUserSettable
        {
            get { return false; }
        }

        public override string GetUserMessage(Rule r)
        {
            return "Return 'true' to fire workflow for event.";
        }
    }
}