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
        MapInfo Map;

        public MapPreviewPage(List<FolderAndFileInfo> data)
        {       
            InitializeComponent();
            Opacity = 0;
            if(data.Count == 0)
            {
                HideAllExceptHeader();
                return;
            }
            if (data.Count > 1)
            {
                mapNameLabel.Content = "Selected " + data.Count + " items";
                HideAllExceptHeader();
                return;
            }

            var item = data[0];    
            if (item is MapInfo map)
            {
                Map = map;   
            }
            else if (item is FolderInfo folder)
            {
                PreviewFolder(folder);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(new TimeSpan(50000));
            if (Map is not null) 
            {
                Map = await Task.Run(() => new MapInfo(Map.FullPath, false));
                DataContext = Map;
                if (!Map.IsWorking) HideAllExceptHeader();
            }
            FadeInAnimation();
        }

        void PreviewFolder(FolderInfo folderInfo)
        {
            HideAllExceptHeader();
            if (folderInfo == null) return;
            ImageSource src = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\Folder.png"));
            mapImage.Source = src;
            mapNameLabel.Content = folderInfo.Name;
        }

        void FadeInAnimation()
        {
            DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromSeconds(0.3));
            BeginAnimation(OpacityProperty, animation);
        }

        void HideAllExceptHeader()
        {
            infoStackPanel.Visibility = Visibility.Collapsed;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Map = null;
            GC.Collect();
        }
    }
}
