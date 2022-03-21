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
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.tmp.Service;
using ThMEPWSS.HydrantLayout.Model;
using NFox.Cad;
using ThMEPEngineCore.Diagnostics;

namespace ThMEPWSS.HydrantLayout.tmp.Service
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

        public List<Polyline> LeanWallList = new List<Polyline>();

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
            ClearShell();
            FindColumns();     //分离立柱
        }

        public void ClearShell()
        {
            //第一筛
            List<int> deleteList = new List<int>();
            int nums = Frame.NumberOfVertices;

            for (int i = 0; i < nums; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > 2500) continue;
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
            deleteList = new List<int>();
            nums = Frame.NumberOfVertices;
            for (int i = 0; i < nums; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > 2500) continue;
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


        //寻找转角
        public void FindTurningPoint(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

            for (int i = 0; i < Frame.NumberOfVertices; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > 2500) continue;

                var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;

                if (clockwiseFrame == true)
                {
                    double angle = dir2.GetAngleTo(dir1, Vector3d.ZAxis);
                    if (angle < Math.PI * 3 / 2 + 0.15 && angle > Math.PI*3/2 - 0.15 && dir1.Length > Info.VPSide /2  && dir2.Length > Info.VPSide /2 && dir1.Length + dir2.Length > 3*Info.VPSide)
                    {
                        Point3d basePoint1 = pt1 - dir1.GetNormal() * 100;
                        Vector3d baseDir1 = dir2.GetNormal();
                        //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                        //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();
                        if (IsVPBlocked(basePoint1,baseDir1))
                        {
                            basePointList.Add(basePoint1);
                            dirList.Add(baseDir1);
                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }
                        
                        //drawVP .IsCCW 
                        //drawVP.ReverseCurve();
                    }
                }
                //for (int i=0;i<Frame.NumberOfVertices; i++)
                //{
                //    var pt = Frame.GetPoint3dAt((i+1)% Frame.NumberOfVertices);
                //}
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
                Point3d center = cl.GetCentroidPoint();

                for (int i = 0; i < cl.NumberOfVertices; i++) 
                {
                    Point3d start = cl.GetPoint3dAt(i);
                    Point3d end = cl.GetPoint3dAt((i + 1) % cl.NumberOfVertices);
                    Vector3d dir01 = end - start;

                    if (dir01.Length < 10) continue;
                    
                    Point3d mid = start + 0.5 * dir01;
                    Vector3d dirOut = new Vector3d(-dir01.Y, dir01.X, dir01.Z).GetNormal();
                    Polyline probe = CreateBoundaryService.CreateBoundary(center, 1500, 190, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(probe, ProcessedData.ParkingIndex)) 
                    {
                        basePointList.Add(mid);
                        dirList.Add(dirOut);
                        LeanWallList.Add(cl);
                    }

                    Point3d left = start + 100 * dir01.GetNormal();
                    Polyline vpLeft = CreateBoundaryService.CreateBoundary(left + Info.VPSide/2 * dirOut, Info.VPSide, Info.VPSide, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(vpLeft, ProcessedData.ForbiddenIndex)) 
                    {
                        basePointList.Add(left);
                        dirList.Add(dirOut);
                        LeanWallList.Add(cl);
                    }

                    Point3d right = end - 100 * dir01.GetNormal();
                    Polyline vpRight = CreateBoundaryService.CreateBoundary(right + Info.VPSide / 2 * dirOut, Info.VPSide, Info.VPSide, dirOut);
                    if (FeasibilityCheck.IsBoundaryOK(vpRight, ProcessedData.ForbiddenIndex))
                    {
                        basePointList.Add(right);
                        dirList.Add(dirOut);
                        LeanWallList.Add(cl);
                    }
                }
            }
        }

        //如果都没找到,寻找第三优先级定位点
        public void FindOtherPoint(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

            for (int i = 0; i < Frame.NumberOfVertices; i++)
            {
                var pt1 = Frame.GetPoint3dAt(i);
                //如果超出距离就跳出
                if (pt1.DistanceTo(CenterPoint) > Info.SearchRadius) continue;

                //var pt0 = Frame.GetPoint3dAt((i + Frame.NumberOfVertices - 1) % Frame.NumberOfVertices);
                var pt2 = Frame.GetPoint3dAt((i + 1) % Frame.NumberOfVertices);
                //Vector3d dir1 = pt1 - pt0;
                Vector3d dir2 = pt2 - pt1;
                Vector3d baseDir1 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();

                Line line0 = new Line(pt1, pt2);
                Point3d closetPt = line0.GetClosestPointTo(CenterPoint, false);
                if (IsVPBlocked(closetPt, baseDir1))
                {
                    basePointList.Add(closetPt);
                    dirList.Add(baseDir1);
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
                        if (IsVPBlocked(basePoint1, baseDir1))
                        {
                            basePointList.Add(basePoint1);
                            dirList.Add(baseDir1);
                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }

                        Point3d basePoint2 = pt1 + dir2/2;

                        if (basePoint2.DistanceTo(CenterPoint) > 2500 && IsVPBlocked(basePoint2, baseDir1))
                        {
                            basePointList.Add(basePoint2);
                            dirList.Add(baseDir1);
                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint2 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }

                        Point3d basePoint3 = pt1 + dir2 - dir2.GetNormal() * 100;

                        if (basePoint3.DistanceTo(CenterPoint) > 2500 && IsVPBlocked(basePoint3, baseDir1))
                        {
                            basePointList.Add(basePoint3);
                            dirList.Add(baseDir1);
                            Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint3 + 100 * baseDir1, 200, 200, baseDir1);
                            DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);
                        }
                    }
                }
            }
        
        
        }

        //判断立柱是否被阻挡
        public bool IsVPBlocked(Point3d basePoint, Vector3d Dir)
        {
            Polyline pl = CreateBoundaryService.CreateBoundary(basePoint + Dir * 0.5 * Info.VPSide, Info.VPSide - 10, Info.VPSide - 10, Dir);
            return FeasibilityCheck.IsFireBlocked(pl);
        }


        //消火栓

        //
        public void FindTurningPoint2(out List<Point3d> basePointList, out List<Vector3d> dirList,double shortside)
        {
            basePointList = new List<Point3d>();
            dirList = new List<Vector3d>();
            bool clockwiseFrame = Frame.IsCCW();

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
                    if (angle < Math.PI * 3 / 2 + 0.15 && angle > Math.PI * 3 / 2 - 0.15 && dir1.Length > Info.VPSide / 2 && dir2.Length > Info.VPSide / 2 && dir1.Length + dir2.Length > 3 * Info.VPSide)
                    {
                        Point3d basePoint1 = pt1 - dir1.GetNormal() * 0.5 * shortside;
                        Vector3d baseDir1 = dir2.GetNormal();
                        //Point3d basePoint2 = pt1 + dir2.GetNormal() * 100;
                        //Vector3d baseDir2 = new Vector3d(-dir2.Y, dir2.X, dir2.Z).GetNormal();
                        basePointList.Add(basePoint1);
                        dirList.Add(baseDir1);
                        Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP, "l1vp", 5, lineWeightNum: 30);                  
                    }
                }
            }
        }

        public void FindColumnPoint2(out List<Point3d> basePointList, out List<Vector3d> dirList)
        {
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
                    if (FeasibilityCheck.IsBoundaryOK(probe, ProcessedData.ParkingIndex))
                    {
                        basePointList.Add(mid);
                        dirList.Add(dirOut);
                    }
                }
            }
        }

    }
}
