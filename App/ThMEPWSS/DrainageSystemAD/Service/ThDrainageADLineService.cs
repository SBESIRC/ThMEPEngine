using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Dreambuild.AutoCAD;

using ThCADExtension;
using ThCADCore.NTS;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageADLineService
    {
        public static void addValveToPipe(List<Line> pipes, List<ThDrainageSDADValve> valveList)
        {
            var blkName = new List<string>() { ThDrainageADCommon.blkName_angleValve, ThDrainageADCommon.blkName_dim };

            var valveCenLines = valveList.Where(x => blkName.Contains(x.type) == false).Select(x => x.centerLine).ToList();
            valveCenLines.ForEach(x => pipes.Add(x.Clone() as Line ));
        }

        /// <summary>
        /// 延长管线到立管中心。副产品：在管线中间的立管
        /// </summary>
        /// <param name="pipes"></param>
        /// <param name="stackList"></param>
        /// <returns></returns>
        public static List<Point3d> connectPipeWhenStack(List<Line> pipes, List<Circle> stackList)
        {
            var stackNotInEnd = new List<Point3d>();

            for (int i = 0; i < stackList.Count(); i++)
            {
                var stack = stackList[i];
                var touchPipes = pipes.Where(x => stackTouchPipes(x, stack)).ToList();

                if (touchPipes.Count == 2)
                {
                    stackNotInEnd.Add(stack.Center);
                    foreach (var pipe in touchPipes)
                    {
                        if (pipe.StartPoint.DistanceTo(stack.Center) > pipe.EndPoint.DistanceTo(stack.Center))
                        {
                            pipe.EndPoint = stack.Center;
                        }
                        else
                        {
                            pipe.StartPoint = stack.Center;
                        }
                    }
                }
            }
            return stackNotInEnd;
        }

        private static bool stackTouchPipes(Line pipe, Curve stack)
        {
            int bufferValveOutline = 2;
            var bReturn = false;

            var extPipe = pipe.ExtendLine(bufferValveOutline);

            var pt = extPipe.Intersect(stack, Intersect.OnBothOperands);

            if (pt.Count > 0)
            {
                bReturn = true;
            }

            return bReturn;
        }

        public static Point3d findStartPoint(List<Line> pipes, List<ThDrainageSDADValve> valveList)
        {
            var blkName = ThDrainageADCommon.blkName_angleValve;
            var tol = new Tolerance(10, 10);

            var endValvePt = valveList.Where(x => x.type == blkName).Select(x => x.blk.Position).ToList();

            var pointList = lineNode(pipes);
            var pointListOnce = pointList.Where(x => x.Value.Count == 1).Select(x => x.Key).ToList();

            var startPtNoValve = pointListOnce.Where(p => endValvePt.Where(v => v.IsEqualTo(p, tol)).Count() == 0).ToList();

            var startPt = startPtNoValve.FirstOrDefault();

            return startPt;
        }

        private static Dictionary<Point3d, List<Line>> lineNode(List<Line> lines)
        {
            var pointList = new Dictionary<Point3d, List<Line>>();

            for (var i = 0; i < lines.Count; i++)
            {
                if (ptInPtList(pointList, lines[i].StartPoint, out var ptKey) == false)
                {
                    pointList.Add(lines[i].StartPoint, new List<Line> { lines[i] });
                }
                else
                {
                    pointList[ptKey.Key].Add(lines[i]);
                }

                if (ptInPtList(pointList, lines[i].EndPoint, out ptKey) == false)
                {
                    pointList.Add(lines[i].EndPoint, new List<Line> { lines[i] });
                }
                else
                {
                    pointList[ptKey.Key].Add(lines[i]);
                }
            }
            return pointList;
        }

        /// <summary>
        /// 必须有。只判断是否点是否被包含有机会xy是一样的但是containskey返回错误的值
        /// </summary>
        /// <returns></returns>
        private static bool ptInPtList(Dictionary<Point3d, List<Line>> pointList, Point3d pt, out KeyValuePair<Point3d, List<Line>> ptKey)
        {
            var bIn = false;
            ptKey = new KeyValuePair<Point3d, List<Line>>();

            foreach (var ptl in pointList)
            {
                if (ptl.Key.IsEqualTo(pt, new Tolerance(1, 1)))
                {
                    ptKey = ptl;
                    bIn = true;
                    break;
                }
            }
            return bIn;
        }

        public static void insertStackNode(List<Point3d> stackNotInEnd, ThDrainageSDTreeNode root, List<int> traversedStack)
        {
            if (root.Child.Count > 0)
            {
                var indx = ifStackNode(stackNotInEnd, root, traversedStack);
                if (indx != -1)
                {
                    traversedStack.Add(indx);
                }
                foreach (var c in root.Child)
                {
                    insertStackNode(stackNotInEnd, c, traversedStack);
                }
            }
        }

        private static int ifStackNode(List<Point3d> stackNotInEnd, ThDrainageSDTreeNode node, List<int> traversedStack)
        {
            var tol = new Tolerance(10, 10);
            var stackNode = stackNotInEnd.Where(x => x.IsEqualTo(node.Node, tol));
            var indx = -1;
            if (stackNode.Count() == 1)
            {
                var indxTemp = stackNotInEnd.IndexOf(stackNode.First());
                if (traversedStack.Contains(indxTemp) == false)
                {
                    indx = indxTemp;
                    var newNode = new ThDrainageSDTreeNode(stackNode.First());
                    newNode.Parent = node;
                    newNode.Child.AddRange(node.Child);
                    node.Child.ForEach(x => x.Parent = newNode);
                    node.Child.Clear();
                    node.Child.Add(newNode);
                }

            }
            return indx;
        }
    }
}
