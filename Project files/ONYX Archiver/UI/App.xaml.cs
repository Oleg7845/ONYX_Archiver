using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using OnyxArchiver.Domain.Interfaces;
using OnyxArchiver.Infrastructure.Data;
using OnyxArchiver.Infrastructure.Repositories;
using OnyxArchiver.Infrastructure.Services;
using OnyxArchiver.Infrastructure.Services.Security;
using OnyxArchiver.UI.Messages;
using OnyxArchiver.UI.MVVM.Archive.Add;
using OnyxArchiver.UI.MVVM.Archive.Open;
using OnyxArchiver.UI.MVVM.Auth.Login;
using OnyxArchiver.UI.MVVM.Auth.Registration;
using OnyxArchiver.UI.MVVM.Main;
using OnyxArchiver.UI.MVVM.Main.Dialogs;
using OnyxArchiver.UI.MVVM.Peers;
using OnyxArchiver.UI.MVVM.Settings;
using Serilog;
using Serilog.Formatting.Json;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OnyxArchiver.UI;

/// <summary>
/// Main WPF application entry point.
/// Manages lifecycle, dependency injection, logging, and single-instance IPC.
/// </summary>
public partial class App : Application
{
    private const string MutexName = "OnyxArchiver_SingleInstance";
    private const string PipeName = "OnyxArchiver_Pipe";
    private const int SW_RESTORE = 9;

    private Mutex? _mutex;
    private readonly CancellationTokenSource _pipeCancellation = new();

    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Global service provider for resolving dependencies across the app.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Fast access to the MainViewModel from anywhere in the UI layer.
    /// </summary>
    public MainViewModel? MainVM => Services?.GetService<MainViewModel>();

    #region Win32 API

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    #endregion

    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Setup diagnostic systems
        InitLogger();
        RegisterGlobalExceptionHandlers();

        // 2. Enforce Single Instance policy using a Mutex
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // If another instance exists, send startup arguments via Pipe and terminate
            SendArgsToRunningInstance(e.Args);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // 3. Initialize Dependency Injection container
        Services = ConfigureServices();

        // 4. Start listening for arguments from potential future instances
        StartPipeServer();

        // 5. Initialize UI
        var mainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        // 6. Handle initial file associations if app was launched via double-click on a file
        if (e.Args.Length > 0)
        {
            HandleFileOpen(e.Args[0]);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pipeCancellation.Cancel();
        Log.CloseAndFlush();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Centralized exception logging for AppDomain, UI Dispatcher, and Tasks.
    /// </summary>
    private void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            Log.Fatal(ex.ExceptionObject as Exception, "Fatal AppDomain exception");

        DispatcherUnhandledException += (s, ex) =>
        {
            Log.Error(ex.Exception, "Unhandled UI Dispatcher exception");
            ex.Handled = true; // Prevent app crash
        };

        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            Log.Warning(ex.Exception, "Unobserved Task exception");
            ex.SetObserved();
        };
    }

    /// <summary>
    /// Routes file/directory paths to specific handlers based on extension or type.
    /// Uses WeakReferenceMessenger to broadcast events to ViewModels.
    /// </summary>
    private void HandleFileOpen(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        if (Directory.Exists(path))
        {
            WeakReferenceMessenger.Default.Send(new PathMessage(path));
            return;
        }

        if (!File.Exists(path)) return;

        var extension = Path.GetExtension(path).ToLowerInvariant();
        switch (extension)
        {
            case ".onx": // Encrypted archive
                WeakReferenceMessenger.Default.Send(new ArchivePathMessage(path));
                break;
            case ".onxk": // Cryptographic key/handshake file
                WeakReferenceMessenger.Default.Send(new HandshakePathMessage(path));
                break;
            default: // General file
                WeakReferenceMessenger.Default.Send(new PathMessage(path));
                break;
        }
    }

    /// <summary>
    /// IPC Client: Sends command-line arguments to the master instance using Named Pipes.
    /// </summary>
    private void SendArgsToRunningInstance(string[] args)
    {
        if (args.Length == 0) return;

        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000); // Wait up to 2 seconds for the pipe server

            using var writer = new StreamWriter(client, leaveOpen: true);
            writer.WriteLine(args[0]);
            writer.Flush();
        }
        catch (Exception ex)
        {
            Log.Warning("Failed to send arguments to running instance: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// IPC Server: Listens for incoming paths from secondary application instances.
    /// </summary>
    private void StartPipeServer()
    {
        Task.Run(async () =>
        {
            while (!_pipeCancellation.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(_pipeCancellation.Token);

                    using var reader = new StreamReader(server, leaveOpen: true);
                    var message = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            BringMainWindowToFront();
                            HandleFileOpen(message);
                        });
                    }
                }
                catch (OperationCanceledException) { /* Normal shutdown */ }
                catch (Exception ex)
                {
                    Log.Error(ex, "Pipe server error");
                }
            }
        }, _pipeCancellation.Token);
    }

    /// <summary>
    /// DI Configuration: Registers all services, repositories, and ViewModels.
    /// </summary>
    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 1. Infrastructure Services
        services.AddSingleton<IAppService, AppService>();
        services.AddSingleton<IDbContextFactory>(sp =>
        {
            var factory = new DbContextFactory(sp.GetRequiredService<IAppService>());
            factory.Initialize();
            return factory;
        });

        // 2. Repositories (Transient for thread-safety per request)
        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IPeerRepository, PeerRepository>();

        // 3. Domain Logic & Security
        services.AddHttpClient();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserVaultService, UserVaultService>();
        services.AddSingleton<IPeerService, PeerService>();
        services.AddTransient<IUserService, UserService>();
        services.AddSingleton<IArchiveService, ArchiveService>();
        services.AddSingleton<IDialogService, DialogService>();

        // 4. MVVM Components (ViewModels)
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<AddArchiveViewModel>();
        services.AddTransient<OpenArchiveViewModel>();
        services.AddTransient<PeersViewModel>();
        services.AddTransient<SettingsViewModel>();

        // 5. Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<UpdateDialog>();

        var provider = services.BuildServiceProvider();

        // Immediate initialization of DB
        provider.GetRequiredService<IDbContextFactory>();

        return provider;
    }

    /// <summary>
    /// Serilog configuration for persistent logging in LocalAppData.
    /// </summary>
    private void InitLogger()
    {
        var logsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ONYX Archiver", "Logs");

        Directory.CreateDirectory(logsDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.File(
                Path.Combine(logsDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}\n"))
            .WriteTo.Async(a => a.File(
                new JsonFormatter(renderMessage: true),
                Path.Combine(logsDirectory, "log-.json"),
                rollingInterval: RollingInterval.Day))
            .Enrich.WithProperty("App", "ONYX Archiver")
            .Enrich.WithProperty("Version", "1.0.0")
            .CreateLogger();
    }

    /// <summary>
    /// Forces the application window to the foreground across all desktop levels.
    /// </summary>
    private void BringMainWindowToFront()
    {
        if (MainWindow == null) return;

        var handle = new WindowInteropHelper(MainWindow).Handle;

        if (MainWindow.WindowState == WindowState.Minimized)
            ShowWindow(handle, SW_RESTORE);

        MainWindow.Show();
        MainWindow.Activate();
        SetForegroundWindow(handle);

        // Force to topmost and back to ensure it punches through other windows
        MainWindow.Topmost = true;
        MainWindow.Topmost = false;
        MainWindow.Focus();
    }
}