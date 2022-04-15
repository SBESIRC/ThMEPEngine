using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using Linq2Acad;
using ThMEPEngineCore.Diagnostics;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model.Hvac;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;



namespace ThMEPWSS.HydrantLayout.Model
{
    class SecondaryModel
    {

    }

    //比较模型，用于比较可摆放的模型中哪一种最优
    class FireCompareModel
    {
        //基本属性
        public Point3d basePoint;
        public Vector3d dir;
        public Point3d fireCenterPoint;
        public Vector3d fireDir;
        public int doorOpenDir = -1;

        public int tIndex = -1;
        //指标
        public double distance = 100000;
        public double againstWallLength = 0;
        public int doorGood = 0;

        //绘图属性
        public double ShortSide = Info.ShortSide;
        public double LongSide = Info.LongSide;

        public FireCompareModel(Point3d basePoint, Vector3d dir, Point3d fireCenterPoint, Vector3d fireDir, int doorOpenDir, double distance, double againstWallLength, int tIndex, bool doorGood)
        {
            this.tIndex = tIndex;
            this.basePoint = basePoint;
            this.dir = dir;
            this.fireCenterPoint = fireCenterPoint;
            this.fireDir = fireDir;
            this.doorOpenDir = doorOpenDir;
            this.distance = distance;
            this.againstWallLength = againstWallLength;
            if (doorGood)
            {
                this.doorGood = 1;
            }
        }

        //画消火栓
        public void Draw(double shortside,double longside)
        {
            double vp = Info.VPSide;
            double ss = shortside;
            double ls = longside;
            double dss = ls;
            double dls = 1.5*ls;
            double doorOffset = 0.25*ls;
            var fclockwise90 = new Vector3d(fireDir.Y, -fireDir.X, fireDir.Z).GetNormal();
            var fclockwise270 = new Vector3d(-fireDir.Y, fireDir.X, fireDir.Z).GetNormal();

            DBObjectCollection objs = new DBObjectCollection();
            Polyline vpPl = CreateBoundaryService.CreateBoundary(basePoint + 0.5 * vp * dir, vp, vp, dir);
            Polyline firePl = CreateBoundaryService.CreateBoundary(fireCenterPoint, ss, ls, fireDir);
            Polyline doorPl;
            if (doorOpenDir % 2 == 0)
            {
                Point3d doorBasePoint = fireCenterPoint + fireDir * 0.5 * (ss + dss) + fclockwise270 * doorOffset;
                doorPl = CreateBoundaryService.CreateBoundary(doorBasePoint, dss, dls, fireDir);
            }
            else
            {
                Point3d doorBasePoint = fireCenterPoint + fireDir * 0.5 * (ss + dss) + fclockwise90 * doorOffset;
                doorPl = CreateBoundaryService.CreateBoundary(doorBasePoint, dss, dls, fireDir);
            }
            objs.Add(vpPl);
            objs.Add(firePl);
            objs.Add(doorPl);
            objs.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1result", 2));
        }

        //画灭火器
        public void Draw2(double shortside, double longside) 
        {
            DBObjectCollection objs = new DBObjectCollection();
            Polyline firePl = CreateBoundaryService.CreateBoundary(fireCenterPoint, shortside, longside, fireDir);
            objs.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1result", 2));
        }
    }
}
