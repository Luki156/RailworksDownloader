﻿using ModernWpf.Controls;
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
using RailworksDownloader.Properties;
using Microsoft.Win32;
using System.IO;

namespace RailworksDownloader
{
    /// <summary>
    /// Interakční logika pro RailworksPathDialog.xaml
    /// </summary>
    public partial class RailworksPathDialog : ContentDialog
    {
        public RailworksPathDialog()
        {
            InitializeComponent();

            UserPath.Text = Settings.Default.RailworksLocation;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (UserPath.Text.Length > 3)
            {
                Settings.Default.RailworksLocation = UserPath.Text;
                Settings.Default.Save();
            }
            else
                args.Cancel = true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "RailWorks|RailWorks.exe";
            ofd.FileName = "RailWorks.exe";
            ofd.Title = "Select RailWorks installation path";

            if (ofd.ShowDialog() == true)
            {
                UserPath.Text = Path.GetDirectoryName(ofd.FileName);
            }
        }
    }
}
