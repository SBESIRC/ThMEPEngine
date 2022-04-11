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

namespace ThMEPWSS.HydrantLayout.Model
{

    class FireHydrant
    {
        //外部传入的模式属性
        //public int Type = 0; //表示是 消防栓（0） 还是 灭火器（1）
        public int Mode = 0; //表示需要调用的模板列表 2(自由) 1（L） 0（I）

        //外部传入的位置属性
        public Point3d OriginalPoint = new Point3d(-100, -100, -100); //原位置
        //距离原点移动距离
        public double Distance = -100;
        

        //以下属性为计算得出

        //立柱的定位点和定位方向
        public Point3d BasePoint = new Point3d(-100, -100, -100);
        public Vector3d vBasePoint = new Vector3d(0,1,0);
        double ShortSide = 0;
        double LongSide = 0;

        //实体中心点
        public Point3d cPointFire = new Point3d(-200, -200, -200);
        public Point3d cPointRiser = new Point3d(-200, -200, -200);
        //实体朝向
        public Vector3d vRiser = new Vector3d(0, 1, 0);
        public Vector3d vFireHydrant = new Vector3d(0, 1, 0);
        public int vDoor = 0;  //门 顺时针开（0） 逆时针开（1）

        //外包框线
        public Polyline FrieObb = new Polyline();
        public Polyline RiserObb = new Polyline();
        public Polyline WholeObb = new Polyline();
        public Polyline DoorAreaObb = new Polyline();

        //模板相关属性
        public List<Point3d>  TFireCenterPointList = new List<Point3d>();
        public List<Vector3d> TFireDirList  =  new List<Vector3d>();
        List<Polyline> TFireObb = new List<Polyline>();
        Vector3d clockwise90;
        Vector3d clockwise270;

        //简写
        double vp = Info.VPSide;
        double ss = 0;
        double ls = 0;
        double dss = 0;
        double dls = 0;
        double doorOffset = 0;


        //构造函数
        public FireHydrant(Point3d basePoint, Vector3d dir, double shortside, double longside, int mode)
        {
            BasePoint = basePoint;
            vBasePoint = dir;
            ShortSide = shortside;
            LongSide = longside;
            Mode = mode;
            clockwise90 = new Vector3d(dir.Y, -dir.X, dir.Z).GetNormal();
            clockwise270 = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();


            //数据更新
            SideDataGeneration();

            //构建模型
            GetFireAttribute();
        }

        //计算所需数据
        private void SideDataGeneration() 
        {
             ss = ShortSide;
             ls = LongSide;
             dss = LongSide;
             dls = LongSide*1.5;
             doorOffset = LongSide*0.25;
        }

        //计算消火栓外包框线
        private void GetFireAttribute()
        {
            Point3d tmppt0;
            tmppt0 = BasePoint + clockwise270 * 0.5 * (vp + ss) - vBasePoint * (0.5 * ls - vp);
            //1
            TFireCenterPointList.Add(tmppt0);
            TFireDirList.Add(clockwise270);
            //2
            TFireCenterPointList.Add(BasePoint + clockwise270 * 0.5 * (vp + ss) + vBasePoint * (0.5 * ls));
            TFireDirList.Add(clockwise270);
            //3
            TFireCenterPointList.Add(BasePoint + clockwise270 * (0.5 * ls - 0.5 * vp) + vBasePoint * (vp + 0.5 * ss));
            TFireDirList.Add(vBasePoint);
            //4
            TFireCenterPointList.Add(BasePoint + clockwise90 * (0.5 * ls - 0.5 * vp) + vBasePoint * (vp + 0.5 * ss));
            TFireDirList.Add(vBasePoint);
            //5
            TFireCenterPointList.Add(BasePoint + clockwise90 * 0.5 * (vp + ss) + vBasePoint * (0.5 * ls));
            TFireDirList.Add(clockwise90);
            //6
            TFireCenterPointList.Add(BasePoint + clockwise90 * 0.5 * (vp + ss) - vBasePoint * (0.5 * ls - vp));
            TFireDirList.Add(clockwise90);
            //7
            TFireCenterPointList.Add(BasePoint + clockwise270 * 0.5 * (vp + ls) + vBasePoint * 0.5* ss);
            TFireDirList.Add(vBasePoint);
            //8
            TFireCenterPointList.Add(BasePoint + clockwise90 * 0.5 * (vp + ls) + vBasePoint * 0.5 * ss);
            TFireDirList.Add(vBasePoint);
            //9
            TFireCenterPointList.Add(BasePoint + clockwise270 * 0.5 * (ss - vp) + vBasePoint * (vp + 0.5 * ls) );
            TFireDirList.Add(clockwise270);
            //10
            TFireCenterPointList.Add(BasePoint + clockwise90 * 0.5 * (ss - vp) +  vBasePoint * (vp + 0.5 * ls));
            TFireDirList.Add(clockwise90);
        }

