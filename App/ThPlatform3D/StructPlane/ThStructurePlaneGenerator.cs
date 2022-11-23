using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using acadApp = Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Model;
using ThPlatform3D.Common;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.StructPlane.Print;
using ThPlatform3D.StructPlane.Service;

namespace ThPlatform3D.StructPlane
{
    public class ThStructurePlaneGenerator
    {
        /// <summary>
        /// 标准层编号
        /// 如果为空，则按所有层打印
        /// </summary>
        private string StdFlrNo { get; set; }
        /// <summary>
        /// 结构出图类型        
        /// </summary>
        private string DrawingType { get; set; }
        /// <summary>
        /// 用于Ifc或Get转Svg
        /// </summary>
        private ThPlaneConfig Config { get; set; }
        /// <summary>
        /// 用于打印Svg
        /// </summary>
        private ThPlanePrintParameter PrintParameter { get; set; }
        public ThStructurePlaneGenerator(ThPlaneConfig config, ThPlanePrintParameter printParameter)
        {
            StdFlrNo = "";
            Config = config;            
            PrintParameter = printParameter;
            DrawingType = ThStructurePlaneCommon.StructurePlanName;
        }

        /// <summary>
        /// Covert之后会生成Svg文件
        /// </summary>
        public bool IsSuccessedBuildSvgFiles => GetGeneratedSvgFiles().Count > 0;

        /// <summary>
        /// 转成Svg,附加：Storey.txt
        /// </summary>
        public void Convert()
        {
            // 先配置
            Config.Configure();

            // 清除
            Clear();

            // 成图
            Plot();
        }

        public void Generate()
        {
            // 获取Svg
            var svgFiles = GetGeneratedSvgFiles();
            svgFiles = Sort(svgFiles);

            // 记录svg的相对楼层
            var flrIndexDict = new Dictionary<string, int>();
            var flrIndex = 1;
            svgFiles.ForEach(o => flrIndexDict.Add(o, flrIndex++));

            // 根据标准层编号过滤
            svgFiles = FilterSvgFiles(svgFiles, StdFlrNo);

            // 打印
            Print(svgFiles,flrIndexDict);

            // 删除
            Erase(svgFiles);
        }

        public void SetDrawingType(string drawingType)
        {
            if(drawingType == ThStructurePlaneCommon.StructurePlanName ||
                drawingType == ThStructurePlaneCommon.WallColumnDrawingName)
            {
                DrawingType  = drawingType; 
            }
        }

        public void SetStdFlrNo(string stdFlrNo)
        {
            this.StdFlrNo = stdFlrNo;
        }

        private List<string> FilterSvgFiles(List<string> svgFiles , string stdFlrNo)
        {
            if (string.IsNullOrEmpty(stdFlrNo))
            {
                return svgFiles;
            }

            // 查找标准层编号相等的SvgFile
            var filters = svgFiles.Where(o =>
            {
                var fileName = Path.GetFileNameWithoutExtension(o);
                var strs = fileName.Split('-');
                if (strs.Length > 3)
                {
                    var str = strs[strs.Length - 3].Trim();
                    return str == stdFlrNo;
                }
                else
                {
                    return false;
                }
            }).ToList();

            if(filters.Count==1)
            {
                return filters;
            }
            else
            {
                //  再按自然层是否有值
                filters = filters.Where(o =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(o);
                    var strs = fileName.Split('-');
                    var str = strs[strs.Length - 2].Trim();
                    return str.IsInteger();
                }).ToList();

                // 再按楼层编号
                filters = filters.OrderBy(o =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(o);
                    var strs = fileName.Split('-');
                    var str = strs[strs.Length - 2].Trim();
                    return int.Parse(str);
                }).ToList();

                return filters;
            }
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
        
