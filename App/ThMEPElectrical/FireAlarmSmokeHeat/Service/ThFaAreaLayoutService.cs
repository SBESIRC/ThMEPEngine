﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using Linq2Acad;
using GeometryExtensions;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.CAD;

using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Command;

using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Model;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Service
{
    class ThFaAreaLayoutService
    {
        public static void ThFaAreaLayoutGrid(Polyline frame, ThSmokeDataQueryService dataQuery, double radius, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
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
            layoutCmd.equipmentType = BlindType.CoverArea;

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

        public static void ThFaAreaLayoutCenterline(Polyline frame, ThSmokeDataQueryService dataQuery, double radius, out Dictionary<Point3d, Vector3d> localPts, out List<Polyline> blines)
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
            layoutCmd.equipmentType = BlindType.CoverArea;

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

        public static bool isAisleArea(Polyline frame, List<Polyline> HoleList, double shrinkValue, double threshold)
        {
            var objs = new DBObjectCollection();
            objs.Add(frame);
            HoleList.ForEach(x => objs.Add(x));
            var geometry = objs.BuildAreaGeometry();
            var isAisleArea = ThMEPEngineCoreGeUtils.IsAisleArea(geometry, shrinkValue, threshold);

            return isAisleArea;
        }

        public static void addResult(List<ThLayoutPt> layoutResult, List<Polyline> blindsResult, Dictionary<Point3d, Vector3d> localPts, List<Polyline> localBlinds, string blkName)
        {
            foreach (var r in localPts)
            {
                layoutResult.Add(new ThLayoutPt() { Pt = r.Key, Dir = r.Value, BlkName = blkName });
            }

            blindsResult.AddRange(localBlinds);
        }

        public static List<Polyline> toPriority(Dictionary<Point3d, Vector3d> localPts, (double, double) size, double Scale, double priorityExtend)
        {
            var priority = new List<Polyline>();

            if (localPts != null && localPts.Count > 0)
            {
                priority = ThFaAreaLayoutParamterCalculationService.getPriorityBoundary(localPts, Scale, size);
                priority = priority.Select(x => x.GetOffsetClosePolyline(priorityExtend)).ToList();
            }
            return priority;
        }

    }
}