        //计算立柱外包框线
        public Polyline GetRiserObb() 
        {
           return CreateBoundaryService.CreateBoundary(BasePoint + 0.5 * vp * vBasePoint, vp, vp, vBasePoint);
        }

        //获取消火栓外包框线列表
        public List<Polyline> GetFireObbList()
        {
            ////L型
            //if (mode != 0) //表示不舍弃L型
            //{
            //    for (int i = 0; i < 6; i++) 
            //    {
            //        TFireObb.Add(CreateBoundaryService.CreateBoundary(TFireCenterPoint[i], Info.ShortSide, Info.LongSide, TFireDir[i]));
            //    }
            //}

            ////I型
            //if (mode != 1) //表示不舍弃I型
            //{
            //    for (int i = 6; i < 9; i++)
            //    {
            //        TFireObb.Add(CreateBoundaryService.CreateBoundary(TFireCenterPoint[i], Info.ShortSide, Info.LongSide, TFireDir[i]));
            //    }
            //}

            //直接输出全体
            for (int i = 0; i < 10; i++)
            {
                TFireObb.Add(CreateBoundaryService.CreateBoundary(TFireCenterPointList[i], ShortSide, LongSide, TFireDirList[i]));
            }

            return TFireObb;
        }

        //获取一种消火栓外包框线
        public void GetFireObb(int tIndex){ }

        //计算开门范围外包框线   0：左开   1：右开     
        public Polyline GetDoorAreaObb(int tIndex , int dtIndex)
        {
            Point3d fireCenterPoint = TFireCenterPointList[tIndex];
            Vector3d dir = TFireDirList[tIndex];
            var fclockwise90 = new Vector3d(dir.Y, -dir.X, dir.Z).GetNormal();
            var fclockwise270 = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();
            Polyline plOut = new Polyline();
            switch (dtIndex) 
            {
                case 0:
                    {
                        Point3d doorBasePoint = fireCenterPoint + dir * 0.5 * (ss + dss) + fclockwise270 * doorOffset;
                        plOut = CreateBoundaryService.CreateDoor(doorBasePoint, dss, dls, dir, 0);
                        break;
                    }
                case 1:
                    {
                        Point3d doorBasePoint = fireCenterPoint + dir * 0.5 * (ss + dss) + fclockwise90 * doorOffset;
                        plOut = CreateBoundaryService.CreateDoor(doorBasePoint, dss, dls, dir, 1);
                        break;
                    }
                case 2:
                    {
                        Point3d doorBasePoint = fireCenterPoint - dir * 0.5 * (ss + dss) + fclockwise90 * doorOffset;
                        plOut = CreateBoundaryService.CreateDoor(doorBasePoint, dss, dls, dir, 0);
                        break;
                    }
                case 3:
                    {
                        Point3d doorBasePoint = fireCenterPoint - dir * 0.5 * (ss + dss) + fclockwise270 * doorOffset;
                        plOut = CreateBoundaryService.CreateDoor(doorBasePoint, dss, dls, dir, 1);
                        break;
                    }
            }
            return plOut;
        }

        //获取开门范围外包框线列表
        public List<Polyline> GetDoorAreaObbList(int tIndex) 
        {
            int end = -1;
            List<Polyline> plList = new List<Polyline>();
            if (tIndex <= 10)
            {
                end = 2;
            }
            //else if (tIndex > 5 && tIndex < 10)
            //{
            //    end = 4;
            //}
            for (int i = 0; i < end; i++)
            {
                plList.Add(GetDoorAreaObb(tIndex, i));
            }
            return plList;
        }

        //固定此消防栓
        public void SetModel(int tIndex,int dtIndex, out Point3d fireCenterPoint,out Vector3d fireDir)
        {
            fireCenterPoint = TFireCenterPointList[tIndex];
            fireDir = TFireDirList[tIndex];
            if (dtIndex > 1)
            {
                fireDir = -TFireDirList[tIndex];
            }
        }
    }
}
