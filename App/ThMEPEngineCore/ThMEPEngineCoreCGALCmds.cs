﻿#if (ACAD2016 || ACAD2018)
using System;
using System.IO;
using System.Linq;
using System.Text;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.IO;
using NetTopologySuite.Features;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Temp;
using CLI;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreCGALCmds
    {
        [CommandMethod("TIANHUACAD", "THACLDDemoTest", CommandFlags.Modal)]
        public void THExtractAreaCenterLineDemoTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
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
                var roomExtractor = new ThRoomExtractor { ColorIndex = 1 };
                roomExtractor.Extract(acadDatabase.Database, pts);
                var geos  = roomExtractor.BuildGeometries();
                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                string fileName = fileInfo.Name;
                int count = 1;
                foreach (var geo in geos)
                {
                    //
                    string newFileName = "";
                    newFileName = fileName + count.ToString("000");
                    count++;
                    ThGeoOutput.Output(new List<Model.ThGeometry> { geo }, path, newFileName);
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

        [CommandMethod("TIANHUACAD", "THLPDCDemoTest", CommandFlags.Modal)]
        public void THLPDCDemoTest()
        {
            //Lightning Protection Down Conductors Test Data(防雷保护引下线)
            using (var acadDatabase = AcadDatabase.Active())
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
                var levelIndex = 3;
                var ner = Active.Editor.GetInteger("\n输入防雷等级类别<三类>");
                if(ner.Status == PromptStatus.OK)
                {
                    levelIndex = ner.Value;
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
                string geoContent = extractEngine.OutputGeo();
                extractEngine.OutputGeo(Active.Document.Name);
                var dclLayoutEngine = new ThDCLayoutEngineMgd();
                var data = new ThDCDataMgd();
                data.ReadFromContent(geoContent);
                var param = new ThDCParamMgd(levelIndex);
                var result = dclLayoutEngine.Run(data, param);
                var parseResults = ThDclResultParseService.Parse(result);
                var printService = new ThDclPrintService(acadDatabase.Database, "AI-DCL");
                printService.Print(parseResults);
                //extractEngine.Print(acadDatabase.Database);
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
                        if (f.Geometry.GeometryType.Equals("Point") && f.Attributes.Exists("Direction"))
                        {
                            var coordinates = f.Geometry.Coordinates;
                            var pt = new Point3d(coordinates[0].X, coordinates[0].Y, 0);


                            var dirArr = f.Attributes["Direction"] as List<object>;
                            var branchLen = 400;
                            var dirVector = new Vector3d(double.Parse(dirArr[0].ToString()) * branchLen, double.Parse(dirArr[1].ToString()) * branchLen, 0);

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

        [CommandMethod("TIANHUACAD", "THFireAlarmTestDataExtract", CommandFlags.Modal)]
        public void THFireAlarmTestDataExtract()
        {

            using (var acadDatabase = AcadDatabase.Active())
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
                // ArchitectureWall、Shearwall、Column、Window、Room
                // Beam、DoorOpening、Railing、FireproofShutter(防火卷帘)
                var storeyExtractor = new ThEStoreyExtractor()
                {
                    ElementLayer = "AI-楼层框定E",
                    ColorIndex = 12,
                    GroupSwitch = false,
                    Group2Switch = false,
                    UseDb3Engine = true,
                    IsolateSwitch = false,
                };
                storeyExtractor.Extract(acadDatabase.Database,pts);

                var extractors = new List<ThExtractorBase>()
                {
                    new ThArchitectureWallExtractor()
                    {
                        ElementLayer = "AI-墙",
                        ColorIndex=1,
                        GroupSwitch=true,
                        Group2Switch=false,
                        UseDb3Engine=true,
                        IsolateSwitch=false,
                    },
                    new ThShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        ColorIndex=2,
                        GroupSwitch=true,
                        Group2Switch=false,
                        UseDb3Engine=true,
                        IsolateSwitch=false,
                    },
                    new ThColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        ColorIndex=3,
                        GroupSwitch=false,
                        Group2Switch=false,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        ColorIndex=4,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThRoomExtractor()   
                    {
                        ColorIndex=5,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        ColorIndex=6,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=true,
                        IsolateSwitch=false,
                    },
                    new ThFaDoorOpeningExtractor()
                    {
                        ElementLayer = "AI-门",
                        ColorIndex=7,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThRailingExtractor()
                    {
                        ElementLayer = "AI-栏杆",
                        ColorIndex=8,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaFireproofshutterExtractor()
                    {
                        ElementLayer = "AI-防火卷帘",
                        ColorIndex=9,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        ColorIndex=10,
                        GroupSwitch=true,
                        Group2Switch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFiredistrictExtractor()
                    {
                        ElementLayer="AI-防火分区,AD-AREA-DIVD",
                        ColorIndex=11,
                        GroupSwitch=true,
                        Group2Switch=false,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },                    
                };
                extractEngine.Accept(extractors);
                var fireApartExtractor = extractors.Where(o => o is ThFiredistrictExtractor).First() as ThFiredistrictExtractor;
                fireApartExtractor.StoreyInfos = storeyExtractor.Storeys.Cast<StoreyInfo>().ToList();
                extractEngine.Extract(acadDatabase.Database, pts);
                extractEngine.Accept(storeyExtractor);
                extractEngine.Group(storeyExtractor.StoreyIds);
                extractEngine.Group2(fireApartExtractor.GetFireAPartIds());

                var faDoorExtractor = extractors.Where(o => o is ThFaDoorOpeningExtractor).First() as ThFaDoorOpeningExtractor;
                faDoorExtractor.SetTags(fireApartExtractor.GetFireAPartIds());
                var fireProofShutter = extractors.Where(o => o is ThFaFireproofshutterExtractor).First() as ThFaFireproofshutterExtractor;
                fireProofShutter.SetTags(fireApartExtractor.GetFireAPartIds());
                //string geoContent = extractEngine.OutputGeo();                
                extractEngine.OutputGeo(Active.Document.Name);
                extractEngine.Print(acadDatabase.Database);
            }
        }
    }
}
#endif