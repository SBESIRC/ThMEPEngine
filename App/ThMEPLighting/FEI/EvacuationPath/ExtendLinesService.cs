using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class ExtendLinesService
    {
        public List<Line> CreateExtendLines(List<Line> lanes, List<BlockReference> enterBlocks, Polyline frame, List<Polyline> holes)
        {
            List<Line> allLanes = new List<Line>(lanes.Select(x => x));

            List<Polyline> resPath = new List<Polyline>();
           
            CreateStartExtendLineService startExtendLineService = new CreateStartExtendLineService();
            var startPath = startExtendLineService.CreateStartLines(frame, allLanes, enterBlocks, holes);

            return startPath.Select(x => x.line).ToList();
            ////得到车道方向
            //var xLaneDir = (xLanes.First().First().EndPoint - xLanes.First().First().StartPoint).GetNormal();
            //var yLaneDir = (yLanes.First().First().EndPoint - yLanes.First().First().StartPoint).GetNormal();

            ////
            //foreach (var block in enterBlocks)
            //{
            //    var blockDir = block.BlockTransform.CoordinateSystem3d.Yaxis;

            //    if (IsXAxisExtend(blockDir, xLaneDir, yLaneDir))
            //    {
            //        var extendDir = GetExtendsDirection(block, xLanes);
            //    }
            //    //var blockDir = GetExtendsDirection(block, xLanes);
            //}
        }

        /// <summary>
        /// 计算是向主车道做延伸线还是副车道做延伸线
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="xLaneDir"></param>
        /// <param name="yLaneDir"></param>
        /// <returns></returns>
        private bool IsXAxisExtend(Vector3d dir, Vector3d xLaneDir, Vector3d yLaneDir)
        {
            double xValue = Math.Abs(dir.DotProduct(xLaneDir));
            double yValue = Math.Abs(dir.DotProduct(yLaneDir));
            if (xValue < yValue)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 计算出入口起点延申方向
        /// </summary>
        /// <param name="block"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private Vector3d GetExtendsDirection(BlockReference block, List<List<Line>> lanes)
        {
            var closetPt = lanes.Select(x => x.First().GetClosestPointTo(block.Position, false))
                .OrderBy(x => x.DistanceTo(block.Position))
                .First();
            var dir = (closetPt - block.Position).GetNormal();
            var blockDir = block.BlockTransform.CoordinateSystem3d.Yaxis;
            if (blockDir.DotProduct(dir) < 0)
            {
                blockDir = -blockDir;
            }

            return blockDir;
        }
    }
}
