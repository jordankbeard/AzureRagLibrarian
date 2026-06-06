using Azure.AI.Projects;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace AzureRagLibrarian.Services;

public sealed class RagChatSession(
    AIProjectClient projectClient,
    IOptions<RagOptions> options,
    ILogger<RagChatSession> logger)
{
    public async Task RunAsync()
    {
        var responseClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(options.Value.AgentName);

        logger.LogInformation("=== Conversation Started (type 'exit' or press Enter to quit) ===");

        string? lastResponseId = null;

        while (true)
        {
            Console.WriteLine();
            Console.Write("You: ");

            string? userInput = Console.ReadLine();

            if (IsExitCommand(userInput))
            {
                logger.LogInformation("Ending session. Goodbye.");
                break;
            }

            logger.LogDebug("Sending message to agent");

            try
            {
                ResponseResult response = await responseClient.CreateResponseAsync(
                    userInputText: userInput,
                    previousResponseId: lastResponseId);

                if (response.Status == ResponseStatus.Completed)
                {
                    logger.LogInformation("Agent > {ResponseText}", response.GetOutputText());
                    lastResponseId = response.Id;
                }
                else if (response.Status == ResponseStatus.Incomplete)
                {
                    string reason = response.IncompleteStatusDetails?.Reason?.ToString() ?? "Unknown";
                    logger.LogWarning("[Error] Agent execution was incomplete. Reason: {Reason}", reason);
                }
                else
                {
                    logger.LogWarning("[Error] Execution ended with status: {Status}", response.Status);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Exception] Error communicating with agent");
            }
        }
    }

    public static bool IsExitCommand(string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            || input.Equals("exit", StringComparison.OrdinalIgnoreCase);
    }
}
