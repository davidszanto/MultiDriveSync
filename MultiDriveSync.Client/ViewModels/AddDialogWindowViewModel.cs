using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using MultiDriveSync.Client.Helpers;
using MultiDriveSync.Models;
using Prism.Commands;
using Prism.Mvvm;
using Application = System.Windows.Application;

namespace MultiDriveSync.Client.ViewModels
{
    public class AddDialogWindowViewModel : BindableBase
    {
        private AppSettings _appSettings;
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


        public AddDialogWindowViewModel(AppSettings appSettings)
        {
            _appSettings = appSettings;
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
            });

            UserAccountSignInCommand = new DelegateCommand(async () =>
            {
                _userAccount = await SignInHelper.SignInAsync(_appSettings.ClientInfo);
                RaisePropertyChanged(nameof(UserAccountEmail));
            });

            BrowseLocalRootCommand = new DelegateCommand(() =>
            {
                var dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                    LocalRoot = dialog.SelectedPath;

                RaisePropertyChanged(nameof(LocalRoot));
            });

            BrowseRemoteRootCommand = new DelegateCommand(() =>
            {
                var dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                {
                    RemoteRoot = dialog.SelectedPath;
                }

                RaisePropertyChanged(nameof(RemoteRoot));
            }, () => _storageAccount != null);

            SaveSessionCommand = new DelegateCommand(() =>
            {
                _appSettings.AddSession(new Session
                {
                    StorageAccountInfo = _storageAccount,
                    UserInfo = _userAccount,
                    LocalRoot = LocalRoot,
                    RemoteRoot = RemoteRoot,
                    EditAccessMode = SelectedEditAccessMode
                });

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
    }
}