﻿using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.MwFoundations;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TmEssentials;

namespace GbxMapBrowser
{
    public class MapInfo : FolderAndFileInfo
    {
        private readonly string _shortName;
        public string Author { get; }
        public string CopperPrice { get; }
        public string MapType { get; }
        public string Titlepack { get; }
        public Uri MoodIcon { get; }
        public string Description { get; }
        public string ObjectiveBronze { get; }
        public string ObjectiveSilver { get; }
        public string ObjectiveGold { get; }
        public string ObjectiveAuthor { get; }
        public ImageSource MapThumbnail { get; }
        public bool IsWorking { get; }

        public MapInfo(string fullnamepath, bool basicInfoOnly)
        {
            CMwNod gbx;
            _shortName = fullnamepath.Split("\\").Last();
            FullPath = fullnamepath;
            FileInfo mapfileInfo = new FileInfo(fullnamepath);
            DateModified = mapfileInfo.LastWriteTime;
            DateCreated = mapfileInfo.CreationTime;
            Size = mapfileInfo.Length;

            try
            {
                if (basicInfoOnly)
                    gbx = Gbx.ParseHeaderNode(fullnamepath);
                else
                    gbx = Gbx.ParseNode(fullnamepath);
                IsWorking = true;
            }
            catch (Exception e)
            {
                DisplayName = "ERROR (" + _shortName + ")";
                ImageSmall = new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\Error.png");
                Debug.WriteLine("Error: Map '" + fullnamepath + "' - impossible to load" + Environment.NewLine + e.Message);
                MapThumbnail = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\Error.png"));
                MapThumbnail.Freeze();
                IsWorking = false;
                return;
            }

            if (gbx is CGameCtnChallenge gbxMap)
            {
                CGameCtnChallenge challenge = gbxMap;

                DisplayName = ToReadableText(challenge.MapName);
                OriginalName = challenge.MapName;
                Titlepack = challenge.TitleId;
                if (string.IsNullOrEmpty(OriginalName))
                {
                    DisplayName = "ERROR - Empty map (" + _shortName + ")";
                    ImageSmall = new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\Error.png");
                    return;
                }
                Uri enviImagePath = EnviManager.GetEnvironmentImagePath(challenge.Collection, Titlepack);
                if (File.Exists(enviImagePath.AbsolutePath))
                    ImageSmall = enviImagePath;
                else
                    ImageSmall = new Uri(Environment.CurrentDirectory + "\\Data\\Environments\\Unknown.png");

                ObjectiveGold = TimeSpanToString(challenge.GoldTime);
                if (basicInfoOnly) return;
                ObjectiveBronze = TimeSpanToString(challenge.BronzeTime);
                ObjectiveSilver = TimeSpanToString(challenge.SilverTime);
                ObjectiveAuthor = TimeSpanToString(challenge.AuthorTime);

                if (!string.IsNullOrEmpty(challenge.Comments))
                    Description = ToReadableText(challenge.Comments);
                MoodIcon = MoodManager.GetMoodImagePath(challenge.Decoration.ToString());

                if (string.IsNullOrEmpty(challenge.AuthorNickname))
                    Author = challenge.AuthorLogin;
                else
                    Author = ToReadableText(challenge.AuthorNickname);

                CopperPrice = challenge.Cost.ToString();

                if (string.IsNullOrEmpty(challenge.ChallengeParameters.MapType))
                {
                    MapType = challenge.Mode.ToString();
                }
                else
                    MapType = challenge.ChallengeParameters.MapType;

                if (challenge.Thumbnail == null)
                {
                    MapThumbnail = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\NoThumbnail.png"));
                    MapThumbnail.Freeze();
                    return;
                }
                var thumbnailMemoryStream = new MemoryStream(challenge.Thumbnail);

                if (thumbnailMemoryStream == null) throw new Exception("buffer is empty");

                Bitmap mapThumbnail = new Bitmap(new StreamReader(thumbnailMemoryStream).BaseStream);
                mapThumbnail.RotateFlip(RotateFlipType.Rotate180FlipX);
                MapThumbnail = ConvertToImageSource(mapThumbnail);
                MapThumbnail.Freeze();

            }
            else if (gbx is CGameCtnReplayRecord gbxReplay)
            {
                CGameCtnReplayRecord replay = gbxReplay;
                OriginalName = _shortName;
                ImageSmall = new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\Replay.png");
                DisplayName = _shortName.Replace(".Replay.Gbx", "", StringComparison.OrdinalIgnoreCase);
                ObjectiveGold = TimeSpanToString(replay.Time);
                if (basicInfoOnly) return;
                MapThumbnail = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Data\\UIIcons\\Replay.png"));
                MapThumbnail.Freeze();
                Author = ToReadableText(replay.AuthorNickname);
                Titlepack = replay.TitleId;
            }
        }

        string TimeSpanToString(TimeSpan? timeSpan)
        {
            if (!timeSpan.HasValue) return "-:--.---";
            TimeSpan time = timeSpan.GetValueOrDefault();
            return time.ToTmString();
        }

        public void RenameAndSave(string newName)
        {
            Gbx gbx;

            try
            {
                gbx = Gbx.Parse(FullPath);
            }
            catch (Exception e)
            {
                throw e;
            }

            if (gbx is Gbx<CGameCtnChallenge> gbxMap)
            {
                CGameCtnChallenge challenge = gbxMap.Node;
                challenge.MapName = newName;
                gbxMap.Save(FullPath);
            }
            else
                throw new NotImplementedException("Only Maps could be renamed.");

        }

        BitmapImage ConvertToImageSource(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        string ToReadableText(string defaultname)
        {
            if (defaultname is null)
                return null;
            string formattedName = defaultname;
            formattedName = TmEssentials.TextFormatter.Deformat(formattedName);
            return formattedName;
        }


        public void OpenMap(GbxGame selGame)
        {
            if (selGame is CustomGbxGame cgg)
                if (cgg.IsUnlimiter)
                {
                    Task.Run(() => OpenMapUnlimiter(selGame));
                    return;
                }


            ProcessStartInfo gameGbxStartInfo = new ProcessStartInfo(selGame.ExeLocation, "/useexedir /singleinst /file=\"" + FullPath + "\"");
            Process gameGbx = new Process();
            gameGbx.StartInfo = gameGbxStartInfo;
            gameGbx.Start();
        }

        async Task OpenMapUnlimiter(GbxGame selGame)
        {
            string exeName = "TmForever.exe";
            bool isRunning = ProcessManager.IsRunning(exeName);

            if (!isRunning)
            {
                //start the unlimiter first
                await Task.Run(() => selGame.Launch());
                string unlimiterExeName = selGame.ExeLocation.Replace(selGame.InstalationFolder + "\\", "");
                while (ProcessManager.IsRunning(unlimiterExeName) == true)
                {
                    await Task.Delay(50);
                }
            }
            else //show msg about running game
                Console.WriteLine("An instance of TMUF is running already");

            ProcessStartInfo gameGbxStartInfo = new ProcessStartInfo((selGame.InstalationFolder + "\\" + exeName), "/useexedir /singleinst /file=\"" + FullPath + "\"");
            Process gameGbx = new Process();
            gameGbxStartInfo.WorkingDirectory = selGame.InstalationFolder; //to avoid exe not found message
            gameGbx.StartInfo = gameGbxStartInfo;
            gameGbx.Start();
        }


        internal async Task ExportThumbnail(string filePath)
        {
            var dirPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            var img = MapThumbnail as BitmapImage;
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
                await Task.CompletedTask;
            }
        }
    }
}
