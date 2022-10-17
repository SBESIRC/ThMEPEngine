using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;

using ThMEPHVAC.FloorHeatingCoil.Heating;
using ThMEPHVAC.FloorHeatingCoil.Data;
using ThMEPHVAC.FloorHeatingCoil.Model;

namespace ThMEPHVAC.FloorHeatingCoil.Heating
{
    class RadiatorModel
    {
        public Polyline oldPl = new Polyline();
        public Polyline nowPl = new Polyline();
        public Point3d OriginalFirstPoint = new Point3d();
        public Point3d OriginalSecondPoint = new Point3d();
        public Point3d FirstPoint = new Point3d();
        public Point3d SecondPoint = new Point3d(); 
        public RadiatorModel(Polyline roomPlOld,Polyline roomPl,List<Point3d> PtList) 
        {
            oldPl = roomPlOld;
            nowPl = roomPl;
            FirstPoint = PtList[0];
            SecondPoint = PtList[1];

            Point3d center = FirstPoint + (SecondPoint - FirstPoint) * 0.5;
            Point3d close = oldPl.GetClosestPointTo(center, false);
            Vector3d nowDir = (SecondPoint - FirstPoint).GetNormal();
            Vector3d dirF = (close - center).GetNormal();
            Vector3d dirReal = new Vector3d(-nowDir.Y, nowDir.X, 0);

            if (dirF.DotProduct(dirReal) > 0.1)
            {
                dirReal = -dirReal;
                FirstPoint = PtList[1];
                SecondPoint = PtList[0];
            }
            OriginalFirstPoint = FirstPoint;
            OriginalSecondPoint = SecondPoint;
            ProcessedData.RadiatorDir = dirReal;
        }


        public void Pipeline() 
        {
            Line doorLine = new Line();


            if (nowPl.Contains(FirstPoint) && nowPl.Contains(FirstPoint))
            {
                doorLine = RadiatorToDoorLineBeside(ref nowPl);
            }
            else
            {
                doorLine = RadiatorToDoorLine(ref nowPl);
            }

            int index = 0;
            int reverse = 0;
        
            //原 start->end 是逆时针，与doorline 的顺逆时针对应。

            //如果在函数WaterSeparatorToDoorLine内部没有反。
            if ((doorLine.EndPoint - doorLine.StartPoint).GetNormal().DotProduct((SecondPoint - FirstPoint).GetNormal()) > 0.95)
                reverse = 0;

            if (reverse == 1)
            {
                FirstPoint = doorLine.EndPoint;
                SecondPoint = doorLine.StartPoint;
            }
            else
            {
                FirstPoint = doorLine.StartPoint;
                SecondPoint = doorLine.EndPoint;
            }

            DrawUtils.ShowGeometry(nowPl, "l3RadiatorRegionObb", 3, lineWeightNum: 30);

            List<Point3d> newPtList = new List<Point3d>();
            newPtList.Add(FirstPoint);
            newPtList.Add(SecondPoint);
            ProcessedData.RadiatorPointList = newPtList;

            CreateArea();

        }

        public Line RadiatorToDoorLineBeside(ref Polyline regionObb)
        {
        
            Polyline differArea = PolylineProcessService.CreateRectangle2(SecondPoint, FirstPoint, 100000);
            //Polyline differArea2 = PolylineProcessService.CreateRectangle2(doorLine.StartPoint, doorLine.EndPoint, 100);
            //ProcessedData.DifferArea = differArea2;
            regionObb = regionObb.Difference(differArea).OfType<Polyline>().ToList().FindByMax(x => x.Area);
            DrawUtils.ShowGeometry(differArea, "l3RadiatorDiff", 3, lineWeightNum: 30);

            PolylineProcessService.ClearPolyline(ref regionObb);
            ProcessedData.RadiatorOffset = new Vector3d(0, 0, 0);

            return new Line(FirstPoint, SecondPoint);
        }

        public Line RadiatorToDoorLine(ref Polyline regionObb)
        {
            //
            Line doorLine = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 0));
            Point3d start = FirstPoint;
            Point3d end = SecondPoint;
            Vector3d radiatorDir = ProcessedData.RadiatorDir;

