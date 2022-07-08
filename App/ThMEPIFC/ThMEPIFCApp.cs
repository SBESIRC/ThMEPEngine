using System.IO;
using System.Windows.Forms;
using AcHelper;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
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

            // 读入并解析TGL XML文件
            var service = new ThDWGToIFCService(filePath);
            var project = service.DWGToProject();
            if (project == null)
            {
                return;
            }

            // 转换并保存IFC数据
            ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
            Tgl2IfcService.GenerateIfcModelAndSave(project, Path.ChangeExtension(filePath, "ifc"));
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
                        acadDatabase.ModelSpace.Add(slab.CreateSlabSolid());
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
