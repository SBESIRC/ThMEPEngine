using System;
using AcHelper;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Diagnostics;
using ThMEPTCH.Services;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using Autodesk.AutoCAD.EditorInput;
using System.Diagnostics;

namespace ThMEPIFC
{
    public class ThMEPIFCExportCmds
    {
        /// <summary>
        /// 天正图纸转IFC
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDWG2IFC", CommandFlags.Modal)]
        public void THDWG2IFC()
        {
            try
            {
                //选择保存路径
                var ifcFilePath = SaveFilePath("ifc");
                if (string.IsNullOrEmpty(ifcFilePath))
                {
                    return;
                }

                // 读取并解析CAD图纸数据
                ThStopWatchService.Start();
                var service = new ThDWGToIFCService(string.Empty);
                var project = service.DWGToProject(false, false, true, true);
                if (project.IsNull())
                    return;
                ThStopWatchService.Stop();
                var readDWGTimeSpan = ThStopWatchService.TimeSpan();

                // 转换并保存IFC数据
                ThStopWatchService.ReStart();
                ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
                Tgl2IfcService.GenerateIfcModelAndSave(project, ifcFilePath);
                ThStopWatchService.Stop();
                var writeIFCTimeSpan = ThStopWatchService.TimeSpan();

                // 打印时间戳
                var msg = string.Format(
                    "读取并解析CAD图纸数据时间：{0}\n转换并保存IFC数据时间：{1}\n导出IFC文件总时间：{2}",
                    readDWGTimeSpan,
                    writeIFCTimeSpan,
                    readDWGTimeSpan + writeIFCTimeSpan);
                Active.Editor.WriteMessage(msg);
            }
            catch
            {
                // 未知错误
            }
        }

        /// <summary>
        /// 天正图纸转IFC，IFC转Viewer
        /// </summary>
        [CommandMethod("TIANHUACAD", "THDWG2IFC2P3D", CommandFlags.Modal)]
        public void THDWG2IFC2P3D()
        {
            try
            {
                Active.Editor.WriteLine($"开始读入图纸。");
                Stopwatch sw = new Stopwatch();
                //选择保存路径
                var time = DateTime.Now.ToString("HHmmss");
                var fileName = "模型数据" + time + ".ifc";
                var ifcFilePath = Path.Combine(Path.GetTempPath(), fileName);
                //var ifcFilePath = SaveFilePath("ifc");
                if (string.IsNullOrEmpty(ifcFilePath))
                {
                    return;
                }
                sw.Start();
                // 读取并解析CAD图纸数据
                ThStopWatchService.Start();
                var service = new ThDWGToIFCService(string.Empty);
                var project = service.DWGToProjectData(false, false);
                if (project != null)
                {
                    sw.Stop();
                    Active.Editor.WriteLine($"读入并解析图纸完成，耗时{sw.ElapsedMilliseconds}毫秒。");
                    sw.Reset();

                    Active.Editor.WriteLine($"开始转IFC。");
                    sw.Start();
                    ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
                    Tgl2IfcService.GenerateIfcModelAndSave(project, ifcFilePath);
                    sw.Stop();
                    Active.Editor.WriteLine($"转IFC完成，耗时{sw.ElapsedMilliseconds}毫秒。");
                    sw.Reset();

                    Active.Editor.WriteLine($"开始传输数据。");
                    sw.Start();
                    using (Stream stream = new FileStream(ifcFilePath, FileMode.Open))
                    using (var pipeClient = new NamedPipeClientStream(".",
                        "THCAD2IFC2P3DPIPE",
                        PipeDirection.Out,
                        PipeOptions.None,
                        TokenImpersonationLevel.Impersonation))
                    {
                        try
                        {
                            BinaryReader r = new BinaryReader(stream);
                            var bytes = r.ReadBytes((int)stream.Length);
                            //var bytes = project.ToThBimData(ProtoBufDataType.PushType, PlatformType.CADPlatform);
                            pipeClient.Connect(5000);
                            pipeClient.Write(bytes, 0, bytes.Length);
                            sw.Stop();
                            Active.Editor.WriteLine($"传输数据完成，耗时{sw.ElapsedMilliseconds}毫秒。");
                        }
                        catch (System.Exception ex)
                        {
                            Active.Editor.WriteLine($"传输数据失败：{ex.Message}。");
                        }
                    }
                    var isFile = System.IO.File.Exists(ifcFilePath);
                    if (isFile)
                    {
                        System.IO.File.Delete(ifcFilePath);
                    }
                }
            }
            catch
            {
                // 未知错误
            }
        }

        private string SaveFilePath(string fileExt)
        {
            var time = DateTime.Now.ToString("HHmmss");
            var fileName = "模型数据" + time;
            var fileDialog = new SaveFileDialog();
            fileDialog.Title = "选择保存位置";
            fileDialog.Filter = string.Format("模型数据(*.{0})|*.{0}", fileExt);
            fileDialog.OverwritePrompt = true;
            fileDialog.DefaultExt = fileExt;
            fileDialog.FileName = fileName;
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string savePath = fileDialog.FileName;
                return savePath;
            }
            return string.Empty;
        }
    }
}
