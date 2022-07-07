using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.Common;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchitecturePlane.Print;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPStructure.ArchitecturePlane
{
    internal class ThArchitectureGenerator
    {
        private ThPlaneConfig Config { get; set; }
        private ThPlanePrintParameter PrintParameter { get; set; }
        public ThArchitectureGenerator(ThPlaneConfig config, ThPlanePrintParameter printParameter)
        {
            Config = config;
            PrintParameter=printParameter;
        }
        public void Generate()
        {
            Config.Configure();

            // 清除
            Clear();

            // 成图
            if(Config.DrawingType == DrawingType.Elevation || Config.DrawingType == DrawingType.Section)
            {
                // elevation的SvgSavePath需要FullFilePath
                var argument = Config.BuildArgument(
                    Config.SvgConfigFilePath,
                    Config.IfcFilePath, 
                    Config.JsonConfig.SvgConfig.save_path,
                    Config.LogSavePath);
                Plot(Config.ExeFilePath, argument);
            }
            else
            {
                Plot(Config.ExeFilePath,Config.Arguments);
            }

            // 获取Svg
            var svgFiles = GetGeneratedSvgFiles();
            svgFiles = Sort(svgFiles);

            // 打印
            Print(svgFiles);

            // 删除
            Erase(svgFiles);
        }
        private void Plot(string exeFilePath,string arguments)
        {
            using (var proc = new Process())
            {
                object output = null;                           
                proc.StartInfo.FileName = exeFilePath;                
                // 是否使用操作系统Shell启动
                proc.StartInfo.UseShellExecute = false;
                // 不显示程序窗口
                proc.StartInfo.CreateNoWindow = true;
                // 由调用程序获取输出信息
                proc.StartInfo.RedirectStandardOutput = true;
                // 接受来自调用程序的输入信息
                proc.StartInfo.RedirectStandardInput = true;
                // 重定向标准错误输出
                proc.StartInfo.RedirectStandardError = true;

                // elevation-generator.exe--config_path config_path --input_path input_path --output_path output_path --log_path log_path
                proc.StartInfo.Arguments = arguments;
                    
                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    output = proc.StandardOutput.ReadToEnd();                    
                }
                else
                {
                    output = proc.StandardError.ReadToEnd();
                }
            }
        }
        
        private void Print(List<string> svgFiles)
        {
            if(svgFiles.Count==0)
            {
                return;
            }
            // 从模板导入要打印的图层
            if (!ThImportDatabaseService.ImportArchDwgTemplate(Active.Database))
            {
                return;
            }
            var printers = PrintToCad(svgFiles);
            var floorObjIds = printers.Select(o => o.ObjIds).ToList();
            floorObjIds.Layout(PrintParameter.FloorSpacing);
            SetLayerOrder(floorObjIds);
            if (floorObjIds.IsIncludeHatch())
            {
                Active.Document.SendCommand("HatchToBack" + "\n");
            }
        }

        private void SetLayerOrder(List<ObjectIdCollection> floorObjIds)
        {
            //AE-WALL＞AE-WIND＞AE-DOOR-INSD＞AE-FNSH＞AE-HDWR＞AE-FLOR
            //线重合时，根据优先级进行图层前后置，以来保证图面显示效果。
            var layerPriority = new List<string> { ThArchPrintLayerManager.AEWALL, ThArchPrintLayerManager.AEWIND,
            ThArchPrintLayerManager.AEDOORINSD,ThArchPrintLayerManager.AEFNSH,ThArchPrintLayerManager.AEHDWR,
            ThArchPrintLayerManager.AEFLOR};
            floorObjIds.SetLayerOrder(layerPriority);
        }

        private void Clear()
        {
            var svgFiles = GetGeneratedSvgFiles();
            Erase(svgFiles);
        }

        private List<string> Sort(List<string> svgFiles)
        {
            //svgFiles已经经过合理性检查
            //C: \Users\XXXX\AppData\Local\Temp\6#建筑结构合模-7-17F.svg
            return svgFiles.OrderBy(o =>
            {
                var fileName = Path.GetFileNameWithoutExtension(o);
                var strs = fileName.Split('-');
                if(strs.Length>2)
                {
                    var str = strs[strs.Length - 2];                    
                    if(str.IsInteger())
                    {
                        return int.Parse(str.Trim());
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }).ToList();
        }

        private void Erase(List<string> svgFiles)
        {
            svgFiles.ForEach(svgFile =>
            {
                var fi = new FileInfo(svgFile);
                if(fi.Exists)
                {
                    fi.Delete();
                }
            });
        }

        private List<string> GetGeneratedSvgFiles()
        {
            var results = new List<string>();
            //文件名，不含后缀
            var ifcFileName = Config.IfcFileName.ToUpper();
            var di = new DirectoryInfo(Config.SvgSavePath);
            foreach(var fileInfo in di.GetFiles())
            {
                if(fileInfo.Extension.ToUpper()==".SVG" &&
                    fileInfo.Name.ToUpper().StartsWith(ifcFileName))
                {
                    results.Add(fileInfo.FullName);
                }
            }
            // 返回以Ifc文件名开始的所有Svg文件
            return results;
        }

        private List<ThArchDrawingPrinter> PrintToCad(List<string> svgFiles)
        {
            var results = new List<ThArchDrawingPrinter>();
            svgFiles.ForEach(svgFile =>
            {
                var svg = new ThArchitectureSVGReader();
                svg.ReadFromFile(svgFile);
                var svgInput = new ThSvgInput()
                {
                    Geos = svg.Geos,
                    FloorInfos = svg.FloorInfos,
                    DocProperties = svg.DocProperties,
                    ComponentInfos = svg.ComponentInfos,
                };                
                ThArchDrawingPrinter printer = null;
                switch (Config.DrawingType)
                {
                    case DrawingType.Plan:
                        printer = new ThArchPlanDrawingPrinter(svgInput, PrintParameter);
                        break;
                    case DrawingType.Elevation:
                        printer = new ThArchElevationDrawingPrinter(svgInput, PrintParameter);
                        break;
                    case DrawingType.Section:
                        printer = new ThArchSectionDrawingPrinter(svgInput, PrintParameter);
                        break;
                }
                if (printer != null)
                {
                    printer.Print(Active.Database);
                    results.Add(printer);
                }
            });
            return results;
        }
    }
}
