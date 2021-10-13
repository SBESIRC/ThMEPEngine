using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;
using ThMEPEngineCore.Config;

using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Command;
using ThMEPElectrical.FireAlarm;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Model;
using ThMEPElectrical.FireAlarmSmokeHeat.Service;

namespace ThMEPElectrical.FireAlarmSmokeHeat
{
    class ThFireAlarmSmokeHeatEngine
    {
        public static void thFaSmokeHeatLayoutEngine(ThSmokeDataQueryService dataQuery, ThFaAreaLayoutParameter layoutParameter, out List<ThLayoutPt> layoutResult, out List<Polyline> blindsResult)
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

                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.heat || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeat)
                    {
                        layoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.heat, out var localPts, out var blines);
                        ThFaAreaLayoutService.addResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameHeat);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.toPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameHeat], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.heatPrf || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeatPrf)
                    {
                        layoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.heat, out var localPts, out var blines);
                        ThFaAreaLayoutService.addResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameHeatPrf);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.toPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameHeatPrf], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }

                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smoke || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeat)
                    {
                        layoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.smoke, out var localPts, out var blines);
                        ThFaAreaLayoutService.addResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameSmoke);
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokePrf || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeatPrf)
                    {
                        layoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.smoke, out var localPts, out var blines);
                        ThFaAreaLayoutService.addResult(layoutResult, blindsResult, localPts, blines, layoutParameter.BlkNameSmokePrf);
                    }
                }
                catch
                {
                    continue;
                }
            }

        }

        private static void layoutProcess(Polyline frame, ThSmokeDataQueryService dataQuery, ThFaAreaLayoutParameter layoutParameter, ThFaSmokeCommon.layoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            var radius = ThFaAreaLayoutParamterCalculationService.calculateRadius(frame.Area, layoutParameter.FloorHightIdx, layoutParameter.RootThetaIdx, layoutType);//to do...frame.area need to remove hole's area
            DrawUtils.ShowGeometry(frame.GetCentroidPoint(), string.Format("r:{0}", radius), "l0radius");

            //区域类型
            var bIsAisleArea = ThFaAreaLayoutService.isAisleArea(frame, dataQuery.FrameHoleList[frame], radius * 0.8, layoutParameter.AisleAreaThreshold);
            if (bIsAisleArea == false)
            {
                debugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThFaAreaLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, out localPts, out blines);
                debugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
            else
            {
                debugShowFrame(frame, dataQuery, layoutType, bIsAisleArea);
                ThFaAreaLayoutService.ThFaAreaLayoutCenterline(frame, dataQuery, radius, out localPts, out blines);
                debugShowResult(localPts, blines, layoutType, bIsAisleArea);
            }
        }

        private static void debugShowFrame(Polyline frame, ThSmokeDataQueryService dataQuery, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.heat ? "-h" : "-s";
            var sCenterLine = isCenterLine == false ? "" : "-cl";

            DrawUtils.ShowGeometry(frame, string.Format("l0{0}{1}-room", sCenterLine, stype), 30);
            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0{0}{1}-wall", sCenterLine, stype), 10);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0{0}{1}-column", sCenterLine, stype), 3);
            DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0{0}{1}-hole", sCenterLine, stype), 140);
            DrawUtils.ShowGeometry(dataQuery.FrameLayoutList[frame].Cast<Entity>().ToList(), string.Format("l0{0}{1}-layoutArea", sCenterLine, stype), 200);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0{0}{1}-priority", sCenterLine, stype), 60);
        }
        private static void debugShowResult(Dictionary<Point3d, Vector3d> layoutPts, List<Polyline> blinds, ThFaSmokeCommon.layoutType type, bool isCenterLine)
        {
            var stype = type == ThFaSmokeCommon.layoutType.heat ? "-h" : "-s";
            var sCenterLine = isCenterLine == false ? "" : "-cl";
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
