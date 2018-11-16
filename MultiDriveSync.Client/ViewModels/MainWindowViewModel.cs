using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MultiDriveSync.Client.Helpers;
using MultiDriveSync.Client.Views;
using MultiDriveSync.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace MultiDriveSync.Client.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public ObservableCollection<Session> Sessions { get; set; } = new ObservableCollection<Session>();

        public DelegateCommand AddCommand { get; }

        public DelegateCommand InitializeViewModelCommand { get; set; }

        public MainWindowViewModel()
        {
            AddCommand = new DelegateCommand(() =>
            {
                var dialog = new AddDialogWindow();
                dialog.ShowDialog();
            });
            InitializeViewModelCommand = new DelegateCommand(async () => await OnInitializedAsync());
        }

        async Task OnInitializedAsync()
        {
            var clientInfo = new ClientInfo
            {
                ClientName = "default",
                ClientId = AppSettings.DefaultClientId,
                ClientSecret = AppSettings.DefaultClientSecret,
                AppName = AppSettings.AppName
            };

            if (!TryGetFirstUserId(clientInfo, out var userId))
            {
                userId = await SignInAsync(clientInfo);
            }

            var users = AppSettings.GetSessions();
            if (users.Any())
                users.ToList().ForEach(x => Sessions.Add(x));

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
            var session = await SignInHelper.SignInAsync(clientInfo);

            AppSettings.AddSession(session);

            return session.UserInfo.UserId;
        }
    }
}