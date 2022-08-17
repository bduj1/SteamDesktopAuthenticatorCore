﻿using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SteamAuthenticatorCore.Mobile.Pages;
using SteamAuthenticatorCore.Shared.Messages;
using SteamAuthenticatorCore.Shared.Models;
using SteamAuthenticatorCore.Shared.Services;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace SteamAuthenticatorCore.Mobile.ViewModels;

public sealed partial class ConfirmationsOverviewViewModel : ObservableObject
{
    public ConfirmationsOverviewViewModel(ConfirmationServiceBase confirmationServiceBase, IMessenger messenger)
    {
        _messenger = messenger;
        ConfirmationServiceBase = confirmationServiceBase;
    }

    private readonly IMessenger _messenger;
    private bool _needRefresh;

    [ObservableProperty]
    private bool _isRefreshing;

    public ConfirmationServiceBase ConfirmationServiceBase { get; }

    [RelayCommand]
    private async Task Refresh()
    {
        if (!_needRefresh)
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.LongPress);
            }
            catch
            {
                //
            }
        }

        await ConfirmationServiceBase.CheckConfirmations();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task OnTouched(ConfirmationAccountModelBase account)
    {
        _needRefresh = true;

        await Shell.Current.GoToAsync($"{nameof(ConfirmationsPage)}");
        _messenger.Send(new UpdateAccountConfirmationPageMessage(account));
    }
}