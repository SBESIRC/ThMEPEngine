using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPTCH.Services;
using ThPlatform3D.Common;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.ArchitecturePlane.Print;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using ThMEPTCH.CAD;

namespace ThPlatform3D.ArchitecturePlane
{
    public class ThArchitectureGenerator
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
            var gridFiles = svgFiles.Select(o => GetGridDataFile(o)).ToList();

            // 打印
            Print(svgFiles);

            // 删除
            Erase(svgFiles);
            Erase(gridFiles);
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
            // 传入的svgFiles是单体建筑的所有楼层
            if (svgFiles.Count == 0)
            {
                return;
            }
            // 从模板导入要打印的图层
            if (!ThImportDatabaseService.ImportArchDwgTemplate(Active.Database))
            {
                return;
            }
            var printers = new List<ThArchDrawingPrinter>();
            var singleBuildingStoreys = new List<ThEditStoreyInfo>();
            for(int i=0;i< svgFiles.Count;i++)
            {
                var svg = new ThArchitectureSVGReader();
                svg.ReadFromFile(svgFiles[i]);
                if (i == 0)
                {
                    singleBuildingStoreys = GetBuildingStoreyInfos(svg.ParseInfo.BuildingName);
                }
                PrintParameter.GridDataFile = GetGridDataFile(svgFiles[i]);
                var printer = PrintToCad(svg.ParseInfo, PrintParameter);
                if (printer != null)
                {
                    printers.Add(printer);
                }
            }

            var drawingBasePt = PrintParameter.BasePoint;
            var floorBasePts = new List<Point3d>();            
            var floorObjIds = new List<ObjectIdCollection>();            
            for (int i=1;i<= printers.Count;i++)
            {
                floorObjIds.Add(printers[i-1].ObjIds);
                floorBasePts.Add(GetFloorBasePt(i, PrintParameter.FloorSpacing, drawingBasePt));
            }

            for(int i=0;i< floorObjIds.Count;i++)
            {
                var displacement = Point3d.Origin.GetVectorTo(floorBasePts[i]);
                Layout(floorObjIds[i], displacement);
            }

            SetLayerOrder(floorObjIds);

            // 把图纸里的轴网拷贝到成图里
            if(svgFiles.Count == singleBuildingStoreys.Count)
            {
                // 一层是一个svg file
                // svg文件的数量和楼层信息的数量必须一致
                // svgFiles必须是从第一层到最后一层的排序
                var allAxisObjs = GetAxisObjects();
                var allBasePoints = GetBasePoints();
                var axisSpatialIndex = new ThCADCoreNTSSpatialIndex(allAxisObjs);
                var axisBasePointSpatialIndex = new ThCADCoreNTSSpatialIndex(allBasePoints);
                var aixObjIds = PrintAxis(floorBasePts, axisSpatialIndex, axisBasePointSpatialIndex, singleBuildingStoreys);
                aixObjIds.MoveToBottom();
            }

