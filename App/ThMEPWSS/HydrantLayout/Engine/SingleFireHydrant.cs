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


namespace ThMEPWSS.HydrantLayout.Engine
{
    class SingleFireHydrant
    {
        //全局变量（debug用的，这样就不用重新启动项目）
        public int Mode = Info.Mode;
        //外部输入
        ThHydrantModel model;
        Point3d CenterPoint;
        //形状信息
        double LongSide = 0;
        double ShortSide = 0;
        //double DoorOffset = 0.25* LongSide ;
        //double DoorSs = LongSide;
        //double DoorLs = LongSide*1.5;


        //是否找到合适的摆放位置
        bool Done = false;
        FireCompareModel BestLayOut;

        //使用的类
        SearchPoint searchPoint0;
        FeasibilityCheck feasibilityCheck0;
        IndexCompute indexCompute0;
        //可倚靠区域
        MPolygon LeanWall;

        //选优
        List<FireCompareModel> fireCompareModelsMix = new List<FireCompareModel>();

        public SingleFireHydrant(ThHydrantModel model)
        {
            this.model = model;
            CenterPoint = model.Center;
            //Type = type;
            CreateBoundaryService.FindLineOfRectangle(model.Outline, ref ShortSide, ref LongSide);

            //搜索实体周边环境
            SearchRangeFrame searchRangeFrame0 = new SearchRangeFrame(CenterPoint);
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
            indexCompute0 = new IndexCompute(CenterPoint);


            //正式开始搜索、测试
            List<Point3d> basePointList;
            List<Vector3d> dirList;

            //寻找第一优先级定位点并测试摆放
            searchPoint0.FindTurningPoint(out basePointList, out dirList);
            FirstPriorityTest(basePointList, dirList);

            //寻找第二优先级定位点并测试摆放
            if(Done == false) 
            {
                searchPoint0.FindColumnPoint(out basePointList, out dirList);
                SecondPriorityTest(basePointList, dirList);
                searchPoint0.FindOtherPoint(out basePointList, out dirList);
                ThirdPriorityTest(basePointList, dirList);
                FindBest();
            }

            ////寻找第三优先级定位点并测试摆放
            //if (Done == false)
            //{
            //    searchPoint0.FindOtherPoint(out basePointList, out dirList);
            //    ThirdPriorityTest(basePointList, dirList);
            //}

            OutPutSingleModel();
        }

        //寻找第一优先级定位点并测试摆放
        public void FirstPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], ShortSide, LongSide, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                if (i == 0)
                {
                    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                }
                //int besttIndex = -1;
                //int bestdoorIndex = -1;
                //int maxFit = -1;
                int start = -1;
                int end = -1;

                switch (Mode)
                {
                    case 2: { start = 0; end = 9; break; }
                    case 1: { start = 0; end = 5; break; }
                    case 0: { start = 6; end = 9; break; }
                }

