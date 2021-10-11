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
using ThMEPLighting.EmgLight.Assistant;

using ThMEPLighting.IlluminationLighting.Data;
using ThMEPLighting.IlluminationLighting.Service;
using ThMEPLighting.IlluminationLighting.Model;

namespace ThMEPLighting.IlluminationLighting
{
    class ThIlluminationEngine
    {
        public static void thIlluminationLayoutEngine(ThIlluminationDataQueryService dataQuery, ThLayoutParameter layoutParameter, out List<ThLayoutPt> layoutResult, out List<Polyline> blindsResult)
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
                    if (layoutParameter.roomType[frame] == ThIlluminationCommon.layoutType.normal || layoutParameter.roomType[frame] == ThIlluminationCommon.layoutType.normalEvac)
                    {
                        layoutProcess(frame, dataQuery, layoutParameter, ThIlluminationCommon.layoutType.normal, out var localPts, out var blines);
                        addResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameN);
                        dataQuery.FramePriorityList[frame].AddRange(toPriority(localPts, ThIlluminationCommon.blk_size[layoutParameter.BlkNameN], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }
                    if (layoutParameter.ifLayoutEmg == true &&
                        (layoutParameter.roomType[frame] == ThIlluminationCommon.layoutType.evacuation || layoutParameter.roomType[frame] == ThIlluminationCommon.layoutType.normalEvac))
                    {
                        layoutProcess(frame, dataQuery, layoutParameter, ThIlluminationCommon.layoutType.evacuation, out var localPts, out var blines);
                        addResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameE);
                    }
                }
                catch
                {
                    continue;
                }
            }

        }

        private static void layoutProcess(Polyline frame, ThIlluminationDataQueryService dataQuery, ThLayoutParameter layoutParameter, ThIlluminationCommon.layoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            var radius = layoutParameter.radiusN;
            if (layoutType == ThIlluminationCommon.layoutType.evacuation)
            {
                radius = layoutParameter.radiusE;
            }

            //区域类型
            var bIsAisleArea = ThIlluminationLayoutService.isAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, layoutParameter.AisleAreaThreshold);
            if (bIsAisleArea == false)
            {
                debugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThIlluminationLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, out localPts, out blines);
                debugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
            else
            {
                debugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThIlluminationLayoutService.ThFaAreaLayoutCenterline(frame, dataQuery, radius, out localPts, out blines);
                debugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
        }

        private static void addResult(List<ThLayoutPt> layoutResult, List<Polyline> blindsResult, Dictionary<Point3d, Vector3d> localPts, List<Polyline> localBlinds, string blkName)
        {
            foreach (var r in localPts)
            {
                layoutResult.Add(new ThLayoutPt() { Pt = r.Key, Dir = r.Value, BlkName = blkName });
            }

            blindsResult.AddRange(localBlinds);
        }

        private static List<Polyline> toPriority(Dictionary<Point3d, Vector3d> localPts, (double, double) size, double Scale, double priorityExtend)
        {
            var priority = new List<Polyline>();

            if (localPts != null && localPts.Count > 0)
            {
                priority = ThParamterCalculationService.getPriorityBoundary(localPts, Scale, size);
                priority = priority.Select(x => x.GetOffsetClosePolyline(priorityExtend)).ToList();
            }
            return priority;
        }

        private static void debugShowFrame(Polyline frame, ThIlluminationDataQueryService dataQuery, ThIlluminationCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThIlluminationCommon.layoutType.normal ? "-n" : "-e";
            var sCenterLine = isCenterLine == false ? "" : "-cl";

            DrawUtils.ShowGeometry(frame, string.Format("l0{0}{1}-room", sCenterLine, stype), 30);
            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0{0}{1}-wall", sCenterLine, stype), 10);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0{0}{1}-column", sCenterLine, stype), 3);
            DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0{0}{1}-hole", sCenterLine, stype), 140);
            DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0{0}{1}-layoutArea", sCenterLine, stype), 200);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0{0}{1}-priority", sCenterLine, stype), 60);
        }

        private static void debugShowResult(Dictionary<Point3d, Vector3d> layoutPts, List<Polyline> blinds, ThIlluminationCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThIlluminationCommon.layoutType.normal ? "-h" : "-s";
            var sCenterLine = isCenterLine == false ? "" : "-cl";
            int color = type == ThIlluminationCommon.layoutType.normal ? 1 : 4;

            foreach (var re in layoutPts)
            {
                DrawUtils.ShowGeometry(re.Key, re.Value, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 200);
                DrawUtils.ShowGeometry(re.Key, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 50);
            }
            DrawUtils.ShowGeometry(blinds, string.Format("l0{0}{1}-blinds", sCenterLine, stype), color);
        }
    }
}
