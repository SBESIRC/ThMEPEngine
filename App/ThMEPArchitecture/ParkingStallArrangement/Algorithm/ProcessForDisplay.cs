using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public static class ProcessForDisplay
    {
        public static Process CreateSubProcess()
        {
            var proc = new Process();
            var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            proc.StartInfo.FileName = System.IO.Path.Combine(currentDllPath, "ThParkingStallProgramDisplay.exe");  
            proc.StartInfo.CreateNoWindow = false;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(currentDllPath);
            return proc;
        }
    }
}
