using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using Linq2Acad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;

using ThMEPElectrical.FireAlarm.Service;

namespace ThMEPElectrical.FireAlarmFixLayout.Logic
{
    public class ThFireTelFixedPointLayoutService : ThFixedPointLayoutService
    {
        public ThMEPEngineCore.Algorithm.ThMEPOriginTransformer Transformer { get; set; }

        public ThFireTelFixedPointLayoutService(List<ThGeometry> data, List<string> LayoutBlkName, List<string> AvoidBlkName) :base(data, LayoutBlkName, AvoidBlkName)
        {
        }

        public override List<KeyValuePair<Point3d, Vector3d>> Layout()
        {
            int ReservedWidth = 250;//预留宽度
            //var bufferValue = 1.0; // 门扩的buffer值
            var fireLinkageSet = DataQueryWorker
                .GetDecorableOutlineBase(DataQueryWorker.FireLinkageRooms
                .Select(o => o.Boundary is MPolygon ? (o.Boundary as MPolygon).Shell() : o.Boundary)
                .Cast<Polyline>().ToList()); //消防关联房间

            var releventElementSet = DataQueryWorker.Avoidence.Select(x => x.Boundary).ToCollection();//避让元素（门窗）
            var spatialAvoidenceIndex = new ThCADCoreNTSSpatialIndex(releventElementSet);
            var doorSet = DataQueryWorker.DoorOpenings.Select(x => x.Boundary).ToCollection();
            var spatialDoorIndex = new ThCADCoreNTSSpatialIndex(doorSet);
            List<KeyValuePair<Point3d, Vector3d>> ans = new List<KeyValuePair<Point3d, Vector3d>>();

            //DrawUtils.ShowGeometry(fireLinkageSet, "l0roomList", 30, 25);

            foreach (Polyline room in fireLinkageSet)  //遍历消防关联房间
            {
                room.Closed = true;
                if (room.IsCCW())
                    room.ReverseCurve(); //房间框线转为顺时针
                var crossingDoor = spatialDoorIndex.SelectCrossingPolygon(room);
                if (crossingDoor.Count == 0)
                    continue;
                Polyline door = crossingDoor.Cast<Polyline>().OrderBy( o => o.Area).FirstOrDefault();
                var avoidenceSet = spatialAvoidenceIndex.SelectCrossingPolygon(room);
                var doorPt = DataQueryWorker.GetAvoidPoints(room, new List<Polyline> { door });
                if (doorPt.Count == 0 )
                    continue;
                List<List<Point3d>> avoidencePt = DataQueryWorker.GetAvoidPoints(room,avoidenceSet.Cast<Polyline>().ToList());
                Tolerance tol = new Tolerance(10, 10);
                int num = room.NumberOfVertices;
                //记录门所在墙体
                var doorPaired = new List<KeyValuePair<int, Point3d>>();
                foreach (Point3d pt in doorPt[0])
                {
                    doorPaired.Add(new KeyValuePair<int, Point3d>(DataQueryWorker.FindIndex(room, pt), pt));
                }
                doorPaired.OrderBy(o => o.Key);
                int rightIndex = -1;  //记录door为左或右的index值
                int leftIndex = -1;
                for(int i = 1; i<doorPaired.Count; ++i)
                {
                    if( doorPaired[i].Key - doorPaired[i-1].Key > 1 )
                    {
                        rightIndex = i;
                        leftIndex = i-1;
                        break;
                    }
                }
                if (rightIndex == -1)
                {
                    rightIndex = 0;
                    leftIndex = doorPaired.Count - 1;
                }

                var rightHandWall = new Polyline();  //门两侧墙
                var leftHandWall = new Polyline();
                var locatePt = new Point3d();
                //使门两侧墙的点位存储顺序为由门开始
                rightHandWall.AddVertexAt(0, doorPaired[rightIndex].Value.ToPoint2D(), 0, 0, 0);
                rightHandWall.AddVertexAt(1, room.GetPoint3dAt((doorPaired[rightIndex].Key+ 1) % (num-1)).ToPoint2D(), 0, 0, 0);
                leftHandWall.AddVertexAt(0, doorPaired[leftIndex].Value.ToPoint2D(), 0, 0, 0);
                leftHandWall.AddVertexAt(1, room.GetPoint3dAt(doorPaired[leftIndex].Key ).ToPoint2D(), 0, 0, 0);
                

                Polyline walls = new Polyline();  //其他墙体顺时针存储，应为不闭合polyline
                for (int i = 0 ; i < num - doorPaired.Count ; ++i)
                {
                    walls.AddVertexAt( i , room.GetPoint3dAt((i + doorPaired[rightIndex].Key  ) % num).ToPoint2D(), 0, 0, 0);
                }
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    //布点优先级为先门右侧，再门左侧，其次剩下墙体顺时针遍历
                    locatePt = DataQueryWorker.WallsFindLocation(rightHandWall, avoidencePt, ReservedWidth);
                    var temp = new List<Entity>();
                    if (!locatePt.IsPositiveInfinity())
                    {
                        ans.Add(DataQueryWorker.FindVector(locatePt, room));
                        temp.Add(rightHandWall);
                        ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(temp, acadDatabase.Database, 5);
                    }
                    else
                    {
                        locatePt = DataQueryWorker.WallsFindLocation(leftHandWall, avoidencePt, ReservedWidth);
                        if (!locatePt.IsPositiveInfinity())
                        {
                            leftHandWall.ReverseCurve();
                            ans.Add(DataQueryWorker.FindVector(locatePt, room));
                            temp.Add(leftHandWall);
                            ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(temp, acadDatabase.Database, 2);

                        }
                        else
                        {
                            locatePt = DataQueryWorker.WallsFindLocation(walls, avoidencePt, ReservedWidth);
                            if (!locatePt.IsPositiveInfinity())
                            {
                                ans.Add(DataQueryWorker.FindVector(locatePt, room));
                                temp.Add(walls);
                                ThMEPEngineCore.CAD.ThAuxiliaryUtils.CreateGroup(temp, acadDatabase.Database, 4);

                            }
                        }
                    }
                }
                    
            }
            return ans;
        }
    }
}
