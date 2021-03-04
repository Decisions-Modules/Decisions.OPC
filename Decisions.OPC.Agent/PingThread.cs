using Decisions.Agent.Service;
using DecisionsFramework.ServiceLayer.Agent;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TitaniumAS.Opc.Client.Da;

namespace Decisions.OPC.Agent
{
    internal class PingThread
    {
        public bool IsRunning;
        public string InstructionId;

        string serverUrl;

        public PingThread(string serverUrl, string instructionId)
        {
            this.serverUrl = serverUrl;
            this.InstructionId = instructionId;
        }

        public void Run()
        {
            try
            {
                AgentServiceClient agentClient = new AgentServiceClient();
                IsRunning = true;
                while (IsRunning)
                {
                    lock (OpcCommunication.GetLockObject(serverUrl))
                    {
                        if (!IsRunning)
                            break;
                        if(OpcCommunication.ServerIsConnected(serverUrl))
                            agentClient.PostResult(DecisionsAgent.SessionUserContext, DecisionsAgent.AGENT_ID, InstructionId, new AgentInstructionsResult());
                    }
                    Thread.Sleep(5000);
                }
            }
            catch
            {
                IsRunning = false;
            }
        }
    }
}
