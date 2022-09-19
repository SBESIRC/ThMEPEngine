using ProtoBuf;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.IO;
using Google.Protobuf;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Services;
using ThMEPTCH.Model.SurrogateModel;
using System.Security.Principal;

namespace ThPlatform3D
{
    public class ThPlatform3DIPCCmds
    {
        [CommandMethod("TIANHUACAD", "THCAD2P3DPUSH", CommandFlags.Modal)]
        public void THCAD2P3DPUSH()
        {
            Active.Editor.WriteLine($"开始读入图纸。");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var service = new ThDWGToIFCService("");
            var project = service.DWGToProjectData(true, true);
            if (project != null)
            {
                sw.Stop();
                Active.Editor.WriteLine($"读入并解析图纸完成，耗时{sw.ElapsedMilliseconds}毫秒。");

                sw.Reset();
                Active.Editor.WriteLine($"开始传输数据。");
                sw.Start();

                using (var pipeClient = new NamedPipeClientStream(".",
                    "THCAD2P3DPIPE",
                    PipeDirection.Out,
                    PipeOptions.None,
                    TokenImpersonationLevel.Impersonation))
                {
                    try
                    {
                        pipeClient.Connect(5000);
                        project.WriteTo(pipeClient);
                        Active.Editor.WriteLine($"传输数据完成，耗时{sw.ElapsedMilliseconds}毫秒。");
                    }
                    catch (System.Exception ex)
                    {
                        Active.Editor.WriteLine($"传输数据失败：{ex.Message}。");
                    }
                }
            }
            sw.Stop();
        }

        [CommandMethod("TIANHUACAD", "THCAD2SUPUSH", CommandFlags.Modal)]
        public void THCAD2SUPUSH()
        {
            var service = new ThDWGToIFCService("");
            var project = service.DWGToProjectData(true, true);
            if (project == null)
            {
                return;
            }
            try
            {
                CancellationTokenSource CTS = new CancellationTokenSource();
                CancellationToken Token = CTS.Token;
                //这里有一个小坑，只有设置了PipeOptions.Asynchronous，管道才会接受取消令牌的取消请求，不然不会生效
                var pipeServer = new NamedPipeServerStream("THCAD2SUPIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                Task task = new Task(() =>
                {
                    try
                    {
                        pipeServer.WaitForConnection();
                        //project.WriteTo(pipeServer);
                        var bytes = project.ToByteArray();
                        var newBytes = new byte[bytes.Length + 10];
                        newBytes[0] = 1;
                        bytes.CopyTo(newBytes, 10);
                        pipeServer.Write(newBytes, 0, newBytes.Length);

                        pipeServer.Close();
                        pipeServer.Dispose();
                    }
                    catch (System.Exception ex)
                    {
                        //线程被外部取消，说明等待连接超时
                        pipeServer.Dispose();
                    }
                }, Token);
                task.Start();
                task.Wait(10000);
                if (task.Status == TaskStatus.Running)
                {
                    CTS.Cancel();
                    pipeServer.Close();
                    pipeServer.Dispose();
                    Active.Database.GetEditor().WriteMessage("未连接到SU ！\r\n");
                }
                else if (task.Status == TaskStatus.RanToCompletion)
                {
                    Active.Database.GetEditor().WriteMessage("已成功发送数据至SU！\r\n");
                }
                else
                {
                    Active.Database.GetEditor().WriteMessage("其他异常 ！\r\n");
                }
            }
            catch (IOException ioEx)
            {
                Active.Database.GetEditor().WriteMessage("Server 异常\r\n");
            }
        }
    }
}
