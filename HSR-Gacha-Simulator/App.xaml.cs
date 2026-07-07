using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using HSR_Gacha_Simulator.Services;
using HSR_Gacha_Simulator.ViewModels;
using HSR_Gacha_Simulator.Views;

namespace HSR_Gacha_Simulator;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        // Services — singleton
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IPoolDataService, PoolDataService>();
        services.AddSingleton<IIconService, IconService>();

        // ViewModels — transient
        services.AddTransient<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
