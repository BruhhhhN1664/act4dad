using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EquipmentBorrowing.Application;
using EquipmentBorrowing.Desktop.ViewModels;
using EquipmentBorrowing.Desktop.Views;
using EquipmentBorrowing.Infrastructure;
using EquipmentBorrowing.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentBorrowing.Desktop;

// Fully qualified base class: the namespace EquipmentBorrowing.Application would otherwise shadow Avalonia.Application.
public partial class App : Avalonia.Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Composition root: the only place that knows about concrete infrastructure.
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)   // git-ignored; may hold a password
                .AddEnvironmentVariables()                                // ConnectionStrings__Postgres
                .Build();
            var connectionString = config.GetConnectionString("Postgres") ?? ConnectionDefaults.Default;

            var services = new ServiceCollection();
            services.AddPersistence(connectionString);
            services.AddApplicationServices();

            services.AddSingleton<CatalogViewModel>();
            services.AddSingleton<DemoFaultSwitch>();
            services.AddSingleton<BorrowViewModel>();
            services.AddSingleton<ActiveBorrowingsViewModel>();
            services.AddSingleton<StudentsViewModel>();
            services.AddSingleton<EquipmentManagementViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<SearchBorrowingsViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            var provider = services.BuildServiceProvider();
            desktop.MainWindow = new MainWindow { DataContext = provider.GetRequiredService<MainWindowViewModel>() };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
