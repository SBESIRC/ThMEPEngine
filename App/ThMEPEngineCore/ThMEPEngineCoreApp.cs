using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.BeamInfo.Business;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Newtonsoft.Json;
using ThCADExtension;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THExtractColumn", CommandFlags.Modal)]
        public void ThExtractColumn()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var columnRecognitionEngine = new ThColumnRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                columnRecognitionEngine.Recognize(acadDatabase.Database, frame.Vertices());
                columnRecognitionEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeam", CommandFlags.Modal)]
        public void ThExtractBeam()
        {
            using (var acadDatabase = AcadDatabase.Active())
            using (var beamEngine = new ThBeamRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                beamEngine.Recognize(Active.Database, frame.Vertices());
                beamEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeamText", CommandFlags.Modal)]
        public void ThExtractBeamText()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var beamTextDbExtension = new ThStructureBeamTextDbExtension(Active.Database))
            {
                beamTextDbExtension.BuildElementTexts();
                beamTextDbExtension.BeamTexts.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractShearWall", CommandFlags.Modal)]
        public void THExtractShearWall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                shearWallEngine.Recognize(acadDatabase.Database, frame.Vertices());
                shearWallEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnect", CommandFlags.Modal)]
        public void ThExtractBeamConnect()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var thBeamTypeRecogitionEngine = new ThBeamConnectRecogitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                thBeamTypeRecogitionEngine.Recognize(Active.Database, frame.Vertices());
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                Active.Editor.WriteMessage("\n本次使用了：" + timespan.TotalSeconds + "秒");

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_PRIMARY;
                }));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_HALFPRIMARY;
                }));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_OVERHANGINGPRIMARY;
                }));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_SECONDARY;
                }));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));

                List<ThIfcBeam> allBeams = new List<ThIfcBeam>();
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => allBeams.AddRange(m.Beams));

                // 输出GeoJson文件
                // 梁
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.beam.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThBeamGeoJsonWriter();
                    geoJsonWriter.Write(allBeams, writer);
                }

                // 柱
                var columns = thBeamTypeRecogitionEngine.ColumnEngine.Elements.Cast<ThIfcColumn>();
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.column.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThColumnGeoJsonWriter();
                    geoJsonWriter.Write(columns.ToList(), writer);
                }

                // 剪力墙
                var shearWalls = thBeamTypeRecogitionEngine.ShearWallEngine.Elements.Cast<ThIfcWall>();
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.shearwall.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThShearWallGeoJsonWriter();
                    geoJsonWriter.Write(shearWalls.ToList(), writer);
                }
            }
        }
        private DBText CreateBeamMarkText(ThIfcBeam thIfcBeam)
        {
            string message = "";
            string beamtype = "未定义";
            switch (thIfcBeam.ComponentType)
            {
                case BeamComponentType.PrimaryBeam:
                    beamtype = "主梁";
                    break;
                case BeamComponentType.HalfPrimaryBeam:
                    beamtype = "半主梁";
                    break;
                case BeamComponentType.OverhangingPrimaryBeam:
                    beamtype = "悬挑主梁";
                    break;
                case BeamComponentType.SecondaryBeam:
                    beamtype = "次梁";
                    break;
            }
            message += beamtype + "，";
            message += thIfcBeam.Width + "，";
            message += thIfcBeam.Height;
            DBText dbText = new DBText();
            dbText.TextString = message;
            dbText.Position = ThGeometryTool.GetMidPt(thIfcBeam.StartPoint, thIfcBeam.EndPoint);
            dbText.Rotation = thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint).GetAngleTo(Vector3d.XAxis);
            dbText.Layer = "0";
            dbText.Height = 200;
            return dbText;
        }
        [CommandMethod("TIANHUACAD", "ThExtractLaneLine", CommandFlags.Modal)]
        public void ThExtractLaneLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneLineRecognitionEngine laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                laneLineEngine.Recognize(Active.Database);
                laneLineEngine.Lanes.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractDivideBeam", CommandFlags.Modal)]
        public void ThExtractDivdeBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThIfcLineBeam thIfcLineBeam = new ThIfcLineBeam();
                thIfcLineBeam.StartPoint = Active.Editor.GetPoint("\n Select beam start point：").Value;
                thIfcLineBeam.EndPoint = Active.Editor.GetPoint("\n Select beam end point：").Value;
                thIfcLineBeam.Outline = acadDatabase.Element<Polyline>(Active.Editor.GetEntity("\n Select beam outline：").ObjectId);
                thIfcLineBeam.ComponentType = BeamComponentType.PrimaryBeam;
                thIfcLineBeam.Width = 300;
                thIfcLineBeam.Height = 400;
                thIfcLineBeam.Uuid = Guid.NewGuid().ToString();
                var components = Active.Editor.GetSelection();
                List<ThSegment> segments = new List<ThSegment>();
                foreach (ObjectId objId in components.Value.GetObjectIds())
                {
                    ThSegmentService thSegmentService = new ThSegmentService(acadDatabase.Element<Polyline>(objId));
                    thSegmentService.SegmentAll(new CalBeamStruService());
                    segments.AddRange(thSegmentService.Segments);
                }
                ThLinealBeamSplitter thSplitLineBeam = new ThLinealBeamSplitter(thIfcLineBeam, segments);
                thSplitLineBeam.Split();
                thSplitLineBeam.SplitBeams.ForEach(o => o.Outline.ColorIndex = 1);
                thSplitLineBeam.SplitBeams.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
            }
        }
        [CommandMethod("TIANHUACAD", "ThTestSegment", CommandFlags.Modal)]
        public void ThTestSegment()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entRes = Active.Editor.GetEntity("\n select a polyline");
                Polyline polyline = acadDatabase.Element<Polyline>(entRes.ObjectId);
                DBObjectCollection objs = new DBObjectCollection();
                objs.Add(acadDatabase.Element<Polyline>(entRes.ObjectId));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var otherRes = Active.Editor.GetEntity("\nselect a polyline");
                Polyline otherPolyline = acadDatabase.Element<Polyline>(otherRes.ObjectId);
                Point3dCollection pts = new Point3dCollection();
                for (int i = 0; i < otherPolyline.NumberOfVertices; i++)
                {
                    pts.Add(otherPolyline.GetPoint3dAt(0));
                }
                var selObjs = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(pts);
                bool res = polyline.Intersects(otherPolyline);
                ThSegmentService thSegmentService = new ThSegmentService(polyline);
                thSegmentService.SegmentAll(new CalBeamStruService());
                thSegmentService.Segments.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
            }
        }
        [CommandMethod("TIANHUACAD", "ThTestPointIn", CommandFlags.Modal)]
        public void ThTestPointIn()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entRes = Active.Editor.GetEntity("\n select a polyline");
                Polyline polyline = acadDatabase.Element<Polyline>(entRes.ObjectId);
                var ptRes = Active.Editor.GetPoint("\n select a point");
                int isIn = polyline.PointInPolylineEx(ptRes.Value, 1.0);
                if (isIn == 0)
                {
                    Active.Editor.WriteLine("CheckPointInPolyline: 点在polyline上");
                }
                else if (isIn == 1)
                {
                    Active.Editor.WriteLine("CheckPointInPolyline: 点在polyline内");
                }
                else if (isIn == -1)
                {
                    Active.Editor.WriteLine("CheckPointInPolyline: 点在polyline外");
                }
                Ray ray = new Ray();
                ray.BasePoint = Point3d.Origin;
                ray.SecondPoint = new Point3d(1000, 0, 0);
                ray.UnitDir = Vector3d.XAxis;
                acadDatabase.ModelSpace.Add(ray);
            }
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
        [CommandMethod("TIANHUACAD", "ThTestSplitBeam", CommandFlags.Modal)]
        public void ThTestSplitBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entRes = Active.Editor.GetEntity("\n 选择梁的外轮廓");
                if(entRes.Status!=PromptStatus.OK)
                {
                    return;
                }
                Polyline polyline = acadDatabase.Element<Polyline>(entRes.ObjectId);
                var startRes = Active.Editor.GetPoint("\n 选择梁的起点");
                if (startRes.Status != PromptStatus.OK)
                {
                    return;
                }
                var endRes = Active.Editor.GetPoint("\n 选择梁的终点");
                if (endRes.Status != PromptStatus.OK)
                {
                    return;
                }    
                var outlineRes = Active.Editor.GetSelection();
                if (outlineRes.Status == PromptStatus.OK)
                {
                    List<ThSegment> segments = new List<ThSegment>();
                    var outline = acadDatabase.Element<Polyline>(entRes.ObjectId);
                    var thIfcLineBeam = new ThIfcLineBeam()
                    {
                        StartPoint = startRes.Value,
                        EndPoint = endRes.Value,
                        Outline = outline.Clone() as Polyline
                    };
                    foreach (var objId in outlineRes.Value.GetObjectIds())
                    {
                        var segment = acadDatabase.Element<Polyline>(objId);
                        segments.Add(
                            new ThLinearSegment
                            {
                                Outline = segment.Clone() as Polyline,
                                StartPoint = Active.Editor.GetPoint("\n选择梁的起点").Value,
                                EndPoint = Active.Editor.GetPoint("\n选择梁的终点").Value,
                            });
                    }
                    ThLinealBeamSplitter thLinealBeamSplitter = new ThLinealBeamSplitter(thIfcLineBeam as ThIfcLineBeam, segments);
                    thLinealBeamSplitter.SplitTType();
                    thLinealBeamSplitter.SplitBeams.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
                }
            }
        }
    }
}
