using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;


using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;

using NFox.Cad;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.HydrantLayout.Service
{
    class SearchPoint
    {
        //外部输入
        MPolygon LeanWall;
        Point3d CenterPoint;
        //经过处理的外部数据
        public Polyline Frame;
        public List<Polyline> Holes = new List<Polyline>();
        public List<Polyline> Columns = new List<Polyline>();

        //public List<Point3d> TurningPoint = new List<Point3d>();
        //public List<Vector3d> Dir

        //这个是给消火栓找柱子用的
        public List<Polyline> LeanWallList = new List<Polyline>();

        //这个给消火栓的柱子用的，判断左 中 右（0，1，2）
        public List<int> BasePointPosition = new List<int>();

        public SearchPoint(MPolygon mp, Point3d centerPoint)
        {
            this.LeanWall = mp;
            this.CenterPoint = centerPoint;
            Frame = LeanWall.Shell();
            Holes =LeanWall.Holes();
            if (!Frame.IsCCW()) Frame.ReverseCurve();
            foreach (Polyline hole in Holes) 
            {
                if (hole.IsCCW()) hole.ReverseCurve();
            }

            //var clockwise90 = new Vector3d(dir.Y, -dir.X, dir.Z).GetNormal();
            //var clockwise270 = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();
           
        }

        public void Pipeline() 
        {
            ClearShell();
            ClearHoles();
            FindColumns();     //分离立柱
        }

        //------------------------------------
        //数据处理

        //筛除 重合点+距离过段线段+直线多余点
        public Polyline ClearPolyline(Polyline a) 
        {
            //第一筛
            //筛除 重合点+距离过段线段
            List<int> deleteList = new List<int>();
            int nums = a.NumberOfVertices;

            for (int i = 0; i < nums; i++)
            {
                var pt1 = a.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;
                var pt2 = a.GetPoint3dAt((i + 1) % a.NumberOfVertices);
                Vector3d dir2 = pt2 - pt1;
                if (dir2.Length < 5)
                {
                    deleteList.Add(i);
                }
            }
            for (int i = nums - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    a.RemoveVertexAt(i);
                }
            }

            //第二筛
            //筛除 直线多余点
            deleteList = new List<int>();
            nums = a.NumberOfVertices;
            for (int i = 0; i < nums; i++)
            {
                var pt1 = a.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;
                var pt0 = a.GetPoint3dAt((i + a.NumberOfVertices - 1) % a.NumberOfVertices);
                var pt2 = a.GetPoint3dAt((i + 1) % a.NumberOfVertices);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;
                double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                if (angle < 0.1 || angle > 2 * Math.PI - 0.1)
                {
                    deleteList.Add(i);
                }
            }
            for (int i = nums - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    a.RemoveVertexAt(i);
                }
            }
            return a;
        }

        //清理Shell
        public void ClearShell()
        {
            //第一筛
            //筛除 重合点+距离过段线段
            List<int> deleteList = new List<int>();
            int nums = Frame.NumberOfVertices;

            for (int i = 0; i < nums; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                Vector3d dir2 = pt2 - pt1;
                if (dir2.Length < 5)
                {
                    deleteList.Add(i);
                }
            }
            for (int i = nums - 1; i >= 0; i--)
            {
                if (deleteList.Contains(i))
                {
                    Frame.RemoveVertexAt(i);
                }
            }

            //第二筛
            //筛除 直线多余点
            deleteList = new List<int>();
            nums = Frame.NumberOfVertices;
            for (int i = 0; i < nums; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;
                var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;
                double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                if (angle < 0.1 || angle > 2 * Math.PI - 0.1) 
                {
                    deleteList.Add(i);
                }
            }
            for (int i = nums - 1; i >= 0; i--) 
            {
                if (deleteList.Contains(i)) 
                {
                    Frame.RemoveVertexAt(i);
                }
            }

            DrawUtils.ShowGeometry(Frame, "l1clearedframe", 5, lineWeightNum: 30);
        }

        //清理Holes
        public void ClearHoles() 
        {
            for (int i = 0; i < Holes.Count; i++)
            {
                Holes[i] = ClearPolyline(Holes[i]);
            }
        }


        //判断是否是立柱
        public void FindColumns()
        {
            foreach (var pl in Holes)
            {
                //这里缺一个判断
                //rectangle 
                if (pl.Area < Info.ColumnAreaBound )
                {
                    if (pl.IsRectangle())
                    {
                        Columns.Add(pl);
                    }
                }
            }
            Holes.RemoveAll(x => Columns.Contains (x));
        }


        //------------------------------------
        //消火栓

        //寻找转角
        public void FindTurningPoint(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
            LeanWallList.Clear();
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

            //寻找Shell
            for (int i = 0; i < Frame.NumberOfVertices; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;

                var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;

                if (clockwiseFrame == true)
                {
                    double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                    if (angle < Math.PI * 3 / 2 + 0.15 && angle > Math.PI*3/2 - 0.15 && dir1.Length > Info.VPSide /4  && dir2.Length > Info.VPSide /4 && dir1.Length + dir2.Length > 3*Info.VPSide)
                    {
                        Point3d basePoint1 = pt1 - dir1.GetNormal() * 100;
                        Vector3d baseDir1 = dir2.GetNormal();
                        //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                        //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();
                        if (IsVPBlocked(basePoint1,baseDir1,Frame))
                        {
                            basePointList.Add(basePoint1);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }
                    }
                }
            }

            //寻找Hole
            for (int h = 0; h < Holes.Count; h++) 
            {
                Polyline hpl = Holes[h];
                for (int i = 0; i < hpl.NumberOfVertices; i++)
                {
                    var pt1 = hpl.GetPoint3dAt(i);
                    //如果超出距离就跳出
                    if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;

                    var pt0 = hpl.GetPoint3dAt((i + hpl.NumberOfVertices - 1) % hpl.NumberOfVertices);
                    var pt2 = hpl.GetPoint3dAt((i + 1) % hpl.NumberOfVertices);
                    Vector3d dir1 = pt1 - pt0;
                    Vector3d dir2 = pt2 - pt1;

                    if (hpl.IsCCW() == false)
                    {
                        double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                        if (angle < Math.PI * 3 / 2 + 0.15 && angle > Math.PI * 3 / 2 - 0.15 && dir1.Length > Info.VPSide / 4 && dir2.Length > Info.VPSide / 4 && dir1.Length + dir2.Length > 3 * Info.VPSide)
                        {
                            Point3d basePoint1 = pt1 - dir1.GetNormal() * 100;
                            Vector3d baseDir1 = dir2.GetNormal();

                            if (IsVPBlocked(basePoint1, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint1);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }

                        }
                    }
                }
            }
        }

        //如果没找到转角，则寻找立柱
        public void FindColumnPoint(out List<Point3d> basePointList, out List<Vector3d> dirList) 
        {
            LeanWallList.Clear();
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            foreach (var cl in Columns) 
            {
                //Point3d center = cl.GetCentroidPoint();

                for (int i = 0; i < cl.NumberOfVertices; i++) 
                {
                    Point3d start = cl.GetPoint3dAt(i);
                    Point3d end = cl.GetPoint3dAt((i + 1) % cl.NumberOfVertices);
                    Vector3d dir01 = end - start;

                    if (dir01.Length < 10) continue;
                    
                    Point3d mid = start + 0.5 * dir01;
                    Vector3d dirOut = new Vector3d(-dir01.Y, dir01.X, dir01.Z).GetNormal();
                    
                    //Polyline probe = CreateBoundaryService.CreateBoundary(center, 1500, 190, dirOut);

                    Polyline vpMid = CreateBoundaryService.CreateBoundary(mid + Info.VPSide / 2 * dirOut, Info.VPSide, Info.VPSide, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(vpMid,Frame, ProcessedData.ParkingIndex)) 
                    {
                        if (IsVPBlocked(mid, dirOut, Frame))
                        {
                            basePointList.Add(mid);
                            dirList.Add(dirOut);
                            LeanWallList.Add(cl);
                            BasePointPosition.Add(1);
                        }
                    }

                    Point3d left = start + 100 * dir01.GetNormal();
                    Polyline vpLeft = CreateBoundaryService.CreateBoundary(left + Info.VPSide/2 * dirOut, Info.VPSide, Info.VPSide, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(vpLeft,Frame, ProcessedData.ForbiddenIndex)) 
                    {
                        if (IsVPBlocked(left, dirOut, Frame))
                        {
                            basePointList.Add(left);
                            dirList.Add(dirOut);
                            LeanWallList.Add(cl);
                            BasePointPosition.Add(0);
                        }
                    }

                    Point3d right = end - 100 * dir01.GetNormal();
                    Polyline vpRight = CreateBoundaryService.CreateBoundary(right + Info.VPSide / 2 * dirOut, Info.VPSide, Info.VPSide, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(vpRight, Frame, ProcessedData.ForbiddenIndex))
                    {
                        if (IsVPBlocked(right, dirOut, Frame))
                        {
                            basePointList.Add(right);
                            dirList.Add(dirOut);
                            LeanWallList.Add(cl);
                            BasePointPosition.Add(2);
                        }
                    }
                }
            }
        }

        //如果都没找到,寻找第三优先级定位点
        public void FindOtherPoint(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
            LeanWallList.Clear();
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

            for (int i = 0; i < Frame.NumberOfVertices; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                //Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;

                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius && pt2.DistanceTo(CenterPoint) > Info.SearchRadius &&  dir2.Length < Info.OriginRadius * 2/3 ) continue;

                Vector3d baseDir1 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                Line line0 = new Line(pt1, pt2);
                Point3d closetPt = line0.GetClosestPointTo(CenterPoint, false);
                
                //不是端点且没被阻挡
                if (!closetPt.Equals(pt1) && !closetPt.Equals(pt2) && IsVPBlocked(closetPt, baseDir1,Frame))
                {
                    basePointList.Add(closetPt);
                    dirList.Add(baseDir1);
                    LeanWallList.Add(Frame);

                    Polyline drawVP = CreateBoundaryService.CreateBoundary(closetPt + 100 * baseDir1, 200, 200, baseDir1);
                    DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                }


                if (clockwiseFrame == true)
                {
                    //double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                    if (dir2.Length >=  Info.VPSide)
                    {
                        Point3d basePoint1 = pt1 + dir2.GetNormal() * 100;
                        Point3d basePoint2 = pt1 + dir2 / 2;
                        Point3d basePoint3 = pt1 + dir2 - dir2.GetNormal() * 100;

                        //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                        //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                        Polyline drawVP0 = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);
                        drawVP0 = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);
                        drawVP0 = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);

                        if (basePoint1.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint1, baseDir1,Frame))
                        {
                            basePointList.Add(basePoint1);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }

                        if (basePoint2.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint2, baseDir1,Frame))
                        {
                            basePointList.Add(basePoint2);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }
                     
                        if (basePoint3.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint3, baseDir1,Frame))
                        {
                            basePointList.Add(basePoint3);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }
                    }
                }
            }



            for (int h = 0; h < Holes.Count; h++) 
            {
                Polyline hpl = Holes[h];

                for (int i = 0; i < hpl.NumberOfVertices; i++)
                {
                    var pt1 = hpl.GetPoint3dAt(i);
                    //var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                    var pt2 = hpl.GetPoint3dAt((i + 1) % hpl.NumberOfVertices);
                    //Vector3d dir1 = pt1 - pt0;
                    Vector3d dir2 = pt2 - pt1;

                    //如果超出距离就跳出
                    if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius && pt2.DistanceTo(CenterPoint) > Info.SearchRadius && dir2.Length < Info.OriginRadius * 2 / 3) continue;

                    Vector3d baseDir1 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                    Line line0 = new Line(pt1, pt2);
                    Point3d closetPt = line0.GetClosestPointTo(CenterPoint, false);

                    //不是端点且没被阻挡
                    if (!closetPt.Equals(pt1) && !closetPt.Equals(pt2) && IsVPBlocked(closetPt, baseDir1, Frame))
                    {
                        basePointList.Add(closetPt);
                        dirList.Add(baseDir1);
                        LeanWallList.Add(hpl);

                        Polyline drawVP = CreateBoundaryService.CreateBoundary(closetPt + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                    }


                    if (hpl.IsCCW() == false)
                    {
                        //double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                        if (dir2.Length >= Info.VPSide)
                        {
                            Point3d basePoint1 = pt1 + dir2.GetNormal() * 100;
                            Point3d basePoint2 = pt1 + dir2 / 2;
                            Point3d basePoint3 = pt1 + dir2 - dir2.GetNormal() * 100;

                            //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                            //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                            Polyline drawVP0 = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);
                            drawVP0 = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);
                            drawVP0 = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);

                            if (basePoint1.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint1, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint1);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }

                            if (basePoint2.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint2, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint2);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }

                            if (basePoint3.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint3, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint3);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }
                        }
                    }
                }


            }
        }

        //判断立柱是否被阻挡
        public bool IsVPBlocked(Point3d basePoint, Vector3d Dir, Polyline shell)
        {
            Polyline pl = CreateBoundaryService.CreateBoundary(basePoint + Dir * 0.5 * Info.VPSide, Info.VPSide, Info.VPSide, Dir);
            return FeasibilityCheck.IsFireFeasible(pl,shell);
        }


        //------------------------------------
        //灭火器

        //寻找转角
        public void FindTurningPoint2(out List<Point3d> basePointList, out List<Vector3d> dirList,double shortside)
        {
            LeanWallList.Clear();
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

            //寻找Shell
            for (int i = 0; i < Frame.NumberOfVertices; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;

                var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;

                if (clockwiseFrame == true)
                {
                    double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                    if (angle < Math.PI * 3 / 2 + 0.15 && angle > Math.PI * 3 / 2 - 0.15 && dir1.Length > Info.VPSide / 4 && dir2.Length > Info.VPSide / 4 && dir1.Length + dir2.Length > 3 * Info.VPSide)
                    {
                        Point3d basePoint1 = pt1 - dir1.GetNormal() * 0.5 * shortside;
                        Vector3d baseDir1 = dir2.GetNormal();
                        //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                        //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();
                        basePointList.Add(basePoint1);
                        dirList.Add(baseDir1);
                        LeanWallList.Add(Frame);

                        Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);                  
                    }
                }
            }

            //寻找Hole
            for (int h = 0; h < Holes.Count; h++)
            {
                Polyline hpl = Holes[h];
                for (int i = 0; i < hpl.NumberOfVertices; i++)
                {
                    var pt1 = hpl.GetPoint3dAt(i);
                    //如果超出距离就跳出
                    if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;

                    var pt0 = hpl.GetPoint3dAt((i + hpl.NumberOfVertices - 1) % hpl.NumberOfVertices);
                    var pt2 = hpl.GetPoint3dAt((i + 1) % hpl.NumberOfVertices);
                    Vector3d dir1 = pt1 - pt0;
                    Vector3d dir2 = pt2 - pt1;

                    if (hpl.IsCCW() == false)
                    {
                        double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                        if (angle < Math.PI * 3 / 2 + 0.15 && angle > Math.PI * 3 / 2 - 0.15 && dir1.Length > Info.VPSide / 4 && dir2.Length > Info.VPSide / 4 && dir1.Length + dir2.Length > 3 * Info.VPSide)
                        {
                            Point3d basePoint1 = pt1 - dir1.GetNormal() * 100;
                            Vector3d baseDir1 = dir2.GetNormal();

                            if (IsVPBlocked(basePoint1, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint1);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }

                        }
                    }
                }
            }

        }

        //如果没找到转角，则寻找立柱
        public void FindColumnPoint2(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
            LeanWallList.Clear();
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            foreach (var cl in Columns)
            {
                Point3d center = cl.GetCentroidPoint();

                for (int i = 0; i < 4; i++)
                {
                    Point3d start = cl.GetPoint3dAt(i);
                    Point3d end = cl.GetPoint3dAt((i + 1) % 4);
                    Vector3d dir01 = end - start;
                    Point3d mid = start + 0.5 * dir01;
                    Vector3d dirOut = new Vector3d(-dir01.Y, dir01.X, dir01.Z).GetNormal();
                    Polyline probe = CreateBoundaryService.CreateBoundary(center, 1000, 190, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(probe,Frame,ProcessedData.ParkingIndex))
                    {
                        basePointList.Add(mid);
                        dirList.Add(dirOut);
                        LeanWallList.Add(cl);
                    }
                }
            }
        }
        
        //如果都没找到,寻找第三优先级定位点
        public void FindOtherPoint2(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
            LeanWallList.Clear();
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

            for (int i = 0; i < Frame.NumberOfVertices; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                //Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;

                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius && pt2.DistanceTo(CenterPoint)> Info.SearchRadius && dir2.Length < Info.OriginRadius *2/3) continue;

                Vector3d baseDir1 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                Line line0 = new Line(pt1, pt2);
                Point3d closetPt = line0.GetClosestPointTo(CenterPoint, false);

                if (!closetPt.Equals(pt1) && !closetPt.Equals(pt2) && IsVPBlocked(closetPt, baseDir1,Frame))
                {
                    basePointList.Add(closetPt);
                    dirList.Add(baseDir1);
                    LeanWallList.Add(Frame);

                    Polyline drawVP = CreateBoundaryService.CreateBoundary(closetPt + 100 * baseDir1, 200, 200, baseDir1);
                    DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                }


                if (clockwiseFrame == true)
                {
                    //double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                    if (dir2.Length > 3 * Info.VPSide)
                    {
                        Point3d basePoint1 = pt1 + dir2.GetNormal() * 100;

                        //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                        //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();
                        if (IsVPBlocked(basePoint1, baseDir1,Frame))
                        {
                            basePointList.Add(basePoint1);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }

                        Point3d basePoint2 = pt1 + dir2 / 2;

                        if (basePoint2.DistanceTo(CenterPoint) > Info.SearchRadius && IsVPBlocked(basePoint2, baseDir1,Frame))
                        {
                            basePointList.Add(basePoint2);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }

                        Point3d basePoint3 = pt1 + dir2 - dir2.GetNormal() * 100;

                        if (basePoint3.DistanceTo(CenterPoint) > Info.SearchRadius && IsVPBlocked(basePoint3, baseDir1,Frame))
                        {
                            basePointList.Add(basePoint3);
                            dirList.Add(baseDir1);
                            LeanWallList.Add(Frame);

                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }
                    }
                }
            }

            for (int h = 0; h < Holes.Count; h++)
            {
                Polyline hpl = Holes[h];

                for (int i = 0; i < hpl.NumberOfVertices; i++)
                {
                    var pt1 = hpl.GetPoint3dAt(i);
                    //var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                    var pt2 = hpl.GetPoint3dAt((i + 1) % hpl.NumberOfVertices);
                    //Vector3d dir1 = pt1 - pt0;
                    Vector3d dir2 = pt2 - pt1;

                    //如果超出距离就跳出
                    if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius && pt2.DistanceTo(CenterPoint) > Info.SearchRadius && dir2.Length < Info.OriginRadius * 2 / 3) continue;

                    Vector3d baseDir1 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                    Line line0 = new Line(pt1, pt2);
                    Point3d closetPt = line0.GetClosestPointTo(CenterPoint, false);

                    //不是端点且没被阻挡
                    if (!closetPt.Equals(pt1) && !closetPt.Equals(pt2) && IsVPBlocked(closetPt, baseDir1, Frame))
                    {
                        basePointList.Add(closetPt);
                        dirList.Add(baseDir1);
                        LeanWallList.Add(hpl);

                        Polyline drawVP = CreateBoundaryService.CreateBoundary(closetPt + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                    }


                    if (hpl.IsCCW() == false)
                    {
                        //double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                        if (dir2.Length >= Info.VPSide)
                        {
                            Point3d basePoint1 = pt1 + dir2.GetNormal() * 100;
                            Point3d basePoint2 = pt1 + dir2 / 2;
                            Point3d basePoint3 = pt1 + dir2 - dir2.GetNormal() * 100;

                            //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                            //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                            Polyline drawVP0 = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);
                            drawVP0 = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);
                            drawVP0 = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP0, "l1vpbefore", 6, lineWeightNum: 30);

                            if (basePoint1.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint1, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint1);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }

                            if (basePoint2.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint2, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint2);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }

                            if (basePoint3.DistanceTo(CenterPoint) < Info.SearchRadius && IsVPBlocked(basePoint3, baseDir1, Frame))
                            {
                                basePointList.Add(basePoint3);
                                dirList.Add(baseDir1);
                                LeanWallList.Add(hpl);

                                Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                                DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                            }
                        }
                    }
                }


            }
        }
    }
}
