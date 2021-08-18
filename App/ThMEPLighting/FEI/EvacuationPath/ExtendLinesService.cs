using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class ExtendLinesService
    {
        double blockDistance = 600;
        double mergeAngle = Math.PI / 6;
        public List<ExtendLineModel> CreateExtendLines(List<List<Line>> xLanes, List<List<Line>> yLanes, List<BlockReference> enterBlocks, Polyline frame, List<Polyline> holes)
        {
            List<Line> allLanes = new List<Line>(xLanes.SelectMany(x => x.Select(y => y)));
            allLanes.AddRange(yLanes.SelectMany(x => x.Select(y => y)));

            //得到车道方向
            var xlanedir = (xLanes.First().First().EndPoint - xLanes.First().First().StartPoint).GetNormal();
            var ylanedir = Vector3d.ZAxis.CrossProduct(xlanedir);
            if (yLanes.Count > 0)  //只有一个方向车道线
            {
                ylanedir = (yLanes.First().First().EndPoint - yLanes.First().First().StartPoint).GetNormal();
            }

            List<ExtendLineModel> resLines = new List<ExtendLineModel>();
            CreateExtendLineWithAStarService startExtendLineService = new CreateExtendLineWithAStarService();
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

                //获取最近的车道线信息
                var closetLane = GetClosetLane(allLanes, blockPt, frame);

                //起点到主车道延伸线
                var startExtendLines = startExtendLineService.CreateStartLines(frame, closetLane.Key, blockPt, holes);

                if (startExtendLines.Count > 0)
                {
                    //创建主车道延伸线
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
            }

            //合并延伸线（删除多余延伸线并尽量均匀）
            MergeExtendLineService mergeExtendLineService = new MergeExtendLineService();
            resLines = mergeExtendLineService.MergeLines(xLanes, yLanes, resLines);
            
            //连接孤立车道线
            List<List<Line>> allLineLanes = new List<List<Line>>(xLanes);
            allLineLanes.AddRange(yLanes);
            ConnectIsolatedLaneService connectIsolatedLane = new ConnectIsolatedLaneService();
            resLines.AddRange(connectIsolatedLane.ConnectIsolatedLane(resLines, allLineLanes, frame, holes));

            return resLines;
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

        /// <summary>
        /// 获取最近的线信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lanes, Point3d startPt, Polyline polyline)
        {
            var closeInfo = GeUtils.GetClosetLane(lanes, startPt);
            Line checkLine = new Line(startPt, closeInfo.Value);
            if (!CheckService.CheckIntersectWithFrame(checkLine, polyline))
            {
                var checkDir = (closeInfo.Value - startPt).GetNormal();
                var lineDir = Vector3d.ZAxis.CrossProduct((closeInfo.Key.EndPoint - closeInfo.Key.StartPoint).GetNormal());
                if (checkDir.IsEqualTo(lineDir, new Tolerance(0.001, 0.001)))
                {
                    return closeInfo;
                }
            }

            BFSPathPlaner pathPlaner = new BFSPathPlaner(400);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lanes, polyline);
            var closetPt = closetLine.GetClosestPointTo(startPt, false);

            return new KeyValuePair<Line, Point3d>(closetLine, closetPt);
        }
    }
}
