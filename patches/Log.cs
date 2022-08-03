using System.IO;

namespace EnableMilkyWayGalaxy.patches
{
    public class Log
    {
        public static string MILKY_WAY_LOG = "MilkyWayLog";
        public static string DEBUG_LOG = "Debug";
        private const string LogDirectory = "Allz-EnableMilkyWayGalaxy";
        private string _logPath = "";
        private string _logFileName;

        public Log()
        {
        }

        public Log(string logFileName)
        {
            _logFileName = logFileName;
        }

        public void LOG(string content)
        {
            StreamWriter sw = new StreamWriter(GetLogPath(), true);
            content = System.DateTime.Now + ": " + content;
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }

        private string GetLogPath()
        {
            if (!string.IsNullOrEmpty(_logPath))
                return _logPath;
            //获取程序的基目录
            string pluginPath = BepInEx.Paths.PluginPath;
            string path = pluginPath + "\\" + LogDirectory;
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

            _logFileName = string.IsNullOrEmpty(_logFileName) ? DEBUG_LOG : _logFileName;
            _logPath = Path.GetFullPath(path + "\\" + _logFileName + ".log");
            return _logPath;
        }
    }
}