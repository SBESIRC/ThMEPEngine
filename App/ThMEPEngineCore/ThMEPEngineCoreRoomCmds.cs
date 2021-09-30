using System;
using AcHelper;
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
                        TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3"),
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
                acadDatabase.Database.CreateAIRoomMarkLayer();
                engine.Elements.Cast<ThIfcTextNote>().Select(o => o.Geometry).ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o);
                    o.Layer = ThMEPEngineCoreLayerUtils.ROOMMARK;
                });
            }
        }

        /// <summary>
        /// 空间拾取
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJSQ", CommandFlags.Modal)]
        public void THKJSQ()
        {
            //记录用户在选择点的位置绘制的叉
            var signObjs = new DBObjectCollection();
            try
            {   
                // 获取框选范围
                var frame = GetRange();

                // 选择房间点
                var selectPts = new List<Point3d>();
                signObjs = SelectUserPoints(selectPts);

                if (selectPts.Count>0)
                {
                    var roomBoundaries = GetRoomBoundaries(frame, selectPts);
                    PrintRoom(roomBoundaries);
                }
            }
            catch(System.Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
            finally
            {
                EraseSigns(signObjs);
            }
        }

        private void PrintRoom(List<DBObjectCollection> roomResults)
        {
            using (var acadDb = AcadDatabase.Active())
            {               
                //交互+获取房间
                // 输出房间
                var layerId = acadDb.Database.CreateAIRoomOutlineLayer();
                roomResults.ForEach(e =>
                {
                    e.Cast<Entity>().ForEach(o =>
                    {
                        acadDb.ModelSpace.Add(o);
                        o.LayerId = layerId;
                        o.ColorIndex = (int)ColorIndex.BYLAYER;
                        o.LineWeight = LineWeight.ByLayer;
                        o.Linetype = "ByLayer";
                    });
                });
            }
        }

        private List<DBObjectCollection> GetRoomBoundaries(Polyline frame, List<Point3d> selectPts)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                //提取数据+封面
                Roomdata data = new Roomdata(acadDb.Database, frame.Vertices());
                //Roomdata构造函数非常慢，可能是其他元素提取导致的
                data.Deburring();
                var totaldata = data.MergeData();

                selectPts = selectPts.Where(o => !data.ContatinPoint3d(o)).ToList();
                var builder = new ThRoomOutlineBuilderEngine(totaldata);

                if (builder.Count == 0)
                    return new List<DBObjectCollection>();
                //从CAD中获取点
                builder.CloseAndFilter();

                selectPts.ForEach(p =>
                {
                    if (!builder.RoomContainPoint(p))
                    {
                        builder.Build(p);
                    }
                });

                return builder.results;
            }
        }

        private DBObjectCollection SelectUserPoints(List<Point3d> selectPts)
        {
            var signObjs = new DBObjectCollection();
            while (true)
            {
                var ppo = new PromptPointOptions("\n选择房间内的一点");
                ppo.AllowNone = true;
                ppo.AllowArbitraryInput = true;
                var ptRes = Active.Editor.GetPoint(ppo);
                if (ptRes.Status == PromptStatus.OK)
                {
                    using (var acadDb = AcadDatabase.Active())
                    {
                        var signs = CreateSign(ptRes.Value);
                        signs.OfType<Curve>().ForEach(c => signObjs.Add(c));
                        signs.OfType<Curve>().ForEach(c => c.TransformBy(Active.Editor.CurrentUserCoordinateSystem));
                        signs.OfType<Curve>().ForEach(c =>
                        {
                            acadDb.ModelSpace.Add(c);
                            c.ColorIndex = (int)ColorIndex.BYLAYER;
                            c.SetDatabaseDefaults();
                        });
                        selectPts.Add(ptRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem));
                    }
                }
                else
                {
                    break;
                }
            }
            return signObjs;
        }

        private DBObjectCollection CreateSign(Point3d pt)
        {
            var results = new DBObjectCollection();
            var length = 15.0;
            var mt1 = Matrix3d.Rotation(Math.PI * 0.25, Vector3d.ZAxis, Point3d.Origin);
            var mt2 = Matrix3d.Rotation(Math.PI * 0.75, Vector3d.ZAxis, Point3d.Origin);
            var vec1 = Vector3d.XAxis.TransformBy(mt1);
            var vec2 = Vector3d.XAxis.TransformBy(mt2);

            var line1 = new Line(pt + vec1.MultiplyBy(length / 2.0), pt - vec1.MultiplyBy(length / 2.0));
            var line2 = new Line(pt + vec2.MultiplyBy(length / 2.0), pt - vec2.MultiplyBy(length / 2.0));

            results.Add(line1);
            results.Add(line2);
            return results;
        }

        private Polyline GetRange()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                var frame = new Polyline();
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return frame;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame;
            }
        }
        private void EraseSigns(DBObjectCollection signObjs)
        {
            // 删除生成的Signs
            using (var acadDb = AcadDatabase.Active())
            {
                signObjs.OfType<Curve>().ForEach(c =>
                {
                    var entity = acadDb.Element<Entity>(c.ObjectId,true);                    
                    entity.Erase();
                });
            }
        }
    }
}
