#define IFC2X3
//#define IFC4
using System.IO;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
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

            // 转换并保存IFC数据
            ThTGL2IFCService Tgl2IfcService = new ThTGL2IFCService();
            Tgl2IfcService.GenerateIfcModelAndSave(project, Path.ChangeExtension(tgl, "ifc"));
        }

        private string OpenTGLXMLFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "TGL XML|*.xml"; // Filter files by extension
            var result = dlg.ShowDialog();
            return (result == DialogResult.OK) ? dlg.FileName : string.Empty;
        }
    }
}
