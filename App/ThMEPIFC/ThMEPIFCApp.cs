using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using Newtonsoft.Json;
using ProtoBuf;
using ThMEPTCH.Model;
using ThMEPTCH.Model.SurrogateModel;
using ThMEPTCH.Services;

namespace ThMEPIFC
{
    public class ThMEPIFCApp : IExtensionApplication
    {
        public void Initialize()
        {
            //add code to run when the ExtApp initializes. Here are a few examples:
            //  Checking some host information like build #, a patch or a particular Arx/Dbx/Dll;
            //  Creating/Opening some files to use in the whole life of the assembly, e.g. logs;
            //  Adding some ribbon tabs, panels, and/or buttons, when necessary;
            //  Loading some dependents explicitly which are not taken care of automatically;
            //  Subscribing to some events which are important for the whole session;
            //  Etc.
        }

        public void Terminate()
        {
            //add code to clean up things when the ExtApp terminates. For example:
            //  Closing the log files;
            //  Deleting the custom ribbon tabs/panels/buttons;
            //  Unloading those dependents;
            //  Un-subscribing to those events;
            //  Etc.
        }

        [CommandMethod("TIANHUACAD", "THTGL2IFC", CommandFlags.Modal)]
        public void THTGL2IFC()
        {
            // 拾取TGL XML文件
            var tgl = OpenTGLXMLFile();
            if (string.IsNullOrEmpty(tgl))
            {
                return;
            }

            // 读入并解析TGL XML文件
            var service = new ThTGLXMLService();
            var project = service.LoadXML(tgl);
            if (project == null)
            {
                return;
            }

            // 读入DWG数据
            var dwgService = new ThTGL2IFCDWGService();
            dwgService.LoadDWG(Active.Database, project);

            // 转换并保存IFC数据
            ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
            Tgl2IfcService.GenerateIfcModelAndSave(project, Path.ChangeExtension(tgl, "ifc"));
        }
        [CommandMethod("TIANHUACAD", "THDB2IFC", CommandFlags.Modal)]
        public void THDBL2IFC()
        {
            // 拾取TGL DB文件
            var filePath = OpenDBFile();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject();
            if (project == null)
            {
                return;
            }
            var dwgDBDate = DateTime.Now;

            // 转换并保存IFC数据
            ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
            Tgl2IfcService.GenerateIfcModelAndSave(project, Path.ChangeExtension(filePath, "ifc"));
            var endDate = DateTime.Now;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string msg = string.Format(
                    "读取DB数据楼层信息，分层组合数据时间：{0},分出组合数据转换IfcModel时间：{1},总时间：{0}", 
                    dwgDBDate - startDate, 
                    endDate - dwgDBDate, 
                    endDate - startDate);
                Active.Database.GetEditor().WriteMessage(msg);
            }
        }
        [CommandMethod("TIANHUACAD", "THDB2File", CommandFlags.Modal)]
        public void THDBL2MidFile()
        {
            // 拾取TGL DB文件
            var filePath = OpenDBFile();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject();
            if (project == null)
            {
                return;
            }
            var dwgDBDate = DateTime.Now;
            // 转换并保存为渲染引擎识别的中间文件
            var Tgl2IfcService = new ThTGL2GeoFileService();
            Tgl2IfcService.GenerateXBimMeshAndSave(project, Path.ChangeExtension(filePath, "midfile"));
            var endDate = DateTime.Now;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string msg = string.Format(
                    "读取DB数据楼层信息，分层组合数据时间：{0},分出组合数据转换IfcModel时间：{1},总时间：{0}",
                    dwgDBDate - startDate,
                    endDate - dwgDBDate,
                    endDate - startDate);
                Active.Database.GetEditor().WriteMessage(msg);
            }
        }
        [CommandMethod("TIANHUACAD", "THTGL2DWG", CommandFlags.Modal)]
        public void THTGL2DWG()
        {
            // 拾取TGL XML文件
            var tgl = OpenTGLXMLFile();
            if (string.IsNullOrEmpty(tgl))
            {
                return;
            }

            // 读入并解析TGL XML文件
            var service = new ThTGLXMLService();
            var project = service.LoadXML(tgl);
            if (project == null)
            {
                return;
            }

            // 读入DWG数据
            var dwgService = new ThTGL2IFCDWGService();
            dwgService.LoadDWG(Active.Database, project);

            // 输出三维实体
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var storey in project.Site.Building.Storeys)
                {
                    foreach (var slab in storey.Slabs)
                    {
                        acadDatabase.ModelSpace.Add(slab.CreateSlabSolid(Point3d.Origin));
                    }

                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDB2Push", CommandFlags.Modal)]
        public void THDB2Push()
        {
            Active.Database.GetEditor().WriteMessage($"Start");

            // 拾取TGL DB文件
            var filePath = OpenDBFile();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            Active.Database.GetEditor().WriteMessage($"开始读入并解析TGL XML文件");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject();
            if (project == null)
            {
                return;
            }
            sw.Stop();
            Active.Database.GetEditor().WriteMessage($"读入并解析TGL XML文件完成，共用时{sw.ElapsedMilliseconds}s");
            sw.Reset();
            Active.Database.GetEditor().WriteMessage($"开始序列化project.");
            sw.Start();


            // Step 1 共享内存
            {
                //var nbytes = 64 * 1024 * 1024;
                //using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("THDB2Push", 10))
                //{
                //    Active.Database.GetEditor().WriteMessage("创建临时管道\r\n");
                //    bool mutexCreated;
                //    //同步基元 又名互斥体
                //    Mutex mutex = new Mutex(true, "THDB2PushMutex", out mutexCreated);
                //    if (!mutexCreated)
                //    {

                //    }
                //    //test
                //    byte[] bytes = new byte[10];
                //    bytes[0] = 1;
                //    bytes[9] = 1;
                //    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                //    {
                //        BinaryWriter writer = new BinaryWriter(stream);
                //        writer.Write(bytes);
                //    }
                //    mutex.ReleaseMutex();

                //    Active.Database.GetEditor().WriteMessage("已上传数据\r\n");
                //    Thread.Sleep(5000);
                //    Mutex mutexRe;
                //    if (Mutex.TryOpenExisting("THDB2PushMutexRe", out mutexRe))
                //    {
                //        Active.Database.GetEditor().WriteMessage("收到回复，Push成功\r\n");
                //    }
                //    else
                //    {
                //        Active.Database.GetEditor().WriteMessage("未收到回复\r\n");
                //    }
                //}
                //Active.Database.GetEditor().WriteMessage("销毁管道\r\n");
            }

            //Step 2 管道
            {
                var pipeClient =
                        new NamedPipeClientStream(".", "testpipe",
                            PipeDirection.Out, PipeOptions.None,
                            TokenImpersonationLevel.Impersonation);
                try
                {
                    pipeClient.Connect(5000);
                    //if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Point3d)))
                    //{
                    //    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Point3d), false).SetSurrogate(typeof(Point3DSurrogate));
                    //}
                    //if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Vector3d)))
                    //{
                    //    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Vector3d), false).SetSurrogate(typeof(Vector3DSurrogate));
                    //}
                    //if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Polyline)))
                    //{
                    //    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Polyline), false).SetSurrogate(typeof(PolylineSurrogate));
                    //}
                    //Serializer.Serialize(pipeClient, project);
                    //pipeClient.Dispose();
                    var stream = new BinaryWriter(pipeClient);
                    //test
                    byte[] bytes = new byte[10];
                    bytes[0] = 1;
                    bytes[9] = 1;
                    stream.Write(bytes);
                    stream.Dispose();
                    pipeClient.Dispose();
                    Active.Database.GetEditor().WriteMessage("已发送至Viewer\r\n");
                }
                catch
                {
                    pipeClient.Dispose();
                    Active.Database.GetEditor().WriteMessage("未连接到Viewer\r\n");
                }
            }

        }

        private string OpenTGLXMLFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "TGL XML|*.xml"; // Filter files by extension
            var result = dlg.ShowDialog();
            return (result == DialogResult.OK) ? dlg.FileName : string.Empty;
        }
        private string OpenDBFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".db"; // Default file extension
            dlg.Filter = "TGL DB|*.db"; // Filter files by extension
            var result = dlg.ShowDialog();
            return (result == DialogResult.OK) ? dlg.FileName : string.Empty;
        }
    }
}
