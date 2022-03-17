using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace MainProcess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch _stopwatch = new Stopwatch();
            _stopwatch.Start();
            var ProcList = new List<Process>();
            for (int i = 0; i < 4; i++)
            {
                var proc = RunSubProcess();
                ProcList.Add(proc);
            }
            while(!ProcList.All(p=>p.HasExited))
            {
               Thread.Sleep(1000);
            }
            Console.Write(_stopwatch.Elapsed.TotalSeconds);
            Console.ReadKey();
        }

        static Process RunSubProcess()
        {
            var proc = new Process();
            proc.StartInfo.FileName = @"D:\DATA\Git2\App\mt2\bin\Debug\ThParkingStall.Core.exe";
            proc.StartInfo.Arguments = "";
            proc.Start();
            //proc.WaitForExit();
            //return proc.ExitCode;
            return proc;
        }
    }
}
