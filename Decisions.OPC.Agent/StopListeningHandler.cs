﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.ServiceLayer.Actions;
using DecisionsFramework.ServiceLayer.Agent;

namespace Decisions.OPC.Agent
{
    public class StopListeningInstructionHandler : IAgentInstructionHandler
    {
        public DataDescription[] InputData => new DataDescription[0];
        public OutcomeScenarioData[] OutcomeScenarios => new OutcomeScenarioData[0];
        public BaseActionType[] GetManualTriggerActions() => new BaseActionType[0];

        public AgentInstructionsResult HandleInstructions(AgentInstructions instruction)
        {
            OpcCommunication.StopListening(instruction.Data.GetValueByKey<string>("opcServerUrl"));
            return new AgentInstructionsResult();
        }
    }
}
