﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SteamAuthCore.Abstractions;
using SteamAuthCore.Extensions;
using SteamAuthCore.Models;
using SteamAuthenticatorCore.Desktop.Services;
using SteamAuthenticatorCore.Desktop.ViewModels;
using SteamAuthenticatorCore.Desktop.Views;
using SteamAuthenticatorCore.Desktop.Views.Pages;
using SteamAuthenticatorCore.Shared;
using SteamAuthenticatorCore.Shared.Abstractions;
using SteamAuthenticatorCore.Shared.Extensions;
using SteamAuthenticatorCore.Shared.Models;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls.Window;
using Wpf.Ui.Services;

namespace SteamAuthenticatorCore.Desktop;

public sealed partial class App : Application
{
    public const string InternalName = "SteamDesktopAuthenticatorCore";
    public const string Name = "Steam desktop authenticator core";
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    private IServiceScope? _serviceScope;
    private readonly ILogger<App> _logger;

    public App()
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        _host = Host
            .CreateDefaultBuilder()
            .ConfigureHostConfiguration(builder =>
            {
                var appName = Assembly.GetEntryAssembly()!.GetName().Name;

                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{appName}.appsettings.json");
                builder.AddJsonStream(stream!);
            })
            .ConfigureLogging((context, builder) =>
            {
                builder.AddConfiguration(context.Configuration);

#if DEBUG
                builder.AddDebug();
#else
                builder.AddSentry();
#endif
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<ITaskBarService, TaskBarService>();
                services.AddSingleton<IThemeService, ThemeService>();

                services.AddSingleton<TaskBarServiceWrapper>();

                services.AddScoped<MainWindow>();

                services.AddScoped<TokenPage>();
                services.AddTransient<ConfirmationsOverviewPage>();
                services.AddTransient<ConfirmationsPage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<LoginPage>();

                services.AddScoped<TokenViewModel>();
                services.AddScoped<SettingsViewModel>();
                services.AddScoped<ConfirmationsOverviewViewModel>();
                services.AddScoped<ConfirmationsViewModel>();
                services.AddScoped<LoginViewModel>();

                services.AddSingleton<ObservableCollection<SteamGuardAccount>>();

                services.AddGoogleDriveApi(Name);
                services.AddSingleton<IPlatformImplementations, DesktopImplementations>();

                services.AddSingleton<AppSettings>(WpfAppSettings.Current);
                services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                services.AddScoped<LocalDriveAccountsFileService>();
                services.AddScoped<GoogleDriveAccountsFileService>();
                services.AddScoped<IUpdateService, DesktopUpdateService>();

                services.AddHttpClient<IUpdateService, DesktopUpdateService>();

                services.AddSteamAuthCoreServices();
                services.AddSharedServices();

                services.AddScoped<AccountsFileServiceResolver>(provider => () =>
                {
                    var appSettings = provider.GetRequiredService<AppSettings>();
                    return appSettings.AccountsLocation switch
                    {
                        AccountsLocationModel.LocalDrive =>
                            provider.GetRequiredService<LocalDriveAccountsFileService>(),
                        AccountsLocationModel.GoogleDrive =>
                            provider.GetRequiredService<GoogleDriveAccountsFileService>(),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                });
            })
            .Build();

        _logger = _host.Services.GetRequiredService<ILogger<App>>();
    }

    private readonly IHost _host;

    public static void OnException(Exception exception, ILogger logger)
    {
        logger.LogCritical(exception, "Exception occurred");

        MessageBox.Show( $"{exception.Message}\n\n{exception.StackTrace}", "Exception occurred", MessageBoxButton.OK, MessageBoxImage.Error);

        Application.Current.Shutdown();
    }

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        var environment = _host.Services.GetRequiredService<IHostEnvironment>();
        if (environment.IsDevelopment())
        {
            _serviceScope = _host.Services.CreateScope();
            ServiceProvider = _serviceScope.ServiceProvider;
        }
        else
            ServiceProvider = _host.Services;

        await _host.StartAsync();

        await ServiceProvider.GetRequiredService<ITimeAligner>().AlignTimeAsync();
        ServiceProvider.GetRequiredService<MainWindow>().Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        OnException(e.Exception, _logger);
    }
}