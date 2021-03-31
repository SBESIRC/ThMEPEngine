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
        double blockDistance = 600;
        double mergeAngle = Math.PI / 6;
        public List<Polyline> CreateExtendLines(List<List<Line>> xLanes, List<List<Line>> yLanes, List<BlockReference> enterBlocks, Polyline frame, List<Polyline> holes)
        {
            List<Line> allLanes = new List<Line>(xLanes.SelectMany(x => x.Select(y => y)));
            allLanes.AddRange(yLanes.SelectMany(x => x.Select(y => y)));

            //得到车道方向
            var xlanedir = (xLanes.First().First().EndPoint - xLanes.First().First().StartPoint).GetNormal();
            var ylanedir = (yLanes.First().First().EndPoint - yLanes.First().First().StartPoint).GetNormal();

            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            CreateStartExtendLineService startExtendLineService = new CreateStartExtendLineService();
            CreateMainLanesService createMainLanes = new CreateMainLanesService();
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

                //起点到主车道延伸线
                var startExtendLines = startExtendLineService.CreateStartLines(frame, allLanes, blockPt, holes);

                //创建主车道延伸线
                var closetLane = GeUtils.GetClosetLane(allLanes, blockPt);
                var dir = (closetLane.Key.EndPoint - closetLane.Key.StartPoint).GetNormal();
                var startPt = startExtendLines.First().line.EndPoint;
                var extendDir = (closetLane.Value - blockPt).GetNormal();
                if (IsXAxisExtend(dir, xlanedir, ylanedir))
                {
                    resLines.AddRange(createMainLanes.CreateLines(frame, startPt, extendDir, xLanes, holes));
                }
                else
                {
                    resLines.AddRange(createMainLanes.CreateLines(frame, startPt, extendDir, yLanes, holes));
                }
                resLines.AddRange(startExtendLines);
            }

            return resLines.Select(x => x.line).ToList();
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
            if (xValue > yValue)
            {
                return true;
            }
            else
            {
                return false;
            }
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
            var line = new Polyline();
            line.AddVertexAt(0, block.Position.ToPoint2D(), 0, 0, 0);
            line.AddVertexAt(0, otherBlock.Position.ToPoint2D(), 0, 0, 0);
            ExtendLineModel extendLine = new ExtendLineModel();
            extendLine.line = line;
            extendLine.priority = Priority.MergeStartLine;
            if (CheckService.CheckIntersectWithHols(line, holes, out List<Polyline> intersectHoles))
            {
                return null;
            }

            return extendLine;
        }
    }
}
