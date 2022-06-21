﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Linq;

namespace GbxMapBrowser
{
    class FileOperations
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public uint fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public int nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        private const int SW_SHOW = 5;
        private const uint SEE_MASK_INVOKEIDLIST = 12;


        public static bool ShowFileProperties(string Filename)
        {
            SHELLEXECUTEINFO info = new SHELLEXECUTEINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpVerb = "properties";
            info.lpFile = Filename;
            info.nShow = SW_SHOW;
            info.fMask = SEE_MASK_INVOKEIDLIST;
            return ShellExecuteEx(ref info);
        }

        public static void CopyFilesToFolder(string[] filesLocation, string folderToCopyFiles, string[] newfileNames)
        {
            int i = 0;
            foreach (var path in filesLocation)
            {
                try
                {
                    string newName;
                    if (newfileNames == null)
                        newName = GetShortNameFromFilePath(path);
                    else
                        newName = newfileNames[i];

                    if (Directory.Exists(path))
                    {
                        CopyDirectory(path, folderToCopyFiles + "\\" + newName, true);
                        continue;
                    }

                    FileInfo fileInfo = new FileInfo(path);
                    fileInfo.CopyTo(folderToCopyFiles + "\\" + newName, true);
                                  
                }
                catch (Exception e)
                {
                    if (e is IOException) { }
                    else throw e;
                }
                i++;
            }
        }

        static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
           var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static void CopyFilesToFolder(string[] filesLocation, string folderToCopyFiles)
        {
            CopyFilesToFolder(filesLocation, folderToCopyFiles, null);
        }

        public static void DeleteFile(string path)
        {
            new FileInfo(path).Delete();
        }

        public static void RenameFile(string oldpath)
        {
            string shortFileName = GetShortNameFromFilePath(oldpath);
            string curFolder = GetFolderFromFilePath(oldpath);

            RenameWindow renameDialog = new RenameWindow(shortFileName, true);           
            renameDialog.ShowDialog();

            if (string.IsNullOrEmpty(renameDialog.newName)) return;
            var mapInfoPaths = new string[] { oldpath }; 
            var newMapInfoPaths = new string[] { renameDialog.newName };
            CopyFilesToFolder(mapInfoPaths, curFolder, newMapInfoPaths);
            DeleteFile(oldpath);
        }

        public static void RenameFolder(string oldpath)
        {
            string shortFolderName = GetShortNameFromFilePath(oldpath);
            string curFolder = GetFolderFromFilePath(oldpath);

            RenameWindow renameDialog = new RenameWindow(shortFolderName, false);
            renameDialog.ShowDialog();
            
            if (string.IsNullOrEmpty(renameDialog.newName)) return;
            var folderInfoPaths = new string[] { oldpath };
            var newFolderInfoPaths = new string[] { renameDialog.newName };
            CopyFilesToFolder(folderInfoPaths, curFolder, newFolderInfoPaths);
            Directory.Delete(oldpath);
        }

        private static string GetShortNameFromFilePath(string path)
        {
            while (path.EndsWith("\\")) 
                path = path.Remove(path.Length - 1);

            return path.Split("\\").ToList().Last();
        }

        public static string GetFolderFromFilePath(string path)
        { 
            while (path.EndsWith("\\")) 
                path = path.Remove(path.Length - 1);

            var shortFileName = GetShortNameFromFilePath(path);
            string newPath = path.Replace(shortFileName, "");
            return newPath;
        }

        public static string SizeToString(double size)
        {
            if (size == 0) return "";

            string[] sizeStrings = { "B", "KB", "MB", "GB", "TB" };
            int sizeStringsIndex = 0;
            double readableSize = size;

            while (readableSize >= 1024 && sizeStringsIndex < sizeStrings.Length-1 )
            {
                readableSize /= 1024;
                sizeStringsIndex += 1;
            }
            return readableSize.ToString("0.##") + " " + sizeStrings[sizeStringsIndex];
        }
    }
}
