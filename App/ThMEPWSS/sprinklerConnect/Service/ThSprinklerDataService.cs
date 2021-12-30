using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.SprinklerConnect.Service
{
    public class ThSprinklerDataService
    {
        public static Polyline GetFrame()
        {
            var frame = new Polyline();
            //画框，提数据，转数据
            var frameOrig = SelectFrame();
            if (frame != null)
            {
                frame = ProcessFrame(frameOrig);
            }

            return frame;
        }

        private static Polyline SelectFrame()
        {
            var polyline = new Polyline();

            // 获取框线
            var options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "请选择布置区域框线",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var layers = new List<string> { "AI-防火分区" };
            var filter = ThSelectionFilterTool.Build(dxfNames, layers.ToArray());
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return polyline;
            }
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {   //获取外包框
                    var frame = acdb.Element<Polyline>(obj);
                    polyline = frame;
                }
            }

            return polyline;

        }

        private static Polyline ProcessFrame(Polyline frame)
        {
            Polyline nFrame = null;

            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            // Polyline nFrameNormal = ThMEPFrameService.NormalizeEx(frame, tol);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(1);
                nFrame = nFrameNormal;
            }

            return nFrame;
        }
    }
}
