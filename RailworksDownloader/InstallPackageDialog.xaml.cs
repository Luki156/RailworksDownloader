﻿using ModernWpf.Controls;
using System.Windows;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace RailworksDownloader
{
    /// <summary>
    /// Interakční logika pro RailworksPathDialog.xaml
    /// </summary>
    public partial class InstallPackageDialog : ContentDialog
    {
        public InstallPackageDialog()
        {
            InitializeComponent();
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            (sender as ListViewItem).IsSelected = true;
            (sender as ListViewItem).UpdateLayout();
        }

        private void ListViewItem_Unselected(object sender, RoutedEventArgs e)
        {
            (sender as ListViewItem).IsSelected = false;
        }
    }
}