            if (floorObjIds.IsIncludeHatch())
            {
                Active.Document.SendCommand("HatchToBack" + "\n");
            }
        }

        private ObjectIdCollection PrintAxis(
            List<Point3d> floorBasePts,            
            ThCADCoreNTSSpatialIndex axisSpatialIndex,
            ThCADCoreNTSSpatialIndex axisBasePointSpatialIndex,
            List<ThEditStoreyInfo> singleBuildingStoreys)
        {
            // floorBasePts成图后计算的自下而上，每层图的基点
            // 轴网是基于现有的轴网的Copy出来的，样式图层已经存在
            using (var acadDb = AcadDatabase.Active())
            {
                var printAxisObjs = new DBObjectCollection();
                for (int i = 0; i < singleBuildingStoreys.Count; i++)
                {
                    if (i >= floorBasePts.Count)
                    {                        
                        continue; // 表示设置的楼层数比生成的楼层数多
                    }
                    var storeyInfo = singleBuildingStoreys[i];
                    var paperBlk = GetPaperFrame(storeyInfo.PaperFrameHandle);
                    if (paperBlk == null)
                    {
                        continue;
                    }
                    else
                    {
                        var frameRange = GetPaperFrameRange(paperBlk);                       
                        if (frameRange == null || frameRange.Area <= 1.0)
                        {
                            continue;
                        }
                        else
                        {
                            // 查询框内的基点数量
                            var frameBasePts = axisBasePointSpatialIndex
                                .SelectWindowPolygon(frameRange)
                                .OfType<DBPoint>().ToCollection();
                            if(frameBasePts.Count!=1)
                            {
                                continue;
                            }
                            var frameBasePt = frameBasePts.OfType<DBPoint>().First().Position;
                            var frameObjs = axisSpatialIndex.SelectCrossingPolygon(frameRange);
                            if (frameObjs.Count == 0)
                            {
                                continue;
                            }
                            else
                            {                                
                                var sourcePt = frameBasePt;
                                var targetPt = floorBasePts[i];
                                var displacement = sourcePt.GetVectorTo(targetPt);
                                var matrix = Matrix3d.Displacement(displacement);
                                var cloneObjs = frameObjs.Clone();
                                cloneObjs.OfType<Entity>().ForEach(o =>
                                {
                                    o.TransformBy(matrix);
                                    printAxisObjs.Add(o);
                                });
                            }
                        }
                    }
                }

                // 打印轴网
                printAxisObjs = printAxisObjs.Distinct();
                return printAxisObjs
                    .OfType<Entity>()
                    .Select(o => acadDb.ModelSpace.Add(o))
                    .ToCollection();
            }  
        }

        private void Layout(ObjectIdCollection floorObjIds, Vector3d displacement)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var mt = Matrix3d.Displacement(displacement);
                floorObjIds.OfType<ObjectId>().ForEach(o =>
                {
                    var entity = acadDb.Element<Entity>(o, true);
                    entity.TransformBy(mt);
                });
            }
        }

        private Point3d GetFloorBasePt(int natureIndex,double floorSpacing,Point3d basePt)
        {
            // natureIndex 从1开始
            // 默认第一层的基点在原点
            return basePt + new Vector3d(0, (natureIndex -1) * floorSpacing, 0);
        }

        private Polyline GetPaperFrameRange(BlockReference br)
        {
            return br.ToOBB(br.BlockTransform);
        }

        private BlockReference GetPaperFrame(string handleValue)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if(string.IsNullOrEmpty(handleValue))
                {
                    return null;
                }
                var value = Convert.ToInt64(handleValue, 16);
                var handle = new Handle(value);
                var objId = acadDb.Database.GetObjectId(false, handle, 0);
                if (objId != ObjectId.Null && !objId.IsErased && objId.IsValid)
                {
                    var entity = acadDb.Element<Entity>(objId, false);
                    return entity is BlockReference br ? br : null;
                }
                else
                {
                    return null;
                }
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

        private List<ThEditStoreyInfo> GetBuildingStoreyInfos(string buildingName)
        {
            var storeyInfos = GetStoreyInfos();
#if DEBUG
            if (string.IsNullOrEmpty(buildingName) && storeyInfos.Count > 0)
            {
                buildingName = storeyInfos.First().Key;
            }
#endif
            if(storeyInfos.ContainsKey(buildingName))
            {
                return storeyInfos[buildingName];
            }
            else
            {
                return new List<ThEditStoreyInfo>();
            }
        }

        private Dictionary<string, List<ThEditStoreyInfo>> GetStoreyInfos()
        {
            var dwgFileName = GetActiveDocName();
            if(File.Exists(dwgFileName))
            {
                var fileInfo = new FileInfo(dwgFileName);
                var dir = fileInfo.Directory.FullName;
                var jsonFileName = Path.GetFileNameWithoutExtension(dwgFileName);
                var jsonFileFullPath =  Path.Combine(dir, jsonFileName + ".StoreyInfo.json");
                return ThIfcStoreyParseTool.DeSerialize(jsonFileFullPath);
            }
            else
            {
                return new Dictionary<string, List<ThEditStoreyInfo>>();
            }
        }

        private string GetActiveDocName()
        {
            if (acadApp.DocumentManager.Count > 0)
            {
                return acadApp.DocumentManager.MdiActiveDocument.Name;
            }
            else
            {
                return "";
            }
        }

        private DBObjectCollection GetAxisObjects()
        {
            //var extraction = new ThTCHAxisLineExtractionEngine();
            //extraction.Extract(Active.Document.Database);
            //return extraction.Results.Select(o => o.Geometry).ToCollection();
            return new DBObjectCollection();
        }

        private DBObjectCollection GetBasePoints()
        {
            var extraction = new ThAxisBasePointExtractionEngine();
            extraction.Extract(Active.Document.Database);
            return extraction.Results.Select(o => o.Geometry).ToCollection();
        }

        private void Clear()
        {
            var svgFiles = GetGeneratedSvgFiles();
            Erase(svgFiles);

            var gridFiles = svgFiles.Select(o=>GetGridDataFile(o)).ToList();
            Erase(gridFiles);
        }

        private List<string> Sort(List<string> svgFiles)
        {
            if (svgFiles.Count == 1)
            {
                return svgFiles;
            }
            //C: \Users\XXXX\AppData\Local\Temp\6#建筑结构合模-01F-GridData.json
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
        private List<string> SortGrids(List<string> gridFiles)
        {
            if (gridFiles.Count == 1)
            {
                return gridFiles;
            }
            //C:\Users\XXXX\AppData\Local\Temp\6#建筑结构合模-01F-GridData.json
            var underGroundFlrs = new List<Tuple<string, float>>(); //B1F,B2F
            var roofFlrs = new List<Tuple<string, float>>(); //R1F,R2F
            var normalFlrs = new List<Tuple<string, float>>(); //1F,2F
            var invalidFlrs = new List<string>(); // 
            gridFiles.ForEach(o =>
            {
                var fileName = Path.GetFileNameWithoutExtension(o);
                var strs = fileName.Split('-');
                var flrNo = strs[strs.Length-2];
                if (IsUnderGroundFloor(flrNo))
                {
                    var flrNoValue = ParseUnderFlrNo(flrNo);
                    if (flrNoValue.HasValue)
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
                    if (flrNoValue.HasValue)
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
                    if (flrNoValue.HasValue)
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
                if (File.Exists(svgFile))
                {
                    var fi = new FileInfo(svgFile);
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

        private string GetGridDataFile(string svgFile)
        {
            var fileName = Path.GetFileNameWithoutExtension(svgFile);
            fileName += "-GridData.json";
            var fi = new FileInfo(svgFile);
            var fullPath = Path.Combine(fi.Directory.Name+ fileName);
            if(File.Exists(fullPath))
            {
                return fullPath;
            }
            else
            {
                return "";
            }
        }

        private ThArchDrawingPrinter PrintToCad(ThSvgParseInfo svgInput, ThPlanePrintParameter printParameter)
        {
            ThArchDrawingPrinter printer = null;
            switch (Config.DrawingType)
            {
                case DrawingType.Plan:
                    printer = new ThArchPlanDrawingPrinter(svgInput, printParameter);
                    break;
                case DrawingType.Elevation:
                    printer = new ThArchElevationDrawingPrinter(svgInput, printParameter);
                    break;
                case DrawingType.Section:
                    printer = new ThArchSectionDrawingPrinter(svgInput, printParameter);
                    break;
            }
            if (printer != null)
            {
                printer.Print(Active.Database);                
            }
            return printer;
        }
    }
}
