using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Model;
using ThMEPElectrical.FireAlarmSmokeHeat.Service;
using ThMEPElectrical.FireAlarmSmokeHeat;

using ThFaAreaLayoutParameter = ThMEPElectrical.FireAlarmCombustibleGas.Model.ThFaAreaLayoutParameter;

namespace ThMEPElectrical.FireAlarmCombustibleGas
{
    class ThFireAlarmGasEngine
    {
        public static void ThFaGasLayoutEngine(ThSmokeDataQueryService dataQuery, ThFaAreaLayoutParameter layoutParameter, out List<ThLayoutPt> layoutResult, out List<Polyline> blindsResult)
        {
            blindsResult = new List<Polyline>();
            layoutResult = new List<ThLayoutPt>();

            foreach (var frame in dataQuery.FrameList)
            {
                try
                {
                    var priority = new List<Polyline>();
                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.gasPrf)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.gasPrf, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameGasPrf);
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.gas)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.gas, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameGasPrf);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private static void LayoutProcess(Polyline frame, ThSmokeDataQueryService dataQuery, ThFaAreaLayoutParameter layoutParameter, ThFaSmokeCommon.layoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            var radius = layoutParameter.ProtectRadius;

            DrawUtils.ShowGeometry(frame.GetCentroidPoint(), string.Format("r:{0}", radius), "l0radius");
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

        private static void DebugShowFrame(Polyline frame, ThSmokeDataQueryService dataQuery, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.gas ? "-g" : "-gp";
            var sCenterLine = isCenterLine == false ? "" : "-cl";

            DrawUtils.ShowGeometry(frame, string.Format("l0{0}{1}-room", sCenterLine, stype), 30);
            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0{0}{1}-wall", sCenterLine, stype), 10);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0{0}{1}-column", sCenterLine, stype), 3);
            DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0{0}{1}-hole", sCenterLine, stype), 140);
            DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0{0}{1}-layoutArea", sCenterLine, stype), 200);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0{0}{1}-priority", sCenterLine, stype), 60);
        }

        private static void DebugShowResult(Dictionary<Point3d, Vector3d> layoutPts, List<Polyline> blinds, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.gas ? "-g" : "-gp";
            var sCenterLine = isCenterLine == false ? "" : "-cl";
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
