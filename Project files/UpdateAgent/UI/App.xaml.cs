using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using System.IO;
using System.Windows;
using UpdateAgent.Domain.Interfaces;
using UpdateAgent.Infrastructure.Services;
using UpdateAgent.UI.MVVM.Main;

namespace UpdateAgent.UI;

/// <summary>
/// Interaction logic for App.xaml. 
/// This class acts as the Composition Root of the application, responsible for 
/// bootstrapping services, configuring logging, and managing global exception boundaries.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Provides a statically accessible, strongly-typed reference to the current application instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// The central Dependency Injection container for the application.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }


    /// <summary>
    /// Orchestrates the application startup sequence.
    /// </summary>
    /// <param name="e">The event arguments containing command-line parameters.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize logging before any other operation to ensure startup issues are captured.
        InitLogger();
        RegisterGlobalExceptionHandlers();

        base.OnStartup(e);

        // Validation of mandatory CLI arguments required for the update process.
        // The agent expects exactly 3 arguments: [AppName] [AppExePath] [UpdateFilePath].
        if (e.Args.Length != 3)
        {
            Log.Fatal("Application terminated: Invalid arguments count. Expected 3, received {Count}.", e.Args.Length);
            Environment.Exit(0);
        }

        // Configure the service provider and resolve the primary window.
        Services = ConfigureServices(e.Args[0], e.Args[1], e.Args[2]);

        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    /// <summary>
    /// Handles the application shutdown sequence.
    /// </summary>
    /// <param name="e">Exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        // Flush Serilog buffers to ensure no log entries are lost during process termination.
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>
    /// Configures the Dependency Injection container and registers all application components.
    /// </summary>
    /// <param name="appName">The name of the target application.</param>
    /// <param name="appExePath">The file path to the target application's executable.</param>
    /// <param name="updateFilePath">The path to the update package.</param>
    /// <returns>A built service provider containing the application's dependency graph.</returns>
    private IServiceProvider ConfigureServices(string appName, string appExePath, string updateFilePath)
    {
        var services = new ServiceCollection();

        // --- Infrastructure Services ---
        // IAppService is registered as a Singleton to persist environment-specific properties.
        services.AddSingleton<IAppService>(s =>
        {
            var appService = new AppService();
            appService.AddPropertiesValues(appName, appExePath, updateFilePath);

            return appService;
        });

        services.AddSingleton<IUpdateService, UpdateService>();

        // --- UI Layer (MVVM) ---
        services.AddSingleton<MainWindowViewModel>();

        // Views are registered in the DI container to support constructor injection.
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();

        // Eagerly resolve IAppService to ensure properties are applied immediately.
        _ = provider.GetRequiredService<IAppService>();

        return provider;
    }

    /// <summary>
    /// Sets up global exception monitoring to prevent silent crashes and improve observability.
    /// </summary>
    private void RegisterGlobalExceptionHandlers()
    {
        // Captures exceptions occurring in any thread within the application's domain.
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            Log.Fatal(ex.ExceptionObject as Exception, "TERMINATING: Unhandled AppDomain exception.");
        };

        // Captures exceptions thrown on the main UI Dispatcher thread.
        DispatcherUnhandledException += (s, ex) =>
        {
            Log.Error(ex.Exception, "NON-TERMINATING: Unhandled UI thread exception.");
            // Mark as handled to prevent the application from crashing on UI-specific glitches.
            ex.Handled = true;
        };

        // Captures exceptions in Tasks that were not awaited, preventing process termination (in some .NET versions).
        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            Log.Error(ex.Exception, "Unobserved Task exception.");
            ex.SetObserved();
        };
    }

    /// <summary>
    /// Configures the Serilog logging engine with dual-sink output (Text and JSON).
    /// </summary>
    private void InitLogger()
    {
        // Logs are stored in %LocalAppData% to ensure the application has write permissions.
        var logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ONYX Archiver",
            "Update Agent",
            "Logs");

        Directory.CreateDirectory(logsDirectory);

        var txtLogPath = Path.Combine(logsDirectory, "log-.txt");
        var jsonLogPath = Path.Combine(logsDirectory, "log-.json");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            // Async sinks are used to decouple logging I/O from the UI and business logic performance.
            .WriteTo.Async(a => a.File(
                txtLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}\n"
            ))
            .WriteTo.Async(a => a.File(
                new JsonFormatter(renderMessage: true),
                jsonLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
            ))
            .Enrich.WithProperty("App", "Update Agent")
            .Enrich.WithProperty("Version", "1.0")
            .CreateLogger();
    }
}