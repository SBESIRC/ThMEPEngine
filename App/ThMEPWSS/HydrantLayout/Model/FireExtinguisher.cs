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
    class FireExtinguisher
    {
        //外部传入的模式属性
        //public int Type = 0; //表示是 消防栓（0） 还是 灭火器（1）
        public int Mode = 0; //表示需要调用的模板列表 2(自由) 1（L） 0（I）

        //外部传入的位置属性
        public Point3d BasePoint = new Point3d(-100, -100, -100);
        public Vector3d vBasePoint = new Vector3d(0, 1, 0);
        double ShortSide = 0;
        double LongSide = 0; 

        //距离原点移动距离
        public double Distance = -100;


        //以下属性为计算得出

        //当前选择的模板
        int nowIndex = -1;

        //实体中心点
        public Point3d cPointFire = new Point3d(-200, -200, -200);

        //外包框线
        public Polyline FrieObb = new Polyline();
        public Polyline DoorAreaObb = new Polyline();

        //模板相关属性
        public List<Point3d> TFireCenterPointList = new List<Point3d>();
        public List<Vector3d> TFireDirList = new List<Vector3d>();
        public List<Polyline> TFireObb = new List<Polyline>();
        Vector3d clockwise90;
        Vector3d clockwise270;

        //简写
        double vp = Info.VPSide;


        //构造函数
        public FireExtinguisher(Point3d basePoint, Vector3d dir, double shortside, double longside, int mode)
        {
            BasePoint = basePoint;
            vBasePoint = dir;
            ShortSide = shortside;
            LongSide = longside;
            Mode = mode;
            clockwise90 = new Vector3d(dir.Y, -dir.X, dir.Z).GetNormal();
            clockwise270 = new Vector3d(-dir.Y, dir.X, dir.Z).GetNormal();

            GetFireAttribute();
        }

        //计算消火栓外包框线
        private void GetFireAttribute()
        {
            Point3d tmppt0;
            tmppt0 = BasePoint + clockwise270 * 0.5 * (LongSide-ShortSide) + vBasePoint * (0.5 * ShortSide);
            //0 横左偏
            TFireCenterPointList.Add(tmppt0);
            TFireDirList.Add(vBasePoint);
            //1 横右偏
            TFireCenterPointList.Add(BasePoint + clockwise90 * 0.5 * (LongSide - ShortSide) + vBasePoint * (0.5 * ShortSide));
            TFireDirList.Add(vBasePoint);
            //2  竖直左
            TFireCenterPointList.Add(BasePoint + vBasePoint * (0.5 * LongSide));
            TFireDirList.Add(clockwise270);
            //3  竖直右
            TFireCenterPointList.Add(BasePoint + vBasePoint * (0.5 * LongSide));
            TFireDirList.Add(clockwise90);
            //4  横中间
            TFireCenterPointList.Add(BasePoint + vBasePoint * 0.5 * ShortSide);
            TFireDirList.Add(vBasePoint);
        }

        //获取消火栓外包框线列表
        public List<Polyline> GetFireObbList()
        {
            //直接输出全体
            for (int i = 0; i < 5; i++)
            {
                TFireObb.Add(CreateBoundaryService.CreateBoundary(TFireCenterPointList[i],ShortSide, LongSide, TFireDirList[i]));
            }
            return TFireObb;
        }
        
        //获取一种消火栓外包框线
        public void GetFireObb(int tIndex) { }

        //计算开门范围外包框线
        public Polyline GetDoorAreaObb(int tIndex)
        {
            Point3d DoorCenter = TFireCenterPointList[tIndex] + TFireDirList[tIndex] * (Info.ExDoorSide + ShortSide) / 2;
            Polyline plOut = CreateBoundaryService.CreateBoundary(DoorCenter, ShortSide, LongSide, TFireDirList[tIndex]);
            return plOut;
        }

        //固定此消防栓
        public void SetModel(int tIndex, out Point3d fireCenterPoint, out Vector3d fireDir)
        {
            fireCenterPoint = TFireCenterPointList[tIndex];
            fireDir = TFireDirList[tIndex];
        }
    }
}
