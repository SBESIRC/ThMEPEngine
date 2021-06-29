using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using DotNetARX;
using ThMEPEngineCore.IO;
using System.Text;
using System.Linq;
using ThMEPEngineCore.Temp;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using ThCADExtension;


#if ACAD2016
using CLI;
using System.IO;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
#endif

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreInternalCmds
    {
        [CommandMethod("TIANHUACAD", "THBUFFER", CommandFlags.Modal)]
        public void ThBuffer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }

                foreach (Entity obj in objs.Buffer(result2.Value))
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THLINEMERGE", CommandFlags.Modal)]
        public void ThLineMerge()
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

                foreach (Entity obj in objs.LineMerge())
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THBUILDAREA", CommandFlags.Modal)]
        public void ThBuildArea()
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
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach (Entity obj in objs.BuildArea())
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THBUILDMPOLYGON", CommandFlags.Modal)]
        public void ThBuildMPolygon()
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
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                var mPolygon = objs.BuildMPolygon();
                acadDatabase.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();
            }
        }

        [CommandMethod("TIANHUACAD", "THAREAUNION", CommandFlags.Modal)]
        public void ThAreaUnion()
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
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }

                var geometry = objs.ToNTSMultiPolygon().Union();
                foreach (Entity obj in geometry.ToDbCollection())
                {
                    acadDatabase.ModelSpace.Add(obj);
                    obj.SetDatabaseDefaults();
                }
            }
        }

