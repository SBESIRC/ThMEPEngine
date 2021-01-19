using System;
using System.IO;
using System.Linq;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Newtonsoft.Json;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using DotNetARX;

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
                    if (o.Outline is Curve curve)
                    {
                        acadDatabase.ModelSpace.Add(curve.WashClone());
                    }
                    else if (o.Outline is MPolygon mPolygon)
                    {
                        mPolygon.SetDatabaseDefaults(Active.Database);
                        acadDatabase.ModelSpace.Add(mPolygon);
                    }
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractArchWall", CommandFlags.Modal)]
        public void THExtractArchWall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var archWallEngine = new ThArchitectureWallRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                archWallEngine.Recognize(acadDatabase.Database, frame.Vertices());
                archWallEngine.Elements.ForEach(o =>
                {
                    if (o.Outline is Curve curve)
                    {
                        acadDatabase.ModelSpace.Add(curve.WashClone());
                    }
                    else if (o.Outline is MPolygon mPolygon)
                    {
                        acadDatabase.ModelSpace.Add(mPolygon);
                    }
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractIfcCloseTool", CommandFlags.Modal)]
        public void ThExtractIfcCloseTool()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var closetoolEngine = new ThClosestoolRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                closetoolEngine.Recognize(acadDatabase.Database, frame.Vertices());
                closetoolEngine.Elements.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractIfcFloorDrain", CommandFlags.Modal)]
        public void ThExtractIfcFloorDrain()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var floorDrainEngine = new ThFloorDrainRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                floorDrainEngine.Recognize(acadDatabase.Database, frame.Vertices());
                floorDrainEngine.Elements.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
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
                engine.Spaces.ForEach(o =>
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
            Vector3d dir=Vector3d.XAxis.CrossProduct(thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint));
            if(dir.Z>=0)
            {
                dbText.Rotation = thIfcBeam.StartPoint.GetVectorTo(thIfcBeam.EndPoint).GetAngleTo(Vector3d.XAxis);
            }
            else
            {
                dbText.Rotation = thIfcBeam.EndPoint.GetVectorTo(thIfcBeam.StartPoint).GetAngleTo(Vector3d.XAxis)+Math.PI;
            }
            dbText.Layer = "0";
            dbText.Height = 200;
            return dbText;
        }
        [CommandMethod("TIANHUACAD", "ThExportGeo", CommandFlags.Modal)]
        public void ThExportGeo()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var pso = new PromptSelectionOptions();
                pso.MessageForAdding = "\n选择线";
                var tvs = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,RXClass.GetClass(typeof(Line)).DxfName)
                };
                var sf = new SelectionFilter(tvs);
                var result1 = Active.Editor.GetSelection(pso, sf);
                if (result1.Status != PromptStatus.OK)
                {
                    return;
                }
                var result2 = Active.Editor.GetSelection(pso, sf);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }
                var firstLines = new List<Line>();
                result1.Value.GetObjectIds().Cast<ObjectId>().ForEach(o => firstLines.Add(acadDatabase.Element<Line>(o)));

                var secondLines = new List<Line>();
                result2.Value.GetObjectIds().Cast<ObjectId>().ForEach(o => secondLines.Add(acadDatabase.Element<Line>(o)));

                var geometry1 = new ThGeometry()
                {
                    Segments = firstLines
                };
                geometry1.Properties.Add("Category","Wall");
                geometry1.Properties.Add("Product", "China");
                geometry1.Properties.Add("Price", "128");

                var geometry2 = new ThGeometry()
                {
                    Segments = secondLines
                };
                geometry2.Properties.Add("Category", "Wall");
                geometry2.Properties.Add("Product", "USA");
                geometry2.Properties.Add("Price", "100");

                var geos = new List<ThGeometry>();
                geos.Add(geometry1);
                geos.Add(geometry2);

                // 输出GeoJson文件
                // 线
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.Line.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThGeometryJsonWriter();
                    geoJsonWriter.Write(geos, writer);
                }
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractGeo", CommandFlags.Modal)]
        public void ThExtractGeo()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var extractEngine =new ThExtractGeometryEngine())
            {
                extractEngine.Extract(acadDatabase.Database);
                var geos = new List<ThGeometry>();
                var spaceIds = new ObjectIdList();
                extractEngine.Spaces.ForEach(o =>
                {
                    o.ColorIndex = 1;
                    spaceIds.Add(acadDatabase.ModelSpace.Add(o));
                    var geometry = new ThGeometry();                    
                    geometry.Properties.Add("Category", "Space");
                    geometry.Segments = o.ToLines();
                    geos.Add(geometry);
                });
                GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), spaceIds);

                var doorIds = new ObjectIdList();
                extractEngine.Doors.ForEach(o =>
                {
                    o.ColorIndex = 2;
                    doorIds.Add(acadDatabase.ModelSpace.Add(o));
                    var geometry = new ThGeometry();
                    geometry.Properties.Add("Category", "Door");
                    geometry.Segments = o.ToLines();
                    geos.Add(geometry);
                });
                GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), doorIds);

                var equipIds = new ObjectIdList();
                extractEngine.Equipments.ForEach(e =>
                {
                    e.Value.ForEach(v =>
                    {
                        v.ColorIndex = 2;
                        equipIds.Add(acadDatabase.ModelSpace.Add(v));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "Equipment");
                        geometry.Properties.Add("Name", e.Key);
                        geometry.Segments = v.ToLines();
                        geos.Add(geometry);
                    });
                });
                GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), equipIds);

                // 输出GeoJson文件
                // 线
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                using (StreamWriter geoJson = File.CreateText(Path.Combine(path, string.Format("{0}.Info.geojson", Active.DocumentName))))
                using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                {
                    Indentation = 4,
                    IndentChar = ' ',
                    Formatting = Formatting.Indented,
                })
                {
                    var geoJsonWriter = new ThGeometryJsonWriter();
                    geoJsonWriter.Write(geos, writer);
                }
            }
        }
    }
}
