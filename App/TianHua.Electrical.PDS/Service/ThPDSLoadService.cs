using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSLoadService
    {
        public static void Draw(ThPDSProjectGraphNode node, ImageLoadType loadType)
        {
            var info = PickPoints();
            if (info != null)
            {
                var engine = new ThPDSAddDimensionEngine();
                engine.InsertNewLoad(node, loadType, info);
            }
        }

        private static ThPDSInsertBlockInfo PickPoints()
        {
            // 指定负载插入点
            var ppr = Active.Editor.GetPoint("\n请指定负载插入点");
            if (ppr.Status != PromptStatus.OK)
            {
                return null;
            }

            // 指定引线端点
            var ppr2 = Active.Editor.GetPoint("\n请指定引线端点");
            if (ppr2.Status != PromptStatus.OK)
            {
                return null;
            }

            // 指定标注线方向
            var ppr3 = Active.Editor.GetPoint("\n请指定标注线方向");
            if (ppr3.Status != PromptStatus.OK)
            {
                return null;
            }

            // 获取拾取数据
            return new ThPDSInsertBlockInfo()
            {
                InsertPoint = ppr.Value,
                SecondPoint = ppr2.Value,
                ThirdPoint = ppr3.Value,
                Scale = new Scale3d(100.0),
            };
        }
    }
}
