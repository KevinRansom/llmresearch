﻿using System.Windows;

namespace ScheduleApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ScheduleApp.ViewModels.MainViewModel();
        }
    }
}
