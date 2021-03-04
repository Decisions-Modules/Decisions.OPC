using DecisionsFramework.Data.ORMapper;
using DecisionsFramework.ServiceLayer.Agent;
using DecisionsFramework.ServiceLayer.Services.ClientEvents;
using DecisionsFramework.ServiceLayer.Services.ContextData;
using DecisionsFramework.ServiceLayer.Services.Folder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Decisions.OPC
{
    // Updates the last ping time, to avoid using a time from the agent.
    internal class StartListeningResultHandler : IAgentResultsHandler
    {
        public void PostResult(AgentInstructions instructions, AgentInstructionsResult r)
        {
            string url = instructions.Data.GetValueByKey<string>("opcServerUrl");
            if (string.IsNullOrEmpty(url))
                return;
            Folder configFolder = EntityCache<OPCServerFolderBehaviorData>.GetCache().AllEntities.FirstOrDefault(s => s.Url == url)?.GetEntity() as Folder;
            if (configFolder == null)
                return;

            DateTime now = DateTime.UtcNow;
            bool refreshFolder = true;
            DateTime? lastTime;
            if(OPCEngine.lastPingTimePerInstruction.TryGetValue(instructions.Id, out lastTime) && lastTime != null)
                refreshFolder = (lastTime.Value.AddSeconds(OPCEngine.LATE_PING_VALUE - 1) < now);
            OPCEngine.lastPingTimePerInstruction.AddOrUpdate(instructions.Id, now, (s, dt) => now);

            if (refreshFolder)
                ClientEventsService.SendEvent(FolderMessage.FolderChangeEventId, new FolderChangedMessage(configFolder.FolderID));
        }

        public void PostEnd(AgentInstructions instructions) { }
        public void PostError(AgentInstructions instructions, string errorDetails) { }
        public void PostStart(AgentInstructions instructions) { }
        public void PostStatus(AgentInstructions instructions, DateTime statusDateTime, string statusMessage) { }
        public void PostTimeout(AgentInstructions instructions) { }
    }
}
