﻿using System.Collections.Generic;
using SteamAuthCore.Models;
using System.Collections.ObjectModel;

namespace SteamAuthenticatorCore.Shared.Models;

public sealed class SteamGuardAccountConfirmationsModel
{
    public SteamGuardAccountConfirmationsModel(SteamGuardAccount account, List<ConfirmationModel> confirmations)
    {
        Account = account;
        Confirmations = new ObservableCollection<ConfirmationModel>(confirmations);
    }

    public SteamGuardAccount Account { get; }
    public ObservableCollection<ConfirmationModel> Confirmations { get; }
}
