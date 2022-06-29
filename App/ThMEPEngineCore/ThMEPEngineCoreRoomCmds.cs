using System;
using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.CAD;
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
    public class ThMEPEngineCoreRoomCmds
    {
        /// <summary>
        /// 空间提取
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJTQ", CommandFlags.Modal)]
        public void THKJTQ()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                // 从外参中提取房间
                var frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var engine = new ThDB3RoomOutlineRecognitionEngine();
                engine.Recognize(acadDatabase.Database, frame.Vertices());

                // 输出房间
                var markLayerId = acadDatabase.Database.CreateAIRoomMarkLayer();
                var outlineLayerId = acadDatabase.Database.CreateAIRoomOutlineLayer();
                var textStyleId = acadDatabase.TextStyles.Element("TH-STYLE3").ObjectId;
                engine.Elements.OfType<ThIfcRoom>().ForEach(r =>
                {
                    // 轮廓线
                    var outline = r.Boundary as Polyline;
                    outline.ConstantWidth = 20;
                    outline.LayerId = outlineLayerId;
                    acadDatabase.ModelSpace.Add(outline);

                    // 名称
                    var dbText = new DBText
                    {
                        TextString = r.Name,
                        TextStyleId = textStyleId,
                        Height = 300,
                        WidthFactor = 0.7,
                        Justify = AttachmentPoint.MiddleCenter,
                        LayerId = markLayerId,
                    };
                    dbText.AlignmentPoint = outline.GetMaximumInscribedCircleCenter();
                    acadDatabase.ModelSpace.Add(dbText);
                });
            }
        }

        /// <summary>
        /// 空间名称提取
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJMCTQ", CommandFlags.Modal)]
        public void THKJMCTQ()
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
                var engine = new ThDB3RoomMarkRecognitionEngine();
                engine.Recognize(acadDatabase.Database, frame.Vertices());
                var markLayerId = acadDatabase.Database.CreateAIRoomMarkLayer();
                var textStyleId = acadDatabase.TextStyles.Element("TH-STYLE3").ObjectId;
                engine.Elements.Cast<ThIfcTextNote>().ForEach(o =>
                {
                    var dbText = new DBText
                    {
                        TextString = o.Text,
                        TextStyleId = textStyleId,
                        Height = 300,
                        WidthFactor = 0.7,
                        Justify = AttachmentPoint.MiddleCenter,
                        LayerId = markLayerId,
                    };
                    dbText.AlignmentPoint = o.Geometry.GetMaximumInscribedCircleCenter();
                    acadDatabase.ModelSpace.Add(dbText);
                });
            }
        }

        /// <summary>
        /// 空间拾取
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJSQ", CommandFlags.Modal)]
        public void THKJSQ()
        {
            // 获取选择框
            var frame = new Polyline();
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
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
            }
            if (frame.Area < 1.0)
            {
                return;
            }

            // 原始数据处理
            Active.Editor.WriteLine("\n数据分析中......");
            var roomPickUpService = new ThKJSQInteractionService();
            roomPickUpService.Process(Active.Database, frame.Vertices());

            // 拾取空间
            roomPickUpService.Run();

            // 输出结果到图纸
            if (roomPickUpService.Status == PickUpStatus.OK)
            {
                roomPickUpService.PrintRooms();
            }
        }

        /// <summary>
        /// 空间绘制
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJHZ", CommandFlags.Modal)]
        public void THKJHZ()
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.CreateAIRoomOutlineLayer();
                acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMOUTLINE);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }

        /// <summary>
        /// 空间分割
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJFG", CommandFlags.Modal)]
        public void THKJFG()
        {
            using (var acdb = AcadDatabase.Active())
            {
                acdb.Database.CreateAIRoomSplitlineLayer();
                acdb.Database.SetCurrentLayer(ThMEPEngineCoreLayerUtils.ROOMSPLITLINE);
                Active.Document.SendStringToExecute("_Pline ", true, false, true);
            }
        }

        /// <summary>
        /// 空间中心线
        /// </summary>

        [CommandMethod("TIANHUACAD", "THKJZX", CommandFlags.Modal)]
        public void THKJZX()
        {
#if (ACAD2016 || ACAD2018)
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择空间框线",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames);
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                // 获取空间框线
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }

                // 简化空间框线
                var simplifer = new ThRoomOutlineSimplifier();
                objs = simplifer.Simplify(objs);
                objs = simplifer.MakeValid(objs);
                objs = simplifer.Simplify(objs);

                // 提取中心线
                ThMEPEngineCoreLayerUtils.CreateAICenterLineLayer(acadDatabase.Database);
                objs.BuildArea()
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        ThMEPPolygonService.CenterLine(e)
                        .ToCollection()
                        .LineMerge()
                        .OfType<Polyline>()
                        .ForEach(o =>
                        {
                            var centerline =  o.TPSimplify(1.0);
                            acadDatabase.ModelSpace.Add(centerline);
                            centerline.Layer = ThMEPEngineCoreLayerUtils.CENTERLINE;
                        });
                    });
            }
#else
            Active.Editor.WriteLine("此功能只支持CAD2016暨以上版本");
#endif
        }
    }
}
