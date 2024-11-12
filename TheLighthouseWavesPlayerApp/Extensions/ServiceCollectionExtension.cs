using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheLighthouseWavesPlayerApp.AI;
using TheLighthouseWavesPlayerApp.ViewModels;

namespace TheLighthouseWavesPlayerApp.Extensions;

public static class ServiceCollectionExtension
{
    public static void AddTheLighthouseWavesPlayerServices(this IServiceCollection services)
    {
        services.AddSingleton<IAiService, AiService>();
        
        services.AddSingleton<IAiProvider>(s =>
        {
            if (AiSettings.Provider == "OpenAI")
            {
                return new OpenAiProvider();
            }

            throw new InvalidOperationException("Unsupported AI provider specified in AiSettings.");
        });
        
    }
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<PickLanguageViewModel>();
        return services;
    }
    
    public static IServiceCollection AddViews(this IServiceCollection services)
    {

        return services;
    }
}