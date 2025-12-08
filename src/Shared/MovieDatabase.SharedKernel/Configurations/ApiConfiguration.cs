namespace MovieDatabase.SharedKernel.Configurations;

public static class ApiConfiguration
{
    public static readonly string ModuleName = Environment.GetEnvironmentVariable("API_MODULE") ?? "movies-db-api";
}