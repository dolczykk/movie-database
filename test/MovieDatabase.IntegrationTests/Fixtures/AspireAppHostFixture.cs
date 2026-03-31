using System.Diagnostics;
using System.Reflection;

using Aspire.Hosting;
using Aspire.Hosting.Testing;

using Microsoft.Extensions.Configuration;

namespace MovieDatabase.IntegrationTests.Fixtures;

public class AspireAppHostFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("App not initialized");

    public async Task InitializeAsync()
    {
        var config = LoadIntegrationTestConfiguration();
        SetJwtEnvironmentVariables(config);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.MovieDatabase_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        await WaitForServicesAsync();
    }

    private async Task WaitForServicesAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        const int initialWaitSeconds = 60;
        await Task.Delay(TimeSpan.FromSeconds(initialWaitSeconds));

        HttpClient? client = null;
        try
        {
            client = CreateHttpClient("movies-db-api");
            client.Timeout = TimeSpan.FromSeconds(30);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create HTTP client: {ex.Message}");
            throw;
        }

        const int maxRetries = 60;
        var retryCount = 0;
        const int retryDelaySeconds = 5;

        while (retryCount < maxRetries)
        {
            try
            {
                Console.WriteLine($"Health check attempt {retryCount + 1}/{maxRetries} (elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s)...");
                var response = await client.GetAsync("/graphql?sdl");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ API is ready after {stopwatch.Elapsed.TotalSeconds:F1}s!");
                    Console.WriteLine("Allowing extra time for database seeding to complete...");

                    // Additional delay to ensure seeding is complete
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    stopwatch.Stop();
                    Console.WriteLine($"=== Initialization complete in {stopwatch.Elapsed.TotalSeconds:F1}s ===");
                    return;
                }

                Console.WriteLine($"  API returned status: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"  Connection error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"  Request timed out after {client.Timeout.TotalSeconds}s");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Unexpected error: {ex.GetType().Name} - {ex.Message}");
            }

            if (retryCount < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
            retryCount++;
        }

        stopwatch.Stop();

        const int totalWaitTime = initialWaitSeconds + (maxRetries * retryDelaySeconds);

        throw new TimeoutException(
            $"API failed to become ready within {totalWaitTime} seconds ({stopwatch.Elapsed.TotalMinutes:F1} minutes). " +
            "The Cosmos DB emulator may need more time to start, or there may be an issue with the API startup. " +
            "Check the Aspire dashboard logs for more details.");
    }

    private static IConfigurationRoot LoadIntegrationTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();

        using var stream = GetIntegrationTestConfigStream();

        configBuilder.AddJsonStream(stream);

        return configBuilder.Build();
    }

    private static void SetJwtEnvironmentVariables(IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"];
        var jwtIssuer = config["Jwt:Issuer"];
        var jwtAudience = config["Jwt:Audience"];

        if (!string.IsNullOrEmpty(jwtKey))
        {
            Environment.SetEnvironmentVariable("Jwt__Key", jwtKey);
        }
        if (!string.IsNullOrEmpty(jwtIssuer))
        {
            Environment.SetEnvironmentVariable("Jwt__Issuer", jwtIssuer);
        }
        if (!string.IsNullOrEmpty(jwtAudience))
        {
            Environment.SetEnvironmentVariable("Jwt__Audience", jwtAudience);
        }
    }

    private static Stream GetIntegrationTestConfigStream()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "MovieDatabase.IntegrationTests.appsettings.IntegrationTest.json";

        var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream != null)
        {
            return stream;
        }

        var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
        throw new FileNotFoundException(
            $"Embedded resource '{resourceName}' not found. " +
            $"Available resources: {availableResources}");

    }

    public HttpClient CreateHttpClient(string resourceName)
    {
        var client = App.CreateHttpClient(resourceName);

        client.Timeout = TimeSpan.FromMinutes(5);
        return client;
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }
    }
}