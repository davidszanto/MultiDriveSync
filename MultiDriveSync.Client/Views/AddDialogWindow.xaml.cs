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
using System.Windows.Shapes;
using MultiDriveSync.Client.ViewModels;

namespace MultiDriveSync.Client.Views
{
    /// <summary>
    /// Interaction logic for AddDialogWindow.xaml
    /// </summary>
    public partial class AddDialogWindow : Window
    {
        public AddDialogWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            (DataContext as AddDialogWindowViewModel).CloseWindowRequestedEvent += (sender, args) => { Close(); };
        }

        
    }
}
