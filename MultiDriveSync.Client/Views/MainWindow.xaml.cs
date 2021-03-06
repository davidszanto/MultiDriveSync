﻿using MultiDriveSync.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MultiDriveSync.Client.ViewModels;

namespace MultiDriveSync.Client.Views
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

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var session = (sender as Button).DataContext as Session;
            (DataContext as MainWindowViewModel).DeleteSession(session);
        }
    }
}
