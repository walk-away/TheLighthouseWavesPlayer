using TheLighthouseWavesPlayerApp.ViewModels;

namespace TheLighthouseWavesPlayerApp.Extensions;

public static class ServiceCollectionExtension
{
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