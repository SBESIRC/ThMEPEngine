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
    public class ThMEPEngineCoreExtractCmds
    {

        [CommandMethod("TIANHUACAD", "THExtractColumn", CommandFlags.Modal)]
        public void  ThExtractColumn()
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
                        var columnBuilder = new ThColumnBuilderEngine();
                        columnBuilder.Build(acadDatabase.Database, frame.Vertices());
                        columnBuilder.Elements
                            .Select(o => o.Outline)
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
                        var db3Engine = new ThDB3ColumnExtractionEngine()
                        {
                            RangePts = frame.Vertices(),
                        };
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
                        var shearwallBuilder = new ThShearWallBuilderEngine();
                        shearwallBuilder.Build(acadDatabase.Database, frame.Vertices());
                        shearwallBuilder.Elements
                            .Select(o => o.Outline)
                            .ForEach(e =>
                            {
                                acadDatabase.ModelSpace.Add(e);
                                e.SetDatabaseDefaults();
                            });
                    }
                }
                else
                {
                    var results = new DBObjectCollection();
                    if (result3.StringResult == "原始")
                    {
                        var engine1 = new ThShearWallExtractionEngine();
                        engine1.Extract(acadDatabase.Database);
                        engine1.Results.ForEach(o => results.Add(o.Geometry));
                    }
                    else if (result3.StringResult == "平台")
                    {
                        var engine1 = new ThDB3ShearWallExtractionEngine();
                        engine1.Extract(acadDatabase.Database);
                        engine1.Results.ForEach(o => results.Add(o.Geometry));
                    }
                    else if (result3.StringResult == "全部")
                    {
                        var engine1 = new ThShearWallExtractionEngine();
                        engine1.Extract(acadDatabase.Database);
                        engine1.Results.ForEach(o => results.Add(o.Geometry));

                        var engine2 = new ThDB3ShearWallExtractionEngine();
                        engine2.Extract(acadDatabase.Database);
                        engine2.Results.ForEach(o => results.Add(o.Geometry));
                    }
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(results)
                    {
                        AllowDuplicate = true,
                    };
                    spatialIndex.SelectCrossingPolygon(frame).Cast<Entity>().ForEach(o =>
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
                var beamBuilder = new ThBeamBuilderEngine();
                beamBuilder.Build(acadDatabase.Database, frame.Vertices());
                beamBuilder.Elements.ForEach(o =>
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
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(engine.Results.Select(o => o.Geometry).ToCollection())
                    {
                        AllowDuplicate = true,
                    };
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
                var engine = new ThDB3CurtainWallExtractionEngine();
                engine.Extract(acadDatabase.Database);
                var results = new DBObjectCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(engine.Results.Select(o => o.Geometry).ToCollection())
                {
                    AllowDuplicate = true,
                };
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
                        if (clone != null)
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

                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                var thBeamTypeRecogitionEngine = ThBeamConnectRecogitionEngine.ExecuteRecognize(
                    Active.Database, frame.Vertices());
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                Active.Editor.WriteMessage("\n本次使用了：" + timespan.TotalSeconds + "秒");

                var layerId = acadDatabase.Database.CreateAIBeamLayer();
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.LayerId = layerId;
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_PRIMARY;
                }));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.LayerId = layerId;
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_HALFPRIMARY;
                }));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.LayerId = layerId;
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_OVERHANGINGPRIMARY;
                }));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var curve = n.Outline as Curve;
                    var clone = curve.WashClone();
                    acadDatabase.ModelSpace.Add(clone);
                    clone.LayerId = layerId;
                    clone.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_SECONDARY;
                }));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var mark = CreateBeamMarkText(n);
                    acadDatabase.ModelSpace.Add(mark);
                    mark.LayerId = layerId;
                    mark.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_TEXT;
                }));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var mark = CreateBeamMarkText(n);
                    acadDatabase.ModelSpace.Add(mark);
                    mark.LayerId = layerId;
                    mark.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_TEXT;
                }));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var mark = CreateBeamMarkText(n);
                    acadDatabase.ModelSpace.Add(mark);
                    mark.LayerId = layerId;
                    mark.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_TEXT;
                }));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n =>
                {
                    var mark = CreateBeamMarkText(n);
                    acadDatabase.ModelSpace.Add(mark);
                    mark.LayerId = layerId;
                    mark.ColorIndex = ThMEPEngineCoreCommon.COLORINDEX_BEAM_TEXT;
                }));

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

                // 准备图层
                var layerId = acadDatabase.Database.CreateAIDoorLayer();
                if (!layerId.IsValid)
                {
                    return ;
                }

                var doorEngine = new ThDB3DoorRecognitionEngine();
                doorEngine.Recognize(acadDatabase.Database, frame.Vertices());
                doorEngine.Elements.ForEach(o =>
                {
                    o.Outline.LayerId = layerId;
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

                var railingRecognitionEngine = new ThDB3RailingRecognitionEngine();
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

        [CommandMethod("TIANHUACAD", "THExtractConcaveHullBoundaries", CommandFlags.Modal)]
        public void THExtractConcaveHullBoundaries()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                PromptDoubleOptions op = new PromptDoubleOptions("请输入容差值");
                var tol = Active.Editor.GetDouble(op);
                if (tol.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs =  result.Value
                    .GetObjectIds()
                    .Select(o => acadDatabase.Element<Entity>(o))
                    .Where(o => o is Line || o is Polyline || o is Circle || o is Arc)
                    .Select(o=>o.Clone() as Entity)
                    .ToCollection();
                var concaveBuilder = new ThMEPConcaveBuilder(objs, tol.Value);
                var objConcaveHull = concaveBuilder.Build();
                var boundaries = new List<Polyline>();
                boundaries.AddRange(objConcaveHull.Cast<Polyline>().ToList());
                boundaries.ForEach(e =>
                {
                    e.AddToCurrentSpace();
                    e.SetDatabaseDefaults();
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractContourLineByConcave", CommandFlags.Modal)]
        public void THExtractContourLineByConcave()
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

                var hersLength = 200.0;
                var pdo = new PromptDoubleOptions("\n输入修复的长度值<200>");
                pdo.AllowNegative = false;
                pdo.AllowZero = false;
                pdo.AllowNone = false;
                pdo.AllowArbitraryInput = true;
                var pdr = Active.Editor.GetDouble(pdo);
                if (pdr.Status == PromptStatus.OK)
                {
                    hersLength = pdr.Value;
                }

                var options = new PromptKeywordOptions("\n选择处理模式");
                options.Keywords.Add("建筑模式", "A", "建筑模式(A)");
                options.Keywords.Add("结构模式", "S", "结构模式(S)");
                options.Keywords.Default = "结构模式";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }
                var datas = new DBObjectCollection();
                var results = new DBObjectCollection();
                if (result2.StringResult == "建筑模式")
                {
                    var data = new Model1Data(acadDatabase.Database, frame.Vertices());
                    datas = data.MergeData();
                }
                else
                {
                    var data = new Model2Data(acadDatabase.Database, frame.Vertices());
                    datas = data.MergeData();
                }
                // print for test
                //datas.Cast<Entity>().ToList().CreateGroup(acadDatabase.Database,1);
                var concaveBuilder = new ThMEPConcaveBuilder(datas, hersLength);
                results = concaveBuilder.Build();
                results.Cast<Entity>().ToList().CreateGroup(acadDatabase.Database, 4);
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractStair", CommandFlags.Modal)]
        public void THExtractStair()
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

                var engine = new ThDB3StairRecognitionEngine();
                engine.Recognize(acadDatabase.Database, frame.Vertices());
                engine.Elements.Cast<ThIfcStair>().ForEach(o =>
                {
                    if (o.PlatForLayout.Count != 0)
                    {
                        var pline = new Polyline();
                        o.PlatForLayout.ForEach(p =>
                        {
                            pline.CreatePolyline(new Point3dCollection(p.ToArray()));
                            pline.Closed = true;
                            acadDatabase.ModelSpace.Add(pline);
                        });

                    }

                    if (o.HalfPlatForLayout.Count != 0)
                    {
                        var halfPline = new Polyline();
                        o.HalfPlatForLayout.ForEach(hp =>
                        {
                            halfPline.CreatePolyline(new Point3dCollection(hp.ToArray()));
                            halfPline.Closed = true;
                            acadDatabase.ModelSpace.Add(halfPline);
                        });
                    }
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeamAreas", CommandFlags.Modal)]
        public void ThExtractBeamAreas()
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

                var engine = new ThBeamAreaBuilderEngine();
                engine.Build(acadDatabase.Database, frame.Vertices());
                engine.BeamAreas.OfType<Entity>().ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.SetDatabaseDefaults(acadDatabase.Database);
                });
            }
        }
    }
}
