using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Business.UserInteraction
{
    /// <summary>
    /// 用户手选图元提取器
    /// </summary>
    public class EntityPicker
    {
        // 选择的图元
        public static List<PolygonInfo> MakeUserPickPolys(string hintText= "请选择要排布的区域框线")
        {
            var polylines = new List<PolygonInfo>();
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = hintText,
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
                return polylines;
            }
            using (AcadDatabase acdb = AcadDatabase.Active())
            {

                List<Curve> frameLst = new List<Curve>();
                foreach (ObjectId obj in result.Value.GetObjectIds())
                {
                    var frame = acdb.Element<Polyline>(obj);
                    frameLst.Add(frame.Clone() as Polyline);
                }
                var plines = HandleFrame(frameLst);
                var keyValues = CalHoles(plines);
                foreach (var keyVal in keyValues) 
                {
                    polylines.Add(new PolygonInfo(keyVal.Key, keyVal.Value));
                }
            }
            return polylines;
        }
        /// <summary>
        /// 计算外包框和其中的洞
        /// </summary>
        /// <param name="frames"></param>
        /// <returns></returns>
        private static Dictionary<Polyline, List<Polyline>> CalHoles(List<Polyline> frames)
        {
            frames = frames.OrderByDescending(x => x.Area).ToList();

            Dictionary<Polyline, List<Polyline>> holeDic = new Dictionary<Polyline, List<Polyline>>(); //外包框和洞口
            while (frames.Count > 0)
            {
                var firFrame = frames[0];
                frames.Remove(firFrame);
                var holes = frames.Where(x => firFrame.Contains(x)).ToList();
                frames.RemoveAll(x => holes.Contains(x));

                holeDic.Add(firFrame, holes);
            }

            return holeDic;
        }

        /// <summary>
        /// 处理外包框线
        /// </summary>
        /// <param name="frameLst"></param>
        /// <returns></returns>
        private static List<Polyline> HandleFrame(List<Curve> frameLst)
        {
            var retPlines = new List<Polyline>();
            var allPolylines = frameLst.Cast<Polyline>().ToList();
            foreach (var item in allPolylines) 
            {
                //retPlines
                if (item == null || item.Area < 1000)
                    continue;
                if (item is Polyline pline && pline.Closed)
                {
                    var tempPL = pline.Buffer(50)[0] as Polyline;
                    tempPL = tempPL.Buffer(-50)[0] as Polyline;
                    retPlines.Add(tempPL);
                }
                else if (item is Polyline secondPL && !secondPL.Closed && secondPL.StartPoint.DistanceTo(secondPL.EndPoint) < 1000) 
                {
                    secondPL.Closed = true;
                    var buffers = secondPL.Buffer(50);
                    if (buffers == null || buffers.Count < 1)
                        continue;
                    var tempPL = buffers[0] as Polyline;
                    tempPL = tempPL.Buffer(-50)[0] as Polyline;
                    retPlines.Add(tempPL);
                }
            }
            return retPlines;
        }

    }

}
