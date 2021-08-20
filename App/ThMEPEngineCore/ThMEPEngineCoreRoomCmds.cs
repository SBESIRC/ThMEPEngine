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
        /// 提取空间
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
                var layerId = acadDatabase.Database.CreateAILayer("AI-空间框线", 30);
                engine.Elements.Cast<ThIfcRoom>().Select(r => r.Boundary as Polyline).ForEach(p =>
                {
                    p.LayerId = layerId;
                    p.ConstantWidth = 20;
                    acadDatabase.ModelSpace.Add(p);
                });
            }
        }

        /// <summary>
        /// 空间拾取
        /// </summary>
        [CommandMethod("TIANHUACAD", "THKJSQ", CommandFlags.Modal)]
        public void THKJSQ()
        {
            using (var acadDb = AcadDatabase.Active())
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

                // 选择房间点
                var selectPts = new List<Point3d>();
                while (true)
                {
                    var ppo = new PromptPointOptions("\n选择房间内的一点");
                    ppo.AllowNone = true;
                    ppo.AllowArbitraryInput = true;
                    var ptRes = Active.Editor.GetPoint(ppo);
                    if (ptRes.Status == PromptStatus.OK)
                    {
                        selectPts.Add(ptRes.Value.TransformBy(Active.Editor.CurrentUserCoordinateSystem));
                    }
                    else
                    {
                        break;
                    }
                }
                if(selectPts.Count==0)
                {
                    return;
                }

                //提取数据+封面
                Roomdata data = new Roomdata(acadDb.Database, frame.Vertices());
                //Roomdata构造函数非常慢，可能是其他元素提取导致的
                data.Deburring();
                var totaldata = data.MergeData();
                selectPts = selectPts.Where(o => !data.ContatinPoint3d(o)).ToList();
                var builder = new ThRoomOutlineBuilderEngine(totaldata);

                if (builder.Count == 0)
                    return;
                //从CAD中获取点
                builder.CloseAndFilter();

                //交互+获取房间
                // 输出房间
                var layerId = acadDb.Database.CreateAILayer("AI-房间框线", 30);

                selectPts.ForEach(p =>
                {
                    if(!builder.RoomContainPoint(p))
                    {
                        builder.Build(p);
                    }
                });
                builder.results.ForEach(e => 
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
    }
}
