﻿using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using SteamAuthenticatorCore.Mobile;
using SteamAuthenticatorCore.Mobile.Helpers;
using Xamarin.Forms;
using SteamAuthenticatorCore.Mobile.Services.Interfaces;

namespace SteamMobileAuthenticatorCore.Droid
{
    [Activity(Label = "SteamMobileAuthenticatorCore", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            global::Xamarin.Forms.FormsMaterial.Init(this, savedInstanceState);
            Startup.Init(NativeConfiguration);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override async void OnBackPressed()
        {
            if (Shell.Current.CurrentPage is not IBackButtonAction backButtonAction)
            {
                base.OnBackPressed();
                return;
            }

            if (backButtonAction.OnBackActionAsync is null)
                return;

            if (!await backButtonAction.OnBackActionAsync.Invoke()) 
                base.OnBackPressed();
        }

        private static void NativeConfiguration(IServiceCollection services)
        {
            services.AddSingleton<IEnvironment, AndroidEnvironment>();
        }
    }
}