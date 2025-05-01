﻿using System;
using System.IO;

namespace WoLightning.Util
{
    public class TextLog
    {
        public string FilePath { get; set; }
        private bool isFileAvailable = false;
        private Plugin Plugin { get; init; }

        public TextLog(Plugin plugin, string confPath)
        {
            Plugin = plugin;
            FilePath = confPath + "log.txt";
            setupFile();

        }

        public bool validateFile()
        {
            isFileAvailable = File.Exists(FilePath);
            return isFileAvailable;
        }

        public void setupFile()
        {
            validateFile();
            if (!isFileAvailable)
            {
                File.Create(FilePath).Close();
                if (File.Exists(FilePath)) isFileAvailable = true;
                else throw new Exception("Cannot create Log.txt");
            }
            else
            {
                try
                {
                    long length = new FileInfo(FilePath).Length;

                    Plugin.PluginLog.Verbose("Log Size: " + length);
                    if (length > 9600) //fixme: maybe dont check for length? we have a thing called filesize, you buffon
                    {
                        File.Delete(FilePath);
                        File.Create(FilePath).Close();
                    }
                }
                catch { }
            }
            try
            {
                File.AppendAllText(FilePath, $"\n\n======================\nNew Session Started\nVersion {Plugin.currentVersion}");
                File.AppendAllText(FilePath, "\n" + DateTime.Now.ToShortDateString() + "  " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second);
            }
            catch { }
        }

        public async void Log(string message)
        {
            if (!validateFile()) return;
            DateTime now = DateTime.Now;
            try
            {
                await File.AppendAllTextAsync(FilePath, "\n" + $"[{now.Hour}:{now.Minute}:{now.Second}] " + message);
            }
            catch { }
        }

        public async void Log(object obj)
        {
            if (!validateFile()) return;

            DateTime now = DateTime.Now;
            try
            {
                await File.AppendAllTextAsync(FilePath, "\n" + $"[{now.Hour}:{now.Minute}:{now.Second}] " + obj.GetType().Name);
                foreach (var prop in obj.GetType().GetProperties())
                {
                    await File.AppendAllTextAsync(FilePath, "\n - " + prop.Name + ": " + prop.GetValue(obj, null));
                }
            }
            catch { }
        }

    }
}
