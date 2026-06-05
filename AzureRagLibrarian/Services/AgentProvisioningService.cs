using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using AzureRagLibrarian.Configuration;
using OpenAI.Responses;
using System.ClientModel;

namespace AzureRagLibrarian.Services;

public sealed class AgentProvisioningService(AIProjectClient projectClient)
{
    public async Task EnsureAgentAsync(RagOptions options, string vectorStoreId)
    {
        if (await AgentExistsAsync(options.AgentName))
        {
            return;
        }

        Console.WriteLine("--> Registering project agent...");

        DeclarativeAgentDefinition agentDefinition = new(model: options.ModelDeploymentName)
        {
            Instructions = "You are a helpful AI assistant. Use the indexed document to answer user questions accurately and cite the relevant details when possible.",
            Tools = { ResponseTool.CreateFileSearchTool(vectorStoreIds: [vectorStoreId]) }
        };

        ClientResult<ProjectsAgentVersion> agentVersion =
            await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                agentName: options.AgentName,
                options: new(agentDefinition));

        Console.WriteLine($"--> Agent version registered: {agentVersion.Value.Version}");
    }

    private async Task<bool> AgentExistsAsync(string agentName)
    {
        Console.WriteLine("Checking for existing project agent...");

        await foreach (ProjectsAgentRecord existingAgent in projectClient.AgentAdministrationClient.GetAgentsAsync())
        {
            if (existingAgent.Name == agentName)
            {
                Console.WriteLine($"--> Reusing agent: {existingAgent.Id}");
                return true;
            }
        }

        return false;
    }
}
