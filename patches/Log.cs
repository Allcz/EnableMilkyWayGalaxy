using System;
using System.IO;
using UnityEngine;

namespace EnableMilkyWayGalaxy.patches
{
    public class Log
    {
        private const string LogDirectory = "Allz-EnableMilkyWayGalaxy";
        private static string _logPath = "";

        public static void SaveToFile(string content)
        {
            string path = GetLogPath();
            SaveToFile(path, content);
        }

        public static void SaveToFile(string path, string content)
        {
            StreamWriter sw = new StreamWriter(path, true);
            content = System.DateTime.Now + ": " + content;
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }

        /*
         * 获取历史 Log 数据
         */
        public static string GetHistoryLogs()
        {
            try
            {
                var logPath = GetLogPath();
                var sr = new StreamReader(logPath);
                var readToEnd = sr.ReadToEnd();
                sr.Close();
                return readToEnd;
            }
            catch
            {
                return "";
            }
        }
        
        
        public static string GetLogPath()
        {
            if (!string.IsNullOrEmpty(_logPath))
                return _logPath;
            //获取程序的基目录
            string pluginPath = BepInEx.Paths.PluginPath;
            string path =pluginPath  + "\\" + LogDirectory;

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    path = pluginPath;
                }
            }
            

            _logPath = Path.GetFullPath(path + "\\Log.log");
            return _logPath;
        }
    }
}