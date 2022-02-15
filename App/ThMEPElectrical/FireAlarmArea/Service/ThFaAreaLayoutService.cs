using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using GeometryExtensions;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Command;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Data;
using ThMEPElectrical.FireAlarmArea.Model;

namespace ThMEPElectrical.FireAlarmArea.Service
{
    public class ThFaAreaLayoutService
    {
        public static void ThFaAreaLayoutGrid(Polyline frame, ThAFASAreaDataQueryService dataQuery, double radius, BlindType blindType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            localPts = new Dictionary<Point3d, Vector3d>();
            blines = new List<Polyline>();
            //ucs处理


            //engine
            var layoutCmd = new AlarmSensorLayoutCmd();
            layoutCmd.frame = frame;
            layoutCmd.holeList = dataQuery.FrameHoleList[frame];
            layoutCmd.layoutList = dataQuery.FrameLayoutList[frame];
            layoutCmd.wallList = dataQuery.FrameWallList[frame];
            layoutCmd.columns = dataQuery.FrameColumnList[frame];
            layoutCmd.prioritys = dataQuery.FramePriorityList[frame];
            layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
            layoutCmd.protectRadius = radius;
            layoutCmd.equipmentType = blindType;

            layoutCmd.Execute();

            if (layoutCmd.layoutPoints != null && layoutCmd.layoutPoints.Count > 0)
            {
                foreach (var pt in layoutCmd.layoutPoints)
                {
                    var ucsPt = layoutCmd.ucs.Where(x => x.Key.Contains(pt)).FirstOrDefault();
                    if (ucsPt.Value != null)
                    {
                        localPts.Add(pt, ucsPt.Value.GetNormal());
                    }
                    else
                    {
                        localPts.Add(pt, new Vector3d(0, 1, 0));
                    }
                }
            }

            blines.AddRange(layoutCmd.blinds);
        }

