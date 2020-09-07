using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Linq2Acad;
using AcHelper;
using Xbim.IO;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedBldgElements;
using Autodesk.AutoCAD.Runtime;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.xBIM;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using NFox.Cad.Collections;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.BeamInfo;
using ThCADCore.NTS;
using ThMEPEngineCore.Model.Segment;

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

        [CommandMethod("TIANHUACAD", "THEXTRACTMODEL", CommandFlags.Modal)]
        public void ThExtractModel()
        {
            JavaScriptSerializer _JavaScriptSerializer = new JavaScriptSerializer();
            var _JsonBlocks = ReadTxt(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Blocks.json"));
            ArrayList _ListBlocks = _JavaScriptSerializer.Deserialize<ArrayList>(_JsonBlocks);


            var _JsonElementTypes = ReadTxt(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ElementTypes.json"));
            ArrayList _ListElementTypes = _JavaScriptSerializer.Deserialize<ArrayList>(_JsonElementTypes);
        }

        [CommandMethod("TIANHUACAD", "THCREATEWALL", CommandFlags.Modal)]
        public void ThCreateWall()
        {
            using (var model = ThModelExtension.CreateAndInitModel("HelloWall"))
            {
                if (model != null)
                {
                    IfcBuilding building = ThModelExtension.CreateBuilding(model, "Default Building");
                    IfcWallStandardCase wall = ThModelExtension.CreateWall(model, 4000, 300, 2400);

                    //if (wall != null) AddPropertiesToWall(model, wall);
                    using (var txn = model.BeginTransaction("Add Wall"))
                    {
                        building.AddElement(wall);
                        txn.Commit();
                    }

                    if (wall != null)
                    {
                        try
                        {
                            Console.WriteLine("Standard Wall successfully created....");
                            //write the Ifc File
                            model.SaveAs(Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HelloWallIfc4.ifc"), StorageType.Ifc);
                            Console.WriteLine("HelloWallIfc4.ifc has been successfully written");
                        }
                        catch (System.Exception e)
                        {
                            Console.WriteLine("Failed to save HelloWall.ifc");
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Failed to initialise the model");
                }
            }
        }

        private string ReadTxt(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(_Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractColumn", CommandFlags.Modal)]
        public void ThExtractColumn()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var columnDbExtension = new ThStructureColumnDbExtension(Active.Database))
            {
                columnDbExtension.BuildElementCurves();
                columnDbExtension.ColumnCurves.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeam", CommandFlags.Modal)]
        public void ThExtractBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThBeamRecognitionEngine beamEngine = new ThBeamRecognitionEngine())
            {
                beamEngine.Recognize(Active.Database);
                beamEngine.Elements.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
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
            using (var shearWallDbExtension = new ThStructureShearWallDbExtension(Active.Database))
            {
                shearWallDbExtension.BuildElementCurves();
                shearWallDbExtension.ShearWallCurves.ForEach(o => acadDatabase.ModelSpace.Add(o));
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnect", CommandFlags.Modal)]
        public void ThExtractBeamConnect()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var thBeamTypeRecogitionEngine = new ThBeamConnectRecogitionEngine())
            {
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                thBeamTypeRecogitionEngine.Recognize(Active.Database);
                stopwatch.Stop();
                TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
                Active.Editor.WriteMessage("\n本次使用了：" + timespan.TotalSeconds+"秒");
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex=1));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex = 2));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex = 3));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => n.Outline.ColorIndex = 4));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n=>acadDatabase.ModelSpace.Add(n.Outline)));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(n.Outline)));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(n.Outline)));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(n.Outline)));

                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m => m.Beams.ForEach(n => acadDatabase.ModelSpace.Add(CreateBeamMarkText(n))));
            }
        }
        private DBText CreateBeamMarkText(ThIfcBeam thIfcBeam)
        {
            string message = "";
            message += "Type：" + thIfcBeam.ComponentType + "，";
            message += "W：" + thIfcBeam.Width + "，";
            message += "H：" + thIfcBeam.Height;
            DBText dbText = new DBText();
            dbText.TextString = message;
            dbText.Position = ThGeometryTool.GetMidPt(thIfcBeam.StartPoint, thIfcBeam.EndPoint);
            dbText.HorizontalMode = TextHorizontalMode.TextCenter;
            dbText.Layer = "0";
            return dbText;
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnectEx", CommandFlags.Modal)]
        public void ThExtractBeamConnectEx()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var thBeamTypeRecogitionEngine = new ThBeamConnectRecogitionEngine())
            {
                thBeamTypeRecogitionEngine.Recognize(Active.Database);
                thBeamTypeRecogitionEngine.PrimaryBeamLinks.ForEach(m =>
                {
                   var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
                thBeamTypeRecogitionEngine.HalfPrimaryBeamLinks.ForEach(m =>
                {
                    var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 2;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
                thBeamTypeRecogitionEngine.OverhangingPrimaryBeamLinks.ForEach(m =>
                {
                    var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 3;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
                thBeamTypeRecogitionEngine.SecondaryBeamLinks.ForEach(m =>
                {
                    var outline = m.CreateExtendBeamOutline(50.0);
                    outline.Item1.ColorIndex = 4;
                    acadDatabase.ModelSpace.Add(outline.Item1);
                });
            }
        }

        [CommandMethod("TIANHUACAD", "ThExtractLaneLine", CommandFlags.Modal)]
        public void ThExtractLaneLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneLineRecognitionEngine laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                laneLineEngine.Recognize(Active.Database);
                laneLineEngine.LaneCurves.ForEach(o => acadDatabase.ModelSpace.Add(o));
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
                thIfcLineBeam.Direction = thIfcLineBeam.StartPoint.GetVectorTo(thIfcLineBeam.EndPoint);
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
                    thSegmentService.SegmentAll();
                    segments.AddRange(thSegmentService.Segments);
                }                
                ThSplitLinearBeamService thSplitLineBeam = new ThSplitLinearBeamService(thIfcLineBeam, segments);
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

                Point3dCollection pts = new Point3dCollection();
                while (true)
                {
                    var ptres = Active.Editor.GetPoint("\n select inters pt");
                    if (ptres.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                    {
                        pts.Add(ptres.Value);
                    }
                    else
                    {
                        break;
                    }
                }
                ThSegmentServiceExtension thSegmentServiceExtension = new ThSegmentServiceExtension(polyline);
                thSegmentServiceExtension.FindPairSegment(pts);

                thSegmentServiceExtension.LinearPairPts.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(new Line(o.Item1, o.Item2));
                });
                thSegmentServiceExtension.ArcPairPts.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(new Line(o.Item1, o.Item2));
                });
            }
        }
#if DEBUG
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
                // 暂时不支持弧梁
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Line)).DxfName,
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                // 梁线的图元图层
                var layers = ThBeamLayerManager.GeometryLayers(acdb.Database);
                var filterlist = OpFilter.Bulid(o =>
                    o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames) &
                    o.Dxf((int)DxfCode.LayerName) == string.Join(",", layers.ToArray()));
                var entSelected = Active.Editor.GetSelection(options, filterlist);
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
#endif
    }
}
