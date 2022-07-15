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
using System;

namespace ThMEPStructure.ArchitecturePlane
{
    internal class ThArchitectureGenerator
    {
        private ThPlaneConfig Config { get; set; }
        private ThPlanePrintParameter PrintParameter { get; set; }
        public ThArchitectureGenerator(ThPlaneConfig config, ThPlanePrintParameter printParameter)
        {
            Config = config;
            PrintParameter = printParameter;
        }
        public void Generate()
        {
            Config.Configure();

            // 清除
            Clear();

            // 成图
            if (Config.DrawingType == DrawingType.Elevation || Config.DrawingType == DrawingType.Section)
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
                Plot(Config.ExeFilePath, Config.Arguments);
            }

            // 获取Svg
            var svgFiles = GetGeneratedSvgFiles();
            if (Config.DrawingType == DrawingType.Plan)
            {
                svgFiles = Sort(svgFiles);
            }

            // 打印
            Print(svgFiles);

            // 删除
            Erase(svgFiles);
        }
        private void Plot(string exeFilePath, string arguments)
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
            if (svgFiles.Count == 0)
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
            if (svgFiles.Count == 1)
            {
                return svgFiles;
            }
            //C: \Users\XXXX\AppData\Local\Temp\6#建筑结构合模-01F.svg
            var underGroundFlrs = new List<Tuple<string, float>>(); //B1F,B2F
            var roofFlrs = new List<Tuple<string, float>>(); //R1F,R2F
            var normalFlrs = new List<Tuple<string, float>>(); //1F,2F
            var invalidFlrs = new List<string>(); // 
            svgFiles.ForEach(o =>
            {
                var fileName = Path.GetFileNameWithoutExtension(o);
                var strs = fileName.Split('-');
                var flrNo = strs.Last();
                if (IsUnderGroundFloor(flrNo))
                {
                    var flrNoValue = ParseUnderFlrNo(flrNo);
                    if(flrNoValue.HasValue)
                    {
                        underGroundFlrs.Add(Tuple.Create(o, flrNoValue.Value));
                    }
                    else
                    {
                        invalidFlrs.Add(o);
                    }
                }
                else if (IsRoofFloor(flrNo))
                {
                    var flrNoValue = ParseNormalFlrNo(flrNo);
                    if(flrNoValue.HasValue)
                    {
                        roofFlrs.Add(Tuple.Create(o, flrNoValue.Value));
                    }
                    else
                    {
                        invalidFlrs.Add(o);
                    }
                }
                else
                {
                    var flrNoValue = ParseNormalFlrNo(flrNo);
                    if(flrNoValue.HasValue)
                    {
                        normalFlrs.Add(Tuple.Create(o, flrNoValue.Value));
                    }
                    else
                    {
                        invalidFlrs.Add(o);
                    }
                }
            });
            underGroundFlrs = underGroundFlrs.OrderBy(o => o.Item2).ToList();
            normalFlrs = normalFlrs.OrderBy(o => o.Item2).ToList();
            roofFlrs = roofFlrs.OrderBy(o => o.Item2).ToList();

            var results = new List<string>();
            results.AddRange(underGroundFlrs.Select(o => o.Item1));
            results.AddRange(normalFlrs.Select(o => o.Item1));
            results.AddRange(roofFlrs.Select(o => o.Item1));
            //results.AddRange(invalidFlrs); //不合格的暂时不打印
            return results;
        }

        private float? ParseUnderFlrNo(string flrNo)
        {
            // B1F
            var integers = ThPlaneUtils.GetIntegers(flrNo);
            if (integers.Count > 0)
            {
                var value = integers[0];
                if (IsMiddleFloor(flrNo))
                {
                    return -1.0F * (value + 0.5F);
                }
                else
                {
                    return -1.0F * value;
                }
            }
            return null;
        }
        private float? ParseNormalFlrNo(string flrNo)
        {
            // 1F,2F,2FM ,R1F
            var integers = ThPlaneUtils.GetIntegers(flrNo);
            if (integers.Count > 0)
            {
                var value = integers[0];
                if (IsMiddleFloor(flrNo))
                {
                    return value + 0.5F;
                }
                else
                {
                    return value;
                }
            }
            return null;
        }

        private bool IsUnderGroundFloor(string flrNo)
        {
            return flrNo.ToUpper().StartsWith("B");
        }
        private bool IsRoofFloor(string flrNo)
        {
            return flrNo.ToUpper().StartsWith("R");
        }
        private bool IsMiddleFloor(string flrNo)
        {
            return flrNo.ToUpper().EndsWith("M");
        }
        private void Erase(List<string> svgFiles)
        {
            svgFiles.ForEach(svgFile =>
            {
                var fi = new FileInfo(svgFile);
                if (fi.Exists)
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
            foreach (var fileInfo in di.GetFiles())
            {
                if (fileInfo.Extension.ToUpper() == ".SVG" &&
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
