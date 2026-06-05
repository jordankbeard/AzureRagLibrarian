using Microsoft.Extensions.Configuration;

namespace AzureRagLibrarian.Configuration;

public static class AppConfiguration
{
    public static IConfiguration Build(string[] args)
    {
        try
        {
            return CreateBuilder(includeUserSecrets: true).Build();
        }
        catch (UnauthorizedAccessException)
        {
            return CreateBuilder(includeUserSecrets: false).Build();
        }
    }

    private static IConfigurationBuilder CreateBuilder(bool includeUserSecrets)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true);

        if (includeUserSecrets)
        {
            builder.AddUserSecrets(typeof(AppConfiguration).Assembly, optional: true);
        }

        return builder.AddEnvironmentVariables();
    }
}
