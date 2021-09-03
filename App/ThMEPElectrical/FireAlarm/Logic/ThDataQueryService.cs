using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPElectrical.FireAlarm.Service;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.IO.ExcelService;
using System.Data;
using ThMEPElectrical.Service;
using GeometryExtensions;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.FireAlarm.Logic
{
    public class ThDataQueryService
    {
        private List<ThGeometry> Data { get; set; } = new List<ThGeometry>();
        public List<ThGeometry>  Storeys { get; private set; } = new List<ThGeometry>(); //StoreyBorder
        public List<ThGeometry>  DoorOpenings { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  FireAparts { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  ArchitectureWalls { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  Shearwalls { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  Columns { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  Windows { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  Rooms { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry>  Holes { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> FireProofs { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Avoidence { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> FireLinkageRooms { get; private set; } = new List<ThGeometry>();
        public string floorTag { get; private set; }
        private Dictionary<Entity, ThGeometry> GeometryMap { get; set; }
        public List<string> FireApartMap { get; set; } = new List<string>();
        public ThDataQueryService(List<ThGeometry> data)
        {
            Data = data;
            PrepareData();
            GeometryMap = new Dictionary<Entity, ThGeometry>();
            data.ForEach(o =>
            {
                if (o.Boundary != null)
                {
                    GeometryMap.Add(o.Boundary, o);
                }
            });
            for (int i = 0; i < FireAparts.Count; ++i)
                FireApartMap.Add(FireAparts[i].Properties[ThExtractorPropertyNameManager.IdPropertyName].ToString());
            
        }
        protected void PrepareData()
        {
            DoorOpenings = QueryC(BuiltInCategory.DoorOpening.ToString());
            Storeys = QueryC(BuiltInCategory.StoreyBorder.ToString());
            Columns = QueryC(BuiltInCategory.Column.ToString());
            Shearwalls = QueryC(BuiltInCategory.ShearWall.ToString());
            FireAparts = QueryC(BuiltInCategory.FireApart.ToString());
            ArchitectureWalls = QueryC(BuiltInCategory.ArchitectureWall.ToString());
            Windows = QueryC(BuiltInCategory.Window.ToString());
            Rooms = QueryC(BuiltInCategory.Room.ToString());
            Holes = QueryC(BuiltInCategory.Hole.ToString());
            FireProofs = QueryC(BuiltInCategory.FireApart.ToString());
            Avoidence.AddRange(DoorOpenings);
            Avoidence.AddRange(Windows);
            floorTag = Storeys[0].Properties[ThExtractorPropertyNameManager.FloorNumberPropertyName].ToString();
            GetFireLinkageRooms();
        }
        
        private List<ThGeometry> QueryC(string category)
        {
            var result = new List<ThGeometry>();
            foreach (ThGeometry geo in Data)
            {
                if (geo.Properties[ThExtractorPropertyNameManager.CategoryPropertyName].ToString() == category)
                {
                    result.Add(geo);
                }
            }
            return result;
        }
        public List<ThGeometry> GetFireDoors()
        {
            return DoorOpenings
                    .Where(o => o.Properties[ThExtractorPropertyNameManager.UseagePropertyName].ToString().Contains("防火门"))
                    .ToList();
        }

        public ThGeometry Query(Entity entity)
        {
            return GeometryMap[entity];
        }

        private void GetFireLinkageRooms()
        {
            ReadRoomConfigTable();
            List<string> FireLinkageNames = new List<string> { "消防水泵房", "发电机房", "配变电室", "计算机网络机房", 
                "主要通风和空调机房", "防排烟机房", "灭火控制系统操作装置处或控制室", "企业消防站", "消防值班室", "总调度室", "消防电梯机房" };
            List<string> NameCollection = new List<string>();
            foreach(string a in FireLinkageNames)
            {
                NameCollection.AddRange(RoomConfigTreeService.CalRoomLst(roomTableConfig,a));
            }
            foreach(ThGeometry room in Rooms)
            {
                foreach (string roomtag in NameCollection)
                {
                    if (room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString().Contains(roomtag))
                    {
                        FireLinkageRooms.Add(room);
                        continue;
                    }
                }
            }
        }
        public List<Polyline> GetDecorableOutlineBase(List<Polyline> targetRooms) // 获取房间可布范围框线（墙体考虑柱子）
        {
            var columnSet = Columns.Select(x => x.Boundary).ToCollection();
            var rooms = targetRooms;
            var spatialColumnSet = new ThCADCoreNTSSpatialIndex(columnSet);
            List<Polyline> decorableOutline = new List<Polyline>();
            foreach(Polyline room in rooms)
            {
                var crossingColumns = spatialColumnSet.SelectCrossingPolygon(room);
                var roomOutlineL = room.Difference(crossingColumns).Cast<Polyline>().Where(o => o.Closed = true).ToList(); //封闭polyline
                var DecOutline = roomOutlineL[0];
                for(int i = 1; i<roomOutlineL.Count; ++i) //找differce后的最大框线作为返回结果防止出错
                {
                    if (roomOutlineL[i].Area > DecOutline.Area)
                        DecOutline = roomOutlineL[i];
                }
                if (DecOutline.IsCCW())  //房间框线顺时针存储
                    DecOutline.ReverseCurve();
                decorableOutline.Add(DecOutline);
            }
            return decorableOutline;
        }

        public KeyValuePair<Point3d, Vector3d> FindVector(Point3d pt, Polyline room) // 给定点找在房间框线的朝向
        {
            if(room.NumberOfVertices > 2)
            {
                if (room.IsCCW())
                    room.ReverseCurve(); //房间框线转为顺时针
            }
            int index = 0;
            Tolerance tol = new Tolerance(5, 5);
            for(int i = 0; i < room.NumberOfVertices; ++i)
            {
                if(new Line(room.GetPoint3dAt(i), room.GetPoint3dAt((i+1)%room.NumberOfVertices)).ToCurve3d().IsOn(pt, tol))
                {
                    index = i;
                    break;
                }
            }
            var vec = (room.GetPoint3dAt(index + 1) - pt).GetNormal().RotateBy(-Math.PI / 2, Vector3d.ZAxis);
            return new KeyValuePair<Point3d, Vector3d> (pt, vec);
        }
        public Dictionary<string, List<ThGeometry>> ClassifyByFireApart(List<ThGeometry> className) //将框线按防火分区分类
        {
            Dictionary<string,List< ThGeometry>> classfiyResultsDict = new Dictionary<string, List<ThGeometry>>();

            foreach (ThGeometry toClassify in className)
            {
                if (toClassify.Properties[ThExtractorPropertyNameManager.ParentIdPropertyName] != null)
                {
                    var parentId = toClassify.Properties[ThExtractorPropertyNameManager.ParentIdPropertyName].ToString();
                    if (classfiyResultsDict.ContainsKey(parentId) == false)
                    {
                        classfiyResultsDict.Add(parentId, new List<ThGeometry> { });
                    }
                    classfiyResultsDict[parentId].Add(toClassify);

                }
                else
                    continue;
            }
            return classfiyResultsDict;
        }

        public int FindIndex(Polyline room, Point3d point)  //找点在polyline的哪个线段所在编号，没有则返回-1
        {
            int num = room.NumberOfVertices;
            Tolerance tol = new Tolerance(5, 5);
            for (int i = 0; i< num - 1; ++i)
            {
                if (new Line(room.GetPoint3dAt(i), room.GetPoint3dAt(i + 1)).ToCurve3d().IsOn(point, tol))
                    return i;
            }
            return -1;
        }
        public List<List<Point3d>> GetAvoidPoints(Polyline room, List<Polyline> avoidence)
        {
            List<List<Point3d>> result = new List<List<Point3d>>();
            int num = room.NumberOfVertices;
            foreach (Polyline avoid in avoidence)
            {
                List<Point3d> temp = new List<Point3d>(); 
                temp.AddRange(avoid.IntersectWithEx(room).OfType<Point3d>().ToList());
                if (temp.Count < 2)

                    continue;
                int zindex = FindIndex(room, temp[0]);
                int oindex = FindIndex(room, temp[temp.Count-1]);
                if(zindex == oindex)
                {
                    result.Add(temp);
                }
                else
                {
                    for(int i = 0; i < num-1 ; ++i)
                    {
                        Point3d pt = room.GetPoint3dAt(i);
                        if (avoid.Contains(pt))
                            temp.Add(pt);
                    }
                    result.Add(temp);
                }
            }
            return result;
        }
        public List<Line> FindAvoidLine(Polyline walls, List<List<Point3d>> avoidencePt, int reservedLength)
        {
            int num = walls.NumberOfVertices;
            Tolerance tol = new Tolerance(5, 5);
            var result = new List<Line>();
            for(int i = 0; i< num-1; ++i)
            {
                Line tempwall = new Line(walls.GetPoint3dAt(i), walls.GetPoint3dAt(i + 1));
                foreach (List<Point3d> avoidEnt in avoidencePt)
                {
                    var tempAvoid = new List<Point3d>();
                    foreach(Point3d pt in avoidEnt)
                    {
                        if (tempwall.ToCurve3d().IsOn(pt,tol))
                            tempAvoid.Add(pt);
                    }
                    if (tempAvoid.Count < 2)
                        continue;
                    for(int j = 0; j< tempAvoid.Count - 1; ++j)
                    {
                        Line avoidOnWall = new Line(tempAvoid[j], tempAvoid[j + 1]);
                        Line actualAvoid = new Line((avoidOnWall.StartPoint + avoidOnWall.Delta.GetNormal().MultiplyBy(-reservedLength)),
                        (avoidOnWall.EndPoint + avoidOnWall.Delta.GetNormal().MultiplyBy(reservedLength)));//实际不可布置区域
                        result.Add(actualAvoid);
                    }
                }
            }
            return result;
        }

        public Point3d WallsFindLocation(Polyline wallsOutline, List<List<Point3d>> avoidencePtSet, int reservedLength)
        {
            Point3d ans = new Point3d(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Tolerance tol = new Tolerance(10, 10);
            double bufferValue = 1.0;
            var avoidenceOnWall = FindAvoidLine(wallsOutline, avoidencePtSet, reservedLength);
            var avoidWallSet = avoidenceOnWall.Select(o => o.Buffer(bufferValue)).ToCollection(); //外扩成矩形

            for (int i = 0; i < wallsOutline.NumberOfVertices - 1; ++i)
            {
                var temp = new Line(wallsOutline.GetPoint3dAt(i), wallsOutline.GetPoint3dAt(i + 1));
                if (temp.Length < 2 * reservedLength)
                    continue;
                Line editableBase = new Line((temp.StartPoint + temp.Delta.GetNormal().MultiplyBy(reservedLength)),
                    (temp.EndPoint + temp.Delta.GetNormal().MultiplyBy(-reservedLength))); //墻可安置部分
                var editablePt = new List<Point3d>();
                var startPt = editableBase.StartPoint;
                var editablePoly = ThCADCoreNTSEntityExtension.Difference(editableBase.Buffer(bufferValue), avoidWallSet);
                foreach (Polyline tempWall in editablePoly)
                {
                    var tempPt = tempWall.Intersect(wallsOutline, Intersect.OnBothOperands);
                    editablePt.AddRange(tempPt);
                }
                foreach (Point3d pt in editablePt)
                {
                    if (startPt.DistanceTo(ans) > startPt.DistanceTo(pt))
                        ans = pt;
                }
                if (!ans.IsPositiveInfinity())
                    break;
            }

            return ans;
        }

        /// 判断当前点是凸点还是凹点(-1，凸点；1，凹点；0，点在线上，不是拐点)
        public int IsConvexPoint(Polyline poly, Point3d pt, Point3d nextP, Point3d preP) 
        {
            Vector3d nextV = (nextP - pt).GetNormal();
            Vector3d preV = (pt - preP).GetNormal();
            Point3d movePt = pt - nextV * 1 + preV * 1;
            if (poly.OnBoundary(movePt))
            {
                return 0;   //点在线上，不是拐点
            }
            if (!poly.Contains(movePt))
            {
                return -1; //凸点
            }
            else
            {
                return 1;    //凹点
            }
        }

        static string roomConfigUrl = ThCADCommon.SupportPath() + "\\房间名称分类处理.xlsx";
        public List<RoomTableTree> roomTableConfig = null;    //房间配置表
        public List<List<string>> MonitorRoomNameMap = new List<List<string>>();       //

        /// <summary>
        /// 读取房间配置表
        /// </summary>
        public void ReadRoomConfigTable()
        {
            ReadExcelService excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(roomConfigUrl, true);
            var table = dataSet.Tables[ThElectricalUIService.Instance.Parameter.RoomNameControl];
            if (table != null)
            {
                roomTableConfig = RoomConfigTreeService.CreateRoomTree(table);
            }
        }
    }
}
