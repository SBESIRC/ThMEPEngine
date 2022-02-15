using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.Diagnostics;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Model;
using ThMEPElectrical.FireAlarmArea.Service;
using ThMEPElectrical.FireAlarmArea;


namespace ThMEPElectrical.FireAlarmArea
{
    class ThAFASGasEngine
    {
        public static void ThFaGasLayoutEngine(ThAFASAreaDataQueryService dataQuery, ThAFASGasLayoutParameter layoutParameter, out List<ThLayoutPt> layoutResult, out List<Polyline> blindsResult)
        {
            blindsResult = new List<Polyline>();
            layoutResult = new List<ThLayoutPt>();
            var tempBlindsResult = new List<Polyline>();

            foreach (var frame in dataQuery.FrameList)
            {
                try
                {
                    var priority = new List<Polyline>();
                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.gasPrf)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.gasPrf, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, tempBlindsResult, localPts, blines, layoutParameter.BlkNameGasPrf);
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.gas)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.gas, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, tempBlindsResult, localPts, blines, layoutParameter.BlkNameGas);
                    }
                }
                catch
                {
                    continue;
                }
            }

            blindsResult = ThFaAreaLayoutService.CleanBlind(tempBlindsResult);

        }

        private static void LayoutProcess(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThAFASGasLayoutParameter layoutParameter, ThFaSmokeCommon.layoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            var blindType = BlindType.CoverArea;
            var radius = layoutParameter.ProtectRadius;

            //区域类型
            var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(dataQuery.FrameLayoutList[frame], radius);
            var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], beamGridWidth, layoutParameter.AisleAreaThreshold);

            if (bIsAisleArea == false)
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea,radius ,beamGridWidth );
                ThFaAreaLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, blindType,out localPts, out blines);
                DebugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
            else
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea, radius, beamGridWidth);
                ThFaAreaLayoutService.ThFaAreaLayoutCenterline(frame, dataQuery, radius, blindType, out localPts, out blines);
                DebugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
        }

        private static void DebugShowFrame(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThFaSmokeCommon.layoutType type, bool isCenterLine, double radius, double beamGridWidth)
        {
            var stype = type == ThFaSmokeCommon.layoutType.gas ? "gas" : "gasEx";
            var sCenterLine = isCenterLine == false ? "grid" : "cl";

            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0{0}{1}-wall", sCenterLine, stype), 10);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0{0}{1}-column", sCenterLine, stype), 3);
            DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0{0}{1}-hole", sCenterLine, stype), 140);
            DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0{0}{1}-layoutArea", sCenterLine, stype), 200);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0{0}{1}-priority", sCenterLine, stype), 60);

            var pt = frame.GetCentroidPoint();
            DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 350 * 0, 0), string.Format("r:{0}", radius), "l0Info", 3, 25, 200);
            DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 350 * 1, 0), string.Format("shrink：{0}", beamGridWidth), "l0Info", 3, 25, 200);
            DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 350 * 2, 0), string.Format("process：{0}:{1}", stype, sCenterLine), "l0Info", 3, 25, 200);

        }

        private static void DebugShowResult(Dictionary<Point3d, Vector3d> layoutPts, List<Polyline> blinds, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.gas ? "-gas" : "-gasEx";
            var sCenterLine = isCenterLine == false ? "-grid" : "-cl";
            int color = type == ThFaSmokeCommon.layoutType.gas ? 1 : 4;

            foreach (var re in layoutPts)
            {
                DrawUtils.ShowGeometry(re.Key, re.Value, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 200);
                DrawUtils.ShowGeometry(re.Key, string.Format("l0{0}{1}-result", sCenterLine, stype), color, 35, 50);
            }
            DrawUtils.ShowGeometry(blinds, string.Format("l0{0}{1}-blinds", sCenterLine, stype), color);
        }
    }
}
