using System;
using ProtoBuf;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Windows.Forms;
using System.Security.Principal;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPTCH.Services;
using ThMEPTCH.Model.SurrogateModel;
using CADApp = Autodesk.AutoCAD.ApplicationServices;
using Google.Protobuf;
using System.Threading.Tasks;
using System.Threading;

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
            if (string.IsNullOrEmpty(ifcFilePath))
            {
                return;
            }
            if (File.Exists(ifcFilePath))
                File.Delete(ifcFilePath);
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(false, false);
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
            if (string.IsNullOrEmpty(midFilePath))
            {
                return;
            }
            if (File.Exists(midFilePath))
                File.Delete(midFilePath);
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(false, false);
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

        [CommandMethod("TIANHUACAD", "THIFCModelMerge", CommandFlags.Modal)]
        public void THIFCModelMerge()
        {
            var filePath1 = OpenIFCFile("请选择需要合模的IFC文件 [1]:");
            if (string.IsNullOrEmpty(filePath1))
            {
                return;
            }
            var filePath2 = OpenIFCFile("请选择需要合模的IFC文件 [2]:");
            if (string.IsNullOrEmpty(filePath1))
            {
                return;
            }
            var ifcFilePath = SaveFilePath("ifc");
            if (!string.IsNullOrWhiteSpace(ifcFilePath))
            {
                try
                {
                    THModelMergeService modelMergeService = new THModelMergeService();
                    var MergeModel = modelMergeService.ModelMerge(filePath1, filePath2);
                    if (!MergeModel.IsNull())
                    {
                        Ifc2x3.ThTGL2IFC2x3Builder.SaveIfcModel(MergeModel, ifcFilePath);
                        MergeModel.Dispose();
                        Active.Database.GetEditor().WriteMessage($"合模成功：已保存至目录:{ifcFilePath}");
                    }
                    else
                    {
                        throw new System.Exception("合模失败!");
                    }
                }
                catch
                {
                    Active.Database.GetEditor().WriteMessage($"合模失败!");
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THJGHM", CommandFlags.Modal)]
        public void THJGHM()
        {
            //选择需要合模的文件
            var filePath = OpenIFCFile("请选择需要合模的IFC文件:");
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(string.Empty);
            var project = service.DWGToProject(false, false, true, true);
            if (project == null)
            {
                return;
            }
            try
            {
                THModelMergeService modelMergeService = new THModelMergeService();
                var MergeModel = modelMergeService.ModelMerge(filePath, project);
                if (!MergeModel.IsNull())
                {
                    Ifc2x3.ThTGL2IFC2x3Builder.SaveIfcModel(MergeModel, filePath);
                    MergeModel.Dispose();
                    Active.Database.GetEditor().WriteMessage($"合模成功：已更新IFC文件");
                }
                else
                {
                    throw new System.Exception("合模失败!");
                }
            }
            catch
            {
                Active.Database.GetEditor().WriteMessage($"合模失败!");
            }
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

        private string OpenIFCFile(string Msg)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = Msg;
            dlg.DefaultExt = ".ifc"; // Default file extension
            dlg.Filter = "TGL IFC|*.ifc"; // Filter files by extension
            var result = dlg.ShowDialog();
            return (result == DialogResult.OK) ? dlg.FileName : string.Empty;
        }
    }
}
