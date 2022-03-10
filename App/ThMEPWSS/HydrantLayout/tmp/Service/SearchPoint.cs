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

        public SearchPoint(MPolygon mp, Point3d centerPoint)
        {
            this.LeanWall = mp;
            this.CenterPoint = centerPoint;
            Frame = LeanWall.Shell();
            Holes = LeanWall.Holes();
            if (!Frame.IsCCW()) Frame.ReverseCurve();
            foreach (Polyline hole in Holes) 
            {
                if (hole.IsCCW()) hole.ReverseCurve();
            }

            FindColumns();     //分离立柱
        }

        public void FindColumns()
        {
            foreach (var pl in Holes)
            {
                //这里缺一个判断
                //rectangle 
                if (pl.Area < Info.ColumnAreaBound && pl.IsRectangle())
                {
                    Columns.Add(pl);
                    Holes.Remove(pl);
                }
            }
        }

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
                        basePointList.Add(basePoint1);
                        dirList.Add(baseDir1);
                        Polyline drawVP = CreateBoundaryService.CreateBoundary(basePoint1 + 100 * baseDir1, 200, 200, baseDir1);
                        DrawUtils.ShowGeometry(drawVP, "l1vp",5 ,lineWeightNum : 30);
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


        public bool IsVPBlocked(Point3d basePoint, Vector3d Dir)
        {
            Polyline pl = CreateBoundaryService.CreateBoundary(basePoint + Dir * 0.5 * Info.VPSide, Info.VPSide - 10, Info.VPSide - 10, Dir);
            return FeasibilityCheck.IsFireBlocked(pl);
        }


        //public Vector3d FindDir(Point3d basePoint) 
        //{

        //} 


    }
}
