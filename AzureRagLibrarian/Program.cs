using AzureRagLibrarian.Configuration;
using AzureRagLibrarian.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

IConfiguration configuration = AppConfiguration.Build(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        restrictedToMinimumLevel: LogEventLevel.Information,
        outputTemplate: "{Message:lj}{NewLine}{Exception}")
    .CreateLogger();

ServiceCollection services = new();

services.AddLogging(b => b.AddSerilog(dispose: true));

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

ILogger<Program> logger = provider.GetRequiredService<ILogger<Program>>();
var options = provider.GetRequiredService<IOptions<RagOptions>>().Value;

logger.LogInformation("Azure RAG Librarian starting");
logger.LogInformation("Document: {DocumentPath}", options.DocumentPath);
logger.LogInformation("Agent: {AgentName}", options.AgentName);

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
    logger.LogError(ex, "Unhandled error");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
