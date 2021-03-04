using DecisionsFramework.ServiceLayer.Agent;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    internal class GetInitialDataResultHandler : IAgentResultsHandler
    {
        public void PostResult(AgentInstructions instructions, AgentInstructionsResult r)
        {
            DataPair[] results = r.Data;
            if (results == null)
                throw new Exception("Get Initial Data received no response data");
            DataPair result = results.FirstOrDefault(x => x.Name == "initialData");
            if (result == null || !(result.OutputValue is OpcInitialData))
                throw new Exception("Get Initial Data did not get the expected data");
            OpcInitialData initialData = result.OutputValue as OpcInitialData;

            string url = instructions.Data.FirstOrDefault(d => d.Name == "opcServerUrl")?.OutputValue as string;
            if (string.IsNullOrEmpty(url))
                throw new Exception("No URL found in agent instructions");
            bool? valuesOnly = instructions.Data.FirstOrDefault(d => d.Name == "valuesOnly")?.OutputValue as bool?;
            if (valuesOnly == false && initialData.Values == null)
                throw new Exception("No values found in agent instructions");

            OPCEngine.HandleInitialData(url, initialData);
        }

        public void PostEnd(AgentInstructions instructions) { }
        public void PostError(AgentInstructions instructions, string errorDetails) { }
        public void PostStart(AgentInstructions instructions) { }
        public void PostStatus(AgentInstructions instructions, DateTime statusDateTime, string statusMessage) { }
        public void PostTimeout(AgentInstructions instructions) { }
    }
}
