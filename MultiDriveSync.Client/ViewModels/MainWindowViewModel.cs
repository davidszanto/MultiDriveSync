using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MultiDriveSync.Client.Services;
using MultiDriveSync.Client.Views;
using MultiDriveSync.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace MultiDriveSync.Client.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly AppSettings _appSettings;
        private readonly MultiDriveClientsService _multiDriveClientsService;

        public ObservableCollection<Session> Sessions { get; set; } = new ObservableCollection<Session>();
        public DelegateCommand AddCommand { get; }
        public DelegateCommand InitializeViewModelCommand { get; set; }
        public DelegateCommand OnClosingCommand { get; }

        public MainWindowViewModel(AppSettings appSettings, MultiDriveClientsService multiDriveClientsService)
        {
            _appSettings = appSettings;
            _multiDriveClientsService = multiDriveClientsService;

            AddCommand = new DelegateCommand(() =>
            {
                var dialog = new AddDialogWindow();
                dialog.ShowDialog();
            });

            InitializeViewModelCommand = new DelegateCommand(() => OnInitialized());
            OnClosingCommand = new DelegateCommand(() => _multiDriveClientsService.StopAll());
        }

        private void OnInitialized()
        {
            foreach (var session in _appSettings.GetSessions())
            {
                _multiDriveClientsService.Start(session);
            }
        }
    }
}