        public static void ThFaAreaLayoutCenterline(Polyline frame, ThAFASAreaDataQueryService dataQuery, double radius, BlindType blindType, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
        {
            localPts = new Dictionary<Point3d, Vector3d>();
            blines = new List<Polyline>();

            //engine
            var layoutCmd = new FireAlarmSystemLayoutCommand();
            layoutCmd.frame = frame;
            layoutCmd.holeList = dataQuery.FrameHoleList[frame];
            layoutCmd.layoutList = dataQuery.FrameLayoutList[frame];
            layoutCmd.wallList = dataQuery.FrameWallList[frame];
            layoutCmd.columns = dataQuery.FrameColumnList[frame];
            layoutCmd.prioritys = dataQuery.FramePriorityList[frame];
            layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
            layoutCmd.radius = radius;
            layoutCmd.equipmentType = blindType;

            layoutCmd.Execute();

            if (layoutCmd.pointsWithDirection != null && layoutCmd.pointsWithDirection.Count > 0)
            {
                foreach (var pt in layoutCmd.pointsWithDirection)
                {
                    localPts.Add(pt.Key, pt.Value.GetNormal());
                }
            }
            blines.AddRange(layoutCmd.blinds);
        }

        //public static bool IsAisleArea(Polyline frame, List<Polyline> HoleList, double shrinkValue, double threshold)
        //{
        //    return ThMEPPolygonShapeRecognitionService.IsAisle(
        //        frame,
        //        HoleList,
        //        shrinkValue,
        //        threshold);
        //}

        /// <summary>
        /// threshold:non aisle area
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="HoleList"></param>
        /// <param name="shrinkValue"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public static bool IsAisleArea(Polyline frame, List<Polyline> HoleList, double shrinkValue, double threshold)
        {
            var ans = ThMEPPolygonShapeRecognitionService.IsAisleBufferShrinkFrame(frame, HoleList, shrinkValue, threshold);
            return ans;
        }

        public static bool IsAisleArea2(Polyline frame, List<Polyline> HoleList, double shrinkValue, double threshold)
        {
            var isAisle = true;

            var objs = new DBObjectCollection
            {
                frame
            };
            HoleList.ForEach(x => objs.Add(x));
            var areaPolygon = objs.BuildAreaGeometry();

            var originalArea = areaPolygon.Area;
            var shrinkedGeom = areaPolygon.Buffer(-shrinkValue);
            var shrinkedArea = shrinkedGeom.Area;
            var objs2 = shrinkedGeom.ToDbCollection();
            foreach (var obj in objs2)
            {
                if (obj is Entity pl)
                {
                    ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(pl, "l1shrink", 2);
                }
            }

            var bufferShrink = shrinkedGeom.Buffer(shrinkValue * 1);
            var bufferShrinkArea = bufferShrink.Area;
            var objs3 = bufferShrink.ToDbCollection();
            foreach (var obj in objs3)
            {
                if (obj is Entity pl)
                {
                    ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(pl, "l2bufferShrinkSmall", 3);
                }
            }

            var bufferShrink2 = shrinkedGeom.Buffer(shrinkValue * 1.5);
            var bufferShrinkArea2 = bufferShrink2.Area;
            var objs4 = bufferShrink2.ToDbCollection();
            foreach (var obj in objs4)
            {
                if (obj is Entity pl)
                {
                    ThMEPEngineCore.Diagnostics.DrawUtils.ShowGeometry(pl, "l2bufferShrinkLarge", 181);
                }
            }

            var shrinkAreaPer = shrinkedArea / originalArea;
            var bufferShrinkAreaPer = bufferShrinkArea / originalArea;

            if (bufferShrinkAreaPer >= threshold)
            {
                isAisle = false;
            }

            return isAisle;
        }

        public static double LayoutAreaWidth(List<MPolygon> LayoutArea, double radius)
        {
            var width = 2700.0;
            var noChange = true;

            if (LayoutArea.Count > 2)
            {
                var layoutWidth = LayoutArea.SelectMany(x => GetLayoutWidth(x)).ToList();
                var w = layoutWidth
                        .OrderByDescending(x => x)
                        .GroupBy(x => Math.Round(x / 1000, MidpointRounding.AwayFromZero))
                        .OrderByDescending(x => x.Count())
                        .First()
                        .ToList()
                        .First();

                width = w + 1000;

                if ((radius / 5) < width && width <= radius)
                {
                    noChange = false;
                }
            }

            if (noChange == true)
            {
                width = radius / 2.5;
            }

            return width;
        }

        private static List<double> GetLayoutWidth(MPolygon layout)
        {
            var obb = layout.Shell().CalObb();
            DrawUtils.ShowGeometry(obb, "l0OBB", 3);

            var d1 = obb.GetPoint3dAt(0).DistanceTo(obb.GetPoint3dAt(1));
            var d2 = obb.GetPoint3dAt(1).DistanceTo(obb.GetPoint3dAt(2));
            var w = new List<double>() { d1, d2 };

            return w;
        }
        public static void AddResult(List<ThLayoutPt> layoutResult, List<Polyline> blindsResult, Dictionary<Point3d, Vector3d> localPts, List<Polyline> localBlinds, string blkName)
        {
            foreach (var r in localPts)
            {
                layoutResult.Add(new ThLayoutPt() { Pt = r.Key, Dir = r.Value, BlkName = blkName });
            }

            blindsResult.AddRange(localBlinds);
        }

        public static List<Polyline> ToPriority(Dictionary<Point3d, Vector3d> localPts, (double, double) size, double Scale, double priorityExtend)
        {
            var priority = new List<Polyline>();

            if (localPts != null && localPts.Count > 0)
            {
                priority = ThFaAreaLayoutParamterCalculationService.GetPriorityBoundary(localPts, Scale, size);
                priority = ThAFASUtils.ExtendPriority(priority, priorityExtend);
            }
            return priority;
        }

        public static List<Polyline> CleanBlind(List<Polyline> tempBlindsResult)
        {
            var blindsResult = new List<Polyline>();

            tempBlindsResult = tempBlindsResult.Where(x => x.Area > 100).ToList();
            if (tempBlindsResult.Count > 0)
            {
                foreach (var blind in tempBlindsResult)
                {
                    var shrinkBlind = blind.Buffer(-1).OfType<Polyline>().Where(x => x.Area > 100);
                    foreach (var bBlind in shrinkBlind)
                    {
                        var bufferBlind = bBlind.Buffer(1).OfType<Polyline>();
                        blindsResult.AddRange(bufferBlind);
                    }
                }
            }
            return blindsResult;
        }
    }
}