            //waterDir = new Vector3d(-waterDir.Y, waterDir.X, waterDir.Z);
            Vector3d offset = radiatorDir.GetNormal() * Parameter.WaterSeparatorDis;

            //做矩形，寻找最近点
            Line line0 = new Line(start - offset, start + offset);
            Line line1 = new Line(end - offset, end + offset);

            //List<Point3d> doorFirstList = line0.Intersect(regionObb, Intersect.OnBothOperands).ToList();
            //List<Point3d> doorSecondList = line1.Intersect(regionObb, Intersect.OnBothOperands).ToList();

            Point3dCollection pts0 = new Point3dCollection();
            Point3dCollection pts1 = new Point3dCollection();
            line0.IntersectWith(regionObb, Intersect.OnBothOperands, pts0, (IntPtr)0, (IntPtr)0);
            line1.IntersectWith(regionObb, Intersect.OnBothOperands, pts1, (IntPtr)0, (IntPtr)0);
            List<Point3d> doorFirstList = pts0.OfType<Point3d>().ToList();
            List<Point3d> doorSecondList = pts1.OfType<Point3d>().ToList();

            double dis1 = 0;
            double dis2 = 0;
            Point3d doorFirst = new Point3d();
            Point3d doorSecond = new Point3d();
            if (doorFirstList.Count > 0)
            {
                doorFirst = doorFirstList.FindByMin(x => x.DistanceTo(start));
                dis1 = doorFirst.DistanceTo(start);
                DrawUtils.ShowGeometry(doorFirst, "l1WaterPoint", 5, lineWeightNum: 30, 30, "C");
            }
            if (doorSecondList.Count > 0)
            {
                doorSecond = doorSecondList.FindByMin(x => x.DistanceTo(end));
                DrawUtils.ShowGeometry(doorSecond, "l1WaterPoint", 5, lineWeightNum: 30, 30, "C");
                dis2 = doorSecond.DistanceTo(end);
            }
            if (dis1 > dis2) //判断哪个距离远
            {
                doorLine = new Line(doorFirst, doorFirst + (end - start));
            }
            else
            {
                doorLine = new Line(doorSecond + (start - end), doorSecond);
            }
            Vector3d waterOffset = start - doorLine.StartPoint;
            ProcessedData.WaterOffset = waterOffset;

            Polyline differArea = PolylineProcessService.CreateRectangle2(SecondPoint, FirstPoint, 5000);
            DrawUtils.ShowGeometry(differArea, "l3RadiatorDiff", 3, lineWeightNum: 30);
            //Polyline differArea2 = PolylineProcessService.CreateRectangle2(doorLine.StartPoint, doorLine.EndPoint, 100);
            //ProcessedData.DifferArea = differArea2;


            DrawUtils.ShowGeometry(start, "l6starttest", 0);
            regionObb = regionObb.Difference(differArea).OfType<Polyline>().ToList().FindByMax(x => x.Area);
            PolylineProcessService.ClearPolyline(ref regionObb);


            //删除超出的边
            Polyline newPl = new Polyline();
            newPl.Closed = false;
            newPl.AddVertexAt(0, doorLine.StartPoint.ToPoint2D(), 0, 0, 0);
            newPl.AddVertexAt(1, doorLine.EndPoint.ToPoint2D(), 0, 0, 0);

            var pls = ThCADCoreNTSOperation.BufferFlatPL(newPl, 20).OfType<Polyline>().ToList();
            var pl = pls.OrderByDescending(x => x.Area).First();
            var plList = pl.Trim(regionObb).OfType<Polyline>().ToList();
            if (plList.Count > 0)
            {
                doorLine = plList.FindByMax(x => x.Length).ToLines().ToList().FindByMax(x => x.Length);
            }

            //doorLine = new Line(start, end);

            return doorLine;
        }


        public void CreateArea() 
        {
            Vector3d offset = FirstPoint - OriginalFirstPoint;
            Polyline addArea = PolylineProcessService.CreateRectangle2(OriginalFirstPoint,OriginalSecondPoint,offset.Length + 50);
            ProcessedData.RadiatorAddArea = addArea;
        }
    }
}
