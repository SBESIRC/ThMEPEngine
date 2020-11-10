using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace ThMEPInstall
{
    class Program
    {
        static void Main(string[] args)
        {
            ImportRegFile(Path.Combine(ExecutablePath(), "ThMEPPlugin.2012.reg"));
            ImportRegFile(Path.Combine(ExecutablePath(), "ThMEPPlugin.2014.reg"));
            ImportRegFile(Path.Combine(ExecutablePath(), "ThMEPPlugin.2016.reg"));
            ImportRegFile(Path.Combine(ExecutablePath(), "ThMEPPlugin.2018.reg"));
        }

        static string ExecutablePath()
        {
            var fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            return fi.Directory.FullName;
        }

        static void ImportRegFile(string regfile)
        {
            Process proc = new Process();
            try
            {
                proc.StartInfo.FileName = "reg.exe";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = false;

                string command = "import " + regfile;
                proc.StartInfo.Arguments = command;
                proc.Start();

                proc.WaitForExit();
            }
            catch (System.Exception)
            {
                proc.Dispose();
            }
        }
    }
}
