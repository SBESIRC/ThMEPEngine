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
using ThMEPWSS.HydrantLayout.tmp.Model;
using ThMEPWSS.HydrantLayout.tmp.Engine;
using ThMEPWSS.HydrantLayout.tmp.Service;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Data;

namespace ThMEPWSS.HydrantLayout.tmp.Engine
{
    class SingleFireHydrant
    {
        //外部输入
        Point3d CenterPoint;
        int Type;
        //是否找到合适的摆放位置
        bool done = false;
        FireHydrant BestLayOut;

        //使用的类
        SearchPoint searchPoint0;
        FeasibilityCheck feasibilityCheck0;
        IndexCompute indexCompute0;
        //可倚靠区域
        MPolygon LeanWall;


        public SingleFireHydrant(Point3d center, int type)
        {
            CenterPoint = center;
            Type = type;

            //搜索实体周边环境
            SearchRangeFrame searchRangeFrame0 = new SearchRangeFrame(center);
            if (searchRangeFrame0.IfFind)
            {
                LeanWall = searchRangeFrame0.output();
            }
            else
            {
                return;
            }

            //建立寻找定位点的类
            searchPoint0 = new SearchPoint(LeanWall, CenterPoint);

            //建立用于测试的类
            feasibilityCheck0 = new FeasibilityCheck();

            //建立用于指标判定的类
            indexCompute0 = new IndexCompute(center);


            //正式开始搜索、测试
            List<Point3d> basePointList;
            List<Vector3d> dirList;

            //寻找第一优先级定位点并测试摆放
            searchPoint0.FindTurningPoint(out basePointList, out dirList);
            FirstPriorityTest(basePointList, dirList);

            //寻找第二优先级定位点并测试摆放
            //SecondPriorityTest();

            //寻找第三优先级定位点并测试摆放
            //ThirdPriorityTest();
        }

        //寻找第一优先级定位点并测试摆放
        public void FirstPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count - 1; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], Type, Info.Mode);
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                if (i == 1)
                {
                    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                }
                //int besttIndex = -1;
                //int bestdoorIndex = -1;
                //int maxFit = -1;
                int start = -1;
                int end = -1;

                switch (Info.Mode)
                {
                    case 2: { start = 0; end = 8; break; }
                    case 1: { start = 0; end = 5; break; }
                    case 0: { start = 6; end = 8; break; }
                }

                for (int j = start; j < end; j++)
                {
                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        List<Polyline> doorAreaList = fireHydrant0.GetDoorAreaObbList(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));
                        for (int k = 0; k < doorAreaList.Count; k++)
                        {
                            if (FeasibilityCheck.IsDoorFeasible(doorAreaList[k], LeanWall.Shell())) //如果门没有被阻挡,找到一个可以摆放的模型
                            {
                                double distance = basePointList[i].DistanceTo(CenterPoint);
                                double againstWallLength = indexCompute0.CalculateWallLength(fireObbList[j], LeanWall.Shell());
                                Point3d fireCenter = new Point3d();
                                Vector3d fireDir = new Vector3d();
                                fireHydrant0.SetModel(j, k, out fireCenter, out fireDir);
                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLength, j);
                                fireCompareModels0.Add(fireCompareModeltmp);
                                break;
                            }
                        }
                    }
                }
            }
            //test

            //寻找最优
            //fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance);
            fireCompareModels0 = fireCompareModels0.OrderByDescending(x => x.againstWallLength).ToList();
            FireCompareModel fireCompareModelbest = fireCompareModels0[0];
            Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, Info.ShortSide, Info.LongSide, fireCompareModelbest.fireDir);
            //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
            DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
        }

        //寻找第二优先级定位点并测试摆放
        public void SecondPriorityTest()
        {


        }


        //寻找第三优先级定位点并测试摆放
        public void ThirdPriorityTest()
        {


        }

        //寻找第三优先级定位点
        public void FindThirdPriorityPt()
        {


        }

    }

    class FireCompareModel
    {
        //属性
        public Point3d basePoint;
        public Vector3d dir;
        public Point3d fireCenterPoint;
        public Vector3d fireDir;
        public int doorOpenDir = -1;
        public int tIndex = -1;
        //指标
        public double distance = 100000;
        public double againstWallLength = 0;

        public FireCompareModel(Point3d basePoint, Vector3d dir, Point3d fireCenterPoint, Vector3d fireDir, int doorOpenDir, double distance, double againstWallLength, int tIndex)
        {
            this.tIndex = tIndex;
            this.basePoint = basePoint;
            this.dir = dir;
            this.fireCenterPoint = fireCenterPoint;
            this.fireDir = fireDir;
            this.doorOpenDir = doorOpenDir;
            this.distance = distance;
            this.againstWallLength = againstWallLength;
        }
    }
}
