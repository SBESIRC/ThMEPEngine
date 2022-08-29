using System;
using AcHelper;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.Diagnostics;
using ThMEPTCH.Services;

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
                var project = service.DWGToProject(false, false);
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
