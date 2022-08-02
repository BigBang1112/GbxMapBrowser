﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace GbxMapBrowser
{
    /// <summary>
    /// Interaction logic for MapPreviewPage.xaml
    /// </summary>
    public partial class MapPreviewPage : Page
    {
        List<FolderAndFileInfo> Data;

        public MapPreviewPage(List<FolderAndFileInfo> data)
        {       
            InitializeComponent();
            Data = data;
            Opacity = 0;
            if(data.Count == 0)
            {
                HideMapPreviewUI();
                return;
            }
            if (data.Count > 1)
            {
                mapNameLabel.Content = "Selected " + data.Count + " items";
                descriptionTextBlock.Text = "Number of maps: " + data.FindAll(x => x is MapInfo).Count.ToString();
                HideMapPreviewUI();
                return;
            }

        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Data.Count != 1)
                return;
            var item = Data[0];

            if (item is null)
                return;

            if (item is MapInfo map)
            {
                var fullMap = await Task.Run(() => new MapInfo(map.FullPath, false));
                if (!fullMap.IsWorking) HideMapPreviewUI();
                DataContext = fullMap;
            }
            else if (item is FolderInfo folder)
            {
                DataContext = folder;
                await Task.Run(
                    () => Dispatcher.BeginInvoke(
                        () => PreviewFolder(folder)
                    )
                );
            }
            FadeInAnimation();
        }

        void PreviewFolder(FolderInfo folderInfo)
        {
            HideMapPreviewUI();
            if (folderInfo == null) return;
            mapImage.Source = new BitmapImage(folderInfo.ImageSmall);
            mapNameLabel.Content = folderInfo.DisplayName;
            descriptionTextBlock.Text = 
                "Contains: " + Environment.NewLine
                + "Files: " + folderInfo.FilesInsideCount + Environment.NewLine
                + "Maps: " + folderInfo.MapsInsideCount;
            
        }

        void FadeInAnimation()
        {
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.3));
            BeginAnimation(OpacityProperty, animation);
        }

        void HideMapPreviewUI()
        {
            mapInfoExpander.Visibility = Visibility.Collapsed;
            medalsViewBox.Visibility = Visibility.Collapsed;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Data = null;
            GC.Collect();
        }
    }
}
