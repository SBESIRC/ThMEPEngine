using System;
using System.IO;
using System.Linq;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.IO.GeoJSON;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Temp;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

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
        [CommandMethod("TIANHUACAD", "THExtractCurtainWall", CommandFlags.Modal)]
        public void THExtractCurtainWall()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var curtainWallEngine = new ThCurtainWallRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                curtainWallEngine.Recognize(acadDatabase.Database, frame.Vertices());
                curtainWallEngine.Elements.ForEach(o =>
                {
                    if (o.Outline is Curve curve)
                    {
                        var clone = curve.WashClone();
                        clone.ColorIndex = 6;
                        clone.SetDatabaseDefaults();
                        acadDatabase.ModelSpace.Add(clone);
                    }                
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractWindow", CommandFlags.Modal)]
        public void THExtractWindow()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var windowEngine = new ThWindowRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                windowEngine.Recognize(acadDatabase.Database, frame.Vertices());
                var objIds = new ObjectIdList();
                windowEngine.Elements.ForEach(o =>
                {
                    if (o.Outline is Curve curve)
                    {
                        var clone = curve.WashClone();
                        clone.ColorIndex = 6;
                        clone.SetDatabaseDefaults();
                        objIds.Add(acadDatabase.ModelSpace.Add(clone));
                    }
                });
                GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), objIds);
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
        [CommandMethod("TIANHUACAD", "ThExtractSpace", CommandFlags.Modal)]
        public void ThExtractSpace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var exportEngine = new ThGemometryExportEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                exportEngine.Export(acadDatabase.Database, frame.Vertices());
                var geos = new List<ThGeometry>();
                var objIds = new ObjectIdList();
                exportEngine.Spaces.ForEach(o =>
                {
                    o.Boundary.ColorIndex = 5;
                    o.Boundary.SetDatabaseDefaults();
                    objIds.Add(acadDatabase.ModelSpace.Add(o.Boundary));
                    var geometry = new ThGeometry();
                    geometry.Boundary = o.Boundary as Polyline;
                    o.Properties.ForEach(p => geometry.Properties.Add(p.Key, p.Value));
                    geos.Add(geometry);
                });
                if(objIds.Count>0)
                {
                    GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), objIds);
                }               

                // 输出GeoJson文件
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
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if(per.Status==PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }
                extractEngine.ExtractParameter.IsExtractCenterLine = true;
                extractEngine.ExtractParameter.IsExtractWall = true;
                extractEngine.ExtractParameter.IsExtractSpace = true;
                extractEngine.Extract(acadDatabase.Database, pts);
                var geos = new List<ThGeometry>();
               
                short colorIndex = 0;
                if(extractEngine.Spaces.Count>0)
                {
                    var spaceIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.Spaces.ForEach(o =>
                    {
                        o.Boundary.ColorIndex = colorIndex;
                        o.Boundary.SetDatabaseDefaults();
                        spaceIds.Add(acadDatabase.ModelSpace.Add(o.Boundary));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "Space");
                        geometry.Properties.Add("Name", string.Join(";", o.Tags.ToArray()));
                        for (int i = 1; i <= o.SubSpaces.Count; i++)
                        {
                            string key = "SubSpace" + i + " ID=";
                            geometry.Properties.Add(key, o.SubSpaces[i - 1].Uuid);
                        }
                        geometry.Boundary = o.Boundary;
                        geos.Add(geometry);
                    });
                    if (spaceIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), spaceIds);
                    }
                }                

                if(extractEngine.Doors.Count>0)
                {
                    var doorIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.Doors.ForEach(o =>
                    {
                        o.ColorIndex = colorIndex;
                        o.SetDatabaseDefaults();
                        doorIds.Add(acadDatabase.ModelSpace.Add(o));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "Door");
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    });
                    if (doorIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), doorIds);
                    }
                }
                
                if(extractEngine.Equipments.Count>0)
                {
                    var equipIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.Equipments.ForEach(e =>
                    {
                        e.Value.ForEach(v =>
                        {
                            v.ColorIndex = colorIndex;
                            v.SetDatabaseDefaults();
                            equipIds.Add(acadDatabase.ModelSpace.Add(v));
                            var geometry = new ThGeometry();
                            geometry.Properties.Add("Category", "Equipment");
                            geometry.Properties.Add("Name", e.Key);
                            geometry.Boundary = v;
                            geos.Add(geometry);
                        });
                    });
                    if (equipIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), equipIds);
                    }
                }
                
                if(extractEngine.Obstructs.Count>0)
                {
                    colorIndex++;
                    extractEngine.Obstructs.ForEach(o =>
                    {
                        var obstructIds = new ObjectIdList();
                        o.Key.ColorIndex = colorIndex;
                        o.Key.SetDatabaseDefaults();
                        obstructIds.Add(acadDatabase.ModelSpace.Add(o.Key));
                        o.Value.ForEach(v =>
                        {
                            v.ColorIndex = colorIndex;
                            v.SetDatabaseDefaults();
                            obstructIds.Add(acadDatabase.ModelSpace.Add(v));
                            var geometry = new ThGeometry();
                            geometry.Properties.Add("Category", "Obstruct");
                            geometry.Boundary = v;
                            geos.Add(geometry);
                        });
                        if (obstructIds.Count > 0)
                        {
                            GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), obstructIds);
                        }
                    });
                }
                
                if(extractEngine.ConnectPorts.Count>0)
                {
                    var connectPortIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.ConnectPorts.ForEach(o =>
                    {
                        o.Key.ColorIndex = colorIndex;
                        o.Key.SetDatabaseDefaults();
                        connectPortIds.Add(acadDatabase.ModelSpace.Add(o.Key));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "ConnectPort");
                        geometry.Properties.Add("Code", o.Value);
                        geometry.Boundary = o.Key;
                        geos.Add(geometry);
                    });
                    if (connectPortIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), connectPortIds);
                    }
                }

                if(extractEngine.Columns.Count>0)
                {
                    var columnIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.Columns.ForEach(o =>
                    {
                        o.ColorIndex = colorIndex;
                        o.SetDatabaseDefaults();
                        columnIds.Add(acadDatabase.ModelSpace.Add(o));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "Column");
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    });
                    if (columnIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), columnIds);
                    }
                }

                if(extractEngine.Walls.Count>0)
                {
                    var wallIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.Walls.ForEach(o =>
                    {
                        o.ColorIndex = colorIndex;
                        o.SetDatabaseDefaults();
                        wallIds.Add(acadDatabase.ModelSpace.Add(o));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "Wall");
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    });
                    if (wallIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), wallIds);
                    }
                }
                
                if(extractEngine.DrainageFacilities.Count>0)
                {
                    var drainageFacilityIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.DrainageFacilities.ForEach(o =>
                    {
                        o.ColorIndex = colorIndex;
                        o.SetDatabaseDefaults();
                        drainageFacilityIds.Add(acadDatabase.ModelSpace.Add(o));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "DrainageFacility");
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    });
                    if (drainageFacilityIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), drainageFacilityIds);
                    }
                }
                
                if(extractEngine.LaneLines.Count>0)
                {
                    var laneLineIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.LaneLines.ForEach(o =>
                    {
                        o.ColorIndex = colorIndex;
                        o.SetDatabaseDefaults();
                        laneLineIds.Add(acadDatabase.ModelSpace.Add(o));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "LaneLine");
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    });
                    if (laneLineIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), laneLineIds);
                    }
                }

                if(extractEngine.CenterLines.Count>0)
                {
                    var centerLineIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.CenterLines.ForEach(o =>
                    {
                        o.ColorIndex = colorIndex;
                        o.SetDatabaseDefaults();
                        centerLineIds.Add(acadDatabase.ModelSpace.Add(o));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "中心线");
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    });
                    if (centerLineIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), centerLineIds);
                    }
                }

                if(extractEngine.ParkingStalls.Count>0)
                {
                    var parkingStallIds = new ObjectIdList();
                    colorIndex++;
                    extractEngine.ParkingStalls.ForEach(o =>
                    {
                        o.Boundary.ColorIndex = colorIndex;
                        o.Boundary.SetDatabaseDefaults();
                        parkingStallIds.Add(acadDatabase.ModelSpace.Add(o.Boundary));
                        var geometry = new ThGeometry();
                        geometry.Properties.Add("Category", "ParkingStall");
                        geometry.Boundary = o.Boundary;
                        geos.Add(geometry);
                    });
                    if (parkingStallIds.Count > 0)
                    {
                        GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), parkingStallIds);
                    }
                }
                // 输出GeoJson文件
                // 线
                var docPath = Active.Document.Name;
                var fileInfo = new FileInfo(docPath);
                //var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var path = fileInfo.Directory.FullName;
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
        
        [CommandMethod("TIANHUACAD", "THExtractDoor", CommandFlags.Modal)]
        public void THExtractDoor()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var doorEngine = new ThDoorRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                doorEngine.Recognize(acadDatabase.Database, frame.Vertices());
                doorEngine.Elements.ForEach(o =>
                {
                    o.Outline.ColorIndex = 4;
                    o.Outline.SetDatabaseDefaults();
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
    }
}
