using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPHVAC.SmokeProofSystem.Model;

namespace ThMEPHVAC.SmokeProofSystem.Data
{
    public static class CalStructrueService
    {
        /// <summary>
        /// 获取框线
        /// </summary>
        /// <param name="acadDatabase"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, ObjectIdCollection> GetFrame(AcadDatabase acadDatabase)
        {
            Dictionary<Polyline, ObjectIdCollection> frameLst = new Dictionary<Polyline, ObjectIdCollection>();
            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择楼层框定",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                    RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return frameLst;
            }

            foreach (ObjectId obj in result.Value.GetObjectIds())
            {
                var frame = acadDatabase.Element<BlockReference>(obj);
                var objs = new DBObjectCollection();
                frame.Explode(objs);
                var boundary = objs.OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                frameLst.Add(boundary, new ObjectIdCollection() { obj });
            }
            return frameLst;
        }

        /// <summary>
        /// 获取房间
        /// </summary>
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="acdb"></param>
        /// <param name="originTransformer"></param>
        /// <returns></returns>
        public static List<ThIfcRoom> GetRoomInfo(AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var roomEngine = new ThAIRoomOutlineExtractionEngine();
            roomEngine.ExtractFromMS(acdb.Database);
            roomEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

            var markEngine = new ThAIRoomMarkExtractionEngine();
            markEngine.ExtractFromMS(acdb.Database);
            markEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

            var boundaryEngine = new ThAIRoomOutlineRecognitionEngine();
            boundaryEngine.Recognize(roomEngine.Results, new Point3dCollection());
            var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
            var markRecEngine = new ThAIRoomMarkRecognitionEngine();
            markRecEngine.Recognize(markEngine.Results, new Point3dCollection());
            var marks = markRecEngine.Elements.Cast<ThIfcTextNote>().ToList();
            var builder = new ThRoomBuilderEngine();
            builder.Build(rooms, marks);

            return rooms;
        }

        /// <summary>
        /// 获取放烟计算图块
        /// </summary>
        /// <param name="acdb"></param>
        /// <param name="polyline"></param>
        public static List<SmkBlockModel> GetSmkBlock(this Polyline polyline, AcadDatabase acdb, ThMEPOriginTransformer originTransformer)
        {
            var blocks = acdb.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.GetEffectiveName() == ThMEPHAVCCommon.SMOKE_PROOF_BLOCK_NAME)
                    .Select(x => x.Clone() as BlockReference)
                    .ToList();
            blocks.ForEach(x => originTransformer.Transform(x));
            blocks = blocks.Where(o =>
            {
                var pts = o.GeometricExtents;
                var position = new Point3d((pts.MinPoint.X + pts.MaxPoint.X) / 2, (pts.MinPoint.Y + pts.MaxPoint.Y) / 2, 0);
                return polyline.Contains(position);
            }).ToList();
            List<SmkBlockModel> resBlocks = new List<SmkBlockModel>();
            blocks.ForEach(x => resBlocks.Add(new SmkBlockModel(x)));
            return resBlocks;
        }
    }
}
