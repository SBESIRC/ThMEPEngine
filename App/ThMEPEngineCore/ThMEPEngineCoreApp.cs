using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Linq2Acad;
using AcHelper;
using ThCADCore.NTS;
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
using NFox.Cad.Collections;
using TianHua.AutoCAD.Utility.ExtensionTools;

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
                var rangeRes = Active.Editor.GetEntity("\nSelect a range polyline");
                Polyline range = acadDatabase.Element<Polyline>(rangeRes.ObjectId);
                columnRecognitionEngine.Recognize(acadDatabase.Database, range.Vertices());
                columnRecognitionEngine.Elements.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
            }
        }
        [CommandMethod("TIANHUACAD", "THExtractBeam", CommandFlags.Modal)]
        public void ThExtractBeam()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThBeamRecognitionEngine beamEngine = new ThBeamRecognitionEngine())
            {
                var rangeRes=Active.Editor.GetEntity("\nSelect a range polyline");
                Polyline range = acadDatabase.Element<Polyline>(rangeRes.ObjectId);
                beamEngine.Recognize(Active.Database, range.Vertices());
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
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            {
                var rangeRes = Active.Editor.GetEntity("\nSelect a range polyline");
                Polyline range = acadDatabase.Element<Polyline>(rangeRes.ObjectId);
                shearWallEngine.Recognize(acadDatabase.Database, range.Vertices());
                shearWallEngine.Elements.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractBeamConnect", CommandFlags.Modal)]
        public void ThExtractBeamConnect()
        {
            List<ThBeamLink> totalBeamLinks = new List<ThBeamLink>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var thBeamTypeRecogitionEngine = new ThBeamConnectRecogitionEngine())
            {
                var rangeRes = Active.Editor.GetEntity("\nSelect a range polyline");
                Polyline range = acadDatabase.Element<Polyline>(rangeRes.ObjectId);
                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                thBeamTypeRecogitionEngine.Recognize(Active.Database, range.Vertices());
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
                var rangeRes = Active.Editor.GetEntity("\nSelect a range polyline");
                Polyline range = acadDatabase.Element<Polyline>(rangeRes.ObjectId);
                thBeamTypeRecogitionEngine.Recognize(Active.Database, range.Vertices());
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

                var otherRes = Active.Editor.GetEntity("\nselect a polyline");
                Polyline otherPolyline = acadDatabase.Element<Polyline>(otherRes.ObjectId);
                bool res = polyline.Intersects(otherPolyline);
                ThSegmentService thSegmentService = new ThSegmentService(polyline);
                thSegmentService.SegmentAll(new CalBeamStruService());
                thSegmentService.Segments.ForEach(o => acadDatabase.ModelSpace.Add(o.Outline));
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
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Arc)).DxfName,
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
