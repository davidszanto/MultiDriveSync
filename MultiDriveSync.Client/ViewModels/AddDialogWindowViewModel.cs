using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using MessageBox = System.Windows.MessageBox;

namespace MultiDriveSync.Client.ViewModels
{
    public class AddDialogWindowViewModel : BindableBase
    {
        private readonly AppSettings _appSettings;
        private readonly MultiDriveClientsService _multiDriveClientsService;
        private EditAccessMode _selectedEditAccessMode;
        private UserInfo _storageAccount;
        private UserInfo _userAccount;
        private string remoteRootId;

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
                var rootId = await driveClient.GetRootIdAsync();
                var dialog = new StorageAccountFolderPicker(async (parentId) => await driveClient.GetChildrenFoldersAsync(parentId), rootId);
                dialog.Show();
                var selectedFolder = await dialog.GetResult();
                RemoteRoot = selectedFolder.Name;
                remoteRootId = selectedFolder.Id;

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
                    RemoteRoot = remoteRootId,
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
            var emptyDirectory = true;
            if (!string.IsNullOrEmpty(LocalRoot) && !Directory.GetFileSystemEntries(LocalRoot).Any())
            {
                emptyDirectory = false;

                string messageBoxText = "Your selected folder is not empty. Please select an empty folder!";
                string caption = "Warning";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBox.Show(messageBoxText, caption, button, icon);
            }

            return _storageAccount != null
                && _userAccount != null
                && !string.IsNullOrEmpty(RemoteRoot)
                && !string.IsNullOrEmpty(LocalRoot)
                && emptyDirectory;
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