using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Model;
using ThMEPStructure.Common;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.StructPlane.Print;
using ThMEPStructure.StructPlane.Service;

namespace ThMEPStructure.StructPlane
{
    internal class ThStructurePlaneGenerator
    {
        /// <summary>
        /// 结构出图类型
        /// </summary>
        public string DrawingType { get; set; }
        private ThPlaneConfig Config { get; set; }
        private ThPlanePrintParameter PrintParameter { get; set; }
        public ThStructurePlaneGenerator(ThPlaneConfig config, ThPlanePrintParameter printParameter)
        {
            Config = config;
            PrintParameter = printParameter;
        }
        public void Generate()
        {
            // 先配置
            Config.Configure();

            // 清除
            Clear();

            // 成图
            Plot();

            // 获取Svg
            var svgFiles = GetGeneratedSvgFiles();
            svgFiles = Sort(svgFiles);

            // 打印
            Print(svgFiles);

            // 删除
            Erase(svgFiles);
        }
        private void Plot()
        {
            using (var proc = new Process())
            {
                object output = null;                           
                proc.StartInfo.FileName = Config.ExeFilePath;                
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
                proc.StartInfo.Arguments = Config.Arguments;
                    
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
            Active.Database.ImportStruPlaneTemplate();
            SetSysVariables();
            var printers = new List<ThStruDrawingPrinter>();
            if (DrawingType == "结构平面图")
            {
                printers = PrintStructurePlan(svgFiles);
            }
            else if (DrawingType == "墙柱施工图")
            {
                printers = PrintWallColumnDrawing(svgFiles);
            }
            var floorObjIds = printers.Select(o => o.ObjIds).ToList();
            floorObjIds.Layout(PrintParameter.FloorSpacing);
            InsertBasePoint();

            // 设置DrawOrder
            SetLayerOrder(floorObjIds);
            if (floorObjIds.IsIncludeHatch())
            {
                Active.Document.SendCommand("HatchToBack" + "\n");
            }
        }

        private void SetLayerOrder(List<ObjectIdCollection> floorObjIds)
        {
            // 按照图层设置DrawOrder
            var layerPriority1 = new List<string> { ThPrintLayerManager.ColumnHatchLayerName, ThPrintLayerManager.BelowColumnHatchLayerName};
            var layerPriority2 = new List<string> { ThPrintLayerManager.ShearWallHatchLayerName, ThPrintLayerManager.BelowShearWallHatchLayerName };
            floorObjIds.SetLayerOrder(layerPriority1);
            floorObjIds.SetLayerOrder(layerPriority2);
        }

        private List<ThStruDrawingPrinter> PrintWallColumnDrawing(List<string> svgFiles)
        {
            var results = new List<ThStruDrawingPrinter>();
            svgFiles.ForEach(svgFile =>
            {
                var svg = new ThStructureSVGReader();
                svg.ReadFromFile(svgFile);

                // 对剪力墙造洞
                var buildAreaSevice = new ThWallBuildAreaService();
                var passGeos = buildAreaSevice.BuildArea(svg.Geos);
                var svgInput = new ThSvgInput()
                {
                    Geos = passGeos,
                    FloorInfos = svg.FloorInfos,
                    DocProperties = svg.DocProperties,
                };
                var printer = new ThStruWallColumnDrawingPrinter(svgInput, PrintParameter);
                printer.Print(Active.Database);
                results.Add(printer);
            });
            return results;
        }

        private List<ThStruDrawingPrinter> PrintStructurePlan(List<string> svgFiles)
        {
            var results = new List<ThStruDrawingPrinter>();
            svgFiles.ForEach(svgFile =>
            {
                var svg = new ThStructureSVGReader();
                svg.ReadFromFile(svgFile);

                // 处理
                // 对剪力墙造洞
                var buildAreaSevice = new ThWallBuildAreaService();
                var newGeos = buildAreaSevice.BuildArea(svg.Geos);

                #region ---------- 梁处理 ----------
                // 用下层墙、柱对梁线进行Trim+合并梁线
                var beamGeos = newGeos.GetBeamGeos();
                var beamMarkGeos = newGeos.GetBeamMarks();
                var passGeos = newGeos.Except(beamGeos).ToList();
                var belowObjs = GetBelowObjs(passGeos);
                var newBeamGeos = ThBeamLineCleaner.Clean(beamGeos, belowObjs,
                    beamMarkGeos.Select(o => o.Boundary).ToCollection());
                passGeos.AddRange(newBeamGeos);
                #endregion

                var svgInput = new ThSvgInput()
                {
                    Geos = passGeos,
                    FloorInfos = svg.FloorInfos,
                    DocProperties = svg.DocProperties,
                };
                var printer = new ThStruPlanDrawingPrinter(svgInput, PrintParameter);
                printer.Print(Active.Database);
                results.Add(printer);
            });
            return results;
        }
        private DBObjectCollection GetBelowObjs(List<ThGeometry> geos)
        {
            var polygons = new DBObjectCollection();
            var belowColumns = geos.GetBelowColumnGeos();
            var belowShearWalls = geos.GetBelowShearwallGeos();
            belowColumns.ForEach(o => polygons.Add(o.Boundary));
            belowShearWalls.ForEach(o => polygons.Add(o.Boundary));
            return polygons;
        }
        private void SetSysVariables()
        {
            acadApp.Application.SetSystemVariable("LTSCALE", PrintParameter.LtScale);
            acadApp.Application.SetSystemVariable("MEASUREMENT", PrintParameter.Measurement);
        }
        private void Clear()
        {
            var svgFiles = GetGeneratedSvgFiles();
            Erase(svgFiles);
        }
        private List<string> Sort(List<string> svgFiles)
        {
            //svgFiles已经经过合理性检查
            //C: \Users\XXXX\AppData\Local\Temp\0407-1-Floor_1-Floor_2.svg
            return svgFiles.OrderBy(o =>
            {
                var fileName = Path.GetFileNameWithoutExtension(o);
                var strs = fileName.Split('-');
                if(strs.Length>3)
                {
                    var str = strs[strs.Length - 3];
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
            return results
                .Where(o=>IsValidSvgFile(o))
                .ToList();
        }
        private bool IsValidSvgFile(string svgFilePath)
        {
            //ifcFileName-> 0407-1
            var ifcFileName = Config.IfcFileName.ToUpper();
            // 0407-1-Floor_1-Floor_2
            var fileName = Path.GetFileNameWithoutExtension(svgFilePath); 
            if(!fileName.ToUpper().StartsWith(ifcFileName))
            {
                return false;
            }
            // 1-Floor_1-Floor_2
            var strs = fileName.Split('-');
            if(strs.Length<=3)
            {
                return false;
            }  
            return strs[strs.Length - 3].IsInteger();
        }
        private void InsertBasePoint()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if (acadDb.Blocks.Contains(ThPrintBlockManager.BasePointBlkName) &&
                    acadDb.Layers.Contains(ThPrintLayerManager.DefpointsLayerName))
                {
                    DbHelper.EnsureLayerOn(ThPrintLayerManager.DefpointsLayerName);
                    acadDb.ModelSpace.ObjectId.InsertBlockReference(
                                       ThPrintLayerManager.DefpointsLayerName,
                                       ThPrintBlockManager.BasePointBlkName,
                                       Point3d.Origin,
                                       new Scale3d(1.0),
                                       0.0);
                }
            }
        }
    }
}
