using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Diagnostics;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class AdjustPolyline
    {
        public List<Polyline> OriginalPolyList = new List<Polyline>();
        public Polyline Boundary = new Polyline();
        public double ClearDis = 0;

        public List<DirLine> DirLineListX = new List<DirLine>();
        public List<DirLine> DirLineListY = new List<DirLine>();
        public List<DirLine> NewLineistX = new List<DirLine>();
        public List<DirLine> NewLineistY = new List<DirLine>();

        public List<Polyline> Skeleton = new List<Polyline>();
        public List<Polyline> Connector = new List<Polyline>();
        public List<Polyline> ExcessPoly = new List<Polyline>();

        //类内全局变量
        int NowPolylineIndex = 0;


        public AdjustPolyline(List<Polyline> pls, List<Polyline> cn, Polyline boundary, double clearDis)
        {
            OriginalPolyList = pls;
            Connector = cn;
            ClearDis = clearDis;
            Boundary = boundary;
        }

        public void Pipeline()
        {
            //集体转换成dirLine;
            ToDirLineList();

            //
            Combine();

            //
            SaveResult();

        }

        public void Pipeline2()
        {
            for (int i = 0; i < OriginalPolyList.Count; i++)
            {
                Polyline newPl = PolylineProcessService.ClearPolylineUnclosed(OriginalPolyList[i]);

                if (Boundary.Area > 5000)
                {
                    newPl = ClearBendsLongFirst(newPl, Boundary, ClearDis, 0);
                }
                Skeleton.Add(newPl);
            }
            Skeleton.AddRange(ExcessPoly);
        }

        public void Pipeline3() 
        {
            for (int i = 0; i < OriginalPolyList.Count; i++)
            {
                NowPolylineIndex = i;

                Polyline newPl = PolylineProcessService.ClearPolylineUnclosed(OriginalPolyList[i]);

                if (i == 0) CheckConnector(newPl);
                //newPl = ClearBendsLongFirst(newPl, Boundary, ClearDis*0.5, 0);
                //DrawUtils.ShowGeometry(newPl, "l3ClearBends", 3, lineWeightNum: 30);
                List<Polyline> mergedPl = GetMergedPolyline(newPl);
                Skeleton.AddRange(mergedPl);

                

            }

            //CheckConnectorOK
            Skeleton.AddRange(Connector);
            //Skeleton.AddRange(ExcessPoly);
            ExcessPoly.ForEach(x=> DrawUtils.ShowGeometry(x, "l3ExcessPoly", 3, lineWeightNum: 30));
        }

        public void ToDirLineList()
        {
            for (int i = 0; i < OriginalPolyList.Count; i++)
            {
                Polyline newPl = PolylineProcessService.ClearPolylineUnclosed(OriginalPolyList[i]);
                List<Point3d> point3Ds = newPl.GetPoints().ToList();
                for (int j = 0; j < point3Ds.Count - 1; j++)
                {
                    Point3d pt0 = point3Ds[j];
                    Point3d pt1 = point3Ds[j + 1];
                    int type = GetDirType(pt1 - pt0);
                    //CreateDirLine(pt0, pt1, type);

                    Point3d newPt0 = new Point3d();
                    Point3d newPt1 = new Point3d();

                    if (type == 0)
                    {
                        int x0 = (int)pt0.X;
                        int y0 = (int)pt0.Y;
                        int x1 = (int)pt1.X;
                        newPt0 = new Point3d(x0, y0, 0);
                        newPt1 = new Point3d(x1, y0, 0);
                        point3Ds[j] = newPt0;
                        point3Ds[j + 1] = newPt1;
                        DirLineListX.Add(new DirLine(newPt0, newPt1, type));
                    }
                    else if (type == 1)
                    {
                        int x0 = (int)pt0.X;
                        int y0 = (int)pt0.Y;
                        int y1 = (int)pt1.Y;
                        newPt0 = new Point3d(x0, y0, 0);
                        newPt1 = new Point3d(x0, y1, 0);
                        point3Ds[j] = newPt0;
                        point3Ds[j + 1] = newPt1;
                        DirLineListY.Add(new DirLine(newPt0, newPt1, type));
                    }
                    else if (type == 2)
                    {
                        int x0 = (int)pt0.X;
                        int y0 = (int)pt0.Y;
                        int x1 = (int)pt1.X;
                        int y1 = (int)pt1.Y;
                        newPt0 = new Point3d(x0, y0, 0);
                        newPt1 = new Point3d(x1, y1, 0);
                        Point3d newPtTurn = new Point3d(x1, y0, 0);
                        point3Ds[j] = newPt0;
                        point3Ds[j + 1] = newPt1;
                        DirLineListX.Add(new DirLine(newPt0, newPtTurn, 0));
                        DirLineListY.Add(new DirLine(newPtTurn, newPt1, 0));
                    }
                }
            }

        }

        public void Combine() 
        {
            for (int i = 0; i < DirLineListX.Count; i++) 
            {
                if (DirLineListX[i].ThisLine.StartPoint.X > DirLineListX[i].ThisLine.EndPoint.X) 
                {
                    DirLineListX[i].ThisLine = new Line(DirLineListX[i].ThisLine.EndPoint, DirLineListX[i].ThisLine.StartPoint);
                }
            }
            for (int i = 0; i < DirLineListY.Count; i++)
            {
                if (DirLineListY[i].ThisLine.StartPoint.Y > DirLineListY[i].ThisLine.EndPoint.Y)
                {
                    DirLineListY[i].ThisLine = new Line(DirLineListY[i].ThisLine.EndPoint, DirLineListY[i].ThisLine.StartPoint);
                }
            }
            DirLineListX = DirLineListX.OrderBy(x => x.ThisLine.StartPoint.Y).ToList();
            DirLineListY = DirLineListY.OrderBy(x => x.ThisLine.StartPoint.X).ToList();


            //while (true)
            List<int> deleteList = new List<int>();
            for (int i = 0; i < DirLineListX.Count - 1; i++)
            {
                DirLine tmpLine0 = DirLineListX[i];
                DirLine tmpLine1 = DirLineListX[i + 1];

                if (tmpLine1.ThisLine.StartPoint.Y - tmpLine0.ThisLine.StartPoint.Y < ClearDis)
                {
                    Vector3d offset = new Vector3d();
                    DirLine newLine = new DirLine();
                    if (AbleToMergeX(tmpLine0, tmpLine1, ref newLine, offset))
                    {
                        DirLineListX[i + 1] = newLine;
                        deleteList.Add(i);
                    }
                }
            }
            for (int i = DirLineListX.Count - 1; i >= 0; i--) 
            {
                if (deleteList.Contains(i))
                DirLineListX.RemoveAt(i);
            }

            deleteList.Clear();
            for (int i = 0; i < DirLineListY.Count - 1; i++)
            {
                DirLine tmpLine0 = DirLineListY[i];
                DirLine tmpLine1 = DirLineListY[i + 1];

                if (tmpLine1.ThisLine.StartPoint.X - tmpLine0.ThisLine.StartPoint.X < ClearDis)
                {
                    Vector3d offset = new Vector3d();
                    DirLine newLine = new DirLine();
                    if (AbleToMergeY(tmpLine0, tmpLine1, ref newLine, offset))
                    {
                        DirLineListY[i + 1] = newLine;
                        deleteList.Add(i);
                    }
                }
            }
            for (int i = DirLineListY.Count - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                DirLineListY.RemoveAt(i);
            }
        }

        public static int GetDirType(Vector3d vec) 
        {
            int type = 0;
            Vector3d dirX = new Vector3d(1, 0, 0);
            Vector3d dirY = new Vector3d(0, 1, 0);
            if (Math.Abs(dirX.DotProduct(vec)) > 0.95)
            {
                return 0;
            }
            else if (Math.Abs(dirY.DotProduct(vec)) > 0.95)
            {
                return 1;
            }
            else 
            {
                return 2;
            }
            return type;
        }

        public bool AbleToMergeX(DirLine dirLine0,DirLine dirLine1, ref DirLine newDirLine,Vector3d offset) 
        {
            if (dirLine0.ThisLine.EndPoint.X + 10 > dirLine1.ThisLine.StartPoint.X && dirLine0.ThisLine.StartPoint.X < dirLine1.ThisLine.EndPoint.X + 10)
            {
                int newX0 = (int)Math.Min(dirLine0.ThisLine.StartPoint.X, dirLine1.ThisLine.StartPoint.X);
                int newX1 = (int)Math.Max(dirLine0.ThisLine.EndPoint.X, dirLine1.ThisLine.EndPoint.X);
                int newY = 0;
                if (dirLine0.ThisLine.Length > dirLine1.ThisLine.Length)
                {
                    newY = (int)dirLine0.ThisLine.StartPoint.Y;
                    offset = new Vector3d(0, dirLine0.ThisLine.StartPoint.Y - dirLine1.ThisLine.StartPoint.Y, 0);
                }
                else
                {
                    newY = (int)dirLine1.ThisLine.StartPoint.Y;
                    offset = new Vector3d(0, dirLine1.ThisLine.StartPoint.Y - dirLine0.ThisLine.StartPoint.Y, 0);
                }

                newDirLine = new DirLine(new Point3d(newX0, newY,0), new Point3d(newX1, newY,0), 0);
                return true;
            }
            
            return false; 
        }

        public bool AbleToMergeY(DirLine dirLine0, DirLine dirLine1, ref DirLine newDirLine, Vector3d offset)
        {
            if (dirLine0.ThisLine.EndPoint.Y + 10 > dirLine1.ThisLine.StartPoint.Y && dirLine0.ThisLine.StartPoint.Y < dirLine1.ThisLine.EndPoint.Y + 10)
            {
                int newY0 = (int)Math.Min(dirLine0.ThisLine.StartPoint.Y, dirLine1.ThisLine.StartPoint.Y);
                int newY1 = (int)Math.Min(dirLine0.ThisLine.EndPoint.Y, dirLine1.ThisLine.EndPoint.Y);
                int newX = 0;
                if (dirLine0.ThisLine.Length > dirLine1.ThisLine.Length)
                {
                    newX = (int)dirLine0.ThisLine.StartPoint.X;
                    offset = new Vector3d(0, dirLine0.ThisLine.StartPoint.X - dirLine1.ThisLine.StartPoint.X, 0);
                }
                else
                {
                    newX = (int)dirLine1.ThisLine.StartPoint.X;
                    offset = new Vector3d(0, dirLine1.ThisLine.StartPoint.Y - dirLine0.ThisLine.StartPoint.Y, 0);
                }

                newDirLine = new DirLine(new Point3d(newX, newY0, 0), new Point3d(newX, newY1, 0), 0);
                return true;
            }

            return false;
        }


        //修线
        public Polyline ClearBendsLongFirst(Polyline originalPl, Polyline boundary, double dis, int Mode)
        {
            Polyline newPl = originalPl.Clone() as Polyline;

            double bufferDis = 0;
            if (Mode == 0)
            {
                bufferDis = 5;
            }
            else if (Mode == 1)
            {
                bufferDis = -50;
            }

            Polyline newBoundary = boundary.Buffer(bufferDis).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();
            int num = newPl.NumberOfVertices;
            for (int i = num - 4; i >= 0; i--)
            {
                if (i + 3 >= newPl.NumberOfVertices) continue;
                //if (i == 0) continue;
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                Point3d pt2 = newPl.GetPoint3dAt(i + 2);
                Point3d pt3 = newPl.GetPoint3dAt(i + 3);

                if ((pt2 - pt1).Length < dis)
                {
                    Point3d newPt1 = FindDiagonalPoint(pt0, pt1, pt2);
                    Point3d newPt2 = FindDiagonalPoint(pt1, pt2, pt3);

                    bool ok1 = newBoundary.Contains(new Line(newPt1, pt2)) && newBoundary.Contains(new Line(newPt1, pt0));
                    bool ok2 = newBoundary.Contains(new Line(newPt2, pt1)) && newBoundary.Contains(new Line(newPt2, pt3));
                    
                    if (i + 3 == num - 1) ok2 = false;
                    if (i == 0) ok1 = false;


                    if (i + 3 == newPl.NumberOfVertices) ok2 = false;
                    if (i == 0) ok1 = false;

                    Vector3d vec0 = pt1 - pt0;
                    Vector3d vec2 = pt3 - pt2;
                    if ((ok1 && ok2 && vec0.Length > vec2.Length) || (ok2 && !ok1))
                    {
                        newPl.AddVertexAt(i + 1, newPt2.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);

                        if (vec2.Length > Parameter.IsLongSide / 2 && vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                        {
                            Polyline excessPl = new Polyline();
                            excessPl.AddVertexAt(0, newPt2.ToPoint2D(), 0, 0, 0);
                            excessPl.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                            ExcessPoly.Add(excessPl);
                        }
                    }
                    else if ((ok1 && ok2 && vec0.Length < vec2.Length) || (!ok2 && ok1))
                    {
                        newPl.AddVertexAt(i, newPt1.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);

                        if (vec0.Length > Parameter.IsLongSide / 2 && vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                        {
                            Polyline excessPl = new Polyline();
                            excessPl.AddVertexAt(0, newPt1.ToPoint2D(), 0, 0, 0);
                            excessPl.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                            ExcessPoly.Add(excessPl);
                        }
                    }
                }
            }

            return newPl;
        }

        public Polyline ClearBendsLongFirstClosed(Polyline originalPl, Polyline boundary, double dis)
        {
            Polyline newPl = originalPl.Clone() as Polyline;

            double bufferDis = 5;
            
            Polyline newBoundary = boundary.Buffer(bufferDis).OfType<Polyline>().ToList().OrderByDescending(x => x.Area).First();
            int num = newPl.NumberOfVertices;
            for (int i = num - 4; i >= 0; i--)
            {
                if (i + 3 >= newPl.NumberOfVertices) continue;
                //if (i == 0) continue;
                Point3d pt0 = newPl.GetPoint3dAt(i);
                Point3d pt1 = newPl.GetPoint3dAt(i + 1);
                Point3d pt2 = newPl.GetPoint3dAt(i + 2);
                Point3d pt3 = newPl.GetPoint3dAt(i + 3);

                if ((pt2 - pt1).Length < dis)
                {
                    Point3d newPt1 = FindDiagonalPoint(pt0, pt1, pt2);
                    Point3d newPt2 = FindDiagonalPoint(pt1, pt2, pt3);

                    bool ok1 = newBoundary.Contains(new Line(newPt1, pt2)) && newBoundary.Contains(new Line(newPt1, pt0));
                    bool ok2 = newBoundary.Contains(new Line(newPt2, pt1)) && newBoundary.Contains(new Line(newPt2, pt3));

                    //if (i + 3 == num - 1) ok2 = false;
                    //if (i == 0) ok1 = false;


                    //if (i + 3 == newPl.NumberOfVertices) ok2 = false;
                    //if (i == 0) ok1 = false;

                    Vector3d vec0 = pt1 - pt0;
                    Vector3d vec2 = pt3 - pt2;
                    if ((ok1 && ok2 && vec0.Length > vec2.Length) || (ok2 && !ok1))
                    {
                        newPl.AddVertexAt(i + 1, newPt2.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);
                        newPl.RemoveVertexAt(i + 2);

                        if (vec2.Length > Parameter.IsLongSide / 2 && vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                        {
                            Polyline excessPl = new Polyline();
                            excessPl.AddVertexAt(0, newPt2.ToPoint2D(), 0, 0, 0);
                            excessPl.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
                            ExcessPoly.Add(excessPl);
                        }
                    }
                    else if ((ok1 && ok2 && vec0.Length < vec2.Length) || (!ok2 && ok1))
                    {
                        newPl.AddVertexAt(i, newPt1.ToPoint2D(), 0, 0, 0);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);
                        newPl.RemoveVertexAt(i + 1);

                        if (vec0.Length > Parameter.IsLongSide / 2 && vec0.GetNormal().DotProduct(vec2.GetNormal()) < -0.95)
                        {
                            Polyline excessPl = new Polyline();
                            excessPl.AddVertexAt(0, newPt1.ToPoint2D(), 0, 0, 0);
                            excessPl.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
                            ExcessPoly.Add(excessPl);
                        }
                    }
                }
            }

            return newPl;
        }


        public Point3d FindDiagonalPoint(Point3d pt0, Point3d pt1, Point3d pt2)
        {
            Vector3d dir = pt1 - pt0;
            Point3d newPt = pt2 - dir;
            return newPt;
        }

        //合并距离接近的线
        public List<Polyline> GetMergedPolyline(Polyline oldPl) 
        {
            List<Polyline> outPolylineList = new List<Polyline>();
            Dictionary<Line, int> lineIndexMap = new Dictionary<Line, int>();
            List<int> deleteList = new List<int>();

            var points = PassageWayUtils.GetPolyPoints(oldPl);
            List<Line> oldLineList = new List<Line>();
            for (int i = 0; i < points.Count -1 ; i++) 
            {
                Line line0 = new Line(points[i], points[i + 1]);
                oldLineList.Add(line0);
                lineIndexMap.Add(line0, i);
            }

            var LineIndex = new ThCADCoreNTSSpatialIndex(oldLineList.ToCollection());

            List<Point3d> newPtList = new List<Point3d>();
            Point3d pin = points[0];
             
            for (int i = 0; i < points.Count - 1;i++)
            {
                if (i == points.Count - 2) 
                {
                    newPtList.Add(points[i]);
                    newPtList.Add(points[i + 1]);
                    Polyline newPl = PassageWayUtils.BuildPolyline(newPtList);
                    outPolylineList.Add(newPl);
                }
                if (deleteList.Contains(i)) 
                {
                    newPtList.Add(points[i]);
                    if (newPtList.Count > 2) 
                    {
                        Polyline newPl = PassageWayUtils.BuildPolyline(newPtList);
                        outPolylineList.Add(newPl);
                        newPtList.Clear();
                    }
                    continue;
                }

                //不是意外情况，先加入当前点。
                newPtList.Add(points[i]);

                Line nowLine = oldLineList[i];
                Polyline linePl = new Polyline();
                Vector3d nowDir = (points[i + 1] - points[i]).GetNormal();
                linePl.AddVertexAt(0, (points[i] + nowDir * 5).ToPoint2D(), 0, 0, 0);
                linePl.AddVertexAt(1, (points[i+1] - nowDir *5).ToPoint2D(), 0, 0, 0);
                var pls = ThCADCoreNTSOperation.BufferFlatPL(linePl,ClearDis).OfType<Polyline>().ToList();

                Polyline flatPl = new Polyline();
                if (pls.Count > 0)
                {
                    flatPl = pls.FindByMax(x => x.Area);
                }
                else
                {
                    continue;
                }
                List<Line> foundLineList = LineIndex.SelectCrossingPolygon(flatPl).OfType<Line>().ToList();
                List<Line> changeLine = new List<Line>();
                List<Vector3d> changeVec = new List<Vector3d>();


                for (int j = 0; j < foundLineList.Count; j++) 
                {
                    Line foundLine = foundLineList[j];
                    bool flag0 = (Math.Abs((foundLine.StartPoint - foundLine.EndPoint).GetNormal().DotProduct(nowDir)) > 0.95);
                    
                    Point3d closePoint = foundLine.GetClosestPointTo(points[i], true);
                    Vector3d vec = points[i] - closePoint;
                    double dis = foundLine.GetClosestPointTo(points[i], true).DistanceTo(points[i]);
                    bool flag1 = dis > 20;

                    if (flag0 && flag1) 
                    {
                        //bool foundLineOk = (foundLine.StartPoint!= pin) && (foundLine.EndPoint != pin);
                        //List<Point3d> ptList = linePl.GetPoints().ToList();
                        //bool linePlOk = !ptList.Contains(pin);
                        if (foundLine.Length < linePl.Length)
                        {
                            changeLine.Add(foundLine);
                            changeVec.Add(vec);

                            int index = lineIndexMap[foundLine];
                            deleteList.Add(index);
                            Point3d newPt0 = foundLine.StartPoint + vec;
                            Point3d newPt1 = foundLine.EndPoint + vec;
                            points[index] = newPt0;
                            points[index + 1] = newPt1;

                            newPtList.Add(newPt0);
                            newPtList.Add(newPt1);
                        }
                        else
                        {
                            changeLine.Add(nowLine);
                            changeVec.Add(-vec);

                            Point3d newPt0 = points[i] - vec;
                            Point3d newPt1 = points[i+1] - vec;
                            points[i] = newPt0;
                            points[i + 1] = newPt1;
                            newPtList[newPtList.Count-1] = newPt0;
                            //当前管道被合并则跳出
                            break;
                        }
                    }
                }

                //如果是最外层的Polyline，则新增连接
                if (NowPolylineIndex == 0)
                {
                    for (int a = 0; a < changeLine.Count; a++)
                    {
                        Line foundLine = changeLine[a];
                        Vector3d vec = changeVec[a];

                        for (int k = 0; k < Connector.Count; k++)
                        {
                            Point3d firstPt = Connector[k].GetPoint3dAt(0);
                            if (foundLine.DistanceTo(firstPt, false) < 5)
                            {
                                Point3d newPt = firstPt + vec;
                                Connector[k].AddVertexAt(0, newPt.ToPoint2D(), 0, 0, 0);
                            }
                            Point3d endPt = Connector[k].GetPoint3dAt(Connector[k].NumberOfVertices - 1);
                            if (foundLine.DistanceTo(endPt, false) < 5)
                            {
                                Point3d newPt = endPt + vec;
                                Connector[k].AddVertexAt(Connector[k].NumberOfVertices, newPt.ToPoint2D(), 0, 0, 0);
                            }
                        }
                    }
                }
                else
                {
                    for (int a = 0; a < changeLine.Count; a++)
                    {
                        Line foundLine = changeLine[a];
                        Vector3d vec = changeVec[a];

                        if (foundLine.DistanceTo(pin, false) < 5)
                        {
                            Point3d newPt = pin;
                            newPtList.Insert(0, newPt);
                        }
                    }
                }
            }

             outPolylineList.ForEach(x => DrawUtils.ShowGeometry(x, "l3MergedPoly", 1, lineWeightNum: 30));
;            return outPolylineList;
        }

        public void CheckConnector(Polyline newPl) 
        {

            for (int k = 0; k < Connector.Count; k++)
            {
                Point3d firstPt = Connector[k].GetPoint3dAt(0);
                Point3d endPt = Connector[k].GetPoint3dAt(Connector[k].NumberOfVertices - 1);
                if (newPl.DistanceTo(firstPt, false) > 2 && newPl.DistanceTo(endPt, false) > 2)
                {
                    Point3d pt0 = newPl.GetClosestPointTo(firstPt, false);
                    double l0 = pt0.DistanceTo(firstPt);
                    Point3d pt1 = newPl.GetClosestPointTo(endPt, false);
                    double l1 = pt1.DistanceTo(endPt);

                    if (l0 < l1)
                    {
                        Connector[k].AddVertexAt(0, pt0.ToPoint2D(), 0, 0, 0);
                    }
                    else 
                    {
                        Connector[k].AddVertexAt(Connector[k].NumberOfVertices, pt1.ToPoint2D(), 0, 0, 0);
                    }
                }        
            }
        }

        //
        public void SaveResult() 
        {
            for (int i = 0; i < DirLineListX.Count; i++) 
            {
                if (DirLineListX[i].ThisLine.Length > ClearDis) 
                {
                    Polyline newPl = DirLineListX[i].ThisLine.ToNTSLineString().ToDbPolyline();
                    Skeleton.Add(newPl);
                }
            }

            for (int i = 0; i < DirLineListY.Count; i++) 
            {
                if (DirLineListY[i].ThisLine.Length > ClearDis)
                {
                    Polyline newPl = DirLineListY[i].ThisLine.ToNTSLineString().ToDbPolyline();
                    Skeleton.Add(newPl);
                }
            }
        }

    }

    class DirLine
    {
        public Line ThisLine = new Line();
        public int Dir = 0;  //0;X ,1:Y 

        public DirLine(Point3d pt0,Point3d pt1,int dir) 
        {
            ThisLine = new Line(pt0, pt1);
            Dir = dir;            
        }
        public DirLine()
        {

        }
    }

}