        private void Print(List<string> svgFiles, Dictionary<string, int> flrIndexDict)
        {            
            if(svgFiles.Count==0)
            {
                return;
            }
            Active.Editor.Command("_.PURGE", "B", " ", "N");

            Active.Database.ImportStruPlaneTemplate();
            SetSysVariables();
            var printers = new List<ThStruDrawingPrinter>();
            if (DrawingType == ThStructurePlaneCommon.StructurePlanName)
            {
                svgFiles.ForEach(o => printers.Add(PrintStructurePlan(o, flrIndexDict[o])));
            }
            else if (DrawingType == ThStructurePlaneCommon.WallColumnDrawingName)
            {
                svgFiles.ForEach(o => printers.Add(PrintWallColumnDrawing(o, flrIndexDict[o])));
            }
            printers.ForEach(o => o.ClearObjIds());

            // 用于控制重叠Hatch的先后顺序 (用图层来设置)   
            // DrawOrder的控制暂时只是简单的处理
            var priorityLayers = ThPrintLayerManager.PriorityLayers;       
            using (var acadDb = AcadDatabase.Active())
            {
                printers.ForEach(o =>
                {
                    var blkName = GetDrawingBlkName();
                    var blkIds =o.BlockObjIds; // 要打块的元素
                    var layerObjDict = blkIds.GroupByLayer();

                    // 优先级高的放在后面                    
                    var priorityObjIds = new List<ObjectIdCollection>();
                    priorityLayers.ForEach(layer =>
                    {
                        if(layerObjDict.ContainsKey(layer))
                        {
                            priorityObjIds.Add(layerObjDict[layer]);
                        }                       
                    });
                    var otherObjIds = new ObjectIdCollection();
                    layerObjDict
                    .Where(d => !ThPrintLayerManager.PriorityLayers.Contains(d.Key))
                    .ForEach(d => otherObjIds.AddRange(d.Value));
                    var blkObjs = new DBObjectCollection();
                    for(int i= priorityObjIds.Count-1;i>=0; i--)
                    {
                        blkObjs = blkObjs.Union(Clone(acadDb, priorityObjIds[i]));
                    }
                    // 暂时认为otherObjIds的物体的优先级要高于上面的物体
                    var hatchObjIds = new ObjectIdCollection();
                    var nonHatchObjIds = new ObjectIdCollection();
                    otherObjIds.OfType<ObjectId>().ForEach(id =>
                    {
                        var entity = acadDb.Element<Entity>(id);
                        if(entity is Hatch)
                        {
                            hatchObjIds.Add(id);
                        }
                        else
                        {
                            nonHatchObjIds.Add(id);
                        }
                    });
                    blkObjs = blkObjs.Union(Clone(acadDb, hatchObjIds));
                    blkObjs = blkObjs.Union(Clone(acadDb, nonHatchObjIds));

                    blkObjs.OfType<Entity>().ForEach(e => ThHyperLinkTool.Add(e, "Major:Structure","Info"));
                    var blockId = BuildBlock(acadDb,blkObjs, blkName);
                    if (blockId != ObjectId.Null)
                    {
                        var blkId  = InsertBlock(acadDb,"0", blkName, Point3d.Origin, new Scale3d(1.0), 0.0);
                        var blkEntity = acadDb.Element<Entity>(blkId, true);
                        ThHyperLinkTool.Add(blkEntity,"Major:Structure", "Info");
                        Erase(acadDb,blkIds);
                    }
                    o.ObjIds.OfType<ObjectId>().Where(x => !x.IsErased && x.IsValid).ForEach(x =>
                      {
                          var entity = acadDb.Element<Entity>(x, true);
                          ThCADExtension.ThHyperLinkTool.Add(entity, "Major:Structure", "Info");
                      });
                });
            }
            Active.Document.SendCommand("HatchToBack" + "\n");
        }

        private DBObjectCollection Clone(AcadDatabase acadDb, ObjectIdCollection objIds)
        {
            var results = new DBObjectCollection();
            objIds.OfType<ObjectId>().ForEach(o =>
            {
                var entity = acadDb.Element<Entity>(o, true);
                results.Add(entity.Clone() as Entity);
            });
            return results;
        }

        private ObjectId InsertBlock(AcadDatabase acadDb,string layer,string blkName,Point3d position,Scale3d scale,double rotateAng)
        {
            return acadDb.CurrentSpace.ObjectId.InsertBlockReference(layer, blkName, position, scale, rotateAng);
        }

        private void Erase(AcadDatabase acadDb, ObjectIdCollection objIds)
        {
            objIds.OfType<ObjectId>().ForEach(o =>
            {
                var entity = acadDb.Element<Entity>(o, true);
                entity.Erase();
            });
        }

