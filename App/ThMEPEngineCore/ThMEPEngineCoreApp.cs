using System;
using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.IO.GeoJSON;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
            ThMPolygonTool.Initialize();
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THVStructuralElement", CommandFlags.Modal)]
        public void THVStructuralElement()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var nFrame = ThMEPFrameService.Normalize(frame);
                var engine = new ThVStructuralElementRecognitionEngine();
                engine.Recognize(acadDatabase.Database, nFrame.Vertices());
                engine.Elements.Where(o => o is ThIfcColumn).ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                    o.Outline.SetDatabaseDefaults();
                    o.Outline.ColorIndex = 4;
                });
                engine.Elements.Where(o => o is ThIfcWall).ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                    o.Outline.SetDatabaseDefaults();
                    o.Outline.ColorIndex = 6;
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractColumn", CommandFlags.Modal)]
        public void ThExtractColumn()
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

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Default = "提取";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var options2 = new PromptKeywordOptions("\n选择数据来源");
                options2.Keywords.Add("原始", "O", "原始(O)");
                options2.Keywords.Add("平台", "P", "平台(P)");
                options2.Keywords.Add("全部", "A", "全部(A)");
                options2.Keywords.Default = "全部";
                var result3 = Active.Editor.GetKeywords(options2);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                if(result2.StringResult == "识别")
                {
                    if (result3.StringResult == "原始")
                    {
                        var engine = new ThColumnRecognitionEngine();
                        engine.Recognize(acadDatabase.Database, frame.Vertices());
                        engine.Elements.Select(o => o.Outline).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                    else if (result3.StringResult == "平台")
                    {
                        var engine = new ThDB3ColumnRecognitionEngine();
                        engine.Recognize(acadDatabase.Database, frame.Vertices());
                        engine.Elements.Select(o => o.Outline).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                    else if (result3.StringResult == "全部")
                    {
                        var elements = new List<ThIfcBuildingElement>();
                        var engine1 = new ThColumnRecognitionEngine();
                        engine1.Recognize(acadDatabase.Database, frame.Vertices());
                        elements.AddRange(engine1.Elements);
                        var engine2 = new ThDB3ColumnRecognitionEngine();
                        engine2.Recognize(acadDatabase.Database, frame.Vertices());
                        elements.AddRange(engine2.Elements);
                        elements.Select(o => o.Outline)
                            .ToCollection()
                            .UnionPolygons()
                            .Cast<Entity>()
                            .ForEach(o =>
                            {
                                acadDatabase.ModelSpace.Add(o);
                                o.SetDatabaseDefaults();
                            });
                    }
                }
                else
                {
                    if (result3.StringResult == "原始")
                    {
                        var engine = new ThColumnExtractionEngine();
                        engine.Extract(acadDatabase.Database, frame);
                        engine.Results.Select(o => o.Geometry).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                    else if (result3.StringResult == "平台")
                    {
                        var db3Engine = new ThDB3ColumnExtractionEngine();
                        db3Engine.Extract(acadDatabase.Database, frame);
                        db3Engine.Results.Select(o => o.Geometry).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                    else if (result3.StringResult == "全部")
                    {
                        var engine = new ThColumnExtractionEngine();
                        engine.Extract(acadDatabase.Database, frame);
                        engine.Results.Select(o => o.Geometry).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                        var db3Engine = new ThDB3ColumnExtractionEngine();
                        db3Engine.Extract(acadDatabase.Database, frame);
                        db3Engine.Results.Select(o => o.Geometry).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractShearWall", CommandFlags.Modal)]
        public void THExtractShearWall()
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

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Default = "提取";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var options2 = new PromptKeywordOptions("\n选择数据来源");
                options2.Keywords.Add("原始", "O", "原始(O)");
                options2.Keywords.Add("平台", "P", "平台(P)");
                options2.Keywords.Add("全部", "A", "全部(A)");
                options2.Keywords.Default = "全部";
                var result3 = Active.Editor.GetKeywords(options2);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                if (result2.StringResult == "识别")
                {
                    if (result3.StringResult == "原始")
                    {
                        var engine = new ThShearWallRecognitionEngine();
                        engine.Recognize(acadDatabase.Database, frame.Vertices());
                        engine.Elements.Select(o => o.Outline).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                    else if (result3.StringResult == "平台")
                    {
                        var engine = new ThDB3ShearWallRecognitionEngine();
                        engine.Recognize(acadDatabase.Database, frame.Vertices());
                        engine.Elements.Select(o => o.Outline).ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            o.SetDatabaseDefaults();
                        });
                    }
                    else if (result3.StringResult == "全部")
                    {
                        var elements = new List<ThIfcBuildingElement>();
                        var engine1 = new ThShearWallRecognitionEngine();
                        engine1.Recognize(acadDatabase.Database, frame.Vertices());
                        elements.AddRange(engine1.Elements);
                        var engine2 = new ThDB3ShearWallRecognitionEngine();
                        engine2.Recognize(acadDatabase.Database, frame.Vertices());
                        elements.AddRange(engine2.Elements);
                        elements.Select(o => o.Outline)
                            .ToCollection()
                            .UnionPolygons()
                            .Cast<Entity>()
                            .ForEach(o =>
                            {
                                acadDatabase.ModelSpace.Add(o);
                                o.SetDatabaseDefaults();
                            });
                    }
                }
                else
                {
                    var engine = new ThDB3ShearWallExtractionEngine();
                    engine.Extract(acadDatabase.Database);
                    var results = new DBObjectCollection();
                    var spatialIndex = new ThCADCoreNTSSpatialIndexEx(engine.Results.Select(o => o.Geometry).ToCollection());
                    foreach (var filterObj in spatialIndex.SelectCrossingPolygon(frame))
                    {
                        results.Add(filterObj as Entity);
                    }
                    results.Cast<Entity>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractBeam", CommandFlags.Modal)]
        public void ThExtractBeam()
        {
            using (var acadDatabase = AcadDatabase.Active())
            using (var beamEngine = ThMEPEngineCoreService.Instance.CreateBeamEngine())
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

        [CommandMethod("TIANHUACAD", "THExtractArchWall", CommandFlags.Modal)]
        public void THExtractArchWall()
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

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Default = "提取";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                if (result2.StringResult == "识别")
                {
                    var engine = new ThDB3ArchWallRecognitionEngine();
                    engine.Recognize(acadDatabase.Database, frame.Vertices());
                    engine.Elements.Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                {
                    var engine = new ThDB3ArchWallExtractionEngine();
                    engine.Extract(acadDatabase.Database);
                    var results = new DBObjectCollection();
                    var spatialIndex = new ThCADCoreNTSSpatialIndexEx(engine.Results.Select(o => o.Geometry).ToCollection());
                    foreach (var filterObj in spatialIndex.SelectCrossingPolygon(frame))
                    {
                        results.Add(filterObj as Entity);
                    }
                    results.Cast<Entity>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractCurtainWall", CommandFlags.Modal)]
        public void THExtractCurtainWall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var nFrame = ThMEPFrameService.Normalize(frame);
                var engine = new ThCurtainWallExtractionEngine();
                engine.Extract(acadDatabase.Database);
                var results = new DBObjectCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndexEx(engine.Results.Select(o => o.Geometry).ToCollection());
                foreach (var filterObj in spatialIndex.SelectCrossingPolygon(nFrame))
                {
                    results.Add(filterObj as Entity);
                }
                results.Cast<Entity>().ForEach(o =>
                {
                    o.SetDatabaseDefaults();
                    acadDatabase.ModelSpace.Add(o);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractWindow", CommandFlags.Modal)]
        public void THExtractWindow()
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

                var windowEngine = new ThDB3WindowRecognitionEngine();
                windowEngine.Recognize(acadDatabase.Database, frame.Vertices());
                var objIds = new ObjectIdList();
                windowEngine.Elements.ForEach(o =>
                {
                    if (o.Outline is Curve curve)
                    {
                        var clone = curve.WashClone();
                        if(clone!=null)
                        {
                            clone.ColorIndex = 6;
                            clone.SetDatabaseDefaults();
                            objIds.Add(acadDatabase.ModelSpace.Add(clone));
                        }
                    }
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractParkingStall", CommandFlags.Modal)]
        public void ThExtractParkingStall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var engine = new ThParkingStallRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                engine.Recognize(acadDatabase.Database, frame.Vertices());
                engine.Elements.Cast<ThIfcParkingStall>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Boundary);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnect", CommandFlags.Modal)]
        public void ThExtractBeamConnect()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                var thBeamTypeRecogitionEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(
                    Active.Database, frame.Vertices());
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
            dbText.SetDatabaseDefaults(Active.Database);
            dbText.TextString = message;
            dbText.Position = ThGeometryTool.GetMidPt(thIfcBeam.StartPoint, thIfcBeam.EndPoint);
            Vector3d dir = Vector3d.XAxis.CrossProduct(thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint));
            if (dir.Z >= 0)
            {
                dbText.Rotation = thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint).GetAngleTo(Vector3d.XAxis);
            }
            else
            {
                dbText.Rotation = thIfcBeam.EndPoint.GetVectorTo(thIfcBeam.StartPoint).GetAngleTo(Vector3d.XAxis) + Math.PI;
            }
            dbText.Layer = "0";
            dbText.Height = 200;
            return dbText;
        }

        [CommandMethod("TIANHUACAD", "THExtractDoor", CommandFlags.Modal)]
        public void THExtractDoor()
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
                
                var doorEngine = new ThDB3DoorRecognitionEngine();
                doorEngine.Recognize(acadDatabase.Database, frame.Vertices());
                doorEngine.Elements.ForEach(o =>
                {
                    o.Outline.ColorIndex = 4;
                    o.Outline.SetDatabaseDefaults();
                    acadDatabase.ModelSpace.Add(o.Outline);
                });

            }
        }

        [CommandMethod("TIANHUACAD", "THExtractSlab", CommandFlags.Modal)]
        public void THExtractSlab()
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

                ThDB3SlabRecognitionEngine floorEngine = new ThDB3SlabRecognitionEngine();
                floorEngine.Recognize(acadDatabase.Database, frame.Vertices());
                floorEngine.Elements.ForEach(o =>
                {
                    o.Outline.ColorIndex = 6;
                    o.Outline.SetDatabaseDefaults();
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }


        [CommandMethod("TIANHUACAD", "THExtractRailing", CommandFlags.Modal)]
        public void THExtractRailing()
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

                var railingRecognitionEngine = new ThRailingRecognitionEngine();
                railingRecognitionEngine.Recognize(acadDatabase.Database, frame.Vertices());
                railingRecognitionEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractCornice", CommandFlags.Modal)]
        public void THExtractCornice()
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



                ThDB3CorniceRecognitionEngine corniceRecognitionEngine = new ThDB3CorniceRecognitionEngine();
                corniceRecognitionEngine.Recognize(acadDatabase.Database, frame.Vertices());
                corniceRecognitionEngine.Elements.ForEach(o =>
                {
                    var curve = o.Outline as Curve;
                    acadDatabase.ModelSpace.Add(curve.WashClone());
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractLaneline", CommandFlags.Modal)]
        public void THExtractLaneline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneLineRecognitionEngine laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var nFrame = ThMEPFrameService.NormalizeEx(frame);
                if (nFrame.Area > 1)
                {
                    var bFrame = ThMEPFrameService.Buffer(nFrame, 100000.0);
                    laneLineEngine.Recognize(acadDatabase.Database, nFrame.Vertices());
                    var lines = laneLineEngine.Spaces.Select(o => o.Boundary).ToCollection();
                    var centerPt = nFrame.GetCentroidPoint();
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(lines);
                    transformer.Transform(nFrame);

                    var curves = ThLaneLineSimplifier.Simplify(lines, 1500);
                    lines = ThCADCoreNTSGeometryClipper.Clip(nFrame, curves.ToCollection());
                    transformer.Reset(lines);

                    lines.Cast<Curve>().ForEach(o =>
                    {
                        o.ColorIndex = 2;
                        o.SetDatabaseDefaults();
                        acadDatabase.ModelSpace.Add(o);                        
                    });
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractDrainageWell", CommandFlags.Modal)]
        public void THExtractDrainageWell()
        {
            using (var acadDatabase = AcadDatabase.Active())
            using (var curveEngine  = new ThDrainageWellRecognitionEngine())
            using (var blockEngine = new ThDrainageWellBlockRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                if(!(acadDatabase.Element<Entity>(result.ObjectId) is Polyline))
                {
                    return;
                }
                var frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var nFrame = ThMEPFrameService.NormalizeEx(frame);
                if (nFrame.Area > 1)
                {
                    curveEngine.Recognize(acadDatabase.Database, nFrame.Vertices());
                    blockEngine.Recognize(acadDatabase.Database, nFrame.Vertices());
                    var objs = new DBObjectCollection();
                    curveEngine.Geos.ForEach(o => objs.Add(o));
                    blockEngine.Geos.Cast<BlockReference>().ForEach(o =>
                    {
                        ThDrawTool.Explode(o)
                            .Cast<Entity>()
                            .Where(p => p is Line || p is Polyline)
                            .ForEach(p => objs.Add(p));
                    });

                    var originIds = new ObjectIdList();
                    objs.Cast<Entity>().ForEach(o =>
                    {
                        o.ColorIndex = 4;
                        o.SetDatabaseDefaults();
                        originIds.Add(acadDatabase.ModelSpace.Add(o));
                    });
                    if (originIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), originIds);
                    }


                    var breakService = new ThBreakDrainageFacilityService();
                    breakService.Break(objs);

                    var drainageWellIds = new ObjectIdList();
                    breakService.CollectingWells.ForEach(o =>
                    {
                        o.ColorIndex = 5;
                        o.SetDatabaseDefaults();
                        drainageWellIds.Add(acadDatabase.ModelSpace.Add(o));
                    });
                    if(drainageWellIds.Count>0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), drainageWellIds);
                    }                    

                    var drainageDitchIds = new ObjectIdList();
                    breakService.DrainageDitches.ForEach(o =>
                    {
                        o.ColorIndex = 6;
                        o.SetDatabaseDefaults();
                        drainageDitchIds.Add(acadDatabase.ModelSpace.Add(o));
                    });
                    if(drainageDitchIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), drainageDitchIds);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractAXISLine", CommandFlags.Modal)]
        public void THExtractAXISLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var nFrame = ThMEPFrameService.Normalize(frame);
                var engine = new ThAXISLineRecognitionEngine();
                engine.Recognize(acadDatabase.Database, nFrame.Vertices());
                engine.Elements.Select(o => o.Outline).ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.SetDatabaseDefaults();
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractRoom", CommandFlags.Modal)]
        public void THExtractRoom()
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
                var builder = new ThRoomBuilderEngine();
                var rooms = builder.BuildFromMS(acadDatabase.Database, frame.Vertices());
                rooms.Select(o => o.Boundary).ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.SetDatabaseDefaults();
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractContourLine", CommandFlags.Modal)]
        public void THExtractContourLine()
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

                var options = new PromptKeywordOptions("\n选择处理模式");
                options.Keywords.Add("建筑模式", "A", "建筑模式(A)");
                options.Keywords.Add("结构模式", "S", "结构模式(S)");
                options.Keywords.Default = "结构模式";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                if (result2.StringResult == "建筑模式")
                {
                    var data = new Model1Data(acadDatabase.Database, frame.Vertices());
                    var builder = new ThArchitectureOutlineBuilder(data.MergeData());
                    builder.Build();
                    builder.Results.Cast<Entity>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                {
                    var data = new Model2Data(acadDatabase.Database, frame.Vertices());
                    var builder = new ThArchitectureOutlineBuilder(data.MergeData());
                    builder.Build();
                    builder.Results.Cast<Entity>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDB3ExtractRoom", CommandFlags.Modal)]
        public void THDB3ExtractRoom()
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


                //var selectRes = Active.Editor.GetSelection();
                //var testDatas = new DBObjectCollection();
                //if (selectRes.Status == PromptStatus.OK)
                //{
                //    var Datas = selectRes.Value.GetObjectIds()
                //        .Select(o => acadDatabase.Element<Curve>(o).Clone() as Curve)
                //        .ToCollection();
                //    Datas.Polygonize().ForEach(o =>
                //    {
                //        foreach(DBObject obj in o.ToDbCollection())
                //        {
                //            testDatas.Add(obj);
                //        }
                //    });
                //    Datas = Datas.FilterSmallArea(10.0);
                //    testDatas = testDatas.FilterSmallArea(10.0);
                //}
                //else
                //{
                //    var data = new Roomdata(acadDatabase.Database, frame.Vertices());
                //    data.Deburring();
                //    testDatas = data.MergeData();
                //}
                Roomdata data = new Roomdata(acadDatabase.Database, frame.Vertices());
                data.Deburring();
                var builder = new ThRoomOutlineBuilderEngine(data.MergeData());
                if (builder.Count == 0)
                    return;
                //从CAD中获取点

                var ptList = new List<Point3d>();
                while(true)
                {
                    var ptRes = Active.Editor.GetPoint("\n选择房间内的一点");
                    if(ptRes.Status==PromptStatus.OK)
                    {
                        ptList.Add(ptRes.Value);
                    }
                    else
                    {
                        break;
                    }
                }
                if(ptList.Count>0)
                {
                    builder.Build(ptList).Cast<Entity>().ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
            }
        }
    }
}
