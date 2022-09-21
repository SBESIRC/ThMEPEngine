using System;
using AcHelper;
using DotNetARX;
using ThMEPTCH.Services;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using System.IO;

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
                    var path = Path.GetDirectoryName(filePath);
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var NewFilePath = Path.Combine(path, fileName + "-100%.ifc");
                    Ifc2x3.ThTGL2IFC2x3Builder.SaveIfcModel(MergeModel, NewFilePath);
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
