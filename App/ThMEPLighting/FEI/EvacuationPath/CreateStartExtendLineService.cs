using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.FEI.AStarAlgorithm;
using Linq2Acad;
using ThMEPLighting.FEI.Service;
using ThMEPLighting.FEI.Model;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class CreateStartExtendLineService
    {
        double distance = 800;
        double blockDistance = 600;
        double mergeAngle = Math.PI / 6;

        public List<ExtendLineModel> CreateStartLines(Polyline polyline, List<Line> lanes, List<BlockReference> enterBlocks, List<Polyline> holes)
        {
            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            //起点段路径规划
            while (enterBlocks.Count > 0)
            {
                var block = enterBlocks.First();
                enterBlocks.Remove(block);

                //计算能够合并的出口
                var mergeblock = CalMergeEnterBlock(enterBlocks, block);

                //计算合并线
                var blockPt = block.Position;
                if (mergeblock != null)
                {
                    var mergeLine = MergeBlocks(block, mergeblock, holes);
                    enterBlocks.Remove(mergeblock);
                    resLines.Add(mergeLine);
                    blockPt = new Point3d((mergeLine.line.EndPoint.X + mergeLine.line.StartPoint.X) / 2, (mergeLine.line.EndPoint.Y + mergeLine.line.StartPoint.Y) / 2, 0);
                }

                //寻找起点
                var startPt = CreateDistancePoint(polyline, blockPt);

                //找到最近的车道线
                var closetLane = GetClosetLane(lanes, startPt);

                //计算逃生路径(用A*算法)
                var dir = (closetLane.Key.EndPoint - closetLane.Key.StartPoint).GetNormal();
                AStarRoutePlanner aStarRoute = new AStarRoutePlanner(polyline, dir, 400, 400);
                //----设置障碍物
                aStarRoute.SetObstacle(holes);
                //----计算路径
                var path = aStarRoute.Plan(startPt, closetLane.Value);

                if (path != null)
                {
                    foreach (var line in PLineToLine(path))
                    {
                        ExtendLineModel extendLine = new ExtendLineModel();
                        extendLine.line = line;
                        extendLine.priority = Priority.startExtendLine;
                        resLines.Add(extendLine);
                    }
                }
            }

            return resLines;
        }

        /// <summary>
        /// 计算得到相对的图块
        /// </summary>
        /// <param name="enterBlocks"></param>
        /// <param name="block"></param>
        /// <returns></returns>
        private BlockReference CalMergeEnterBlock(List<BlockReference> enterBlocks, BlockReference block)
        {
            var blockDir = block.BlockTransform.CoordinateSystem3d.Yaxis;
            var mergeBlock = enterBlocks.Where(x => x.Position.DistanceTo(block.Position) < blockDistance)
                .Where(x =>
                {
                    var dir = (x.Position - block.Position).GetNormal();
                    var angle = dir.GetAngleTo(blockDir);
                    return angle < mergeAngle || angle > (Math.PI - mergeAngle);
                })
                .FirstOrDefault();

            return mergeBlock;
        }

        /// <summary>
        /// 合并能合并图块
        /// </summary>
        /// <param name="block"></param>
        /// <param name="otherBlock"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private ExtendLineModel MergeBlocks(BlockReference block, BlockReference otherBlock, List<Polyline> holes)
        {
            var line = new Line(block.Position, otherBlock.Position);
            ExtendLineModel extendLine = new ExtendLineModel();
            extendLine.line = line;
            extendLine.priority = Priority.MergeStartLine;
            if (CheckService.CheckIntersectWithHols(line, holes, out List<Polyline> intersectHoles))
            {
                return null;
            }

            return extendLine;
        }

        /// <summary>
        /// 计算起始点离外框线大于800距离
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="blockPt"></param>
        /// <returns></returns>
        private Point3d CreateDistancePoint(Polyline frame, Point3d blockPt)
        {
            Point3d resPt = blockPt;
            int i = 0;
            while (i <= 4)
            {
                i++;
                var closetPt = frame.GetClosestPointTo(resPt, false);
                var ptDistance = resPt.DistanceTo(closetPt);
                if (ptDistance >= distance)
                {
                    break;
                }

                var moveDir = (resPt - closetPt).GetNormal();
                resPt = resPt + moveDir * (distance - ptDistance);
            }

            return resPt;
        }

        /// <summary>
        /// 获取最近车道线
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        private KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lanes, Point3d startPt)
        {
            var lanePtInfo = lanes.ToDictionary(x => x, y => y.GetClosestPointTo(startPt, false))
                .OrderBy(x => x.Value.DistanceTo(startPt))
                .First();

            return lanePtInfo;
        }

        /// <summary>
        /// polyline转换成line
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Line> PLineToLine(Polyline polyline)
        {
            List<Line> resLines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                resLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1)));
            }

            if (polyline.Closed)
            {
                resLines.Add(new Line(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1), polyline.GetPoint3dAt(0)));
            }

            return resLines;
        }
    }
}
