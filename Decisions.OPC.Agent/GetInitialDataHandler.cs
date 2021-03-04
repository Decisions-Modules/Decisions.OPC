using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Agent;
using DecisionsFramework.ServiceLayer.Services.ContextData;

namespace Decisions.OPC.Agent
{
    public class GetInitialDataInstructionHandler : IAgentInstructionHandler
    {
        public DataDescription[] InputData => new DataDescription[0];
        public OutcomeScenarioData[] OutcomeScenarios => new OutcomeScenarioData[0];
        public BaseActionType[] GetManualTriggerActions() => new BaseActionType[0];

        public AgentInstructionsResult HandleInstructions(AgentInstructions instruction)
        {
            OpcInitialData result = OpcCommunication.GetInitialData(
                instruction.Data.GetValueByKey<string>("opcServerUrl"),
                instruction.Data.GetValueByKey<bool>("valuesOnly")
                );
            return new AgentInstructionsResult
            {
                Data = new DataPair[]
                {
                    new DataPair("initialData", result)
                }
            };
        }
    }
}
