using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantBranchLine
    {
        public Polyline BranchPolylineObb { set; get; }
        public Polyline BranchPolyline { set; get; }
        public List<Line> DrawLineList { set; get; }
        public ThHydrantBranchLine()
        {
            BranchPolyline = new Polyline();
        }
        public static ThHydrantBranchLine Create(Entity data)
        {
            var branchLine = new ThHydrantBranchLine();
            if (data is Polyline)
            {
                var polyline = data.Clone() as Polyline;
                branchLine.BranchPolyline = polyline;
                var objcets = polyline.BufferPL(50);
                var obb = objcets[0] as Polyline;
                branchLine.BranchPolylineObb = obb;
                branchLine.DrawLineList = branchLine.BranchPolyline.ToLines();
            }
            return branchLine;
        }
        public void Draw(AcadDatabase acadDatabase)
        {
            foreach (var l in DrawLineList)
            {
                acadDatabase.ModelSpace.Add(l);
                l.Layer = "W-FRPT-HYDT-PIPE";
                l.Linetype = "ByLayer";
                l.LineWeight = LineWeight.ByLayer;
                l.ColorIndex = (int)ColorIndex.BYLAYER;
            }
        }
        public Point3d InsertValve(AcadDatabase acadDatabase, List<Line> lines, List<Line> avoidLines, string strMapScale, bool isTchPipeValve = false)
        {
            double scale = 1;
            switch (strMapScale)
            {
                case "1:100":
                    scale = 1;
                    break;
                case "1:150":
                    scale = 1.5;
                    break;
                default:
                    break;
            }
            var posPt = new Point3d();
            double angle = 0.0;
            bool isInsert = true;
            var tmpLines = lines.OrderBy(o => o.Length).ToList();
            var bakLines = lines.OrderBy(o => o.Length).ToList();
            var line = tmpLines.Last();
            int lineIndex = tmpLines.Count - 1;
            while (!InsertValve(avoidLines, line, scale, out posPt, out angle))//考虑躲避
            {
                tmpLines.RemoveAt(tmpLines.Count - 1);
                if (tmpLines.Count != 0)
                {
                    lineIndex--;
                    line = tmpLines.Last();
                }
                else
                {
                    isInsert = false;
                    break;
                }
            }

            if (isTchPipeValve)
            {
                if (!isInsert)
                {
                    InsertValve(line, out posPt, out angle);
                }
            }
            else
            {
                if (!isInsert)
                {
                    var tmpline = bakLines.Last();
                    bakLines.Remove(tmpline);
                    if (tmpline.Length >= 560)
                    {
                        //在这条线位置插入蝶阀
                        if (InsertValve(line, out posPt, out angle))
                        {
                            var brkLine = InsertValve(acadDatabase, tmpline, posPt, angle, scale);
                            bakLines.AddRange(brkLine);
                        }
                    }
                }
                else
                {
                    var tmpline = bakLines[lineIndex];
                    bakLines.Remove(tmpline);
                    var brkLine = InsertValve(acadDatabase, tmpline, posPt, angle, scale);
                    bakLines.AddRange(brkLine);
                    //此线 pt 和angle 插入阀门
                }
                DrawLineList = bakLines;
            }
            return posPt;
        }
        private bool InsertValve(Line line, out Point3d pt, out double angle)//不考虑躲避
        {
            var normal = line.EndPoint.GetVectorTo(line.StartPoint).GetNormal();
            pt = line.GetCenter();
            var refVector = new Vector3d(0, 0, 1);
            var basVector = new Vector3d(1, 0, 0);
            angle = basVector.GetAngleTo(normal, refVector);
            if (angle > Math.PI / 2.0 && angle <= Math.PI)
            {
                angle = angle + Math.PI;
            }
            else if (angle > Math.PI && angle <= Math.PI * 3.0 / 2.0)
            {
                angle = angle - Math.PI;
            }
            return true;
        }
        private bool InsertValve(List<Line> avoidLines, Line line, double scale, out Point3d pt, out double angle)//考虑躲避
        {
            pt = line.GetCenter();
            var normal = line.EndPoint.GetVectorTo(line.StartPoint).GetNormal();
            var refVector = new Vector3d(0, 0, 1);
            var basVector = new Vector3d(1, 0, 0);
            angle = basVector.GetAngleTo(normal, refVector);
            if (angle > Math.PI / 2.0 && angle <= Math.PI)
            {
                angle = angle + Math.PI;
            }
            else if (angle > Math.PI && angle <= Math.PI * 3.0 / 2.0)
            {
                angle = angle - Math.PI;
            }
            //判断该位置是否与avoidLines相交
            if (!IsIntersectWith(avoidLines, pt, scale, angle))
            {
                //如果没有相交
                return true;
            }
            else
            {
                double step = 100;
                var point1 = new Point3d(pt.X, pt.Y, pt.Z);
                var point2 = new Point3d(pt.X, pt.Y, pt.Z);

                var vector1 = pt.GetVectorTo(line.StartPoint).GetNormal() * step;
                var vector2 = pt.GetVectorTo(line.EndPoint).GetNormal() * step;
                point1 = point1 + vector1;

                bool isOK1 = true;
                while (IsIntersectWith(avoidLines, point1, scale, angle))//向start点找rigthPoint
                {
                    point1 = point1 + vector1;
                    //构造蝶阀包围盒
                    var vector = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);
                    var point11 = point1 + vector * (240.0 * scale);
                    var valveLine = new Line(point1, point11);
                    var valveBox = valveLine.Buffer(90 * scale);

                    if (valveBox.Contains(line.StartPoint))
                    {
                        isOK1 = false;
                        break;
                    }
                }

                point2 = point2 + vector2;
                bool isOK2 = true;
                while (IsIntersectWith(avoidLines, point2, scale, angle))//向end点找leftPoint
                {
                    point2 = point2 + vector2;
                    //构造蝶阀包围盒
                    var vector = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);
                    var point21 = point2 + vector * (240.0 * scale);
                    var valveLine = new Line(point2, point21);
                    var valveBox = valveLine.Buffer(90 * scale);
                    if (valveBox.Contains(line.EndPoint))
                    {
                        isOK2 = false;
                        break;
                    }
                }

                if (isOK1 && isOK2)//如果都找到，比较pt和他们的距离
                {
                    if (point1.DistanceTo(pt) < point2.DistanceTo(pt))
                    {
                        pt = point1;
                    }
                    else
                    {
                        pt = point2;
                    }
                }
                else if (isOK1 && (!isOK2))//如果只找到一个，pt = 该点
                {
                    pt = point1;
                }
                else if (!isOK1 && isOK2)
                {
                    pt = point2;
                }
                else//如果都没找到，返回false
                {
                    return false;
                }
            }

            return true;
        }
        private List<Line> InsertValve(AcadDatabase acadDatabase, Line line, Point3d pt, double angle, double scale)
        {
            var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "蝶阀", pt, new Scale3d(scale, scale, scale), angle);
            var blk = acadDatabase.Element<BlockReference>(blkId);
            if (blk.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "可见性")
                    {
                        property.Value = "蝶阀";
                        break;
                    }
                }
            }
            return BreakLine(blk, line, scale);
        }
        private bool IsIntersectWith(List<Line> avoidLines, Point3d pt, double scale, double angle)
        {
            //构造蝶阀包围盒
            var vector = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);
            var pt1 = pt + vector * (240.0 * scale);
            var valveLine = new Line(pt, pt1);
            var newLine = valveLine.ExtendLine(50.0);
            var valveBox = newLine.Buffer(90 * scale + 50.0);
            foreach (var l in avoidLines)
            {
                if (valveBox.IsIntersects(l))
                {
                    return true;
                }
            }
            return false;
        }
        public void InsertPipeMark(AcadDatabase acadDatabase, string strMapScale)
        {
            string riserName = "";
            switch (strMapScale)
            {
                case "1:100":
                    riserName = "消火栓管线管径";
                    break;
                case "1:150":
                    riserName = "消火栓管径150";
                    break;
                default:
                    break;
            }

            List<Line> lines = BranchPolyline.ToLines();
            if (lines.Count != 0)
            {
                var tmpLines = lines.OrderBy(o => o.Length).ToList();
                while (!InsertPipeMart(acadDatabase, riserName, tmpLines))
                {
                    tmpLines.RemoveAt(tmpLines.Count - 1);
                    if (tmpLines.Count == 0)
                    {
                        return;
                    }
                }
            }
        }
        private bool InsertPipeMart(AcadDatabase acadDatabase, string riserName, List<Line> lines)
        {
            if (lines.Count != 0)
            {
                var line = lines.Last();
                if (line.Length < 560)
                {
                    return false;
                }

                var position = line.GetCenter();
                var vector = line.EndPoint.GetVectorTo(line.StartPoint).GetNormal();
                if (vector.Y < 0)
                {
                    vector = -vector;
                }
                double angle = vector.GetAngleTo(Vector3d.YAxis, Vector3d.ZAxis);
                if (angle > Math.PI)
                {
                    angle = Math.PI * 2 - angle;
                }
                var tmpVecotr = Vector3d.ZAxis.CrossProduct(vector);
                var rotateAngle = vector.GetAngleTo(Vector3d.XAxis);
                if (angle > 10 / 180.0 * Math.PI)
                {
                    if (tmpVecotr.Y < 0)
                    {
                        rotateAngle = rotateAngle - Math.PI;
                        tmpVecotr = -tmpVecotr;
                    }
                }
                else
                {
                    if (tmpVecotr.X > 0)
                    {
                        tmpVecotr = -tmpVecotr;
                    }
                }
                position = position + 450 * tmpVecotr;
                var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-DIMS", riserName, position, new Scale3d(1, 1, 1), rotateAngle);
                var blk = acadDatabase.Element<BlockReference>(blkId);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性")
                        {
                            property.Value = "DN65";
                            break;
                        }
                        if (property.PropertyName == "可见性1")
                        {
                            property.Value = "DN65";
                            break;
                        }
                    }
                }
                var obb = blk.ToOBB(blk.BlockTransform);
                var center = obb.GetCenter();
                var movVector = center.GetVectorTo(position);
                position = position + movVector;
                blk.Position = position;
                return true;
            }
            else
            {
                return false;
            }

        }
        private List<Line> BreakLine(BlockReference blk, Line line, double scale)
        {
            double length = 240 * scale;
            var resLines = new List<Line>();
            var startPt = line.StartPoint;
            var endPt = line.EndPoint;
            var normal = startPt.GetVectorTo(endPt).GetNormal();
            var centPt = blk.GetCenter();
            centPt = line.GetClosestPointTo(centPt, false);
            var pt1 = centPt + (-normal * length / 2.0);
            var pt2 = centPt + (normal * length / 2.0);
            var line1 = new Line(startPt, pt1);
            var line2 = new Line(pt2, endPt);
            resLines.Add(line1);
            resLines.Add(line2);
            return resLines;
        }
    }
}
