using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.IO
{
    public static class GetPath
    {
        private static string _Path = "THCWBZ";
        private static string _DeBugPath = "THCWBZ/DeBug";
        public static string GetTempPath()
        {
            var path = Path.Combine(Path.GetTempPath(), _Path);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
        public static string GetAppDataPath()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _Path);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        public static string GetDebugPath()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _DeBugPath);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }
    }
}
