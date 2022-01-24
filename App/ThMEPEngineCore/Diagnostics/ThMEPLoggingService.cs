using System.IO;

namespace ThMEPEngineCore.Diagnostics
{
    public static class ThMEPLoggingService
    {
        public static void WriteToFile(string path, string contents)
        {
            try
            {
                File.WriteAllText(path, contents);
            }
            catch
            {
                // 忽略IO异常
            }
        }
    }
}
