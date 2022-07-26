using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.IO;
using System.IO.Pipes;
using ThMEPTCH.Services;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using ThMEPTCH.Model.SurrogateModel;
using Autodesk.AutoCAD.DatabaseServices;
using CADApp = Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPIFC
{
    public class ThMEPIFCApp : IExtensionApplication
    {
        string dbFilePath = @"C:\Tangent\TArchT20V8\SYS\output\TG20.db";
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
            var isDB = (Convert.ToInt16(CADApp.Application.GetSystemVariable("USERR3")) == 1);
            var ifcFilePath = "";
            var filePath = "";
            if (isDB)
            {
                // 拾取天正 DB文件
                filePath = OpenDBFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }
                ifcFilePath = Path.ChangeExtension(filePath, "ifc");
            }
            else 
            {
                //选择保存路径
                ifcFilePath = SaveFilePath("ifc");
            }
            if (File.Exists(ifcFilePath))
                File.Delete(ifcFilePath);
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(false,false);
            if (project == null)
            {
                return;
            }
            var dwgDBDate = DateTime.Now;
            // 转换并保存IFC数据
            ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
            Tgl2IfcService.GenerateIfcModelAndSave(project, ifcFilePath);
            var endDate = DateTime.Now;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string msg = string.Format(
                    "读取DB数据楼层信息，分层组合数据时间：{0},分出组合数据转换IfcModel时间：{1},总时间：{2}",
                    (dwgDBDate - startDate).TotalSeconds,
                    (endDate - dwgDBDate).TotalSeconds,
                    (endDate - startDate).TotalSeconds);
                Active.Database.GetEditor().WriteMessage(msg);
            }
        }

        [CommandMethod("TIANHUACAD", "THDB2File", CommandFlags.Modal)]
        public void THDBL2MidFile()
        {
            var isDB = (Convert.ToInt16(CADApp.Application.GetSystemVariable("USERR3")) == 1);
            var midFilePath = "";
            var filePath = "";
            if (isDB)
            {
                // 拾取天正 DB文件
                filePath = OpenDBFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }
                midFilePath = Path.ChangeExtension(filePath, "midfile");
            }
            else
            {
                //选择保存路径
                midFilePath = SaveFilePath("midfile");
            }
            if (File.Exists(midFilePath))
                File.Delete(midFilePath);
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(false,false);
            if (project == null)
            {
                return;
            }
            var dwgDBDate = DateTime.Now;
            // 转换并保存为渲染引擎识别的中间文件
            var Tgl2IfcService = new ThTGL2GeoFileService();
            Tgl2IfcService.GenerateXBimMeshAndSave(project, midFilePath);
            var endDate = DateTime.Now;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string msg = string.Format(
                        "读取DB数据楼层信息，分层组合数据时间：{0},分出组合数据转换IfcModel时间：{1},总时间：{2}",
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
            var isDB = (Convert.ToInt16(CADApp.Application.GetSystemVariable("USERR3")) == 1);
            var filePath = "";
            if (isDB)
            {
                // 拾取天正 DB文件
                filePath = OpenDBFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }
            }
            Active.Database.GetEditor().WriteMessage($"开始读入并解析TGL XML文件");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(true,false);
            if (project == null)
            {
                return;
            }
            sw.Stop();
            Active.Database.GetEditor().WriteMessage($"读入并解析TGL XML文件完成，共用时{sw.ElapsedMilliseconds}ms");
            sw.Reset();
            Active.Database.GetEditor().WriteMessage($"开始序列化project.");
            sw.Start();

            // 管道
            //"THDB2Push_TestPipe" 作为管道名称，两端管道名称需一致
            var pipeClient = new NamedPipeClientStream(".", "THDB2Push_TestPipe",
                        PipeDirection.Out, PipeOptions.None,
                        TokenImpersonationLevel.Impersonation);
            try
            {
                pipeClient.Connect(5000);
                if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Point3d)))
                {
                    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Point3d), false).SetSurrogate(typeof(Point3DSurrogate));
                }
                if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Vector3d)))
                {
                    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Vector3d), false).SetSurrogate(typeof(Vector3DSurrogate));
                }
                if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Matrix3d)))
                {
                    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Matrix3d), false).SetSurrogate(typeof(Matrix3DSurrogate));
                }
                if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Polyline)))
                {
                    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Polyline), false).SetSurrogate(typeof(PolylineSurrogate));
                }
                if (!ProtoBuf.Meta.RuntimeTypeModel.Default.IsDefined(typeof(Entity)))
                {
                    ProtoBuf.Meta.RuntimeTypeModel.Default.Add(typeof(Entity), false).SetSurrogate(typeof(PolylineSurrogate));
                }
                Serializer.Serialize(pipeClient, project);
                pipeClient.Close();
                Active.Database.GetEditor().WriteMessage("已发送至Viewer\r\n");
            }
            catch(System.Exception ex)
            {
                pipeClient.Dispose();
                Active.Database.GetEditor().WriteMessage("未连接到Viewer\r\n");
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
