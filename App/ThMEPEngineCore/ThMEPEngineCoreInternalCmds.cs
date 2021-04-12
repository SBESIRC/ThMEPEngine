using CLI;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Temp;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

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
        [CommandMethod("TIANHUACAD", "THTRIANGULATE", CommandFlags.Modal)]
        public void ThTriangulate()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var shell = acadDatabase.Element<Polyline>(result.ObjectId);

                var holes = new List<Polyline>();
                var options = new PromptSelectionOptions()
                {
                    MessageForAdding = "请选择洞",
                };
                var result2 = Active.Editor.GetSelection(options);
                if (result2.Status == PromptStatus.OK)
                {
                    foreach (var obj in result2.Value.GetObjectIds())
                    {
                        holes.Add(acadDatabase.Element<Polyline>(obj));
                    }
                }
                var triangles = ThMEPTriangulationService.EarCut(shell, holes.ToArray());
                foreach (Polyline triangle in triangles)
                {
                    triangle.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(triangle);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPOLYDECOMPOSE", CommandFlags.Modal)]
        public void ThPolyDecompose()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var poly = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Entity e in ThMEPPolyDecomposer.Decompose(poly))
                {
                    e.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(e);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDXCX", CommandFlags.Modal)]
        public void THDXCX()
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

                //输入冲洗点位参数
                var washPara = GetWashParameter();

                var extractors = new List<ThExtractorBase>()
                {
                    //包括Space<隔油池、水泵房、垃圾房、停车区域>,
                    //通过停车区域的Space来制造阻挡物
                    new ThSpaceExtractor{ IsBuildObstacle=true,NameLayer="空间名称",ColorIndex=1},
                    new ThColumnExtractor{UseDb3ColumnEngine=false,ColorIndex=2},
                    new ThShearWallExtractor{ColorIndex=3},
                    new ThDrainageFacilityExtractor{ColorIndex=4},
                };

                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);
                extractEngine.OutputGeo(Active.Document.Name);

                var washData = new ThWashGeoData();
                washData.ReadFromFile(Active.Document.Name + ".Info.geojson");
                var washPoint = new ThWashPointLayoutEngine();
                double[] points = washPoint.Layout(washData, washPara);
                var coords = GetPoints(points);
                BuildCircleHatch(coords);
            }
        }

        private void BuildCircleHatch(List<Point3d> pts, double radius = 500.0)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                foreach (var pt in pts)
                {
                    var circle1 = new Circle(pt, Vector3d.ZAxis, radius + 100);
                    circle1.ColorIndex = 3;
                    circle1.SetDatabaseDefaults();
                    acadDatabase.ModelSpace.Add(circle1);

                    var circle = new Circle(pt, Vector3d.ZAxis, radius);
                    circle.ColorIndex = 3;
                    circle.SetDatabaseDefaults();
                    ObjectIdCollection ObjIds = new ObjectIdCollection();
                    ObjIds.Add(acadDatabase.ModelSpace.Add(circle));

                    Hatch oHatch = new Hatch();

                    Vector3d normal = new Vector3d(0.0, 0.0, 1.0);

                    oHatch.Normal = normal;

                    oHatch.Elevation = 0.0;

                    oHatch.PatternScale = 2.0;

                    oHatch.SetHatchPattern(HatchPatternType.PreDefined, "ZIGZAG");

                    oHatch.ColorIndex = 1;


                    acadDatabase.ModelSpace.Add(oHatch);
                    //this works ok  
                    oHatch.Associative = true;
                    oHatch.AppendLoop((int)HatchLoopTypes.Default, ObjIds);
                    oHatch.EvaluateHatch(true);
                }
            }
        }

        private static List<Point3d> GetPoints(double[] coords)
        {
            var results = new List<Point3d>();
            for (int i = 0; i < coords.Length; i += 2)
            {
                results.Add(new Point3d(coords[i], coords[i + 1], 0));
            }
            return results;
        }

        private ThWashParam GetWashParameter()
        {
            var param = new ThWashParam();
            param.R = GetValue("\n输入保护半径");
            param.protect_arch = GetValue("\n是否保护建筑空间[true(1)/false(0)] <true>",1) == 0 ? false : true;
            param.protect_park = GetValue("\n是否保护停车空间[true(1)/false(0)] <true>",1) == 0 ? false : true;
            param.protect_other = GetValue("\n是否保护不可布空间[true(1)/false(0)] <false>") == 0 ? false : true;
            param.extend_arch = GetValue("\n建筑空间是否能保护停车空间和不可布空间[true(1)/false(0)] <false>") == 0 ? false : true;
            param.extend_park = GetValue("\n停车空间是否能保护到不可布空间[true(1)/false(0)] <false>") == 0 ? false : true;
            return param;
        }
        private int GetValue(string message,int init=0)
        {
            var pdo = new PromptIntegerOptions(message);
            var protectRadiusPdr = Active.Editor.GetInteger(pdo);
            if (protectRadiusPdr.Status == PromptStatus.OK)
            {
                return protectRadiusPdr.Value;
            }
            else
            {
                return init;
            }
        }

        [CommandMethod("TIANHUACAD", "THExtractAreaCenterLineTestData", CommandFlags.Modal)]
        public void THExtractAreaCenterLineTestData()
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
#endif

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
