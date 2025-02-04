using Backend.Services;

namespace Backend.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<ChatCompletionService>();
        return services;
    }
}
