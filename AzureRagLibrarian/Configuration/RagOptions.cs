namespace AzureRagLibrarian.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public const string DefaultModelDeploymentName = "gpt-4o-mini";
    public const string DefaultDocumentPath = "samples/quiet-hours.txt";
    public const string DefaultVectorStoreName = "quiet-hours-vector-store";
    public const string DefaultAgentName = "quiet-hours-librarian";

    public Uri? ProjectEndpoint { get; set; }
    public string? TenantId { get; set; }
    public string ModelDeploymentName { get; set; } = DefaultModelDeploymentName;
    public string DocumentPath { get; set; } = DefaultDocumentPath;
    public string VectorStoreName { get; set; } = DefaultVectorStoreName;
    public string AgentName { get; set; } = DefaultAgentName;
}