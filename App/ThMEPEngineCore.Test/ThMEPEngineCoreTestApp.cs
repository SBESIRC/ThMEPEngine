using System;
using System.IO;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.BeamInfo.Utils;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using System.Text.RegularExpressions;
using System.Linq;
using DotNetARX;
using GeometryExtensions;
using ThMEPEngineCore.Diagnostics;
using NetTopologySuite.Geometries;
using NFox.Cad;
using ThMEPEngineCore.Algorithm.FrameComparer;

namespace ThMEPEngineCore.Test
{
    public class ThMEPEngineCoreTestApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        /// <summary>
        /// 提取指定区域内的梁信息
        /// </summary>
        [CommandMethod("TIANHUACAD", "THGETBEAMINFO", CommandFlags.Modal)]
        public void THGETBEAMINFO()
        {
            // 选择楼层区域
            // 暂时只支持矩形区域
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                DBObjectCollection curves = new DBObjectCollection();
                var selRes = Active.Editor.GetSelection();
                foreach (ObjectId objId in selRes.Value.GetObjectIds())
                {
                    curves.Add(acadDatabase.Element<Curve>(objId));
                }
                var spatialIndex = new ThCADCoreNTSSpatialIndex(curves);
                Point3d pt1 = Active.Editor.GetPoint("select left down point: ").Value;
                Point3d pt2 = Active.Editor.GetPoint("select right up point: ").Value;
                DBObjectCollection filterCurves = spatialIndex.SelectCrossingWindow(pt1, pt2);
                ThDistinguishBeamInfo thDisBeamInfo = new ThDistinguishBeamInfo();
                var beams = thDisBeamInfo.CalBeamStruc(filterCurves);
                foreach (var beam in beams)
                {
                    acadDatabase.ModelSpace.Add(beam.BeamBoundary);
                }
            }
        }
        /// <summary>
        /// 提取所选图元的梁信息
        /// </summary>
        [CommandMethod("TIANHUACAD", "THGETBEAMINFO2", CommandFlags.Modal)]
        public void THGETBEAMINFO2()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 选择对象
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    RejectObjectsOnLockedLayers = true,
                };

                // 梁线的图元类型
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Arc)).DxfName,
                    RXClass.GetClass(typeof(Line)).DxfName,
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                // 梁线的图元图层
                var layers = ThBeamLayerManager.GeometryLayers(acdb.Database);
                var filter = ThSelectionFilterTool.Build(dxfNames, layers.ToArray());
                var entSelected = Active.Editor.GetSelection(options, filter);
                if (entSelected.Status != PromptStatus.OK)
                {
                    return;
                };

                // 执行操作
                DBObjectCollection dBObjects = new DBObjectCollection();
                foreach (ObjectId obj in entSelected.Value.GetObjectIds())
                {
                    var entity = acdb.Element<Entity>(obj);
                    dBObjects.Add(entity.GetTransformedCopy(Matrix3d.Identity));
                }

                ThDistinguishBeamInfo thDisBeamCommand = new ThDistinguishBeamInfo();
                var beams = thDisBeamCommand.CalBeamStruc(dBObjects);
                using (var acadDatabase = AcadDatabase.Active())
                {
                    foreach (var beam in beams)
                    {
                        acadDatabase.ModelSpace.Add(beam.BeamBoundary);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THBE", CommandFlags.Modal)]
        public void ThBuildElement()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var hyperlinks = acadDatabase.Element<Entity>(result.ObjectId).Hyperlinks;
                var buildElement = ThPropertySet.CreateWithHyperlink(hyperlinks[0].Description);
            }
        }

        [CommandMethod("TIANHUACAD", "ThArcBeamOutline", CommandFlags.Modal)]
        public void ThArcBeamOutline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new List<Arc>();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Arc>(obj));
                }

                Polyline polyline = ThArcBeamOutliner.TessellatedOutline(objs[0], objs[1]);
                polyline.ColorIndex = 1;
                acadDatabase.ModelSpace.Add(polyline);
            }
        }

        [CommandMethod("TIANHUACAD", "THPOLYGONPARTITION", CommandFlags.Modal)]
        public void THPolygonPartition()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Polyline>(obj));
                }
                foreach (Polyline item in objs)
                {
                    var polylines = ThMEPPolygonPartitioner.PolygonPartition(item);
                    //polylines.ColorIndex = 1;
                    //acadDatabase.ModelSpace.Add(polylines);
                    foreach (var obj in polylines)
                    {
                        obj.ColorIndex = 1;
                        acadDatabase.ModelSpace.Add(obj);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THLineSimplifer", CommandFlags.Modal)]
        public void THLineSimplifer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }
                var lines = ThMEPLineExtension.LineSimplifier(objs, 5.0, 20.0, 2.0, Math.PI / 180.0);
                foreach (var obj in lines)
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THHatchPrint", CommandFlags.Modal)]
        public void THHatchPrint()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var hatchRes = Active.Editor.GetEntity("\nselect a hatch");
                Hatch hatch = acadDatabase.Element<Hatch>(hatchRes.ObjectId);
                hatch.Boundaries().ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "THLineMergeTest", CommandFlags.Modal)]
        public void THLineMergeTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var lineRes = Active.Editor.GetSelection();
                if (lineRes.Status != PromptStatus.OK)
                {
                    return;
                }
                List<Line> lines = new List<Line>();
                lineRes.Value.GetObjectIds().ForEach(o => lines.Add(acadDatabase.Element<Line>(o)));
                var newLines = ThLineMerger.Merge(lines);
                newLines.ForEach(o =>
                {
                    o.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(o);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THTestIsCollinear", CommandFlags.Modal)]
        public void THTestIsCollinear()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var line1Res = Active.Editor.GetEntity("\nselect first line");
                var line2Res = Active.Editor.GetEntity("\nselect second line");
                Line line1 = acadDatabase.Element<Line>(line1Res.ObjectId);
                Line line2 = acadDatabase.Element<Line>(line2Res.ObjectId);
                if (ThGeometryTool.IsCollinearEx(
                    line1.StartPoint, line1.EndPoint, line2.StartPoint, line2.EndPoint))
                {
                    Active.Editor.WriteMessage("共线");
                }
                else
                {
                    Active.Editor.WriteMessage("不共线");
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THRemoveDangles", CommandFlags.Modal)]
        public void THRemoveDangles()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var objs = Active.Editor.GetSelection();
                if (objs.Status != PromptStatus.OK)
                {
                    return;
                }
                var result = new DBObjectCollection();
                foreach (var obj in objs.Value.GetObjectIds())
                {
                    result.Add(acadDatabase.Element<Curve>(obj));
                }
                var lines = ThLaneLineSimplifier.RemoveDangles(result, 100);
                foreach (var obj in lines)
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THTESTMIR", CommandFlags.Modal)]
        public void THTestMaximumRactangle()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                //获取外包框
                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                foreach (var pline in frameLst)
                {
                    ThMEPMaximumInscribedRectangle thMaximumInscribedRectangle = new ThMEPMaximumInscribedRectangle();
                    var rectangle = thMaximumInscribedRectangle.GetRectangle(pline);
                    acdb.ModelSpace.Add(rectangle);
                }
            }
        }



        [CommandMethod("TIANHUACAD", "THTESTMD", CommandFlags.Modal)]
        public void THTestMaximumRactangleByDir()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                // 获取框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                //获取外包框
                List<Polyline> frameLst = new List<Polyline>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }

                foreach (var pFrame in frameLst)
                {
                    var pline = pFrame.Clone() as Polyline;
                    var allLines = new List<Line>();
                    for (int i = 1; i < pline.NumberOfVertices; i++)
                    {
                        allLines.Add(new Line(pline.GetPoint3dAt(i - 1), pline.GetPoint3dAt(i)));
                    }
                    var longestLine = allLines.OrderByDescending(x => x.Length).First();
                    var dir = (longestLine.EndPoint - longestLine.StartPoint).GetNormal();
                    ThMEPMaximumInscribedRectangleByDirection thMEPMaximumInscribedRectangleByDirection = new ThMEPMaximumInscribedRectangleByDirection();
                    var rectangle = thMEPMaximumInscribedRectangleByDirection.GetRectangle(pline, dir);
                    if (rectangle != null)
                    {
                        acdb.ModelSpace.Add(rectangle);
                        rectangle.Layer = "0";
                        rectangle.ColorIndex = (int)ColorIndex.Blue;
                    }
                    else
                    {
                        //分割段太小，找不到最大内接矩形
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THTestOldBuildingExtractor", CommandFlags.Modal)]
        public void THTestOldBuildingExtractor()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                ThStopWatchService.Start();
                //建筑墙
                var db3ArchWallEngine = new ThDB3ArchWallExtractionEngine();
                var shearWallEngine = new ThShearWallExtractionEngine();
                var db3ShearWallEngine = new ThDB3ShearWallExtractionEngine();
                var columnEngine = new ThColumnExtractionEngine();
                var db3ColumnEngine = new ThDB3ColumnExtractionEngine();


                db3ArchWallEngine.Extract(acadDatabase.Database);
                shearWallEngine.Extract(acadDatabase.Database);
                db3ShearWallEngine.Extract(acadDatabase.Database);
                columnEngine.Extract(acadDatabase.Database);
                db3ColumnEngine.Extract(acadDatabase.Database);
                ThStopWatchService.Stop();
                var shearWallCount = shearWallEngine.Results.Count + db3ShearWallEngine.Results.Count;
                var columnCount = columnEngine.Results.Count + db3ColumnEngine.Results.Count;
                Active.Editor.WriteMessage("\n建筑墙数量：" + db3ArchWallEngine.Results.Count + "个");
                Active.Editor.WriteMessage("\n剪力墙数量：" + shearWallCount + "个");
                Active.Editor.WriteMessage("\n柱子数量：" + columnCount + "个");
                Active.Editor.WriteMessage("\n耗时："+ ThStopWatchService.TimeSpan()+"秒");
                //results.Cast<Entity>().ForEach(o =>
                //{
                //    acadDatabase.ModelSpace.Add(o);
                //    o.SetDatabaseDefaults();
                //});
            }
        }
        [CommandMethod("TIANHUACAD", "THTestNewBuildingExtractor", CommandFlags.Modal)]
        public void THTestNewBuildingExtractor()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                ThStopWatchService.Start();
                var archWallVisitor = new ThDB3ArchWallExtractionVisitor()
                {
                    LayerFilter = ThArchitectureWallLayerManager.CurveXrefLayers(acdb.Database),
                };
                var pcArchWallVisitor = new ThDB3ArchWallExtractionVisitor()
                {
                    LayerFilter = ThPCArchitectureWallLayerManager.CurveXrefLayers(acdb.Database),
                };
                var shearWallVisitor = new ThShearWallExtractionVisitor()
                {
                    LayerFilter = ThStructureShearWallLayerManager.HatchXrefLayers(acdb.Database),
                };
                var db3ShearWallVisitor = new ThDB3ShearWallExtractionVisitor();
                var columnVisitor = new ThColumnExtractionVisitor()
                {
                    LayerFilter = ThStructureColumnLayerManager.HatchXrefLayers(acdb.Database),
                };
                var db3ColumnVisitor = new ThDB3ColumnExtractionVisitor();
                var extractor = new ThBuildingElementExtractor();
                extractor.Accept(archWallVisitor);
                extractor.Accept(pcArchWallVisitor);
                extractor.Accept(shearWallVisitor);
                extractor.Accept(db3ShearWallVisitor);
                extractor.Accept(columnVisitor);
                extractor.Accept(db3ColumnVisitor);
                extractor.Extract(acdb.Database);

                ThStopWatchService.Stop();
                Active.Editor.WriteMessage("\n建筑墙数量：" + 
                    (archWallVisitor.Results.Count+ pcArchWallVisitor.Results.Count) + "个");
                Active.Editor.WriteMessage("\n剪力墙数量：" +
                    (shearWallVisitor.Results.Count + db3ShearWallVisitor.Results.Count) + "个");
                Active.Editor.WriteMessage("\n柱子数量：" +
                    (columnVisitor.Results.Count + db3ColumnVisitor.Results.Count) + "个");
                Active.Editor.WriteMessage("\n耗时：" + ThStopWatchService.TimeSpan() + "秒");
                Active.Editor.WriteMessage("\n耗时：" + ThStopWatchService.TimeSpan() + "秒");
            }
        }

        [CommandMethod("TIANHUACAD", "ThBuildMPolygon", CommandFlags.Modal)]
        public void ThBuildMPolygon()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var shellPER = Active.Editor.GetEntity("\n选择洞的外壳");
                if (shellPER.Status != PromptStatus.OK)
                {
                    return;
                }
                var prompOptions = new PromptSelectionOptions();
                prompOptions.MessageForAdding = "\n选择洞";
                var holesPsr = Active.Editor.GetSelection(prompOptions);
                if (holesPsr.Status != PromptStatus.OK)
                {
                    return;
                }
                var shell = acdb.Element<Polyline>(shellPER.ObjectId);

                //获取洞
                var holes = new List<Curve>();
                foreach (ObjectId obj in holesPsr.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    holes.Add(frame.Clone() as Polyline);
                }
                var mPolygon = ThMPolygonTool.CreateMPolygon(shell, holes);
                acdb.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();
            }
        }
        [CommandMethod("TIANHUACAD", "ThBuildMPolygonCenterLine", CommandFlags.Modal)]
        public void ThBuildMPolygonCenterLine()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                MPolygon mPolygon = getMpolygon();

                PromptDoubleResult result2 = Active.Editor.GetDistance("\n请输入差值距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }
                var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(mPolygon.ToNTSPolygon(), result2.Value);
                //删除之前生成的带动多边形，以防影响之后操作
                mPolygon.UpgradeOpen();
                mPolygon.Erase();
                mPolygon.DowngradeOpen();

                // 生成、显示中线
                centerlines.Cast<Entity>().ToList().CreateGroup(acdb.Database, 1);
            }
        }

        public static MPolygon getMpolygon()
        {
            MPolygon mPolygon;
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return null;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acdb.Element<Entity>(obj));
                }
                mPolygon = objs.BuildMPolygon();
                acdb.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();
            }
            return mPolygon;
        }

        [CommandMethod("TIANHUACAD", "THNodingTest", CommandFlags.Modal)]
        public void THNodingTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                //认为输出结果是线段的集合
                var objs = new List<Line>();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Line>(obj));
                }

                var res = NodingLines(objs.ToCollection());
                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(
                res.Cast<Entity>().Select(o => o.Clone() as Entity).ToList(),
                AcHelper.Active.Database, 1);
            }
        }
        private List<Line> NodingLines(DBObjectCollection curves)
        {
            var results = new List<Line>();
            var geometry = curves.ToNTSNodedLineStrings();
            if (geometry is LineString line)
            {
                results.Add(line.ToDbline());
            }
            else if (geometry is MultiLineString lines)
            {
                results.AddRange(lines.Geometries.Cast<LineString>().Select(o => o.ToDbline()));
            }
            else
            {
                throw new NotSupportedException();
            }
            return results;
        }
    }
}