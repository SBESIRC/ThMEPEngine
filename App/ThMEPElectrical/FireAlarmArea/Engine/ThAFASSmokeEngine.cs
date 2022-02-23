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
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.Diagnostics;
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
            var tempBlindsResult = new List<Polyline>();
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
                        ThFaAreaLayoutService.AddResult(layoutResult, tempBlindsResult, localPts, blines, layoutParameter.BlkNameHeat);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.ToPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameHeat], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.heatPrf || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeatPrf)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.heat, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, tempBlindsResult, localPts, blines, layoutParameter.BlkNameHeatPrf);
                        dataQuery.FramePriorityList[frame].AddRange(ThFaAreaLayoutService.ToPriority(localPts, ThFaCommon.blk_size[layoutParameter.BlkNameHeatPrf], layoutParameter.Scale, layoutParameter.priorityExtend));
                    }

                    if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smoke || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeat)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.smoke, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, tempBlindsResult, localPts, blines, layoutParameter.BlkNameSmoke);
                    }
                    else if (layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokePrf || layoutParameter.RoomType[frame] == ThFaSmokeCommon.layoutType.smokeHeatPrf)
                    {
                        LayoutProcess(frame, dataQuery, layoutParameter, ThFaSmokeCommon.layoutType.smoke, out var localPts, out var blines);
                        ThFaAreaLayoutService.AddResult(layoutResult, tempBlindsResult, localPts, blines, layoutParameter.BlkNameSmokePrf);
                    }
                }
                catch (System.Exception ex)
                {
                    var pt = frame.GetPoint3dAt(0);
                    var ptOri = ThAFASDataPass.Instance.Transformer.Reset(pt);
                    var err = System.Environment.NewLine;
                    err = err + string.Format("point:{0},{1} Ori point:{2},{3} ", pt.X, pt.Y, ptOri.X, ptOri.Y) + System.Environment.NewLine;
                    err = err + ex.Message + System.Environment.NewLine;
                    err = err + ex.StackTrace.ToString() + System.Environment.NewLine;

                    var logger = layoutParameter.Log;
                    logger.WriteErrLog(err);

                    continue;
                }
            }

            blindsResult = ThFaAreaLayoutService.CleanBlind(tempBlindsResult);

        }

        private static void LayoutProcess(Polyline frame, ThAFASAreaDataQueryService dataQuery, ThAFASSmokeLayoutParameter layoutParameter, ThFaSmokeCommon.layoutType layoutType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            localPts = new Dictionary<Point3d, Vector3d>();
            blines = new List<Polyline>();

            var blindType = BlindType.CoverArea;
            var radius = ThFaAreaLayoutParamterCalculationService.CalculateRadius(frame.Area, layoutParameter.FloorHightIdx, layoutParameter.RootThetaIdx, layoutType);//to do...frame.area need to remove hole's area
            //区域类型
            var beamGridWidth = ThFaAreaLayoutService.LayoutAreaWidth(dataQuery.FrameLayoutList[frame], radius);
            var bIsAisleArea = ThFaAreaLayoutService.IsAisleArea(frame, dataQuery.FrameHoleList[frame], beamGridWidth, layoutParameter.AisleAreaThreshold);

            if (bIsAisleArea == false)
            {
                DebugShowFrame(frame, dataQuery, layoutType, bIsAisleArea, radius, beamGridWidth);
                ThFaAreaLayoutService.ThFaAreaLayoutGrid(frame, dataQuery, radius, blindType, out localPts, out blines);
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
            var stype = type == ThFaSmokeCommon.layoutType.heat ? "heat" : "smoke";
            var sCenterLine = isCenterLine == false ? "grid" : "cl";

            var pt = frame.GetCentroidPoint();
            DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 350 * 0, 0), string.Format("r:{0}", radius), "l0Info", 3, 25, 200);
            DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 350 * 1, 0), string.Format("shrink：{0}", beamGridWidth), "l0Info", 3, 25, 200);
            DrawUtils.ShowGeometry(new Point3d(pt.X, pt.Y - 350 * 2, 0), string.Format("process：{0}:{1}", stype, sCenterLine), "l0Info", 3, 25, 200);

            DrawUtils.ShowGeometry(frame, string.Format("l0roomFrame"), 30);
            DrawUtils.ShowGeometry(dataQuery.FrameHoleList[frame], string.Format("l0FrameHole"), 150);
            DrawUtils.ShowGeometry(dataQuery.FrameColumnList[frame], string.Format("l0FrameColumn"), 1);
            DrawUtils.ShowGeometry(dataQuery.FrameWallList[frame], string.Format("l0FrameWall"), 1);
            dataQuery.FrameLayoutList[frame].ForEach(x => DrawUtils.ShowGeometry(x, string.Format("l0Framelayout"), 6));
            DrawUtils.ShowGeometry(dataQuery.FrameDetectAreaList[frame], string.Format("l0FrameDetec"), 91);
            DrawUtils.ShowGeometry(dataQuery.FramePriorityList[frame], string.Format("l0FrameEquipment"), 152);

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
