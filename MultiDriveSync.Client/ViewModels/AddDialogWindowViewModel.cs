using System;
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
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string LocalRoot { get; set; }


        public DelegateCommand SignInCommand { get; set; }
        public DelegateCommand BrowseCommand { get; set; }


        public event EventHandler CloseWindowRequestedEvent;


        public AddDialogWindowViewModel()
        {
            SignInCommand = new DelegateCommand(async () =>
            {
                var session = await SignInHelper.SignInAsync(new ClientInfo
                {
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    AppName = AppSettings.AppName
                });

                //TODO: egyéb beállítások hozzáadása a sessionhöz

                if(session != null)
                    AppSettings.AddSession(session);
                CloseWindowRequestedEvent.Invoke(null, null);
            });

            BrowseCommand = new DelegateCommand(() =>
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                    LocalRoot = dialog.SelectedPath;

                RaisePropertyChanged(nameof(LocalRoot));
            });
        }
    }
}