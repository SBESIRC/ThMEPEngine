using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLight.Model;


using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;

using Autodesk.AutoCAD.DatabaseServices;

using Autodesk.AutoCAD.Colors;


namespace ThMEPLighting.EmgLight.Service
{
    class ThStructFilterService
    {
        private ThLaneService m_thLaneService;
        private List<Polyline> m_columns;
        private List<Polyline> m_walls;

        public ThStructFilterService(ThLaneService thLaneService, List<Polyline> columns, List<Polyline> walls)
        {
            m_thLaneService = thLaneService;
            m_columns = columns;
            m_walls = walls;

        }

        public void filterStruct(out List<ThStruct> columnsStructs, out List<ThStruct> wallStructs)
        {
            //获取该车道线上的构建
            var closeColumn = m_thLaneService.GetStruct(m_columns, EmgLightCommon.TolLane);
            var closeWall = m_thLaneService.GetStruct(m_walls, EmgLightCommon.TolLane);

            DrawUtils.ShowGeometry(closeColumn, EmgLightCommon.LayerGetStruct, Color.FromColorIndex(ColorMethod.ByColor, 1), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(closeWall, EmgLightCommon.LayerGetStruct, Color.FromColorIndex(ColorMethod.ByColor, 92), LineWeight.LineWeight035);

            foreach (Line l in m_thLaneService.thLane.geom)
            {
                var linePoly = StructUtils.ExpandLine(l, EmgLightCommon.TolLane, 0, EmgLightCommon.TolLane, 0);
                DrawUtils.ShowGeometry(linePoly, EmgLightCommon.LayerSeparatePoly, Color.FromColorIndex(ColorMethod.ByColor, 44));
            }

            //打散构建并生成数据结构
            var columnSegment = StructureService.BrakePolylineToLineList(closeColumn);
            var wallSegment = StructureService.BrakePolylineToLineList(closeWall);

            DrawUtils.ShowGeometry(columnSegment.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructSeg, Color.FromColorIndex(ColorMethod.ByColor, 1), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(wallSegment.Select(x => x.geom).ToList(), EmgLightCommon.LayerStructSeg, Color.FromColorIndex(ColorMethod.ByColor, 92), LineWeight.LineWeight035);

            //选取构建平行车道线的边
            var parallelColmuns = m_thLaneService.getStructureParallelPart(columnSegment);
            var parallelWalls = m_thLaneService.getStructureParallelPart(wallSegment);

            //破墙
            var brokeWall = StructureService.breakWall(parallelWalls, EmgLightCommon.TolBrakeWall);

            DrawUtils.ShowGeometry(parallelColmuns.Select(x => x.geom).ToList(), EmgLightCommon.LayerParallelStruct, Color.FromColorIndex(ColorMethod.ByColor, 5), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(brokeWall.Select(x => x.geom).ToList(), EmgLightCommon.LayerParallelStruct, Color.FromColorIndex(ColorMethod.ByColor, 5), LineWeight.LineWeight035);

            //过滤柱与墙交叉的部分
            var filterColumns = StructureService.FilterStructIntersect(parallelColmuns, m_walls, EmgLightCommon.TolIntersect);
            var filterWalls = StructureService.FilterStructIntersect(brokeWall, m_columns, EmgLightCommon.TolIntersect);

            DrawUtils.ShowGeometry(filterColumns.Select(x => x.geom).ToList(), EmgLightCommon.LayerNotIntersectStruct, Color.FromColorIndex(ColorMethod.ByColor, 140), LineWeight.LineWeight035);
            DrawUtils.ShowGeometry(filterWalls.Select(x => x.geom).ToList(), EmgLightCommon.LayerNotIntersectStruct, Color.FromColorIndex(ColorMethod.ByColor, 140), LineWeight.LineWeight035);

            columnsStructs = filterColumns;
            wallStructs = filterWalls;
        }
   
    
    }


}
