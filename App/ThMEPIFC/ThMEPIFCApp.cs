﻿using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using AcHelper;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using ThMEPTCH.Services;

namespace ThMEPIFC
{
    public class ThMEPIFCApp : IExtensionApplication
    {
        string dbFilePath = @"C:\Tangent\TArchT20V8\SYS\output\TG20.db";
        string tgExportArchCmd = "TGARCHEXPORT ";
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
            var filePath = dbFilePath;
            var ifcPath = Path.ChangeExtension(filePath, "ifc");
            // 拾取TGL DB文件
            //var filePath = OpenDBFile();
            //if (string.IsNullOrEmpty(filePath))
            //{
            //    return;
            //}

            if (File.Exists(ifcPath))
                File.Delete(ifcPath);
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(false);
            if (project == null)
            {
                return;
            }
            var dwgDBDate = DateTime.Now;
            // 转换并保存IFC数据
            ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
            Tgl2IfcService.GenerateIfcModelAndSave(project, ifcPath);
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

        [CommandMethod("TIANHUACAD", "THDB2File", CommandFlags.Modal)]
        public void THDBL2MidFile()
        {
            var filePath = dbFilePath;
            var midFilePath = Path.ChangeExtension(filePath, "midfile");
            if (File.Exists(midFilePath))
                File.Delete(midFilePath);
            var startDate = System.DateTime.Now;
            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject(false);
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