                for (int j = start; j <= end; j++)
                {
                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        List<Polyline> doorAreaList = fireHydrant0.GetDoorAreaObbList(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));
                        for (int k = 0; k < doorAreaList.Count; k++)
                        {
                            if (!Info.AllowDoorInPaking) 
                            {
                                if (!FeasibilityCheck.IsFireFeasible(doorAreaList[k], LeanWall.Shell())) 
                                {
                                    continue;
                                }
                            }

                            if (FeasibilityCheck.IsDoorFeasible(doorAreaList[k], LeanWall.Shell())) //如果门没有被阻挡,找到一个可以摆放的模型
                            {
                                double distance = basePointList[i].DistanceTo(CenterPoint);

                                double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, LeanWall.Shell());
                                double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], LeanWall.Shell());
                                int againstWallLength = (int)(againstWallLength0 + againstWallLength1)/100;

                                Point3d fireCenter = new Point3d();
                                Vector3d fireDir = new Vector3d();
                                fireHydrant0.SetModel(j, k, out fireCenter, out fireDir);
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], LeanWall.Shell(), ProcessedData.ParkingIndex);
                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLength, j, doorGood);
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
            fireCompareModels0 = fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();
            if (fireCompareModels0.Count > 0)
            {
                Done = true;
                FireCompareModel fireCompareModelbest = fireCompareModels0[0];
                BestLayOut = fireCompareModelbest;

                Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, ShortSide, LongSide, fireCompareModelbest.fireDir);
                //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
                DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
                fireCompareModelbest.Draw(ShortSide,LongSide);
            }
        }

        //寻找第二优先级定位点并测试摆放
        public void SecondPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], ShortSide, LongSide, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                //if (i == 0)
                //{
                //    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                //}
                int start = -1;
                int end = -1;

                switch (Mode)
                {
                    case 2: { start = 0; end = 9; break; }
                    case 1: { start = 0; end = 5; break; }
                    case 0: { start = 8; end = 9; break; }
                }

                for (int j = start; j <= end; j++)
                {
                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        List<Polyline> doorAreaList = fireHydrant0.GetDoorAreaObbList(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));

                        double distance = fireHydrant0.TFireCenterPointList[j].DistanceTo(CenterPoint);
                        double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, searchPoint0.LeanWallList[i]);
                        double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], searchPoint0.LeanWallList[i]);
                        int againstWallLength = (int)(againstWallLength0 + againstWallLength1) / 100;
                        //double againstWallLength = 200;
                        againstWallLength = againstWallLength + (int)((3000 - distance) / 100 * Info.DistanceWeight);


                        for (int k = 0; k < doorAreaList.Count; k++)
                        {
                            if (!Info.AllowDoorInPaking)
                            {
                                if (!FeasibilityCheck.IsFireFeasible(doorAreaList[k], LeanWall.Shell()))
                                {
                                    continue;
                                }
                            }

                            if (FeasibilityCheck.IsDoorFeasible(doorAreaList[k], LeanWall.Shell())) //如果门没有被阻挡,找到一个可以摆放的模型
                            {    
                                Point3d fireCenter = new Point3d();
                                Vector3d fireDir = new Vector3d();
                                fireHydrant0.SetModel(j, k, out fireCenter, out fireDir);
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], LeanWall.Shell(),ProcessedData.ParkingIndex);
                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLength, j, doorGood);
                                fireCompareModelsMix.Add(fireCompareModeltmp);
                            }
                        }
                    }
                }
            }
            //test

            //寻找最优
            ////fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance);
            //fireCompareModels0 = fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ThenBy(x => x.doorGood).ToList();
            //if (fireCompareModels0.Count > 0)
            //{
            //    Done = true;
            //    FireCompareModel fireCompareModelbest = fireCompareModels0[0];
            //    Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, Info.ShortSide, Info.LongSide, fireCompareModelbest.fireDir);
            //    //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
            //    DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
            //    fireCompareModelbest.Draw();
            //}
        }

        //寻找第三优先级定位点并测试摆放
        public void ThirdPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], ShortSide, LongSide, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                if (i == 0)
                {
                    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                }
                int start = -1;
                int end = -1;

                switch (Mode)
                {
                    case 2: { start = 0; end = 9; break; }
                    case 1: { start = 0; end = 5; break; }
                    case 0: { start = 6; end = 9; break; }
                }

                for (int j = start; j <= end; j++)
                {
                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        List<Polyline> doorAreaList = fireHydrant0.GetDoorAreaObbList(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));
                        for (int k = 0; k < doorAreaList.Count; k++)
                        {
                            if (!Info.AllowDoorInPaking)
                            {
                                if (!FeasibilityCheck.IsFireFeasible(doorAreaList[k], LeanWall.Shell()))
                                {
                                    continue;
                                }
                            }

                            if (FeasibilityCheck.IsDoorFeasible(doorAreaList[k], LeanWall.Shell())) //如果门没有被阻挡,找到一个可以摆放的模型
                            {
                                double distance = basePointList[i].DistanceTo(CenterPoint);

                                double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, LeanWall.Shell());
                                double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], LeanWall.Shell());
                                int againstWallLength = (int)(againstWallLength0 + againstWallLength1) / 100;

                                againstWallLength = againstWallLength + (int)((3000 - distance) / 100 * Info.DistanceWeight);

                                Point3d fireCenter = new Point3d();
                                Vector3d fireDir = new Vector3d();
                                fireHydrant0.SetModel(j, k, out fireCenter, out fireDir);
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], LeanWall.Shell(), ProcessedData.ParkingIndex);
                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLength, j, doorGood);
                                fireCompareModelsMix.Add(fireCompareModeltmp);
                                break;
                            }
                        }
                    }
                }
            }
            //test

            //寻找最优
            ////fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance);

            //fireCompareModels0 = fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();
            //if (fireCompareModels0.Count > 0)
            //{
            //    Done = true;
            //    FireCompareModel fireCompareModelbest = fireCompareModels0[0];
            //    Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, Info.ShortSide, Info.LongSide, fireCompareModelbest.fireDir);
            //    
            //    DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
            //    fireCompareModelbest.Draw();
            //}
        }

        void FindBest() 
        {            
            fireCompareModelsMix= fireCompareModelsMix.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();
            if (fireCompareModelsMix.Count > 0)
            {
                Done = true;
                FireCompareModel fireCompareModelbest = fireCompareModelsMix[0];
                BestLayOut = fireCompareModelbest;

                Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, ShortSide, LongSide, fireCompareModelbest.fireDir);
                //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
                DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
                fireCompareModelbest.Draw(ShortSide,LongSide);
            }
        }

        public List<OutPutModel> OutPutSingleModel() 
        {
            List<OutPutModel> outPutModels = new List<OutPutModel>();

            OutPutModel vp = new OutPutModel();
            OutPutModel hydrant = new OutPutModel();

            if (Done)
            {
                Point3d centerpoint = BestLayOut.basePoint + BestLayOut.dir * Info.VPSide/2;
                vp = new OutPutModel(true, 0, centerpoint, BestLayOut.dir, BestLayOut.doorOpenDir, model);

                hydrant = new OutPutModel(true, 1, BestLayOut.fireCenterPoint, BestLayOut.fireDir, BestLayOut.doorOpenDir, model);
            }
            else 
            {
                vp.Type = 0;
                vp.OriginModel = model;
                hydrant.Type = 1;
                hydrant.OriginModel = model;
            }

            outPutModels.Add(vp);
            outPutModels.Add(hydrant);
            return outPutModels;
        }
    }
}
