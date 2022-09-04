﻿using SteamAuthenticatorCore.MobileMaui.Abstractions;
using SteamAuthenticatorCore.MobileMaui.Pages;
using SteamAuthenticatorCore.Shared;
using SteamAuthenticatorCore.Shared.Abstractions;

namespace SteamAuthenticatorCore.MobileMaui;

public partial class App : Application
{
    public App(IEnvironment environment, AppSettings appSettings, IPlatformImplementations platformImplementations)
    {
        InitializeComponent();

        _environment = environment;
        _appSettings = appSettings;
        _platformImplementations = platformImplementations;

        MainPage = new AppShell();
        Shell.Current.Navigating += CurrentOnNavigating;
    }

    private readonly IEnvironment _environment;
    private readonly AppSettings _appSettings;
    private readonly IPlatformImplementations _platformImplementations;

    protected override void OnStart()
    {
        VersionTracking.Track();

        _appSettings.LoadSettings();
        _platformImplementations.SetTheme(_appSettings.Theme);

        RequestedThemeChanged += OnRequestedThemeChanged;
    }

    protected override void OnSleep()
    {
        RequestedThemeChanged -= OnRequestedThemeChanged;
    }

    protected override void OnResume()
    {
        RequestedThemeChanged += OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        _environment.SetStatusBarColorBasedOnAppTheme();
    }

    private static void CurrentOnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (e.Target.Location.OriginalString != string.Empty)
            return;

        if (e.Current.Location.OriginalString.Contains(nameof(TokenPage)))
            return;

        e.Cancel();

        var shell = (Shell)sender!;
        shell.GoToAsync($"//{nameof(TokenPage)}");
    }
}
