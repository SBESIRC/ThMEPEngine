﻿using System;
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
    class SingleFireExtinguisher
    {
        //全局变量
        public int Mode = Info.Mode; 
        //外部输入
        ThHydrantModel model;
        Point3d CenterPoint;
        double LongSide = 0;
        double ShortSide = 0;
        int Type;
        //是否找到合适的摆放位置
        bool Done = false;
        FireHydrant BestLayOut;

        //使用的类
        SearchPoint searchPoint0;
        FeasibilityCheck feasibilityCheck0;
        IndexCompute indexCompute0;
        //可倚靠区域
        MPolygon LeanWall;


        public SingleFireExtinguisher(ThHydrantModel model, int type)
        {
            //读取属性
            this.model = model;
            CenterPoint = model.Center;
            Type = type;
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
            searchPoint0.FindTurningPoint2(out basePointList, out dirList, ShortSide);
            FirstPriorityTest(basePointList, dirList);

            //寻找第二优先级定位点并测试摆放
            if (Done == false)
            {
                searchPoint0.FindColumnPoint2(out basePointList, out dirList);
                SecondPriorityTest(basePointList, dirList);
            }

            //寻找第三优先级定位点并测试摆放
            if (Done == false)
            {
                //searchPoint0.FindOtherPoint(out basePointList, out dirList);
                //ThirdPriorityTest(basePointList, dirList);
            }

        }

        //寻找第一优先级定位点并测试摆放
        public void FirstPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                FireExtinguisher fireExtinguisher0 = new FireExtinguisher(basePointList[i], dirList[i], ShortSide, LongSide, Type, Info.Mode);
                List<Polyline> fireObbList = fireExtinguisher0.GetFireObbList();
                if (i == 0)
                {
                    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                }

                int start = -1;
                int end = -1;

                switch (Mode)
                {
                    case 2: { start = 0; end = 4; break; }
                    case 1: { start = 0; end = 4; break; }
                    case 0: { start = 0; end = 4; break; }
                }

                for (int j = start; j <= end; j++)
                {
                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        Polyline doorArea = fireExtinguisher0.GetDoorAreaObb(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));
                        if (FeasibilityCheck.IsDoorFeasible(doorArea, LeanWall.Shell()))      //如果门没有被阻挡,找到一个可以摆放的模型
                        {
                            double distance = fireExtinguisher0.TFireCenterPointList[j].DistanceTo(CenterPoint);

                            double againstWallLength = indexCompute0.CalculateWallLength(fireObbList[j], LeanWall.Shell());
                
                            Point3d fireCenter = new Point3d();
                            Vector3d fireDir = new Vector3d();
                            fireExtinguisher0.SetModel(j, out fireCenter, out fireDir);
                            bool doorGood = FeasibilityCheck.IsBoundaryOK(doorArea, ProcessedData.ParkingIndex);
                            FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, -1, distance, againstWallLength, j, doorGood);
                            fireCompareModels0.Add(fireCompareModeltmp);
                            break;
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
                Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, Info.ShortSide, Info.LongSide, fireCompareModelbest.fireDir);
                //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
                DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
                fireCompareModelbest.Draw2();
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
                FireExtinguisher fireExtinguisher0 = new FireExtinguisher(basePointList[i], dirList[i], ShortSide, LongSide, Type, Info.Mode);
                List<Polyline> fireObbList = fireExtinguisher0.GetFireObbList();
                if (i == 0)
                {
                    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                }

                int start = -1;
                int end = -1;

                switch (Mode)
                {
                    case 2: { start = 0; end = 4; break; }
                    case 1: { start = 2; end = 3; break; }
                    case 0: { start = 2; end = 3; break; }
                }


                for (int j = start; j <= end; j++)
                {
                    if (FeasibilityCheck.IsFireFeasible(fireObbList[j], LeanWall.Shell()))   //如果消防栓可行
                    {
                        Polyline doorArea = fireExtinguisher0.GetDoorAreaObb(j);
                        //doorAreaList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1doorarea", 10));
                        if (FeasibilityCheck.IsDoorFeasible(doorArea, LeanWall.Shell()))      //如果门没有被阻挡,找到一个可以摆放的模型
                        {
                            double distance = fireExtinguisher0.TFireCenterPointList[j].DistanceTo(CenterPoint);

                            double againstWallLength = indexCompute0.CalculateWallLength(fireObbList[j], LeanWall.Shell());

                            Point3d fireCenter = new Point3d();
                            Vector3d fireDir = new Vector3d();
                            fireExtinguisher0.SetModel(j, out fireCenter, out fireDir);
                            bool doorGood = FeasibilityCheck.IsBoundaryOK(doorArea, ProcessedData.ParkingIndex);
                            FireCompareModel fireCompareModeltmp = new FireCompareModel(basePointList[i], dirList[i], fireCenter, fireDir, -1, distance, againstWallLength, j, doorGood);
                            fireCompareModels0.Add(fireCompareModeltmp);
                            break;
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
                Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, Info.ShortSide, Info.LongSide, fireCompareModelbest.fireDir);
                //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
                DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
                fireCompareModelbest.Draw2();
            }
        }

        //寻找第三优先级定位点并测试摆放
        public void ThirdPriorityTest(List<Point3d> basePointList, List<Vector3d> dirList)
        {
            //可行解存放处
            List<FireCompareModel> fireCompareModels0 = new List<FireCompareModel>();

            //开始循环
            for (int i = 0; i < basePointList.Count; i++)
            {
                var fireHydrant0 = new FireHydrant(basePointList[i], dirList[i], Type, Info.Mode);
                Polyline vpPl = fireHydrant0.GetRiserObb();
                List<Polyline> fireObbList = fireHydrant0.GetFireObbList();
                //if (i == 0)
                //{
                //    fireObbList.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1fireobblist", 10));
                //}
                int start = -1;
                int end = -1;

                switch (Info.Mode)
                {
                    case 2: { start = 6; end = 8; break; }
                    case 1: { start = 6; end = 8; break; }
                    case 0: { start = 6; end = 8; break; }
                }

                for (int j = start; j <= end; j++)
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

                                double againstWallLength0 = indexCompute0.CalculateWallLength(vpPl, LeanWall.Shell());
                                double againstWallLength1 = indexCompute0.CalculateWallLength(fireObbList[j], LeanWall.Shell());
                                int againstWallLength = (int)(againstWallLength0 + againstWallLength1) / 100;

                                Point3d fireCenter = new Point3d();
                                Vector3d fireDir = new Vector3d();
                                fireHydrant0.SetModel(j, k, out fireCenter, out fireDir);
                                bool doorGood = FeasibilityCheck.IsBoundaryOK(doorAreaList[k], ProcessedData.ParkingIndex);
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
                Polyline drawFire = CreateBoundaryService.CreateBoundary(fireCompareModelbest.fireCenterPoint, Info.ShortSide, Info.LongSide, fireCompareModelbest.fireDir);
                //Polyline drawFire = CreateBoundaryService.CreateBoundary(new Point3d(411898,722948,0), Info.ShortSide, Info.LongSide,new Vector3d(0,1,0));
                DrawUtils.ShowGeometry(drawFire, "l1fire", 2, lineWeightNum: 30);
                fireCompareModelbest.Draw2();
            }
        }
    }



}
