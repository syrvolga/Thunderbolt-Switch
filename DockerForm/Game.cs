﻿using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace DockerForm
{
    public enum SettingsType
    {
        File = 0,
        Registry = 1
    }

    [Serializable]
    public class GameSettings
    {
        public int GUID;
        public SettingsType Type;
        public bool IsEnabled;
        public string Uri;
        public bool IsRelative;
        public Dictionary<string, byte[]> data = new Dictionary<string, byte[]>();

        public GameSettings(int _guid, SettingsType _type, string _uri, bool _enabled, bool _relative)
        {
            this.GUID = _guid;
            this.Type = _type;
            this.Uri = _uri;
            this.IsEnabled = _enabled;
            this.IsRelative = _relative;
        }

        public string GetUri(DockerGame thisGame)
        {
            string filename = this.Uri;
            if (this.IsRelative)
                filename = Path.Combine(thisGame.Uri, filename);

            return filename;
        }

        public bool IsFile()
        {
            return Type == SettingsType.File;
        }
    }

    public enum ErrorCode
    {
        None = 0,
        MissingFolder = 1,
        MissingExecutable = 2,
        MissingSettings = 3
    }

    [Serializable]
    public class DockerGame
    {
        public string Executable = "";      // Executable Name
        public string Name = "";            // Description
        public string ProductName = "";     // Product Name
        public string FolderName = "";      // Folder Name
        public string Version = "";         // Product Version
        public string GUID = "";            // Product GUID (harcoded)
        public string IGDB_Url = "";        // IGDB GUID
        public string Uri = "";             // File path
        public string Company = "";         // Product Company
        public bool Enabled = true;         // IsEnabled
        public DateTime LastCheck;          // Last time the game settings were saved
        public Bitmap Image = Properties.Resources.DefaultBackgroundImage;

        public Dictionary<int, GameSettings> Settings = new Dictionary<int, GameSettings>();

        [NonSerialized()] public ErrorCode ErrorCode = 0;
        public void SanityCheck()
        {
            ErrorCode = ErrorCode.None;

            if (!HasReachableFolder())
                ErrorCode = ErrorCode.MissingFolder;
            else if (!HasReachableExe())
                ErrorCode = ErrorCode.MissingExecutable;
            else if (!HasFileSettings())
                ErrorCode = ErrorCode.MissingSettings;
            
            Enabled = (ErrorCode == ErrorCode.None ? true : false);
        }

        public DockerGame(string filePath)
        {
            Dictionary<string, string> AppProperties = GetAppProperties(filePath);

            Executable = AppProperties["FileName"];
            ProductName = AppProperties.ContainsKey("FileDescription") ? AppProperties["FileDescription"] : AppProperties["ItemFolderNameDisplay"];
            Version = AppProperties.ContainsKey("FileVersion") ? AppProperties["FileVersion"] : "1.0.0.0";
            Company = AppProperties.ContainsKey("Company") ? AppProperties["Company"] : AppProperties.ContainsKey("Copyright") ? AppProperties["Copyright"] : "Unknown";
            Name = ProductName;
            GUID = "0x" + Math.Abs((Executable + ProductName).GetHashCode()).ToString();

            FileInfo fileInfo = new FileInfo(filePath);
            Uri = fileInfo.DirectoryName.ToLower();

            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            FolderName = System.Text.RegularExpressions.Regex.Replace(ProductName, invalidRegStr, "_").Replace(" ", "");

            try { Image = ShellEx.GetBitmapFromFilePath(filePath, ShellEx.IconSizeEnum.LargeIcon48); } catch (Exception) { }
        }

        private Dictionary<string, string> GetAppProperties(string filePath1)
        {
            Dictionary<string, string> AppProperties = new Dictionary<string, string>();

            var shellFile = Microsoft.WindowsAPICodePack.Shell.ShellObject.FromParsingName(filePath1);
            foreach (var property in typeof(ShellProperties.PropertySystem).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var shellProperty = property.GetValue(shellFile.Properties.System, null) as IShellProperty;
                if (shellProperty?.ValueAsObject == null) continue;
                var shellPropertyValues = shellProperty.ValueAsObject as object[];
                if (shellPropertyValues != null && shellPropertyValues.Length > 0)
                {
                    foreach (var shellPropertyValue in shellPropertyValues)
                        AppProperties.Add(property.Name, "" + shellPropertyValue);
                }
                else
                    AppProperties.Add(property.Name, "" + shellProperty.ValueAsObject);
            }

            return AppProperties;
        }

        public bool CanSerialize()
        {
            if (GUID != "" && ProductName != "" && Executable != "")
                return true;
            return false;
        }

        public void Serialize()
        {
            // Store the last time this game was updated
            LastCheck = DateTime.Now;

            string filename = Path.Combine(Form1.path_database, FolderName) + ".dat";
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, this);
                fs.Close();
            }
        }

        public bool HasReachableFolder()
        {
            return Directory.Exists(Uri);
        }

        public bool HasReachableExe()
        {
            string filename = Path.Combine(Uri, Executable);
            return File.Exists(filename);
        }

        public bool HasFileSettings()
        {
            foreach (GameSettings setting in Settings.Values)
                if (setting.IsFile())
                    return true;
            return false;
        }
    }
}
