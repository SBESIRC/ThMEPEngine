using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Command;

namespace ThMEPWSS.SprinklerConnect.Service
{
    class ThSprinklerDataService
    {
        public static Polyline GetFrame()
        {
            var frame = new Polyline();
            //画框，提数据，转数据
            var frameOrig = selectFrame();
            if (frame != null)
            {
                 frame = processFrame(frameOrig);
            }

            return frame;
        }

        private static Polyline selectFrame()
        {
            var polyline = new Polyline();

            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
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

        private static Polyline processFrame(Polyline frame)
        {
            Polyline nFrame = null;
            var tol = 1000;

            Polyline nFrameNormal = ThMEPFrameService.Normalize(frame);
            // Polyline nFrameNormal = ThMEPFrameService.NormalizeEx(frame, tol);
            if (nFrameNormal.Area > 10)
            {
                nFrameNormal = nFrameNormal.DPSimplify(1);
                nFrame = nFrameNormal;
            }

            return nFrame;
        }

        /// <summary>
        /// 将数据转回原点。同时返回transformer
        /// </summary>
        /// <param name="geos"></param>
        /// <returns></returns>
        //public static ThMEPOriginTransformer transformToOrig(Point3dCollection pts, List<ThGeometry> geos)
        //{
        //    ThMEPOriginTransformer transformer = null;

        //    if (pts.Count > 0)
        //    {
        //        var center = pts.Envelope().CenterPoint();
        //        transformer = new ThMEPOriginTransformer(center);
        //    }

        //    foreach (var o in geos)
        //    {
        //        if (o.Boundary != null)
        //        {
        //            transformer.Transform(o.Boundary);
        //        }
        //    }

        //    ThFireAlarmUtils.MoveToXYPlane(geos);

        //    return transformer;
        //}


    }
}
