﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SteamAuthCore;
using SteamAuthenticatorAndroid.Services;
using SteamAuthenticatorAndroid.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace SteamAuthenticatorAndroid.ViewModels
{
    internal class MainPageViewModel : BaseViewModel
    {
        public MainPageViewModel()
        {
            _manifest = new ManifestModel();

            Task.Run(async () =>
            {
                Manifest = await ManifestModelService.GetManifest();

                AutoConfirmTradesTimer = new Timer(Manifest.PeriodicCheckingInterval, AutoTradeConfirmationTimerOnTick);

                if (Manifest!.AutoConfirmTrades)
                {
                    AutoConfirmTradesTimer.Start();
                }
            });

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                SteamGuardTimerOnTick();

                return true;
            });

            ImportAccount = new Command(AddMaFile);
            DeleteAccount = new Command(DeleteAccountMethod);
            CopyCommand = new Command(CopyCommandMethod);
            MoveAccountUpCommand = new Command(MoveAccountUp);
            MoveAccountDownCommand = new Command(MoveAccountDown);
            LoginCommand = new Command(ShowLoginWindow);
            ForceRefreshSessionCommand = new Command(ForceRefreshSession);
            ListTappedCommand = new Command(ListTappedCommandMethod);

            SettingsPageViewModel.Page = this;
        }

        #region Variables

        private long _steamTime;
        private long _currentSteamChunk;

        private ManifestModel _manifest;
        private SteamGuardAccount? _selectedAccount;
        private string _loginTokenText = string.Empty;
        private double _progressBar;

        #endregion

        #region Fields

        public Timer AutoConfirmTradesTimer = null!;

        public static SteamGuardAccount? SelectedGuardAccount;

        public ManifestModel Manifest
        {
            get => _manifest;
            private set => SetProperty(ref _manifest, value);
        }

        public SteamGuardAccount? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                SelectedGuardAccount = value;
                SetProperty(ref _selectedAccount, value);
            }
        }

        public string LoginTokenText
        {
            get => _loginTokenText;
            set => SetProperty(ref _loginTokenText, value);
        }

        public double ProgressBar
        {
            get => _progressBar;
            set
            {
                value /= 30;

                SetProperty(ref _progressBar, value);
            }
        }

        #endregion

        #region Commands
        public Command ImportAccount { get; }

        public Command CopyCommand { get; }

        public Command DeleteAccount { get; }

        public Command MoveAccountUpCommand { get; }

        public Command MoveAccountDownCommand { get; }

        public Command LoginCommand { get; }

        public Command ForceRefreshSessionCommand { get; }

        public Command ListTappedCommand { get; }

        #endregion

        #region Methods
        private void LoadAccountInfo()
        {
            if (SelectedAccount is null || _steamTime == 0) return;

            if (SelectedAccount.GenerateSteamGuardCodeForTime(_steamTime) is not { } token)
                return;

            LoginTokenText = token;
        }

        private async void SteamGuardTimerOnTick()
        {
            _steamTime = await TimeAligner.GetSteamTimeAsync();

            _currentSteamChunk = _steamTime / 30L;
            var secondsUntilChange = (int) (_steamTime - _currentSteamChunk * 30L);

            LoadAccountInfo();
            if (SelectedAccount is not null)
                ProgressBar = 30 - secondsUntilChange;
        }

        private static async void AddMaFile()
        {
            FileResult[] files;

            try
            {
                files = (await FilePicker.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Select maFile"
                })).ToArray();
            }
            catch
            {
                return;
            }

            foreach (var file in files)
            {
                if (!Path.GetExtension(file.FileName).Contains("maFile")) continue;

                await ManifestModelService.AddSteamGuardAccount(file.FullPath);
            }
        }

        private async void DeleteAccountMethod(object? obj)
        {
            if (obj is not SteamGuardAccount account) return;

            await ManifestModelService.DeleteSteamGuardAccount(account);
        }

        private void CopyCommandMethod()
        {
            Clipboard.SetTextAsync(LoginTokenText);
        }

        private async void MoveAccountUp(object obj)
        {
            if (Manifest is not { } ) return;
            if(obj is not SteamGuardAccount account) return;


            int index = Manifest.Accounts.IndexOf(account);
            if (index < 0 || Manifest.Accounts.Count <= 1) return;

            Manifest.Accounts.Move(index -1, index);
            await ManifestModelService.SaveManifest();
        }

        private async void MoveAccountDown(object obj)
        {
            if (Manifest is not { }) return;
            if (obj is not SteamGuardAccount account) return;

            int index = Manifest.Accounts.IndexOf(account);
            if (index + 1 >= Manifest.Accounts.Count || Manifest.Accounts.Count <= 1) return;

            Manifest.Accounts.Move(index, index +1);
            await ManifestModelService.SaveManifest();
        }

        private async void AutoTradeConfirmationTimerOnTick()
        {
            Dictionary<SteamGuardAccount, List<ConfirmationModel>> autoAcceptConfirmations = new();
            SteamGuardAccount[] accounts = Manifest!.Accounts.ToArray();

            foreach (var account in accounts)
            {
                try
                {
                    ConfirmationModel[] tmp = await account.FetchConfirmationsAsync();
                    foreach (var confirmationModel in tmp)
                    {
                        if ((confirmationModel.ConfType == ConfirmationModel.ConfirmationType.MarketSellTransaction && Manifest.AutoConfirmMarketTransactions) ||
                            (confirmationModel.ConfType == ConfirmationModel.ConfirmationType.Trade && Manifest.AutoConfirmTrades))
                        {
                            if (!autoAcceptConfirmations.ContainsKey(account))
                                autoAcceptConfirmations[account] = new List<ConfirmationModel>();

                            autoAcceptConfirmations[account].Add(confirmationModel);
                        }
                    }
                }
                catch (SteamGuardAccount.WgTokenInvalidException)
                {
                    await account.RefreshSessionAsync();
                }
                catch (SteamGuardAccount.WgTokenExpiredException)
                {
                    //Prompt to relogin
                    await Application.Current.MainPage.DisplayAlert("AutoTradeConfirmation error", "Relogin into your account", "Ok");
                    ShowLoginWindow(account);
                    break;
                }
                catch (WebException)
                {

                }
            }

            foreach (var acc in autoAcceptConfirmations.Keys)
            {
                var confirmations = autoAcceptConfirmations[acc].ToArray();
                acc.SendConfirmationAjax(confirmations, SteamGuardAccount.Confirmation.Allow);
            }
        }

        private async void ShowLoginWindow(object? obj)
        {
            if (obj is not SteamGuardAccount account) return;

            LoginPageViewModel.Account = account;
            await Shell.Current.GoToAsync($"{nameof(LoginPage)}?Account={account}", true);
        }

        private async Task<bool> RefreshAccountSession(SteamGuardAccount account, bool attemptRefreshLogin = true)
        {
            if (SelectedAccount is null) return false;

            try
            {
                bool refreshed = await SelectedAccount.RefreshSessionAsync();
                return refreshed;
            }
            catch (SteamGuardAccount.WgTokenExpiredException)
            {
                if (!attemptRefreshLogin) return false;

                ShowLoginWindow(account);

                return await RefreshAccountSession(account, false);
            }
        }

        private async void ForceRefreshSession(object? obj)
        {
            if (obj is not SteamGuardAccount account) return;

            if (await RefreshAccountSession(account))
            {
                await Application.Current.MainPage.DisplayAlert("Session refresh", "Your session has been refreshed", "Ok");
                await ManifestModelService.SaveManifest();

                return;
            }

            await Application.Current.MainPage.DisplayAlert("Session refresh", "Failed to refresh your session.\nTry using the \"Login again\" option.", "Ok");
        }

        private void ListTappedCommandMethod(object? obj)
        {
            if (obj is not ListView {SelectedItem: SteamGuardAccount account} list) return;

            SelectedAccount = account;

            list.SelectedItem = null;
        }

        #endregion
    }
}