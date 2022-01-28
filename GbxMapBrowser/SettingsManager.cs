﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace GbxMapBrowser
{
    internal static class SettingsManager
    {
        public enum SettingState
        {
            Unknown = 0,
            NotLoaded = 1,
            Loaded = 2,
            Saved = 3,
            Error = 4
        }

        static string settingsFilePath = Directory.GetCurrentDirectory() + "\\config\\settings.dat";
        static string customGamesSettingsFilePath = Directory.GetCurrentDirectory() + "\\config\\customgames.dat";
        static string settingsFolderPath = Directory.GetCurrentDirectory() + "\\config";

        public static SettingState SettingsState = SettingsManager.SettingState.NotLoaded;

        #region Stock games settings
        private static void LoadSettingsFromFile(GbxGameController controller)
        {
            if (!File.Exists(settingsFilePath))
            {
                Properties.Settings.Default.Reset();
                Properties.Settings.Default.IsFirstRun = true;
                SettingsState = SettingState.NotLoaded;
                return;
            }

            var settingsText = File.ReadAllLines(settingsFilePath);

            int emptyGamesCount = 0;
            foreach (var line in settingsText)
            {
                foreach (GbxGame game in controller.GbxGames)
                {
                    if (line.Contains(game.Name)) //game is found
                    {
                        string newline = line.Replace(game.Name + ": ", "");
                        string[] props = newline.Split('|');

                        if (props.Length == 0) // this part of code is used for old type of setting file
                        {
                            game.MapsFolder = line;
                            continue;
                        }

                        string mapPath = props[0];
                        string exePath = props[1];

                        if (string.IsNullOrWhiteSpace(mapPath) || string.IsNullOrWhiteSpace(exePath))
                        {
                            emptyGamesCount++;
                            game.IsVisibleInGameList = false;
                            game.IsVisibleInGameLaunchMenu = false;
                            continue;
                        }

                        game.MapsFolder = mapPath;
                        game.ExeLocation = exePath;

                        if (props.Length >= 3)
                        {
                            string enabledString = props[2];
                            if (enabledString == "E")
                                game.IsVisibleInGameLaunchMenu = true;
                            else
                                game.IsVisibleInGameLaunchMenu = false;
                        }

                        if (props.Length >= 4)
                        {
                            string visibleString = props[3];
                            if (visibleString == "V")
                                game.IsVisibleInGameList = true;
                            else
                                game.IsVisibleInGameList = false;
                        }

                    }
                }
            }

            if (emptyGamesCount == controller.GbxGames.Count)
                Properties.Settings.Default.IsFirstRun = true;


        }

        private static void SaveSettings(GbxGameController controller)
        {
            if (!Directory.Exists(settingsFolderPath))
                Directory.CreateDirectory(settingsFolderPath);

            List<string> settingsText = new List<string>();
            foreach (GbxGame game in controller.GbxGames)
            {
                if (game is CustomGbxGame)
                    continue;
                string enabledString = "N";
                if (game.IsVisibleInGameLaunchMenu)
                    enabledString = "E";
                string visibleInGameListString = "N";
                if (game.IsVisibleInGameList)
                    visibleInGameListString = "V";

                settingsText.Add(game.Name + ": " + game.MapsFolder + "|" + game.ExeLocation + "|" + enabledString + "|" + visibleInGameListString);
            }
            File.WriteAllLinesAsync(settingsFilePath, settingsText);
            SettingsState = SettingState.Saved;
        }
        #endregion

        #region Custom games settings
        private static void LoadCustomGamesSettingsFromFile(GbxGameController controller)
        {
            if (!File.Exists(customGamesSettingsFilePath)) //no custom games, no big deal
                return;

            var settingsText = File.ReadAllLines(customGamesSettingsFilePath);
            foreach (var line in settingsText)
            {
                string[] props = line.Split('|');

                if (props.Length == 0)
                    continue;

                try
                {
                    var game = new CustomGbxGame();
                    game.Name = props[0];
                    game.MapsFolder = props[1];
                    game.ExeLocation = props[2];

                    string enabledString = props[3];
                    if (enabledString == "E")
                        game.IsVisibleInGameLaunchMenu = true;
                    else
                        game.IsVisibleInGameLaunchMenu = false;

                    string visibleString = props[4];
                    if (visibleString == "V")
                        game.IsVisibleInGameList = true;
                    else
                        game.IsVisibleInGameList = false;

                    controller.GbxGames.Add(game);
                    //game.Icon = props[5]; //to do
                    game.Icon = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Data\\GameIcons\\Custom.png"));
         
                }
                catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        private static void SaveCustomGamesSettings(GbxGameController controller)
        {
            if (!Directory.Exists(settingsFolderPath))
                Directory.CreateDirectory(settingsFolderPath);

            List<string> settingsText = new List<string>();
            foreach (GbxGame game in controller.GbxGames)
            {
                if (game is not CustomGbxGame)
                    continue;
                string enabledString = "N";
                if (game.IsVisibleInGameLaunchMenu)
                    enabledString = "E";
                string visibleInGameListString = "N";
                if (game.IsVisibleInGameList)
                    visibleInGameListString = "V";

                settingsText.Add(game.Name + "|" + game.MapsFolder + "|" + game.ExeLocation + "|" + enabledString + "|" + visibleInGameListString + "|" + game.Icon.ToString());
            }
            File.WriteAllLinesAsync(customGamesSettingsFilePath, settingsText);
        }
    #endregion

        public static void SaveAllSettings(GbxGameController controller)
        {
            SaveSettings(controller);
            SaveCustomGamesSettings(controller);
        }

        public static void LoadAllSettingsFromFile(GbxGameController controller)
        {
            LoadSettingsFromFile(controller);
            LoadCustomGamesSettingsFromFile(controller);
        }

    }
}
