﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using RailworksDownloader.Properties;
using SWC = System.Windows.Controls;

namespace RailworksDownloader
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Uri ApiUrl = new Uri("https://dls.rw.jachyhm.cz/api/");

        internal static Brush Blue = new SolidColorBrush(Color.FromArgb(255, 0, 151, 230));
        internal static Brush Green = new SolidColorBrush(Color.FromArgb(255, 76, 209, 55));
        internal static Brush Yellow = new SolidColorBrush(Color.FromArgb(255, 251, 197, 49));
        internal static Brush Red = new SolidColorBrush(Color.FromArgb(255, 232, 65, 24));
        internal static Brush Purple = new SolidColorBrush(Color.FromArgb(255, 190, 46, 221));

        private bool Saving = false;
        private bool CheckingDLC = false;

        Railworks RW;

        PackageManager PM;

        bool crawlingComplete = false;
        
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                App.Window = this;

                App.SteamManager = new SteamManager();

                Closing += MainWindowDialog_Closing;

                string savedRWPath = Settings.Default.RailworksLocation;
                App.Railworks = new Railworks(string.IsNullOrWhiteSpace(App.SteamManager.RWPath) ? savedRWPath : App.SteamManager.RWPath);
                App.Railworks.ProgressUpdated += RW_ProgressUpdated;
                App.Railworks.RouteSaving += RW_RouteSaving;
                App.Railworks.CrawlingComplete += RW_CrawlingComplete;

                RW = App.Railworks;

                if (string.IsNullOrWhiteSpace(RW.RWPath))
                {
                    RailworksPathDialog rpd = new RailworksPathDialog();
                    rpd.ShowAsync();
                }

                if (string.IsNullOrWhiteSpace(Settings.Default.RailworksLocation) && !string.IsNullOrWhiteSpace(RW.RWPath))
                {
                    Settings.Default.RailworksLocation = RW.RWPath;
                    Settings.Default.Save();
                }

                PathChanged();

                Settings.Default.PropertyChanged += PropertyChanged;
            } 
            catch (Exception e)
            {
                Desharp.Debug.Log(e, Desharp.Level.DEBUG);
            }

            Task.Run(async () =>
            {
                try
                {
                    RW_CheckingDLC(false);
                    List<SteamManager.DLC> dlcList = App.SteamManager.GetInstalledDLCFiles();
                    await WebWrapper.ReportDLC(dlcList, ApiUrl);
                    RW_CheckingDLC(true);
                }
                catch (Exception e)
                {
                    Desharp.Debug.Log(e, Desharp.Level.DEBUG);
                }
            });

            /*if (!string.IsNullOrWhiteSpace(RW.RWPath))
                ScanRailworks_Click(this, null);*/

            //RoutesList.Items.Add(new RouteInfo("TEST", ""));
        }

        private void MainWindowDialog_Closing(object sender, CancelEventArgs e)
        {
            if (Saving || CheckingDLC)
            {
                MessageBoxResult result = MessageBox.Show("Some operation is still running.\nDo you really want to close this app?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private async void RW_CrawlingComplete()
        {
            TotalProgress.Dispatcher.Invoke(() => TotalProgress.IsIndeterminate = true);
            await RW.GetMissing();
            await PM.GetDownloadableDependencies();

            foreach (RouteInfo route in RW.Routes)
            {
                route.Crawler.ParseRouteMissingAssets(RW.MissingDependencies);
                route.Crawler.ParseRouteDownloadableAssets(PM.DownloadableDependencies);

                route.MissingCount = route.Crawler.MissingDependencies.Count;
                route.DownloadableCount = route.Crawler.DownloadableDependencies.Count;

                route.MissingScenariosCount = route.Crawler.MissingScenarioDeps.Count;
                route.DownloadableScenarioCount = route.Crawler.DownloadableScenarioDeps.Count;
            }

            TotalProgress.Dispatcher.Invoke(() => TotalProgress.IsIndeterminate = false);
            ScanRailworks.Dispatcher.Invoke(() => ScanRailworks.IsEnabled = true);
            crawlingComplete = true;
        }

        private void ToggleSavingGrid(string type)
        {
            SavingGrid.Dispatcher.Invoke(() => {
                SavingLabel.Content = type;
                SavingGrid.Visibility = (Saving || CheckingDLC) ? Visibility.Visible : Visibility.Hidden;
            });
        }

        private void RW_CheckingDLC(bool @checked)
        {
            CheckingDLC = !@checked;
            ToggleSavingGrid("Checking installed DLCs");
        }

        private void RW_RouteSaving(bool saved)
        {
            Saving = !saved;
            ToggleSavingGrid("Saving");
        }

        private void RW_ProgressUpdated(int percent)
        {
            TotalProgress.Dispatcher.Invoke(() => { TotalProgress.Value = percent; });
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "RailworksLocation")
            {
                RW.RWPath = Settings.Default.RailworksLocation;
                PathChanged();
            }
        }

        private void PathChanged()
        {
            PathSelected.IsChecked = DownloadMissing.IsEnabled = ScanRailworks.IsEnabled = !string.IsNullOrWhiteSpace(RW.RWPath);

            if (RW.RWPath != null && System.IO.Directory.Exists(RW.RWPath))
            {
                App.PackageManager = new PackageManager(RW.RWPath, ApiUrl);
                PM = App.PackageManager;

                LoadRoutes();
            }
        }

        private void LoadRoutes()
        {
            if (string.IsNullOrWhiteSpace(RW.RWPath))
                return;

            RW.InitRoutes();

            foreach (var r in RW.Routes.OrderBy(x => x.Name))
            {
                RoutesList.Items.Add(r);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try {
                Railworks rw = new Railworks();

                Stopwatch sw = new Stopwatch();

                rw.InitCrawlers();

                //rw.ProgressUpdated += (perc) => { PB.Dispatcher.Invoke(() => { PB.Value = perc; }); };

                rw.RunAllCrawlers();

                //RouteCrawler rc = new RouteCrawler(@"D:\Hry\Steam\steamapps\common\RailWorks\Content\Routes\bd4aae03-09b5-4149-a133-297420197356", rw.RWPath);
                /*RouteCrawler rc = new RouteCrawler(Path.Combine(rw.RWPath, "Content", "Routes", "bd4aae03-09b5-4149-a133-297420197356"), rw.RWPath);


                rc.ProgressUpdated += (perc) => { PB.Value = perc; };
                rc.Complete += () => 
                { 
                    sw.Stop();
                    MessageBox.Show(sw.Elapsed.ToString());
                };

                sw.Start();
                await rc.Start();*/
            }
            catch (Exception ex)
            {
                Desharp.Debug.Log(ex, Desharp.Level.DEBUG);
            }
        }

        private void SelectRailworksLocation_Click(object sender, RoutedEventArgs e)
        {
            RailworksPathDialog rpd = new RailworksPathDialog();
            rpd.ShowAsync();
        }

        private void ScanRailworks_Click(object sender, RoutedEventArgs e)
        {
            try {
                ScanRailworks.IsEnabled = false;
                crawlingComplete = false;
                TotalProgress.Value = 0;
                RW.RunAllCrawlers();
            }
            catch (Exception ex)
            {
                Desharp.Debug.Log(ex, Desharp.Level.DEBUG);
            }
        }

        private void ListViewItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SWC.ListViewItem item = (SWC.ListViewItem)sender;

            if (item?.IsSelected == true && crawlingComplete)
            {
                DependencyWindow dw = new DependencyWindow((RouteInfo)item.Content);
                dw.ShowDialog();
            }
        }

        private void ManagePackages_Click(object sender, RoutedEventArgs e)
        {
            PackageManagerWindow pmw = new PackageManagerWindow();
            pmw.ShowDialog();
        }
    }
}
