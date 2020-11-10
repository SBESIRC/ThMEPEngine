using System;
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
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        static void ImportRegFile(string regfile)
        {
            using (var process = new Process())
            {
                try
                {
                    process.StartInfo.FileName = "reg.exe";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    string command = "import " + regfile;
                    process.StartInfo.Arguments = command;
                    process.Start();
                    process.WaitForExit();
                }
                catch (System.Exception)
                {
                    //
                }
            }
        }
    }
}