        private ObjectId BuildBlock(AcadDatabase acadDb, DBObjectCollection objs,string blkName)
        {
            if (objs.Count == 0 || string.IsNullOrEmpty(blkName))
            {
                return ObjectId.Null;
            }
            var bt = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId, true);
            var btr = new BlockTableRecord()
            {                
                Name = blkName,
            };
            objs.OfType<Entity>().ForEach(o => btr.AppendEntity(o));
            var blkId = bt.Add(btr);
            acadDb.Database.TransactionManager.AddNewlyCreatedDBObject(btr, true);
            return blkId;
        }

        private void SetLayerOrder(List<ObjectIdCollection> floorObjIds)
        {
            // 按照图层设置DrawOrder
            floorObjIds.SetLayerOrder(ThPrintLayerManager.PriorityLayers);
        }

        private ThStruDrawingPrinter PrintWallColumnDrawing(string svgFile, int flrNaturalNumber)
        {
            var svg = new ThStructureSVGReader();
            svg.ReadFromFile(svgFile);
            var svgInput = svg.ParseInfo;

            // 移动
            svgInput.MoveToOrigin();
            if (flrNaturalNumber>1)
            {
                var moveDir = new Vector3d(0, PrintParameter.FloorSpacing * (flrNaturalNumber - 1), 0);
                var mt = Matrix3d.Displacement(moveDir);
                svgInput.Geos.ForEach(o => o.Boundary.TransformBy(mt));
            }
            
            // 对剪力墙造洞
            var buildAreaSevice = new ThWallBuildAreaService();
            var passGeos = buildAreaSevice.BuildArea(svgInput.Geos);
            svgInput.Geos = passGeos;

            // 用PCwall Difference StandardWall
            PCWallDifferenceStandardWall(svgInput.Geos);

            var printer = new ThStruWallColumnDrawingPrinter(svgInput, PrintParameter);
            printer.Print(Active.Database);
            return printer;
        }

        private void PCWallDifferenceStandardWall(List<ThGeometry> geos)
        {
            var upperPCWallGeos = geos.GetUpperPCWallGeos();
            var belowPCWallGeos = geos.GetBelowPCWallGeos();
            geos = geos.Except(upperPCWallGeos).ToList();
            geos = geos.Except(belowPCWallGeos).ToList();
            var belowStandardWalls = geos.GetBelowStandardShearwallGeos()
                .Select(o => o.Boundary).ToCollection();
            using (var processor = new ThPCWallProcessService(belowStandardWalls))
            {
                belowPCWallGeos.ForEach(o =>
                {
                    var newPcWalls = processor.Difference(o.Boundary);
                    newPcWalls.OfType<Entity>().ForEach(e =>
                    {
                        var newPcWallGeo = ThGeometry.Create(e, o.Properties);
                        geos.Add(newPcWallGeo);
                    });
                });
            }  
        }

        private ThStruDrawingPrinter PrintStructurePlan(string svgFile,int flrNaturalNumber)
        {
            var svg = new ThStructureSVGReader();
            svg.ReadFromFile(svgFile);
            var svgInput = svg.ParseInfo;

            // 移到原位
            svgInput.MoveToOrigin();

            // 移动
            if (flrNaturalNumber > 1)
            {
                var moveDir = new Vector3d(0, PrintParameter.FloorSpacing * (flrNaturalNumber - 1), 0);
                var mt = Matrix3d.Displacement(moveDir);
                svgInput.Geos.ForEach(o => o.Boundary.TransformBy(mt));
                PrintParameter.BasePoint = Point3d.Origin + moveDir;
            }

            #region ---------- 数据处理 ----------
            // 对剪力墙造洞
            var buildAreaSevice = new ThWallBuildAreaService();
            var newGeos = buildAreaSevice.BuildArea(svgInput.Geos);

            // 梁处理
            // 用下层墙、柱对梁线进行Trim+合并梁线
            var beamGeos = newGeos.GetBeamGeos();
            var beamMarkGeos = newGeos.GetBeamMarks();
            var passGeos = newGeos.Except(beamGeos).ToList();
            var belowObjs = GetBelowObjs(passGeos);
            var newBeamGeos = ThBeamLineCleaner.Clean(beamGeos, belowObjs,
                beamMarkGeos.Select(o => o.Boundary).ToCollection());
            passGeos.AddRange(newBeamGeos);

            // 处理空调板
            if (PrintParameter.IsFilterCantiSlab)
            {
                passGeos = ThSlabFilter.FilterCantiSlabs(passGeos);
            }

            // 过滤指定厚度的楼板标注
            passGeos = ThSlabFilter.FilterSpecifiedThickSlabs(passGeos, PrintParameter.DefaultSlabThick);
            #endregion

            // 打印
            svgInput.Geos = passGeos;           
            var printer = new ThStruPlanDrawingPrinter(svgInput, PrintParameter);
            printer.Print(Active.Database);
            return printer;
        }

        private string GetDrawingBlkName()
        {
            return Config.IfcFileName + Guid.NewGuid().ToString();
        }

        private DBObjectCollection GetBelowObjs(List<ThGeometry> geos)
        {
            var polygons = new DBObjectCollection();
            var belowColumns = geos.GetBelowStandardColumnGeos();
            var belowShearWalls = geos.GetBelowStandardShearwallGeos();
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
                if(File.Exists(svgFile))
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
    }
}
