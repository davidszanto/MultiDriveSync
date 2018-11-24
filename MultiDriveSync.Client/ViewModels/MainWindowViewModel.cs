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
        private AppSettings _appSettings;

        public ObservableCollection<Session> Sessions { get; set; } = new ObservableCollection<Session>();

        public DelegateCommand AddCommand { get; }

        public DelegateCommand InitializeViewModelCommand { get; set; }

        public MainWindowViewModel(AppSettings appSettings)
        {
            _appSettings = appSettings;

            AddCommand = new DelegateCommand(() =>
            {
                var dialog = new AddDialogWindow();
                dialog.ShowDialog();
            });
            InitializeViewModelCommand = new DelegateCommand(async () => await OnInitializedAsync());
        }

        async Task OnInitializedAsync()
        {
            var sessions = _appSettings.GetSessions().ToList();


            //var clientInfo = new ClientInfo
            //{
            //    ClientName = "default",
            //    ClientId = _appSettings.ClientId,
            //    ClientSecret = _appSettings.ClientSecret,
            //    AppName = _appSettings.AppName
            //};

            //if (!TryGetFirstUserId(clientInfo, out var userId))
            //{
            //    userId = await SignInAsync(clientInfo);
            //}

            //var users = _appSettings.GetSessions();
            //if (users.Any())
            //    users.ToList().ForEach(x => Sessions.Add(x));

            //var multiDriveSync = new MultiDriveSyncService(settings =>
            //{
            //    settings.StorageAccountId = "";
            //    settings.UserAccountId = userId;
            //    settings.StorageRootPath = "";
            //    settings.LocalRootPath = "";
            //    settings.EditAccessMode = EditAccessMode.OwnedOnly;
            //    settings.ClientInfo = clientInfo;
            //});

            //var fileNames = await multiDriveSync.ListFilesAsync();
        }

        private bool TryGetFirstUserId(ClientInfo clientInfo, out string userId)
        {
            var email = _appSettings.GetUserEmails().FirstOrDefault();
            if (!string.IsNullOrEmpty(email))
            {
                if (_appSettings.TryGetUserId(email, out userId))
                {
                    return true;
                }
            }

            userId = string.Empty;
            return false;
        }

        //private async Task<string> SignInAsync(ClientInfo clientInfo)
        //{
        //    var session = await SignInHelper.SignInAsync(clientInfo);

        //    _appSettings.AddSession(session);

        //    return session.UserInfo.UserId;
        //}
    }
}