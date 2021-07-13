using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.BuildRoom.Interface;
using ThMEPEngineCore.BuildRoom.Service;

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
        //[CommandMethod("TIANHUACAD", "THKJSQ", CommandFlags.Modal)]
        //public void THKJSQ()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    using (IRoomBuilder roomBuilder = new ThRoomOutlineBuilderEngine())
        //    {
        //        var result1 = Active.Editor.GetEntity("\n选择框线");
        //        if (result1.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        var result2 = Active.Editor.GetPoint("\n选取房间内一点");
        //        if (result2.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        var data = new ThBuildRoomDataService();
        //        var frame = acadDatabase.Element<Polyline>(result1.ObjectId);
        //        var nFrame = ThMEPFrameService.Normalize(frame);
        //        data.Build(acadDatabase.Database, nFrame.Vertices());
        //        roomBuilder.Build(data);
        //        roomBuilder.Outlines
        //            .Where(r => r.IsContains(result2.Value))
        //            .ForEach(r =>
        //            {
        //                acadDatabase.ModelSpace.Add(r);
        //                r.SetDatabaseDefaults();
        //                r.Layer = "AD-AREA-OUTL";
        //                r.ColorIndex = (int)ColorIndex.BYLAYER;
        //            });
        //    }
        //}
    }
}
