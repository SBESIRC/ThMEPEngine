﻿using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.HydrantConnectPipe.Command;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantBranchLine
    {
        public Polyline BranchPolylineObb { set; get; }
        public Polyline BranchPolyline { set; get; }
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
            }
            return branchLine;
        }
//        public void Draw(AcadDatabase acadDatabase)
//        {
//            var lines = BranchPolyline.ToLines();
//            foreach (var line in lines)
//            {
////                line.ColorIndex = 5;
//                line.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE-AI");
//                acadDatabase.CurrentSpace.Add(line);
//            }
//        }
        public void InsertValve(AcadDatabase acadDatabase, List<Line> avoidLines, string strMapScale)
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
            List<Line> lines = BranchPolyline.ToLines();
            if(lines.Count != 0)
            {
                var posPt = new Point3d();
                double angle = 0.0;
                bool isInsert = true;
                var tmpLines = lines.OrderBy(o => o.Length).ToList();
                var bakLines = lines.OrderBy(o => o.Length).ToList();
                var line = tmpLines.Last();
                int lineIndex = tmpLines.Count - 1;
                while (!InsertValve(avoidLines,line, scale, out posPt, out angle))//考虑躲避
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

                if(!isInsert)
                {
                    var tmpline = bakLines.Last();
                    bakLines.Remove(tmpline);
                    if (tmpline.Length >= 560)
                    {
                        //在这条线位置插入蝶阀
                        if(InsertValve(line,out posPt,out angle))
                        {
                            var brkLine = InsertValve(acadDatabase,tmpline, posPt,angle, scale);
                            bakLines.AddRange(brkLine);
                        }
                    }
                }
                else
                {
                    var tmpline = bakLines[lineIndex];
                    bakLines.Remove(tmpline);
                    var brkLine = InsertValve(acadDatabase,tmpline, posPt, angle, scale);
                    bakLines.AddRange(brkLine);
                    //此线 pt 和angle 插入阀门
                }
                //绘制bakLines
                foreach (var l in bakLines)
                {
                    //line.ColorIndex = 5;
                    l.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE-AI");
                    acadDatabase.CurrentSpace.Add(l);
                }
            }
        }
        private bool InsertValve(Line line, out Point3d pt,out double angle)//不考虑躲避
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
            if(!IsIntersectWith(avoidLines, pt, scale, angle))
            {
                return true;
            }
            else
            {
                double step = 100;
                var point1 = new Point3d(pt.X, pt.Y, pt.Z);
                var point2 = new Point3d(pt.X, pt.Y, pt.Z);

                var vector1 = pt.GetVectorTo(line.StartPoint).GetNormal()*step;
                var vector2 = pt.GetVectorTo(line.EndPoint).GetNormal()*step;
                point1 = point1 + vector1;

                bool isOK1 = true;
                while (IsIntersectWith(avoidLines, point1, scale, angle))
                {
                    point1 = point1 + vector1;
                    if(point1.DistanceTo(line.StartPoint) < 500)
                    {
                        isOK1 = false;
                    }
                }

                point2 = point2 + vector2;
                bool isOK2 = true;
                while (IsIntersectWith(avoidLines, point2, scale, angle))
                {
                    point2 = point2 + vector2;
                    if (point2.DistanceTo(line.EndPoint) < 500)
                    {
                        isOK2 = false;
                    }
                }

                if(isOK1 && isOK2)
                {
                    if(point1.DistanceTo(pt) < point2.DistanceTo(pt))
                    {
                        pt = point1;
                    }
                    else
                    {
                        pt = point2;
                    }
                }
                else if(isOK1 && (!isOK2))
                {
                    pt = point1;
                }
                else if (!isOK1 && isOK2)
                {
                    pt = point2;
                }
                else
                {
                    return false;
                }
                //向end点找leftPoint
                //向start点找rigthPoint
                //如果都找到，比较pt和他们的距离
                //如果只找到一个，pt = 该点
                //如果都没找到，返回false
            }


            return true;
        }
        private List<Line> InsertValve(AcadDatabase acadDatabase,Line line,Point3d pt,double angle,double scale)
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
        private bool IsIntersectWith(List<Line> avoidLines,Point3d pt, double scale, double angle)
        {
            //构造蝶阀包围盒
            var vector = new Vector3d(Math.Cos(angle), Math.Sin(angle), 0.0);
            var pt1 = pt + vector *240* scale;
            var valveLine = new Line(pt, pt1);
            var newValveLine = valveLine.ExtendLine(200);
            var valveBox = newValveLine.Buffer(100);
            foreach (var l in avoidLines)
            {
                if(valveBox.IsIntersects(l))
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
            if(lines.Count != 0)
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
                var refVector = new Vector3d(0, 0, 1);
                var basVector = new Vector3d(1, 0, 0);
                double angle = basVector.GetAngleTo(vector, refVector);
                if (angle > Math.PI / 2.0 && angle <= Math.PI)
                {
                    angle = angle + Math.PI;
                }
                else if (angle > Math.PI && angle <= Math.PI * 3.0 / 2.0)
                {
                    angle = angle - Math.PI;
                }
                double tmpAngle = angle + Math.PI / 2.0;
                var tmpVecotr = new Vector3d(Math.Cos(tmpAngle), Math.Sin(tmpAngle), 0.0);
                position = position + 150 * tmpVecotr;

                var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-DIMS", riserName, position, new Scale3d(1, 1, 1), angle);
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
                return true;
            }
            else
            {
                return false;
            }

        }
        private bool InsertValve(AcadDatabase acadDatabase, double scale,ref List<Line> lines)
        {
            if (lines.Count != 0)
            {
                var line = lines.Last();
                var normal = line.EndPoint.GetVectorTo(line.StartPoint).GetNormal();
                var vector = normal * 500;
                if (line.Length < 1000 && line.Length >= 560)
                {
                    vector = normal * (line.Length / 2.0);
                }
                else if (line.Length < 560)
                {
                    return false;
                }
                var postion = line.GetCenter();
                var refVector = new Vector3d(0, 0, 1);
                var basVector = new Vector3d(1, 0, 0);
                double angle = basVector.GetAngleTo(vector, refVector);
                if (angle > Math.PI / 2.0 && angle <= Math.PI)
                {
                    angle = angle + Math.PI;
                }
                else if (angle > Math.PI && angle <= Math.PI * 3.0 / 2.0)
                {
                    angle = angle - Math.PI;
                }

                //判断这条线是否可以插入阀
                var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "蝶阀", postion, new Scale3d(1, 1, 1), angle);
                var blk = acadDatabase.Element<BlockReference>(blkId);
                blk.ScaleFactors = new Scale3d(scale, scale, scale);

                var tmpLines = BreakLine(blk, line , scale);
                lines.Remove(line);
                line.Dispose();
                lines.AddRange(tmpLines);
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
                return true;
            }
            else
            {
                return false;
            }
        }
        private List<Line> BreakLine(BlockReference blk , Line line , double scale)
        {
            double length = 240 * scale;
            var resLines = new List<Line>();
            var startPt = line.StartPoint;
            var endPt = line.EndPoint;
            var normal = startPt.GetVectorTo(endPt).GetNormal();
            var centPt = blk.GetCenter();
            centPt = line.GetClosestPointTo(centPt, false);
            var pt1 = centPt + (-normal*length/2.0);
            var pt2 = centPt + (normal * length / 2.0);
            var line1 = new Line(startPt, pt1);
            var line2 = new Line(pt2, endPt);
            resLines.Add(line1);
            resLines.Add(line2);
            return resLines;
        }
    }
}
