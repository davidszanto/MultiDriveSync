using MultiDriveSync.Client.Helpers;
using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiDriveSync.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            var clientInfo = new ClientInfo
            {
                ClientId = AppSettings.ClientId,
                ClientSecret = AppSettings.ClientSecret,
                AppName = AppSettings.AppName
            };

            if (!TryGetFirstUserId(clientInfo, out var userId))
            {
                userId = await SignInAsync(clientInfo);
            }

            var multiDriveSync = new MultiDriveSyncService(settings =>
            {
                settings.StorageAccountId = "";
                settings.UserAccountId = userId;
                settings.StorageRootPath = "";
                settings.LocalRootPath = "";
                settings.EditingAccessLevel = EditAccessMode.OwnedOnly;
                settings.ClientInfo = clientInfo;
            });

            var fileNames = await multiDriveSync.ListFilesAsync();
        }

        private bool TryGetFirstUserId(ClientInfo clientInfo, out string userId)
        {
            var email = AppSettings.GetUserEmails().FirstOrDefault();
            if (!string.IsNullOrEmpty(email))
            {
                if (AppSettings.TryGetUserId(email, out userId))
                {
                    return true;
                }
            }

            userId = string.Empty;
            return false;
        }

        private async Task<string> SignInAsync(ClientInfo clientInfo)
        {
            var (email, userId) = await SignInHelper.SignInAsync(clientInfo);
            AppSettings.AddUser(email, userId);

            return userId;
        }
    }
}
