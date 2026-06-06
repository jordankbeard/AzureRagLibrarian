using AzureRagLibrarian.Configuration;
using AzureRagLibrarian.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

IConfiguration configuration = AppConfiguration.Build(args);

ServiceCollection services = new();

services
    .AddOptions<RagOptions>()
    .Bind(configuration.GetSection("Rag"))
    .ValidateOnStart()
    .Services
    .AddSingleton<IValidateOptions<RagOptions>, RagOptionsValidator>();

services.AddSingleton<IAzureProjectClientFactory, AzureProjectClientFactory>();
services.AddSingleton(sp => sp.GetRequiredService<IAzureProjectClientFactory>().Create());
services.AddSingleton<IDocumentIndexService, DocumentIndexService>();
services.AddSingleton<IAgentProvisioningService, AgentProvisioningService>();
services.AddSingleton<IRagChatSession, RagChatSession>();

services.PostConfigure<RagOptions>(opts =>
{
    opts.ProjectEndpoint = configuration.GetValue<Uri>("AzureAI:ProjectEndpoint");
    opts.TenantId = configuration["AzureAI:TenantId"];
    opts.ModelDeploymentName =
        configuration["AzureAI:ModelDeploymentName"] ?? RagOptions.DefaultModelDeploymentName;
});

await using ServiceProvider provider = services.BuildServiceProvider();

var options = provider.GetRequiredService<IOptions<RagOptions>>().Value;

Console.WriteLine("Azure RAG Librarian starting");
Console.WriteLine($"Document: {options.DocumentPath}");
Console.WriteLine($"Agent: {options.AgentName}");

try
{
    var documentIndexService = provider.GetRequiredService<IDocumentIndexService>();
    var vectorStore = await documentIndexService.EnsureVectorStoreAsync(options);

    var agentProvisioningService = provider.GetRequiredService<IAgentProvisioningService>();
    await agentProvisioningService.EnsureAgentAsync(options, vectorStore.Id);

    var chatSession = provider.GetRequiredService<IRagChatSession>();
    await chatSession.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Unhandled error {ex}");
    return 1;
}
