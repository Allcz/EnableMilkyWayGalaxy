using System.IO;

namespace EnableMilkyWayGalaxy.patches
{
    public class Log
    {
        private const string LogDirectory = "Allz-EnableMilkyWayGalaxy";
        private static string _logPath = "";

        public static void LOG(string content)
        {
            string path = GetLogPath();
            LOG(path, content);
        }

        private static void LOG(string path, string content)
        {
            StreamWriter sw = new StreamWriter(path, true);
            content = System.DateTime.Now + ": " + content;
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }

        private static string GetLogPath()
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