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
    class FirstDoorProcess
    {
        //输入
        public Polyline RegionPl = new Polyline();
        public Point3d DoorLeft = new Point3d();
        public Point3d DoorRight = new Point3d();
        public Point3d WaterStart = new Point3d();
        public Point3d WaterEnd = new Point3d();

        //中间变量
        List<double> XList = new List<double>();

        //输出
        public double LeftCut = 0;
        public double RightCut = 0;

        public FirstDoorProcess(Polyline pl,Point3d  dl ,Point3d dr , Point3d ws, Point3d wd) 
        {
            Polyline newPl = pl.Clone() as Polyline;
            var point = newPl.GetPoint3dAt(0);
            PassageShowUtils.ShowPoint(point, 3);
            PassageShowUtils.ShowText(point, "center", 3);

            // 多段线旋转

            double angle = GetAngle(dl,dr);

            Matrix3d m = Matrix3d.Rotation(angle, Vector3d.ZAxis, point);
            newPl.TransformBy(m);
            RegionPl = newPl.Buffer(5).OfType<Polyline>().ToList().First();

            PassageShowUtils.ShowEntity(newPl);
            // 点的旋转
            DoorLeft = dl.TransformBy(m);
            DoorRight = dr.TransformBy(m);
            WaterStart = ws.TransformBy(m);
            WaterEnd = wd.TransformBy(m);
        }


        public void Pipeline() 
        {
            double maxY = WaterStart.Y > WaterEnd.Y ? WaterStart.Y : WaterEnd.Y;

            XList.Add(DoorLeft.X);
            XList.Add(DoorRight.X);

            List<Point3d> point3Ds = PassageWayUtils.GetPolyPoints(RegionPl);
            for (int i = 0; i < point3Ds.Count; i++) 
            {
                double x = point3Ds[i].X;
                if (x > DoorLeft.X  && x < DoorRight.X) 
                {
                    XList.Add(x);    
                }
            }
            
            XList = XList.OrderBy(x=>x).ToList();


            List<int> OKList = new List<int>();
            for (int i = 0; i < XList.Count; i++) 
            {
                Point3d testPt = new Point3d(XList[i],maxY,0);
                if (RegionPl.Contains(testPt))
                {
                    OKList.Add(1);
                }
                else 
                {
                    OKList.Add(0);
                }
            }


            double maxLength = 0;
            int indexLeft = -1;
            int indexRight = -1;

            for (int i = 0; i < XList.Count - 1; i++) 
            {
                if (OKList[i] == 1)
                {
                    int step = 0;

                    while (i + step < XList.Count - 1 && OKList[i + step] == 1) 
                    {
                        step = step + 1;
                    }

                    if (step > 0) 
                    {
                        double length = XList[i + step] - XList[i];
                        if (length > maxLength) 
                        {
                            maxLength = length;
                            indexLeft = i;
                            indexRight = i + step;
                        }
                    }
                }
            }

            if (maxLength > 50) 
            {
                LeftCut = XList[indexLeft] - XList[0];
                RightCut = XList.Last() - XList[indexRight];
            }
        }

        double GetAngle(Point3d dl, Point3d dr) 
        {
            Vector3d nowVec = dr - dl;
            Vector3d vec0 = new Vector3d(1, 0, 0);
            double angle = nowVec.GetAngleTo(vec0, Vector3d.ZAxis);

            //Vector3d vec1 = new Vector3d(0, 1, 0);
            //Vector3d vec2 = new Vector3d(-1, 0, 0);
            //Vector3d vec3 = new Vector3d(0, -1, 0);
            return angle;
        }
    }
}