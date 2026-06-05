using Azure.AI.Projects;
using AzureRagLibrarian.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace AzureRagLibrarian.Services;

public sealed class RagChatSession(AIProjectClient projectClient, IOptions<RagOptions> options)
{
    public async Task RunAsync()
    {
        var responseClient = projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(options.Value.AgentName);

        Console.WriteLine();
        Console.WriteLine("=== Conversation Started (type 'exit' or press Enter to quit) ===");

        string? lastResponseId = null;

        while (true)
        {
            Console.WriteLine();
            Console.Write("You: ");

            string? userInput = Console.ReadLine();

            if (IsExitCommand(userInput))
            {
                Console.WriteLine("Ending session. Goodbye.");
                break;
            }

            Console.WriteLine("Sending message to agent...");

            try
            {
                ResponseResult response = await responseClient.CreateResponseAsync(
                    userInputText: userInput,
                    previousResponseId: lastResponseId);

                if (response.Status == ResponseStatus.Completed)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Agent > {response.GetOutputText()}");
                    lastResponseId = response.Id;
                }
                else if (response.Status == ResponseStatus.Incomplete)
                {
                    string reason = response.IncompleteStatusDetails?.Reason?.ToString() ?? "Unknown";
                    Console.WriteLine();
                    Console.WriteLine($"[Error] Agent execution was incomplete. Reason: {reason}");
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine($"[Error] Execution ended with status: {response.Status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"[Exception] {ex.Message}");
            }
        }
    }

    public static bool IsExitCommand(string? input)
    {
        return string.IsNullOrWhiteSpace(input)
            || input.Equals("exit", StringComparison.OrdinalIgnoreCase);
    }
}