#if ACAD2016
        [CommandMethod("TIANHUACAD", "THPREPAIR", CommandFlags.Modal)]
        public void ThPRepair()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var poly = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Entity e in ThMEPPolygonRepairService.Repair(poly))
                {
                    acadDatabase.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THACLDDemoTest", CommandFlags.Modal)]
        public void THExtractAreaCenterLineDemoTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var extractEngine = new ThExtractGeometryEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }
                else
                {
                    return;
                }

                var extractors = new List<ThExtractorBase>()
                {
                    //包括Space<隔油池、水泵房、垃圾房、停车区域>,
                    //通过停车区域的Space来制造阻挡物
                    new ThSpaceExtractor{ IsBuildObstacle=false,NameLayer="AD-NAME-ROOM",ColorIndex=1},
                    new ThWallExtractor{ColorIndex=2},
                    new ThCenterLineExtractor{ColorIndex=3},
                };

                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);
                extractEngine.OutputGeo(Active.Document.Name);
                extractEngine.Print(acadDatabase.Database);
            }
        }
        [CommandMethod("TIANHUACAD", "ThFHCRDemoTest", CommandFlags.Modal)]
        public void ThFHCRDemoTest()
        {
            //消火栓保护半径
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var extractEngine = new ThExtractGeometryEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }

                //获取空间名称

                // ,ShearWall, 门洞，门扇,柱
        
                var extractors = new List<ThExtractorBase>()
                {
                    //包括Space<隔油池、水泵房、垃圾房、停车区域>,
                    //通过停车区域的Space来制造阻挡物                    
                    new ThArchitectureWallExtractor {UseDb3Engine=true,ColorIndex=2},  // ArchitectureWall
                    new ThShearWallExtractor{UseDb3Engine=false,ColorIndex=3,ElementLayer = "Wall"},  // ShearWall
                    new ThColumnExtractor{UseDb3Engine=false,ColorIndex=4,ElementLayer="柱"}, // Column
                    new ThDoorExtractor {ColorIndex=5,ElementLayer = "门" }, // 门扇
                    new ThDoorOpeningExtractor{ ColorIndex=6,ElementLayer = "门"}, // 门洞
                    new ThEquipmentExtractor{ ColorIndex=7}, // 设备(消火栓/灭火器)                    
                    new ThExternalSpaceExtractor{ ColorIndex=9,ElementLayer="AI-OuterBoundary"},
                };
                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);

                var spaceExtractor = new ThSpaceExtractor
                {
                    IsBuildObstacle = false,
                    ColorIndex = 1,
                    ElementLayer = "AD-AREA-OUTL",
                    NameLayer = "AD-NAME-ROOM",
                    PrivacyLayer = "空间名称"
                };
                spaceExtractor.SpaceNeibours.AddRange((extractors[0] as ThArchitectureWallExtractor).Walls);
                spaceExtractor.SpaceNeibours.AddRange((extractors[1] as ThShearWallExtractor).Walls);
                spaceExtractor.SpaceNeibours.AddRange((extractors[2] as ThColumnExtractor).Columns);
                spaceExtractor.Extract(acadDatabase.Database, pts);

                extractEngine.Accept(spaceExtractor);
                extractEngine.OutputGeo(Active.Document.Name);
                extractEngine.Print(acadDatabase.Database);
                if(false)
                {
                    var parseService = new ThFireHydrantResultParseService();
                    parseService.Parse(@"E:\ZheDa\FireHydrantCover\GeoJsonTest\t2.result.txt");

                    var colorIndexes = new List<int>();
                    foreach (var data in parseService.Results)
                    {
                        if (data.IsValid())
                        {
                            ObjectIdList objIds = new ObjectIdList();
                            GetColorIndex(colorIndexes);
                            var colorIndex = colorIndexes[colorIndexes.Count - 1];
                            Circle circle = new Circle(data.Position, Vector3d.ZAxis, 0.5);
                            circle.ColorIndex = colorIndex;
                            circle.SetDatabaseDefaults();
                            objIds.Add(acadDatabase.ModelSpace.Add(circle));
                            var outerPoly = new Polyline();
                            for (int i = 0; i < data.OuterPoints.Count; i++)
                            {
                                var pt = data.OuterPoints[i];
                                outerPoly.AddVertexAt(i, new Point2d(pt.X, pt.Y), 0, 0, 0);
                            }
                            outerPoly.ColorIndex = colorIndex;
                            outerPoly.SetDatabaseDefaults();
                            objIds.Add(acadDatabase.ModelSpace.Add(outerPoly));

                            if (data.InnerPoints.Count >= 3)
                            {
                                var innerPoly = new Polyline();
                                for (int i = 0; i < data.InnerPoints.Count; i++)
                                {
                                    var pt = data.InnerPoints[i];
                                    innerPoly.AddVertexAt(i, new Point2d(pt.X, pt.Y), 0, 0, 0);
                                }
                                innerPoly.ColorIndex = colorIndex;
                                innerPoly.SetDatabaseDefaults();
                                objIds.Add(acadDatabase.ModelSpace.Add(innerPoly));
                            }

                            if (objIds.Count > 0)
                            {
                                GroupTools.CreateGroup(acadDatabase.Database, Guid.NewGuid().ToString(), objIds);
                            }
                        }
                    }
                }
            }
        }
        private void GetColorIndex(List<int> colorIndexes)
        {
            Random r = new Random();
            while (true)
            {
                int colorIndex = r.Next(0, 255);
                if (colorIndex != 2)
                {
                    if (colorIndexes.IndexOf(colorIndex) < 0)
                    {
                        colorIndexes.Add(colorIndex);
                        break;
                    }
                }
            }
        }
        [CommandMethod("TIANHUACAD", "ThWSDIDemoTest", CommandFlags.Modal)]
        public void ThWSDIDemoTest()
        {
            //Water Supply Detail Drawing (给排水大样图)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var extractEngine = new ThExtractGeometryEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }

                var extractors = new List<ThExtractorBase>()
                {
                    //包括Space<隔油池、水泵房、垃圾房、停车区域>,
                    //通过停车区域的Space来制造阻挡物
                    new ThSpaceExtractor{ IsBuildObstacle=false,ColorIndex=1},
                    new ThColumnExtractor{UseDb3Engine=true,ColorIndex=2},
                    new ThWaterSupplyPositionExtractor{ColorIndex=3},
                    new ThWaterSupplyStartExtractor{ColorIndex=4},
                    new ThToiletGroupExtractor { ColorIndex=5},
                };

                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);

                var toiletGroupDic = new Dictionary<Entity, string>();
                foreach (var item in (extractors[4] as ThToiletGroupExtractor).ToiletGroupId)
                {
                    toiletGroupDic.Add(item.Key, item.Value);
                }

                extractEngine.Group(toiletGroupDic);

                extractEngine.OutputGeo(Active.Document.Name);
                extractEngine.Print(acadDatabase.Database);
            }
        }

        [CommandMethod("TIANHUACAD", "ThRLPDemoTest", CommandFlags.Modal)]
        public void ThRLPDemoTest()
        {
            //Roof thunder protection (屋顶防雷)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var extractEngine = new ThExtractGeometryEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }
                var levelMarkService = new ThExtractLevelMarkService()
                {
                    Types = new List<Type> { typeof(BlockReference) },
                };
                levelMarkService.Extract(acadDatabase.Database, pts);

                var extractors = new List<ThExtractorBase>()
                {
                    //包括墙、小屋面、大屋面、雨棚、管井(可能就是墙)
                    new ThWallExtractor
                    {
                        ElementLayer="墙看线",
                        Types=new List<Type>{ typeof(Hatch)},
                        ColorIndex=1,
                        BuildAreaSwitch=false,
                        CheckIsolated=false,
                        IEleQuery = levelMarkService
                    },
                    new ThRainshedExtractor
                    {
                        Types = new List<Type>{ typeof(Hatch) },
                        ColorIndex=3,
                        ElementLayer = "雨棚",
                        IEleQuery = levelMarkService
                    },
                    new ThSmallRoofExtractor
                    {
                        Types = new List<Type>{ typeof(Hatch) },
                        ColorIndex=4,
                        ElementLayer = "屋面",
                        IEleQuery = levelMarkService
                    },
                    new ThBigRoofExtractor
                    {
                        Types = new List<Type>{ typeof(Polyline) },
                        ColorIndex=5,
                        ElementLayer = "大屋面",
                        IEleQuery = levelMarkService
                    },
                };

                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);

                extractEngine.OutputGeo(Active.Document.Name);
                extractEngine.Print(acadDatabase.Database);
            }
        }

        [CommandMethod("TIANHUACAD", "THHYDRANTDemoTest", CommandFlags.Modal)]
        public void THHYDRANTDemoTest()
        {
            //
            using (var sr = new StreamReader("D:\\有洞空间和母子空间.dwg.Info.geojson"))
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var hydrant = new ThHydrantEngineMgd();
                var context = new ThProtectionContextMgd()
                {
                    HydrantHoseLength = 25,
                    HydrantClearanceRadius = 0,
                };
                var regions = hydrant.Validate(sr.ReadToEnd(), context);
                var serializer = GeoJsonSerializer.Create();
                regions.ForEach(o =>
                {
                    using (var stringReader = new StringReader(o))
                    using (var jsonReader = new JsonTextReader(stringReader))
                    {
                        var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                        features.ForEach(f =>
                        {
                            if (f.Geometry != null)
                            {
                                if (f.Attributes.Exists("Name"))
                                {
                                    if (f.Attributes["Name"] as string == "Covered region")
                                    {
                                        if (f.Geometry is Polygon polygon)
                                        {
                                            acadDatabase.ModelSpace.Add(polygon.ToDbMPolygon());
                                        }
                                        else if (f.Geometry is MultiPolygon mPolygon)
                                        {
                                            acadDatabase.ModelSpace.Add(mPolygon.ToDbMPolygon());
                                        }
                                        else
                                        {
                                            throw new NotSupportedException();
                                        }
                                    }
                                }
                            }
                        });
                    }
                });
            }
        }

        [CommandMethod("TIANHUACAD", "THROUTEMAINPIPE", CommandFlags.Modal)]
        public void ThRouteMainPipe()
        {
            //todo: find all water suply points, and draw them in the drawing
            //

            //Water Supply Detail Drawing (给排水大样图)

            string geojsonContent = string.Empty;
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var extractEngine = new ThExtractGeometryEngine())
                {
                    //Get Geojson input string
                    var per = Active.Editor.GetEntity("\n选择一个框线");
                    var pts = new Point3dCollection();
                    if (per.Status == PromptStatus.OK)
                    {
                        var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                        var newFrame = ThMEPFrameService.NormalizeEx(frame);
                        pts = newFrame.VerticesEx(100.0);
                    }

                    var extractors = new List<ThExtractorBase>()
                    {
                        new ThSpaceExtractor{ IsBuildObstacle=false,ColorIndex=1},
                        new ThColumnExtractor{UseDb3Engine=false,ColorIndex=2},
                        new ThWaterSupplyPositionExtractor{ColorIndex=3},
                        new ThWaterSupplyStartExtractor{ColorIndex=4},
                        new ThToiletGroupExtractor { ColorIndex=5},
                    };

                    extractEngine.Accept(extractors);
                    extractEngine.Extract(acadDatabase.Database, pts);

                    var areaGroupDic = new Dictionary<Entity, string>();

                    var areaPolylineToIdDic = (extractors[4] as ThToiletGroupExtractor).ToiletGroupId;
                    var areaPolyline = areaPolylineToIdDic.Keys.First();
                    var areaId = areaPolylineToIdDic[areaPolyline];

                    areaGroupDic.Add(areaPolyline, areaId);

                    extractEngine.Group(areaGroupDic);
                    //geojsonContent = Active.Document.Name;
                    //geojsonContent = extractEngine.OutputGeo(Active.Document.Name);
                    geojsonContent = extractEngine.OutputGeo();

                    //string path = @"D:\project\2.drainage\jsonSample\1-1.input.geojson";
                    //File.WriteAllText(path, geojsonContent);
               
                }
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                ThPipeSystemDiagramMgd SystemDiagramMethods = new ThPipeSystemDiagramMgd();

                //string inputFile = @"D:\DATA\Git\GeometricTools\GTE\Samples\CSharpCppManaged\CppLibrary\GroupingPipe\data\pipe\case-0.geojson";
                //string inputFile = inputFileName;
                //string inputGeoJson = File.ReadAllText(inputFile);

                var groupedContent = SystemDiagramMethods.ProcessGrouping(geojsonContent);

                //string outputFile = @"D:\project\2.drainage\jsonSample\1-2.output.geojson";
                //File.WriteAllText(outputFile, geojsonContent);

                var serializer = GeoJsonSerializer.Create();
                var revisedContent2 = string.Empty;
                using (var stringReader = new StringReader(groupedContent))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    var FeaturesWithLineString = new List<IFeature>();
                    foreach (var f in features)
                    {
                        if(f.Geometry.GeometryType.Equals("Point") && f.Attributes.Exists("Direction"))
                        {
                            var coordinates = f.Geometry.Coordinates;
                            var pt = new Point3d(coordinates[0].X, coordinates[0].Y, 0);
                            
                            
                            var dirArr = f.Attributes["Direction"] as List<object>;
                            var branchLen = 400;
                            var dirVector = new Vector3d(double.Parse(dirArr[0].ToString()) * branchLen, double.Parse(dirArr[1].ToString()) * branchLen,0);

                            var revisedPt = pt + dirVector;
                            f.Geometry.Coordinates[0].X = revisedPt.X;
                            f.Geometry.Coordinates[0].Y = revisedPt.Y;
                            var linePts = new List<Point3d>();
                            linePts.Add(pt);
                            linePts.Add(revisedPt);
                            Draw.Line(linePts.ToArray());
                        }
                    }

                    using (var geoJson = new ExtentedStringWriter(new UTF8Encoding(false)))
                    using (JsonTextWriter writer = new JsonTextWriter(geoJson)
                    {
                        Indentation = 4,
                        IndentChar = ' ',
                        Formatting = Formatting.Indented,
                    })
                    {
                        var geoJsonWriter = new GeoJsonWriter();
                        geoJsonWriter.Write(features, writer);
                        revisedContent2 = geoJson.ToString();
                    }
                }

                //string path2 = @"D:\project\2.drainage\jsonSample\1-3.input.geojson";
                //File.WriteAllText(path2, revisedContent2);

                var output = SystemDiagramMethods.ProcessMainBranchs(revisedContent2);

                //string path3 = @"D:\project\2.drainage\jsonSample\1-4.output.geojson";
                //File.WriteAllText(path3, output);

                using (var stringReader = new StringReader(output))
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                    var FeaturesWithLineString = new List<IFeature>();

                    foreach (var f in features)
                    {
                        if (f.Attributes.Exists("Category") && f.Attributes["Category"].Equals("Pipe"))
                        {
                            if (f.Geometry.GeometryType.Equals("LineString"))
                            {
                                var coordinates = f.Geometry.Coordinates;
                                var linePts = new List<Point3d>();
                                foreach (var coord in coordinates)
                                {
                                    linePts.Add(new Point3d(coord.X, coord.Y, 0));
                                }

                                Draw.Line(linePts.ToArray());
                            }
                        }
                    }
                }
            }
        }
