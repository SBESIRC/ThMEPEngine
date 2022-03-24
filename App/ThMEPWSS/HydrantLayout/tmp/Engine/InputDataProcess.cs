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
    class InputDataProcess
    {
        RawData rawData0;

        //寻找是否要处理的立柱
        public List<Point3d> VerticalPipeUsed0 = new List<Point3d>();
        public List<Point3d> VerticalPipeUsed1 = new List<Point3d>();
        public List<ThIfcVirticalPipe> VerticalPipeIgnore = new List<ThIfcVirticalPipe>();
        public DBObjectCollection VerticalPipePoint = new DBObjectCollection();
        public Dictionary<DBObject, ThIfcVirticalPipe> PointToVP = new Dictionary<DBObject, ThIfcVirticalPipe>();
        //对消火栓和灭火器进行分类
        public List<ThHydrantModel> HydrantModel0 = new List<ThHydrantModel>();
        public List<ThHydrantModel> HydrantModel1 = new List<ThHydrantModel>();

        // 可倚靠的墙体
        public List<MPolygon> LeanWall = new List<MPolygon>();

        //空间框线
        public List<Polyline> IgnoreVPBlock = new List<Polyline>();

        //空间索引
        public ThCADCoreNTSSpatialIndex VerticalPipeIndex;
        public DBObjectCollection ForbiddenObjs = new DBObjectCollection();
        public ThCADCoreNTSSpatialIndex ForbiddenIndex;
        public ThCADCoreNTSSpatialIndex LeanWallIndex;
        public DBObjectCollection PakingObjs = new DBObjectCollection();
        public ThCADCoreNTSSpatialIndex PakingIndex;
        public ThCADCoreNTSSpatialIndex DifferIndex;
        //消防栓的大小属性
        public double ShortSide = 200;
        public double LongSide = 800;
        public double DoorShortSide = 800;
        public double DoorLongSide = 1200;
        public double DoorOffset = 200;

        //其他属性
        

        public InputDataProcess(RawData rawData)
        {
            rawData0 = rawData;
            FindLeanWall();
            InstallationClassification();
            ParkingSpaceExpansion();
            ForbiddenCreate();
            Classification();
        }


        //求差集，求出可倚靠框线
        public void FindLeanWall()
        {
            var room = rawData0.Room;

            var obj = new DBObjectCollection();
            rawData0.Wall.ForEach(x => obj.Add(x));
            rawData0.Column.ForEach(x => obj.Add(x));
            rawData0.Door.ForEach(x => obj.Add(x));
            rawData0.FireProof.ForEach(x => obj.Add(x));
            DifferIndex = new ThCADCoreNTSSpatialIndex(obj);
            //var mroom = room.OfType<MPolygon>().ToList();
            //var differ0 = mroom[0].DifferenceMP(obj);
            //differ0.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x,"l0mroom"));


            //List<MPolygon> RoomList = new List<MPolygon>();
            //foreach (var singleroom in room) 
            //{
            //    if (singleroom is Polyline pl)
            //    {
            //        var sdNew = ThMPolygonTool.CreateMPolygon(pl);
            //        RoomList.Add(sdNew);
            //    }
            //    else if (singleroom is MPolygon mpl) 
            //    {
            //        RoomList.Add(mpl);
            //    }
            //}

            //foreach (var singleroom in RoomList)
            //{
            //    if (singleroom is MPolygon mpl)
            //    {
            //        var differ = mpl.DifferenceMP(obj);
            //        foreach (var singlediffer in differ)
            //        {
            //            if (singlediffer is Polyline sd)
            //            {
            //                var sdNew = ThMPolygonTool.CreateMPolygon(sd);
            //                LeanWall.Add(sdNew);
            //            }
            //            else if (singlediffer is MPolygon mp)
            //            {
            //                LeanWall.Add(mp);
            //            }
            //        }
            //    }
            //}

            foreach (var singleroom in room)
            {
                if (singleroom is Polyline pl)
                {
                    var differobj = DifferIndex.SelectCrossingPolygon(pl);              
                    var plNew = ThMPolygonTool.CreateMPolygon(pl);
                    var differ = plNew.DifferenceMP(differobj);
                    foreach (var singlediffer in differ)
                    {
                        if (singlediffer is Polyline sd)
                        {
                            var sdNew = ThMPolygonTool.CreateMPolygon(sd);
                            LeanWall.Add(sdNew);
                        }
                        else if (singlediffer is MPolygon mp)
                        {
                            LeanWall.Add(mp);
                        }
                    }

                }
                else if (singleroom is MPolygon mpl)
                {
                    var differobj = DifferIndex.SelectCrossingPolygon(mpl);
                    foreach (var hole in  mpl.Holes()) 
                    {
                        differobj.Add(hole);
                    }
                    var differ = mpl.DifferenceMP(differobj);
                    foreach (var singlediffer in differ)
                    {
                        if (singlediffer is Polyline sd)
                        {
                            var sdNew = ThMPolygonTool.CreateMPolygon(sd);
                            LeanWall.Add(sdNew);
                        }
                        else if (singlediffer is MPolygon mp)
                        {
                            LeanWall.Add(mp);
                        }
                    }
                }
            }
            LeanWall.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x,"l1newroom",6));
        }


        //设施分类
        public void InstallationClassification()
        {
            //var objs = new DBObjectCollection();
            //verticalPipe ： Entity(DBPoint)=>DBPoint.Position => Point3d
            foreach (var model in rawData0.VerticalPipe)
            {
                //VerticalPipePoint.Add((model.Outline as DBPoint).Position);
                VerticalPipePoint.Add(model.Outline);
                PointToVP.Add(model.Outline, model);
            }

            //建立空间索引
            VerticalPipeIndex = new ThCADCoreNTSSpatialIndex(VerticalPipePoint);

            //遍历每一个模型
            foreach (var model in rawData0.HydrantModel)
            {
                var OutObb = model.Outline;
                OutObb.Buffer(200);
                var VerticalPipeList = VerticalPipeIndex.SelectCrossingPolygon(OutObb);
                if (VerticalPipeList.Count != 0) //找到立管 
                {
                    rawData0.VerticalPipe.Remove(PointToVP[VerticalPipeList[0]]);
                    if (model.Type == 0)
                    {
                        //VerticalPipeUsed0.Add((PointToVP[VerticalPipeList[0]].Outline as DBPoint).Position);
                        HydrantModel0.Add(model);
                    }
                    if (model.Type == 1)
                    {
                        //VerticalPipeUsed1.Add((PointToVP[VerticalPipeList[0]].Outline as DBPoint).Position);
                        HydrantModel1.Add(model);
                    }
                }
                else if (VerticalPipeList.Count == 0) //没找到立管
                {
                    if (model.Type == 0)
                    {
                        //VerticalPipeUsed0.Add(model.Center);
                        HydrantModel0.Add(model);
                    }
                    if (model.Type == 1)
                    {
                        //VerticalPipeUsed1.Add(model.Center);
                        HydrantModel1.Add(model);
                    }
                }
            }

            //将剩下的立柱保存
            VerticalPipeIgnore = rawData0.VerticalPipe;
        }


        //车位外扩
        public void ParkingSpaceExpansion()
        {
            var pakingObjs = new DBObjectCollection();
            rawData0.Car.ForEach(x => pakingObjs.Add(x));
            pakingObjs.BufferPolygons(50);
            rawData0.Car = pakingObjs.OfType<Polyline>().ToList();
            rawData0.Car.OfType<Entity>().ForEachDbObject(x => DrawUtils.ShowGeometry(x, "l1car", 6));
        }

        //门/卷帘/不使用立柱 
        public void ForbiddenCreate()
        {
            //门 buffer
            var doorObjs = new DBObjectCollection();
            rawData0.Door.ForEach(x => doorObjs.Add(x));
            doorObjs.BufferPolygons(50);
            rawData0.Door = doorObjs.OfType<Polyline>().ToList();

            //卷帘 buffer
            var fpObjs = new DBObjectCollection();
            rawData0.FireProof.ForEach(x => fpObjs.Add(x));
            fpObjs.BufferPolygons(50);
            rawData0.FireProof = fpObjs.OfType<Polyline>().ToList();

            //立柱buffer
            foreach (var model in VerticalPipeIgnore)
            {
                var boundary = CreateBoundaryService.CreateBoundary((model.Outline as DBPoint).Position, 300, 300, new Vector3d(0, 1, 0));
                IgnoreVPBlock.Add(boundary);
            }
        }


        //按照空间索引分类
        public void Classification()
        {
            //禁区
            rawData0.Door.ForEach(x => ForbiddenObjs.Add(x));
            rawData0.FireProof.ForEach(x => ForbiddenObjs.Add(x));
            rawData0.Wall.ForEach(x => ForbiddenObjs.Add(x));
            //IgnoreVPBlock.ForEach(x => ForbiddenObjs.Add(x));
            ForbiddenIndex = new ThCADCoreNTSSpatialIndex(ForbiddenObjs);

            //可倚靠区
            LeanWallIndex = new ThCADCoreNTSSpatialIndex(LeanWall.ToCollection());

            //车位以及水井
            rawData0.Car.ForEach(x => PakingObjs.Add(x));
            rawData0.Well.ForEach(x => PakingObjs.Add(x));
            PakingIndex = new ThCADCoreNTSSpatialIndex(PakingObjs);

            //var model_list = model_spindex.SelectCrossingPolygon(circle);
            //foreach (var db in model_list)
            //{
            //}
        }

        public ProcessedData Output()
        {
            //Info
            Info.DoorLongSide = DoorLongSide;
            Info.DoorShortSide = DoorShortSide;
            Info.ShortSide = ShortSide;
            Info.LongSide = LongSide;
            Info.DoorOffset = DoorOffset;

            //输出处理后的数据
            ProcessedData processedData1 = new ProcessedData();
            ProcessedData.ForbiddenIndex = ForbiddenIndex;
            ProcessedData.LeanWallIndex = LeanWallIndex;
            ProcessedData.ParkingIndex = PakingIndex;

            processedData1.FireHydrant = HydrantModel0;
            processedData1.FireExtinguisher = HydrantModel1;
            //processedData1.FireHydrant = HydrantModel0;
            //processedData1.FireExtinguisher = HydrantModel1;

            return processedData1;
        }

    }
}
