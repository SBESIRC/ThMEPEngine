using System;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLight.Service;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLight.Model;


namespace ThMEPLighting.EmgLight
{
    class LayoutEmgLightEngine
    {

        /// <summary>
        /// 计算布置信息
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="lanes"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        /// <returns></returns>
        public Dictionary<Polyline, (Point3d, Vector3d)> LayoutLight(Polyline frame, List<List<Line>> lanes, List<Polyline> columns, List<Polyline> walls)
        {
            Dictionary<Polyline, (Point3d, Vector3d)> layoutPtInfo = new Dictionary<Polyline, (Point3d, Vector3d)>();
            List<ThStruct> layoutList = new List<ThStruct>();
            List<ThLane> laneList = new List<ThLane>();

            //跳过完全没有可布点的图
            if (columns.Count == 0 && walls.Count == 0)
            {
                return layoutPtInfo;
            }

            for (int i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];

                //特别短的线跳过
                var laneLength = lane.Sum(x => x.Length);
                if (laneLength < EmgLightCommon.TolLightRangeMin)
                {
                    continue;
                }

                var thLane = new ThLane(lane);
                laneList.Add(thLane);
            }

            for (int i = 0; i < laneList.Count; i++)
            {
                var thLane = laneList[i];
                var StructFilterService = new StructFilterService(thLane, columns, walls);

                StructFilterService.filterStruct(out var filterColumns, out var filterWalls);
                var layoutServer = StructFilterService.moreFilter(filterColumns, filterWalls, frame);

                if (layoutServer.UsefulColumns[0].Count == 0 && layoutServer.UsefulColumns[1].Count == 0 &&
                    layoutServer.UsefulWalls[0].Count == 0 && layoutServer.UsefulWalls[1].Count == 0)
                {
                    continue;
                }

                var b = false;
                if (b == true)
                {
                    continue;
                }

                ////找出平均的一边. -1:no side 0:left 1:right.
                int uniformSide = FindUniformSideService.IfHasUniformSide(layoutServer, out var columnDistList);
                FindUniformSideService.DetermineStartSide(layoutServer, out var uniformSideStructsList, ref uniformSide, ref columnDistList);

                var layoutUniform = new LayoutUniformSideService(layoutList, layoutServer, uniformSideStructsList, columnDistList);
                layoutUniform.LayoutUniformSide(uniformSide, laneList, out var uniformSideLayout);

                var layoutNonUniform = new LayoutNonUniformSideService(layoutList, layoutServer, uniformSideLayout);
                layoutNonUniform.LayoutOppositeSide(uniformSide, laneList);

                layoutServer.AddLayoutStructPt(layoutList, ref layoutPtInfo);
            }

            return layoutPtInfo;
        }

    }
}
