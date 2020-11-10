using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ThMEPInstall
{
    class Program
    {
        static void Main(string[] args)
        {
            ImportRegFile("ThMEPPlugin.2012.reg");
            ImportRegFile("ThMEPPlugin.2014.reg");
            ImportRegFile("ThMEPPlugin.2016.reg");
            ImportRegFile("ThMEPPlugin.2018.reg");
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
