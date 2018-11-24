using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using MultiDriveSync.Client.Services;
using MultiDriveSync.Client.Views;
using MultiDriveSync.Models;
using Prism.Commands;
using Prism.Mvvm;
using Application = System.Windows.Application;

namespace MultiDriveSync.Client.ViewModels
{
    public class AddDialogWindowViewModel : BindableBase
    {
        private readonly AppSettings _appSettings;
        private readonly MultiDriveClientsService _multiDriveClientsService;
        private EditAccessMode _selectedEditAccessMode;
        private UserInfo _storageAccount;
        private UserInfo _userAccount;

        public string LocalRoot { get; set; }
        public string RemoteRoot { get; set; }
        public string StorageAccountEmail => _storageAccount?.Email;
        public string UserAccountEmail => _userAccount?.Email;
        public List<EditAccessMode> EditAccessModes { get; }
        public EditAccessMode SelectedEditAccessMode
        {
            get => _selectedEditAccessMode;
            set => SetProperty(ref _selectedEditAccessMode, value);
        }

        public DelegateCommand StorageAccountSignInCommand { get; set; }
        public DelegateCommand UserAccountSignInCommand { get; set; }
        public DelegateCommand BrowseLocalRootCommand { get; set; }
        public DelegateCommand BrowseRemoteRootCommand { get; set; }
        public DelegateCommand SaveSessionCommand { get; set; }
        public DelegateCommand CloseWindowCommand { get; set; }


        public event EventHandler CloseWindowRequestedEvent;


        public AddDialogWindowViewModel(AppSettings appSettings, MultiDriveClientsService multiDriveClientsService)
        {
            _appSettings = appSettings;
            _multiDriveClientsService = multiDriveClientsService;
            _selectedEditAccessMode = EditAccessMode.OwnedOnly;

            EditAccessModes = new List<EditAccessMode>
            {
                EditAccessMode.All,
                EditAccessMode.OwnedOnly
            };

            StorageAccountSignInCommand = new DelegateCommand(async () =>
            {
                _storageAccount = await SignInHelper.SignInAsync(_appSettings.ClientInfo);
                RaisePropertyChanged(nameof(StorageAccountEmail));
                BrowseRemoteRootCommand.RaiseCanExecuteChanged();
                SaveSessionCommand.RaiseCanExecuteChanged();
            });

            UserAccountSignInCommand = new DelegateCommand(async () =>
            {
                _userAccount = await SignInHelper.SignInAsync(_appSettings.ClientInfo);
                RaisePropertyChanged(nameof(UserAccountEmail));
                SaveSessionCommand.RaiseCanExecuteChanged();
            });

            BrowseLocalRootCommand = new DelegateCommand(() =>
            {
                var dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                    LocalRoot = dialog.SelectedPath;

                RaisePropertyChanged(nameof(LocalRoot));
                SaveSessionCommand.RaiseCanExecuteChanged();
            });

            BrowseRemoteRootCommand = new DelegateCommand(async () =>
            {
                var creds = await GetStoredCredentialAsync(_storageAccount.UserId, _appSettings.ClientInfo);
                var driveClient = new GoogleDriveClient(creds, _appSettings.ClientInfo.AppName);
                var dialog = new StorageAccountFolderPicker(async (parentId) => await driveClient.GetChildrenFoldersAsync(parentId));
                dialog.Show();
                var selectedFolderId = await dialog.GetResult();
                RemoteRoot = selectedFolderId;

                RaisePropertyChanged(nameof(RemoteRoot));
                SaveSessionCommand.RaiseCanExecuteChanged();
            }, () => _storageAccount != null);

            SaveSessionCommand = new DelegateCommand(() =>
            {
                var session = new Session
                {
                    StorageAccountInfo = _storageAccount,
                    UserInfo = _userAccount,
                    LocalRoot = LocalRoot,
                    RemoteRoot = RemoteRoot,
                    EditAccessMode = SelectedEditAccessMode
                };

                _appSettings.AddSession(session);
                _multiDriveClientsService.Start(session);

                CloseWindowRequestedEvent?.Invoke(this, EventArgs.Empty);
            }, () => ValidateInputs());

            CloseWindowCommand = new DelegateCommand(() => CloseWindowRequestedEvent?.Invoke(this, EventArgs.Empty));
        }

        private bool ValidateInputs()
        {
            return _storageAccount != null
                && _userAccount != null
                && !string.IsNullOrEmpty(RemoteRoot)
                && !string.IsNullOrEmpty(LocalRoot);
        }


        private async Task<UserCredential> GetStoredCredentialAsync(string userId, ClientInfo clientInfo)
        {
            var dataStore = new FileDataStore(clientInfo.AppName);

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientInfo.ClientId,
                ClientSecret = clientInfo.ClientSecret
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                DataStore = dataStore
            });

            var token = await dataStore.GetAsync<TokenResponse>(userId);

            return new UserCredential(flow, userId, token);
        }
    }
}