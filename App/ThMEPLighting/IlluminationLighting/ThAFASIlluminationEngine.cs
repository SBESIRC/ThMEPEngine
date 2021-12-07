using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPLighting.IlluminationLighting.Model;

namespace ThMEPLighting.IlluminationLighting
{
    class ThAFASIlluminationEngine
    {
        public static void ThFaIlluminationLayoutEngine(ThAFASAreaDataQueryService dataQuery, ThAFASIlluminationLayoutParameter layoutParameter, out List<ThLayoutPt> layoutResult, out List<Polyline> blindsResult)
        {
            blindsResult = new List<Polyline>();
            layoutResult = new List<ThLayoutPt>();

            foreach (var frame in dataQuery.FrameList)
            {
                try
                {
                    var stairInFrame = layoutParameter.stairPartResult.Where(x => frame.Contains(x));
                    if (stairInFrame.Count() > 0)
                    {
                        continue;
                    }

                    var priority = new List<Polyline>();
                    if (layoutParameter.roomType[frame] == ThIlluminationCommon.LayoutType.normal || layoutParameter.roomType[frame] == ThIlluminationCommon.LayoutType.normalEvac)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThIlluminationCommon.LayoutType.normal, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameN);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.ToPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameN], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }
                    if (layoutParameter.ifLayoutEmg == true &&
                        (layoutParameter.roomType[frame] == ThIlluminationCommon.LayoutType.evacuation || layoutParameter.roomType[frame] == ThIlluminationCommon.LayoutType.normalEvac))
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThIlluminationCommon.LayoutType.evacuation, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameE);
                    }
                }
                catch
                {
                    continue;
                }
            }

        }

        private static void LayoutProcess(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThAFASIlluminationLayoutParameter layoutParameter, ThIlluminationCommon.LayoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            var blindType = BlindType.CoverArea;
            var radius = layoutParameter.radiusN;
            if (layoutType == ThIlluminationCommon.LayoutType.evacuation)
            {
                radius = layoutParameter.radiusE;
            }
            DrawUtils.ShowGeometry(frame.GetCentroidPoint(), string.Format("r:{0}", radius), "l0radius", 3, 200, 300);


            //区域类型
            var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, layoutParameter.AisleAreaThreshold);
            if (bIsAisleArea == false)
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThFaAreaLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, blindType, out localPts, out blines);
                DebugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
            else
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThFaAreaLayoutService.ThFaAreaLayoutCenterline(frame, dataQuery, radius, blindType, out localPts, out blines);
                DebugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
        }

        private static void DebugShowFrame(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThIlluminationCommon.LayoutType type, bool isCenterLine)
        {
            var stype = type == ThIlluminationCommon.LayoutType.normal ? "normal" : "emergency";
            var sCenterLine = isCenterLine == false ? "grid" : "cl";

            var pt = frame.GetCentroidPoint();
            var ptNew = new Point3d(pt.X, pt.Y - 350, 0);
            DrawUtils.ShowGeometry(ptNew, string.Format("process：{0}:{1}", stype, sCenterLine), "l0process", 3, 200, 300);

            //DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0wall"), 10);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0column"), 3);
            //DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
            DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0layoutArea"), 200);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0priority"), 60);

        }

        private static void DebugShowResult(Dictionary<Point3d, Vector3d> layoutPts, List<Polyline> blinds, ThIlluminationCommon.LayoutType type, bool isCenterLine)
        {
            var stype = type == ThIlluminationCommon.LayoutType.normal ? "-normal" : "-emergency";
            var sCenterLine = isCenterLine == false ? "-grid" : "-cl";
            int color = type == ThIlluminationCommon.LayoutType.normal ? 1 : 4;

            foreach (var re in layoutPts)
            {
                DrawUtils.ShowGeometry(re.Key, re.Value, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 200);
                DrawUtils.ShowGeometry(re.Key, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 50);
            }
            DrawUtils.ShowGeometry(blinds, string.Format("l0{0}{1}-blinds", sCenterLine, stype), color);
        }
    }
}
