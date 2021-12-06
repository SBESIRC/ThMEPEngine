using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Model;
using ThMEPElectrical.FireAlarmArea.Service;

namespace ThMEPElectrical.FireAlarmArea
{
    class ThAFASSmokeEngine
    {
        public static void ThFaSmokeHeatLayoutEngine(ThAFASAreaDataQueryService dataQuery, ThAFASSmokeLayoutParameter layoutParameter, out List<ThLayoutPt> layoutResult, out List<Polyline> blindsResult)
        {
            blindsResult = new List<Polyline>();
            layoutResult = new List<ThLayoutPt>();

            foreach (var frame in dataQuery.FrameList)
            {
                try
                {
                    var stairInFrame = layoutParameter.StairPartResult.Where(x => frame.Contains(x));
                    if (stairInFrame.Count() > 0)
                    {
                        continue;
                    }

                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.heat || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeat)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.heat, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameHeat);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.ToPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameHeat], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.heatPrf || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeatPrf)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.heat, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameHeatPrf);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.ToPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameHeatPrf], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }

                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smoke || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeat)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.smoke, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameSmoke);
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokePrf || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeatPrf)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.smoke, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameSmokePrf);
                    }
                }
                catch
                {
                    continue;
                }
            }

        }

        private static void LayoutProcess(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThAFASSmokeLayoutParameter layoutParameter, ThFaSmokeCommon.layoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, layoutParameter.FloorHightIdx, layoutParameter.RootThetaIdx, layoutType);//to do...frame.area need to remove hole's area
            //radius = radius * 0.9;
            DrawUtils.ShowGeometry(frame.GetCentroidPoint(), string.Format("r:{0}", radius), "l0radius", 3, 200,300);

            //区域类型
            var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, layoutParameter.AisleAreaThreshold);
            if (bIsAisleArea == false)
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThFaAreaLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, out localPts, out blines);
                DebugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
            else
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThFaAreaLayoutService.ThFaAreaLayoutCenterline(frame, dataQuery, radius, out localPts, out blines);
                DebugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
        }

        private static void DebugShowFrame(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.heat ? "heat" : "smoke";
            var sCenterLine = isCenterLine == false ? "grid" : "cl";

            //DrawUtils.ShowGeometry(frame, string.Format("l0{0}{1}-room", sCenterLine, stype), 30);
            //DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0{0}{1}-wall", sCenterLine, stype), 10);
            //DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0{0}{1}-column", sCenterLine, stype), 3);
            //DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0{0}{1}-hole", sCenterLine, stype), 140);
            //DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0{0}{1}-layoutArea", sCenterLine, stype), 200);
            //DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0{0}{1}-priority", sCenterLine, stype), 60);

            var pt = frame.GetCentroidPoint();
            var ptNew = new Point3d(pt.X, pt.Y - 350, 0);
            DrawUtils.ShowGeometry(ptNew, string.Format("process：{0}:{1}", stype, sCenterLine), "l0process", 3, 200,300);

            //DrawUtils.ShowGeometry(frame, string.Format("l0room"), 30);
            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0wall"), 10);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0column"), 3);
            //DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0hole"), 140);
            DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0layoutArea"), 200);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0priority"), 60);
            DrawUtils.ShowGeometry(dataQuery.FrameDetectAreaList[frame], string.Format("l0DetectArea"), 96);

        }
        private static void DebugShowResult(Dictionary<Point3d, Vector3d> layoutPts, List<Polyline> blinds, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.heat ? "-heat" : "-smoke";
            var sCenterLine = isCenterLine == false ? "-grid" : "-cl";
            int color = type == ThFaSmokeCommon.layoutType.heat ? 1 : 4;

            foreach (var re in layoutPts)
            {
                DrawUtils.ShowGeometry(re.Key, re.Value, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 200);
                DrawUtils.ShowGeometry(re.Key, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 50);
            }
            DrawUtils.ShowGeometry(blinds, string.Format("l0{0}{1}-blinds", sCenterLine, stype), color);
        }
    }
}
