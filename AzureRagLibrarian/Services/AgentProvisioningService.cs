using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using System.ClientModel;

namespace AzureRagLibrarian.Services;

public sealed class AgentProvisioningService(AIProjectClient projectClient, ILogger<AgentProvisioningService> logger) : IAgentProvisioningService
{
    private const string SystemPrompt = """
        You are the Quiet Hours Librarian — a knowledgeable guide to the Brackenford Quiet Hours with an ol' English way with words
        Operating Guide. Your sole source of truth is the indexed document. Follow these rules:


        1. CITE YOUR SOURCES. When answering, quote the relevant passage or section number from the
           document (e.g. "Section 3 states: '…'"). Keep quotes concise but specific.

        2. BE PRECISE ABOUT NAMES AND TIMES. The document contains exact times, role names, and
           procedures. Use them exactly as written; do not paraphrase them away.

        3. ADMIT WHEN THE DOCUMENT IS SILENT. If the document does not contain enough information
           to answer the question, say so clearly: "The document does not cover this." Do not
           speculate, infer from general knowledge, or fill gaps with assumptions.

        4. STAY IN SCOPE. Only answer questions about the Quiet Hours programme and related content
           in the document. For unrelated questions, politely decline and redirect.

        5. BE CONCISE. One to three paragraphs is usually enough. Avoid padding.
        """;

    public async Task EnsureAgentAsync(RagOptions options, string vectorStoreId)
    {
        if (await AgentExistsAsync(options.AgentName))
        {
            return;
        }

        logger.LogDebug("Registering project agent");

        DeclarativeAgentDefinition agentDefinition = new(model: options.ModelDeploymentName)
        {
            Instructions = SystemPrompt,
            Tools = { ResponseTool.CreateFileSearchTool(vectorStoreIds: [vectorStoreId]) }
        };

        ClientResult<ProjectsAgentVersion> agentVersion =
            await projectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                agentName: options.AgentName,
                options: new(agentDefinition));

        logger.LogDebug("Agent version registered: {Version}", agentVersion.Value.Version);
    }

    private async Task<bool> AgentExistsAsync(string agentName)
    {
        logger.LogDebug("Checking for existing project agent");

        await foreach (ProjectsAgentRecord existingAgent in projectClient.AgentAdministrationClient.GetAgentsAsync())
        {
            if (existingAgent.Name == agentName)
            {
                logger.LogDebug("Reusing agent: {AgentId}", existingAgent.Id);
                return true;
            }
        }

        return false;
    }
}
