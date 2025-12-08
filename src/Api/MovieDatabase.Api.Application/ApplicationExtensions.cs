using Microsoft.Extensions.DependencyInjection;

using MovieDatabase.Api.Application.Films.CreateFilm;
using MovieDatabase.Api.Application.Films.DeleteFilm;
using MovieDatabase.Api.Application.Films.EditFilm;
using MovieDatabase.Api.Application.Users.AuthenticateUser;
using MovieDatabase.Api.Application.Users.CreateUser;
using MovieDatabase.Api.Application.Users.RevokeToken;
using MovieDatabase.Api.Core.Cqrs;
using MovieDatabase.Api.Core.Dtos.Films;
using MovieDatabase.Api.Core.Dtos.Users;

namespace MovieDatabase.Api.Application;

public static class ApplicationExtensions
{
    public static void AddApplicationDefaults(this IServiceCollection services)
        => services
            .AddDispatcher()
            .RegisterHandlers();

    private static IServiceCollection AddDispatcher(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        return services;
    }

    private static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<CreateFilmRequest, FilmDto>, CreateFilmRequestHandler>();
        services.AddScoped<IRequestHandler<CreateUserRequest, UserCredentialsDto>, CreateUserRequestHandler>();
        services.AddScoped<IRequestHandler<AuthenticateUserRequest, UserCredentialsDto>, AuthenticateUserRequestHandler>();
        services.AddScoped<IRequestHandler<DeleteFilmRequest, string>, DeleteFilmRequestHandler>();
        services.AddScoped<IRequestHandler<EditFilmRequest, FilmDto>, EditFilmRequestHandler>();
        services.AddScoped<IRequestHandler<RevokeTokenRequest, RevokeTokenDto>, RevokeTokenRequestHandler>();

        return services;
    }
}