#endif
        [CommandMethod("TIANHUACAD", "THLPDCDemoTest", CommandFlags.Modal)]
        public void THLPDCDemoTest()
        {
            //Lightning Protection Down Conductors Test Data(防雷保护引下线)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var extractEngine = new ThExtractGeometryEngine())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }
                else
                {
                    return;
                }
                var extractors = new List<ThExtractorBase>()
                {
                    new ThStoreyExtractor()
                    {
                        ColorIndex=1,
                        GroupSwitch=false,
                        UseDb3Engine=true,
                        IsolateSwitch=false,
                    },
                    new ThArchitectureOutlineExtractor()
                    {
                        ColorIndex=2,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThOuterOtherColumnExtractor()
                    {
                        ColorIndex=3,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false
                    },
                    new ThOuterOtherShearWallExtractor()
                    {
                        ColorIndex=4,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false
                    },
                    new ThBeamExtractor()
                    {
                        ColorIndex =5,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThLightningReceivingBeltExtractor
                    {
                        ColorIndex=6,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                };
                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);
                extractEngine.Group((extractors[0] as ThStoreyExtractor).StoreyIds);
                extractEngine.OutputGeo(Active.Document.Name);
                extractEngine.Print(acadDatabase.Database);
            }
        }
        [CommandMethod("TIANHUACAD", "THCENTERLINE", CommandFlags.Modal)]
        public void ThCenterline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n请输入差值距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(pline, result2.Value);
                foreach (Entity centerline in centerlines)
                {
                    acadDatabase.ModelSpace.Add(centerline);
                    centerline.SetDatabaseDefaults();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THSIMPLIFY", CommandFlags.Modal)]
        public void ThSimplify()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n请输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var options = new PromptKeywordOptions("\n请指定简化方式")
                {
                    AllowNone = true
                };
                options.Keywords.Add("DP", "DP", "DP(D)");
                options.Keywords.Add("VW", "VW", "VW(V)");
                options.Keywords.Add("TP", "TP", "TP(T)");
                options.Keywords.Default = "DP";
                var result3 = Active.Editor.GetKeywords(options);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline pline = null;
                double distanceTolerance = result2.Value;
                var obj = acadDatabase.Element<Polyline>(result.ObjectId);
                if (result3.StringResult == "DP")
                {
                    pline = obj.DPSimplify(distanceTolerance);
                }
                else if (result3.StringResult == "VW")
                {
                    pline = obj.VWSimplify(distanceTolerance);
                }
                else if (result3.StringResult == "TP")
                {
                    pline = obj.TPSimplify(distanceTolerance);
                }
                acadDatabase.ModelSpace.Add(pline);
                pline.SetDatabaseDefaults();
            }
        }

        [CommandMethod("TIANHUACAD", "THLANELINECLEAN", CommandFlags.Modal)]
        public void ThLaneLineClean()
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

                var service = new ThLaneLineCleanService();
                foreach (Line line in service.Clean(objs))
                {
                    acadDatabase.ModelSpace.Add(line);
                    line.SetDatabaseDefaults();
                }
            }
        }
    }
}
