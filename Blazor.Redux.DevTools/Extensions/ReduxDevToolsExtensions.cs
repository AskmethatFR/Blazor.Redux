using Blazor.Redux.DevTools.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Blazor.Redux.DevTools.Extensions;

public static class ReduxDevToolsExtensions
{
    public static IServiceCollection AddReduxDevTools(this IServiceCollection services)
    {
        services.AddScoped<IReduxDevTools, ReduxDevTools>();
        services.AddScoped<ReduxDevToolsSubscriber>();

        return services;
    }
}