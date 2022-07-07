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
        Point3d CenterPoint;    //中心点
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

        //占车位面积
        Dictionary<FireCompareModel, double> overlappedAreaDoor = new Dictionary<FireCompareModel, double>();

        //混排(二、三优先级混排)情况下，可摆放的情况的列表
        List<FireCompareModel> fireCompareModelsMix1 = new List<FireCompareModel>();
        List<FireCompareModel> fireCompareModelsMix2 = new List<FireCompareModel>();

        public SingleFireHydrant(ThHydrantModel model)
        {
            this.model = model;
            this.CenterPoint = model.Center;
            //Type = type;

            //读取边长
            CreateBoundaryService.FindLineOfRectangle(model.Outline, ref ShortSide, ref LongSide);
            TMPDATA.TmpVPSideLength = LongSide;
        }

        public void Pipeline() 
        {
            //搜索实体周边环境
            SearchRangeFrame searchRangeFrame0 = new SearchRangeFrame(CenterPoint);
            searchRangeFrame0.Pipeline();

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
            searchPoint0.Pipeline();

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
            if (Done == false)
            {
                //测试第三优先级是否存在点位
                List<Point3d> basePointTest3 = new List<Point3d>();
                List<Vector3d> dirListTest3 = new List<Vector3d>();
                searchPoint0.FindOtherPoint(out basePointList, out dirList);

                //只存在柱子
                if (basePointTest3.Count == 0)
                {
                    searchPoint0.FindColumnPointOnly(out basePointList, out dirList);
                    SecondPriorityTest(basePointList, dirList);
                }
                else 
                {
                    searchPoint0.FindColumnPoint(out basePointList, out dirList);
                    SecondPriorityTest(basePointList, dirList);
                    //searchPoint0.FindOtherPoint(out basePointList, out dirList);
                    ThirdPriorityTest(basePointList, dirList);
                    FindBest();
                }
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
        private void FirstPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], ShortSide, LongSide, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                Polyline AgainstTest = searchPoint0.LeanWallList[i];

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
                    case 1: { start = 0; end = 9; break; }
                    case 0: { start = 0; end = 9; break; }
                }

                for (int j = start; j <= end; j++)
                {
                    //if (j == 6 || j == 7) continue;
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
                                //double distance = basePointList[i].DistanceTo(CenterPoint);
                                double distance = fireHydrant0.TFireCenterPointList[j].DistanceTo(CenterPoint);
                                Line tmpLine = new Line(CenterPoint, fireHydrant0.TFireCenterPointList[j]);
                                if (tmpLine.IsIntersects(LeanWall.Shell())) distance = distance + 1000;

                                double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, AgainstTest);
                                double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], AgainstTest);
                                int againstWallLength = (int)(againstWallLength0 + againstWallLength1)/100;
                                //againstWallLength = againstWallLength + (int)((3000 - distance) / 100 * Info.DistanceWeight/2);

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
        private void SecondPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
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
                    if (j != 0 && j != 5 && j!= 8 && j!=9) continue;

                    if (searchPoint0.BasePointPosition[i] == 0 && j == 8) continue;
                    if (searchPoint0.BasePointPosition[i] == 2 && j == 9) continue;

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
                        if (distance == 0) 
                        { distance = 0; }
                        if (searchPoint0.BasePointPosition[i] == 1) { againstWallLength = againstWallLength - 2; }


                        for (int k = 0; k < doorAreaList.Count; k++)
                        {
                            // 固定开门方向
                            if ((k == 0) &&(searchPoint0.BasePointPosition[i] == 2)) continue;
                            if ((k == 1) &&(searchPoint0.BasePointPosition[i] == 0)) continue;

                            //是否允许在车位上开门
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
                                double againstWallLengthB = againstWallLength;

                                //检验开门遮挡
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], LeanWall.Shell(),ProcessedData.ParkingIndex);
                                if (doorGood) againstWallLengthB = againstWallLengthB + 2;

                                double overlappedArea = 0;
                                if (!doorGood) overlappedArea = IndexCompute.ComputeOverlapArea(doorAreaList[k], LeanWall.Shell(), ProcessedData.ParkingIndex);
                                
                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLengthB, j, doorGood);

                                overlappedAreaDoor.Add(fireCompareModeltmp, overlappedArea);
                                fireCompareModelsMix1.Add(fireCompareModeltmp);
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

        private void SecondPriorityTestOnlyColumn(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                //判断能否放中心
                if (searchPoint0.BasePointPosition[i] == 1 && !Info.ColumnCenterOK) continue;

                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], ShortSide, LongSide, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                //if (i == 0)
                //{
                //    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                //}
                List<int> feasibleTypeList = new List<int>();

                switch (Mode)
                {
                    case 2: { feasibleTypeList = new List<int> { 0,1,4,5,6,7,8,9 }; break; }
                    case 1: { feasibleTypeList = new List<int> { 0,1,4,5,6,7}; break; }
                    case 0: { feasibleTypeList = new List<int> { 6, 7, 8, 9 }; break; }
                }

                for (int a = 0; a < feasibleTypeList.Count; a++)
                {
                    int j = feasibleTypeList[a];

                    ////大量逻辑
                    //如果在柱子两侧，则不能外开;
                    if (searchPoint0.BasePointPosition[i] == 0 && j == 8) continue;
                    if (searchPoint0.BasePointPosition[i] == 2 && j == 9) continue;

                    //部分情况只能放0，5
                    if (searchPoint0.BasePointPosition[i] == -1 && j !=  5) continue;
                    if (searchPoint0.BasePointPosition[i] == 3 && j !=  0) continue;

                    //6，7只有部分情况能放
                    if (searchPoint0.BasePointPosition[i] != 19 && j == 7) continue;
                    if (searchPoint0.BasePointPosition[i] != 20 && j == 6) continue;

                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        List<Polyline> doorAreaList = fireHydrant0.GetDoorAreaObbList(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));

                        //确定位置优先级的逻辑
                        double positionScore = -1;
                        if (j == 6 || j == 7) positionScore = 3;
                        if (j == 1 || j == 4) 
                        {
                            if (searchPoint0.ColumnDirMode[i] == 1) { positionScore = 0; }
                            else positionScore = 2;
                        }
                        if (j == 0 || j == 5) positionScore = 1;
                        if (j == 8 || j == 9) positionScore = 0;

                        double distance = fireHydrant0.TFireCenterPointList[j].DistanceTo(CenterPoint);
                        double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, searchPoint0.LeanWallList[i]);
                        double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], searchPoint0.LeanWallList[i]);
                        int againstWallLength = (int)(againstWallLength0 + againstWallLength1) / 100;
                        //double againstWallLength = 200;
                        againstWallLength = againstWallLength + (int)((3000 - distance) / 100 * Info.DistanceWeight);
                        
                        //if (distance == 0) { distance = 0; }
                        //if (searchPoint0.BasePointPosition[i] == 1) { againstWallLength = againstWallLength - 2; }

                        for (int k = 0; k < doorAreaList.Count; k++)
                        {
                            // 固定开门方向
                            if ((k == 0) && (searchPoint0.BasePointPosition[i] == 2)) continue;
                            if ((k == 1) && (searchPoint0.BasePointPosition[i] == 0)) continue;

                            //是否允许在车位上开门
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
                                double againstWallLengthB = againstWallLength;

                                //检验开门遮挡
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], LeanWall.Shell(), ProcessedData.ParkingIndex);
                                //if (doorGood) againstWallLengthB = againstWallLengthB + 2;

                                double doorScore = 0;
                                //double overlappedArea = 0;
                                if (doorGood) doorScore = 100;
                                else
                                {
                                    doorScore = IndexCompute.ComputeOverlapScore(doorAreaList[k], LeanWall.Shell(), ProcessedData.ParkingIndex);
                                }

                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLengthB, j, doorGood);
                                fireCompareModeltmp.PositionScore = positionScore;
                                fireCompareModeltmp.DoorScore = doorScore;

                                //overlappedAreaDoor.Add(fireCompareModeltmp, overlappedArea);
                                fireCompareModels0.Add(fireCompareModeltmp);
                            }
                        }
                    }
                }
            }

            //寻找最优
            //fireCompareModels0.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance);
            fireCompareModels0 = fireCompareModels0.OrderByDescending(x => x.PositionScore).ThenBy(x => x.DoorScore).ThenBy(x => x.againstWallLength).ToList();
            if (fireCompareModels0.Count > 0)
            {
                Done = true;
                FireCompareModel fireCompareModelbest = fireCompareModels0[0];
                Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, ShortSide, LongSide, fireCompareModelbest.fireDir);
                DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
                fireCompareModelbest.Draw(ShortSide, LongSide);
            }
        }

        //寻找第三优先级定位点并测试摆放
        private void ThirdPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();
            
            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], ShortSide, LongSide, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                Polyline AgainstTest = searchPoint0.LeanWallList[i];

                if (i == 0)
                {
                    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                }
                int start = -1;
                int end = -1;

                switch (Mode)
                {
                    case 2: { start = 0; end = 9; break; }
                    case 1: { start = 0; end = 9; break; }
                    case 0: { start = 0; end = 9; break; }
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
                                Line tmpLine = new Line(CenterPoint, fireHydrant0.TFireCenterPointList[j]);
                                if (tmpLine.IsIntersects(LeanWall.Shell())) distance = distance + 1000;

                                double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, AgainstTest);
                                double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], AgainstTest);
                                int againstWallLength = (int)(againstWallLength0 + againstWallLength1) / 100;

                                againstWallLength = againstWallLength + (int)((3000 - distance) / 100 * Info.DistanceWeight);

                                Point3d fireCenter = new Point3d();
                                Vector3d fireDir = new Vector3d();
                                fireHydrant0.SetModel(j, k, out fireCenter, out fireDir);
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], LeanWall.Shell(), ProcessedData.ParkingIndex);
                                FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, k, distance, againstWallLength, j, doorGood);
                                fireCompareModelsMix2.Add(fireCompareModeltmp);
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

        //混排(二、三优先级混排)情况下，选取最优摆放方式
        private void FindBest() 
        {
            //fireCompareModelsMix = fireCompareModelsMix.OrderBy(x => x.doorGood).ThenByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();
            fireCompareModelsMix1 = fireCompareModelsMix1.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();
            fireCompareModelsMix2 = fireCompareModelsMix2.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();
            List<FireCompareModel> fireCompareModelsMix3 = new List<FireCompareModel>();
            if (fireCompareModelsMix1.Count > 0)
            {
                if (fireCompareModelsMix1.Count >= 2) 
                {
                    FireCompareModel fireCompareModelsa = fireCompareModelsMix1[0];
                    FireCompareModel fireCompareModelsb = fireCompareModelsMix1[1];
                    if (fireCompareModelsa.fireCenterPoint.DistanceTo(fireCompareModelsb.fireCenterPoint) < 300
                        && fireCompareModelsa.doorOpenDir + fireCompareModelsb.doorOpenDir == 1
                        ) 
                    {
                        if (overlappedAreaDoor[fireCompareModelsa] > overlappedAreaDoor[fireCompareModelsb]) 
                        {
                            fireCompareModelsMix1[0] = fireCompareModelsMix1[1];
                        }
                    }
                }
                
                fireCompareModelsMix3.Add(fireCompareModelsMix1[0]);
            }
            if (fireCompareModelsMix2.Count > 0)
            {
                fireCompareModelsMix3.Add(fireCompareModelsMix2[0]);
            }
            fireCompareModelsMix3 = fireCompareModelsMix3.OrderByDescending(x => x.againstWallLength).ThenBy(x => x.distance).ToList();

            if (fireCompareModelsMix3.Count > 0)
            {
                Done = true;
                FireCompareModel fireCompareModelbest = fireCompareModelsMix3[0];
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
