using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageADConvertLineService
    {
        public static void convertTree(ThDrainageSDTreeNode parent, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertDict, List<Point3d> stackNotInEnd, ref int stackDir)
        {
            if (parent.Child.Count > 0)
            {
                foreach (var c in parent.Child)
                {
                    convertNode(c, convertDict, stackNotInEnd, ref stackDir);
                    convertTree(c, convertDict, stackNotInEnd, ref stackDir);
                }
            }
        }

        private static void convertNode( ThDrainageSDTreeNode child, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertDict, List<Point3d> stackNotInEnd, ref int stackDir)
        {
            var parent = child.Parent;
            var bIsStackNode = isStackNode(parent, child, stackNotInEnd);
            Vector3d newDir = new Vector3d();

            //计算角度
            var vector = child.Node - parent.Node;
            var dir = vector.GetNormal();
            var dirAngle = dir.GetAngleTo(Vector3d.XAxis, -Vector3d.ZAxis);

            if (Math.Abs(Math.Cos(dirAngle)) >= Math.Cos(1 * Math.PI / 180))
            {
                //0 180度
                newDir = dir;
            }
            else if (0 * Math.PI / 180 < dirAngle && dirAngle <= 91 * Math.PI / 180)
            {
                var turnAngle = dirAngle / 2;
                newDir = Vector3d.XAxis.RotateBy(turnAngle, Vector3d.ZAxis);
            }
            else if (90 * Math.PI / 180 < dirAngle && dirAngle < 180 * Math.PI / 180)
            {
                var turnAngle = (dirAngle - 90 * Math.PI / 180) / 2 + 90 * Math.PI / 180;
                newDir = Vector3d.XAxis.RotateBy(turnAngle, Vector3d.ZAxis);
            }
            else if (180 * Math.PI / 180 < dirAngle && dirAngle <= 271 * Math.PI / 180)
            {
                var turnAngle = (dirAngle - 180 * Math.PI / 180) / 2 + 180 * Math.PI / 180;
                newDir = Vector3d.XAxis.RotateBy(turnAngle, Vector3d.ZAxis);
            }
            else if (270 * Math.PI / 180 < dirAngle && dirAngle < 360 * Math.PI / 180)
            {
                var turnAngle = (dirAngle - 270 * Math.PI / 180) / 2 + 270 * Math.PI / 180;
                newDir = Vector3d.XAxis.RotateBy(turnAngle, Vector3d.ZAxis);
            }

            //线段长度
            var toPLength = vector.Length;

            if (child.Child.Count() == 0)
            {
                //末端支管
                if (vector.Length <= ThDrainageADCommon.tol_pipe_end)
                {
                    toPLength = ThDrainageADCommon.length_pipe_end;
                }
            }
            else if (bIsStackNode)
            {
                //线中间的立管
                toPLength = ThDrainageADCommon.length_stack_WM;
                if (stackDir == 0)
                {
                    newDir = -Vector3d.YAxis;
                    stackDir = 1;
                }
                else
                {
                    newDir = Vector3d.YAxis;
                    stackDir = 0;
                }
            }

            var newPt = convertDict[parent].Node + newDir * toPLength;
            var newNode = new ThDrainageSDTreeNode(newPt);
            convertDict.Add(child, newNode);
        }

        private static bool isStackNode(ThDrainageSDTreeNode parent, ThDrainageSDTreeNode child, List<Point3d> stackNotInEnd)
        {
            var tol = new Tolerance(10, 10);
            var bReturn = false;

            var stackP = stackNotInEnd.Where(x => x.IsEqualTo(parent.Node, tol));
            var stackC = stackNotInEnd.Where(x => x.IsEqualTo(child.Node, tol));

            if (stackP.Count() == 1 && stackC.Count() == 1)
            {
                if (stackP.First().IsEqualTo(stackC.First(), tol))
                {
                    bReturn = true;
                }
            }

            return bReturn;
        }

        public static Dictionary<ThDrainageSDTreeNode, List<Line>> addEndStackPipe(List<ThDrainageSDTreeNode> endNode, List<Line> convertedPipes, Dictionary<ThDrainageSDTreeNode, ThDrainageSDTreeNode> convertDict)
        {
            var endStackPipe = new Dictionary<ThDrainageSDTreeNode, List<Line>>();
            var dir = -Vector3d.YAxis.GetNormal();
            var end_length = ThDrainageADCommon.length_stack_end;
            var break_length = ThDrainageADCommon.length_stack_end_break;

            foreach (var end in endNode)
            {
                endStackPipe.Add(end, new List<Line>());

                var endPt = convertDict[end].Node + dir * end_length;
                var endLineTemp = new Line(convertDict[end].Node, endPt);

                var incPts = new List<Point3d>();
                convertedPipes.ForEach(x => incPts.AddRange(endLineTemp.Intersect(x, Intersect.OnBothOperands)
                                                                       .Where(y => y.IsEqualTo(convertDict[end].Node, new Tolerance(10, 10)) == false)
                                                            ));
                if (incPts.Count > 0)
                {
                    //打断
                    var breakLine = new List<Line>();

                    incPts = incPts.OrderByDescending(x => x.Y).ToList();
                    incPts.Insert(0, endLineTemp.StartPoint - dir * break_length);
                    incPts.Add(endLineTemp.EndPoint + dir * break_length);

                    for (int i = 1; i < incPts.Count; i++)
                    {
                        if (incPts[i].DistanceTo(incPts[i - 1]) >= break_length * 2)
                        {
                            var l = new Line(incPts[i - 1] + dir * break_length, incPts[i] - dir * break_length);
                            breakLine.Add(l);
                        }
                    }
                    endStackPipe[end].AddRange(breakLine);
                }
                else
                {
                    endStackPipe[end].Add(endLineTemp);
                }
            }
            return endStackPipe;
        }
    }
}
