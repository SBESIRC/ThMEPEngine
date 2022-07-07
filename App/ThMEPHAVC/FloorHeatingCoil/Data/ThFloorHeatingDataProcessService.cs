using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using NFox.Cad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.CAD;


using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Service;
using Linq2Acad;
using AcHelper;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThFloorHeatingDataProcessService
    {
        //input
        public bool WithUI = false;
        public List<ThExtractorBase> InputExtractors { get; set; }
        public List<ThIfcDistributionFlowElement> FurnitureObstacleData { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Polyline> FurnitureObstacleDataTemp { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        public List<BlockReference> WaterSeparatorData { get; set; } = new List<BlockReference>();
        public List<DBText> RoomSuggestDist { get; set; } = new List<DBText>();
        public List<Polyline> RoomSetFrame { get; set; } = new List<Polyline>();

        //----private
        private List<Polyline> Door { get; set; } = new List<Polyline>();
        private List<MPolygon> RoomBoundary { get; set; } = new List<MPolygon>();
        private List<ThFloorHeatingWaterSeparator> WaterSeparator { get; set; } = new List<ThFloorHeatingWaterSeparator>();
        private List<Polyline> FurnitureObstacle { get; set; } = new List<Polyline>();

        //----output

        public List<ThRoomSetModel> RoomSet = new List<ThRoomSetModel>();
        public List<ThRoomSetModel> WarningRoomSet = new List<ThRoomSetModel>();

        public ThFloorHeatingDataProcessService()
        {

        }

        public void ProcessData()
        {
            ProcessDoorData();
            ProcessRoomData();
            ProcessFurnitureObstacle();
            ProcessWaterSeparator();
            CleanSeparateLine();

            ProcessSeparateLine();
            var separateRooms = SeparateRoomWithLine();
            CreateRoomSet(separateRooms);
        }

        private void ProcessDoorData()
        {
            var doorExtractor = InputExtractors.Where(o => o is ThFloorHeatingDoorExtractor).First() as ThFloorHeatingDoorExtractor;
            doorExtractor.Doors.ForEach(x => Door.Add(x.Outline as Polyline));
        }
        private void ProcessRoomData()
        {
            var roomExtractor = InputExtractors.Where(o => o is ThFloorHeatingRoomExtractor).First() as ThFloorHeatingRoomExtractor;
            foreach (var room in roomExtractor.Rooms)
            {
                if (room.Boundary is Polyline pl)
                {
                    RoomBoundary.Add(ThMPolygonTool.CreateMPolygon(pl));
                }
                else if (room.Boundary is MPolygon mpl)
                {
                    RoomBoundary.Add(mpl);
                }
            }
        }
        private void ProcessWaterSeparator()
        {
            foreach (var waterSeparator in WaterSeparatorData)
            {
                var w = new ThFloorHeatingWaterSeparator(waterSeparator);
                WaterSeparator.Add(w);
            }
        }

        private void ProcessFurnitureObstacle()
        {
            foreach (var obstacle in FurnitureObstacleData)
            {
                var blk = obstacle.Outline as BlockReference;
                var pl = ThGeomUtil.GetVisibleOBB(blk);
                FurnitureObstacle.Add(pl);
            }

            FurnitureObstacle.AddRange(FurnitureObstacleDataTemp);
        }
        private void CleanSeparateLine()
        {
            RemoveInDoorLine();
            RemoveNoInRoomLine();
        }

        private void RemoveNoInRoomLine()
        {
            RoomSeparateLine = RoomSeparateLine.Where(x => RoomBoundary.Where(r => r.Contains(x) || r.Intersects(x)).Any()).ToList();
        }

        private void RemoveInDoorLine()
        {
            var tol = 0.5;
            var objSelect = new DBObjectCollection();
            Door.ForEach(x => objSelect.Add(x));
            var objIndex = new ThCADCoreNTSSpatialIndex(objSelect);

            var separateLineTemp = new List<Line>();
            foreach (var separateL in RoomSeparateLine)
            {
                var objL = new List<Line>();
                var doorContain = Door.Where(x => x.Contains(separateL)).Any();
                if (doorContain)
                {
                    objL.Add(separateL);
                }
                else
                {
                    var doorCross = Door.Where(x => x.Intersects(separateL)).ToList();
                    foreach (var d in doorCross)
                    {
                        var objTrim = d.Trim(separateL);
                        var trimL = ThDrawTool.GetLines(objTrim);

                        objL.AddRange(trimL);
                    }
                }

                var lenthInDoor = objL.Select(x => x.Length).Sum();
                var rate = lenthInDoor / separateL.Length;
                if (rate > tol)
                {
                    separateLineTemp.Add(separateL);
                }
            }

            RoomSeparateLine.RemoveAll(x => separateLineTemp.Contains(x));

        }

        private void ProcessSeparateLine()
        {
            GenerateLineToRoomBoundary();
        }

        private void GenerateLineToRoomBoundary()
        {
            var obj = new DBObjectCollection();
            RoomBoundary.ForEach(x => obj.Add(x.Shell()));
            RoomBoundary.ForEach(x => x.Holes().ForEach(h => obj.Add(h)));
            FurnitureObstacle.ForEach(x => obj.Add(x));

            var tol = 30;
            var spIdx = new ThCADCoreNTSSpatialIndex(obj);
            var changeLine = new Dictionary<Line, Line>();

            var lineToRoom = new List<Line>();
            foreach (var line in RoomSeparateLine)
            {
                //分两轮，第一轮只看房间和障碍物。因为分割线自己相交后面有变动会对自己产生影响
                var newLine = GenerateLineToRoomBoundary(line, spIdx, tol);
                lineToRoom.Add(newLine);
            }

            var lintToLine = new List<Line>();
            foreach (var line in lineToRoom)
            {
                var newLine = GenerateLineToLine(line, lineToRoom, tol);
                lintToLine.Add(newLine);
            }

            RoomSeparateLine.Clear();
            RoomSeparateLine.AddRange(lintToLine);
        }

        private Line GenerateLineToRoomBoundary(Line line, ThCADCoreNTSSpatialIndex spIdx, double tol)
        {

            var newSp = GeneratePtToRoomBoundary(line.StartPoint, spIdx, tol);
            var newEp = GeneratePtToRoomBoundary(line.EndPoint, spIdx, tol);
            //DrawUtils.ShowGeometry(newSp, "l0lineEnd", r: 50, symbol: "S");
            //DrawUtils.ShowGeometry(newEp, "l0lineEnd", r: 50, symbol: "S");

            return new Line(newSp, newEp);
        }

        private Point3d GeneratePtToRoomBoundary(Point3d pt, ThCADCoreNTSSpatialIndex spIdx, double tol)
        {
            var newPtList = new List<Point3d>();

            var sq = PtToSqure(pt, tol);
            var selectObj = spIdx.SelectCrossingPolygon(sq);
            var selectPl = selectObj.OfType<Polyline>();

            foreach (var pl in selectPl)
            {
                //这里不能找最近点，应该先找最近拐点再找最近点
                var nearPt = pl.Vertices().OfType<Point3d>().OrderBy(x => x.DistanceTo(pt)).FirstOrDefault();
                if (nearPt != Point3d.Origin && nearPt.DistanceTo(pt) < tol)
                {
                    newPtList.Add(nearPt);
                }
                else
                {
                    var newPtTemp = pl.GetClosestPointTo(pt, false);
                    newPtList.Add(newPtTemp);
                }
            }

            var newPt = newPtList.OrderBy(x => x.DistanceTo(pt)).FirstOrDefault();
            if (newPt == Point3d.Origin)
            {
                newPt = pt;
            }
            return newPt;
        }

        private static Polyline PtToSqure(Point3d pt, double r)
        {
            var sq = new Polyline();
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X - r, pt.Y + r), 0, 0, 0);
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X + r, pt.Y + r), 0, 0, 0);
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X + r, pt.Y - r), 0, 0, 0);
            sq.AddVertexAt(sq.NumberOfVertices, new Point2d(pt.X - r, pt.Y - r), 0, 0, 0);
            sq.Closed = true;
            return sq;
        }


        private Line GenerateLineToLine(Line l, List<Line> lineList, double tol)
        {
            var newSp = new Point3d();
            var newEp = new Point3d();

            foreach (var line in lineList)
            {
                if (l != line)
                {
                    var spt = line.GetClosestPointTo(l.StartPoint, false);
                    if (spt.DistanceTo(l.StartPoint) < tol)
                    {
                        newSp = spt;
                    }
                    var ept = line.GetClosestPointTo(l.EndPoint, false);
                    if (ept.DistanceTo(l.EndPoint) < tol)
                    {
                        newEp = ept;
                    }
                }
            }
            if (newSp == Point3d.Origin)
            {
                newSp = l.StartPoint;
            }
            if (newEp == Point3d.Origin)
            {
                newEp = l.EndPoint;
            }
            var newLine = new Line(newSp, newEp);
            return newLine;
        }

        private static double GetSuggestDist(Polyline room, List<DBText> roomSuggestDist)
        {
            var dist = 0;
            var text = roomSuggestDist.Where(x => room.Contains(x.Position)).FirstOrDefault();
            dist = ThGeomUtil.GetNumberInText(text);
            return dist;
        }

        private List<MPolygon> SeparateRoomWithLineOri()
        {
            var boundary = new List<LineString>();

            foreach (var room in RoomBoundary)
            {
                var bound = room.Shell().ToNTSLineString();
                var holes = room.Holes().Select(x => x.ToNTSLineString());
                boundary.Add(bound);
                boundary.AddRange(holes);
            }

            var separate = RoomSeparateLine.Select(x => x.ExtendLine(50)).ToList();
            //DrawUtils.ShowGeometry(separate, "l0sepExtend", 1);
            boundary.AddRange(separate.Select(x => x.ToNTSLineString()));
            var separateRoom = boundary.GetPolygons();
            var rooms = separateRoom.Select(x => x.ToDbEntity()).OfType<Entity>().ToList();

            var newRoom = new List<MPolygon>();
            newRoom.AddRange(rooms.OfType<MPolygon>());
            newRoom.AddRange(rooms.OfType<Polyline>().Select(x => ThMPolygonTool.CreateMPolygon(x)));

            return newRoom;
        }

        private List<Entity> SeparateRoomWithLine()
        {
            double Tol_Area = 1000;
            var bufferValue = 200;

            var obj = new DBObjectCollection();
            RoomBoundary.ForEach(x => x.Holes().ForEach(h => obj.Add(h)));
            RoomBoundary.ForEach(x => obj.Add(x.Shell()));
            FurnitureObstacle.ForEach(x => obj.Add(x));
            RoomSeparateLine.ForEach(x => obj.Add(x));

            var sbService = new ThRoomSuperBoundaryProcessService();
            var separateSpaceObj = sbService.ProcessBoundary(obj, WithUI);
            var separateSpace = separateSpaceObj.OfType<Polyline>().ToList();
            DrawUtils.ShowGeometry(separateSpace, "l0separateSpace", 1);

            //清除太小的面
            separateSpace = separateSpace.Where(x => x.Area > Tol_Area).ToList();
            var spaceObj = separateSpace.ToCollection();

            //清除重复
            var spIdx = new ThCADCoreNTSSpatialIndex(spaceObj);
            var areaObj = spIdx.SelectAll();
           
            //造空间
            ThMEPEngineCore.Service.ThNTSBufferService bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
            for (int i = 0; i < areaObj.Count; i++)
            {
                areaObj[i] = bufferService.Buffer(areaObj[i] as Entity, -1);
            }
            var separateSpaceM = areaObj.BuildArea();
            for (int i = 0; i < separateSpaceM.Count; i++)
            {
                separateSpaceM[i] = bufferService.Buffer(separateSpaceM[i] as Entity, 1);
            }
            //separateSpaceM.OfType<Entity>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l0separateMpoly", 3));

            //删除原来洞，障碍物空间
            var holeList = new List<Polyline>();
            holeList.AddRange(RoomBoundary.SelectMany(x => x.Holes()));
            holeList.AddRange(FurnitureObstacle);
            RemoveHoleSpace(holeList, ref separateSpaceM);
            //separateSpaceM.OfType<Entity>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l0separateRemoveHole", 4));

            //缩小
            for (int i = 0; i < separateSpaceM.Count; i++)
            {
                separateSpaceM[i] = bufferService.Buffer(separateSpaceM[i] as Entity, -bufferValue);
            }

            //放大
            for (int i = 0; i < separateSpaceM.Count; i++)
            {
                separateSpaceM[i] = bufferService.Buffer(separateSpaceM[i] as Entity, bufferValue);
            }

            var separateRoom = separateSpaceM.OfType<Entity>().ToList();
            //separateRoom.ForEach(x => DrawUtils.ShowGeometry(x, "l0enlargeRoom", 6));

            return separateRoom;
        }

        private static void RemoveHoleSpace(List<Polyline> holeList, ref DBObjectCollection separateSpace)
        {
            var tol = new Tolerance(10000, 10000);
            var removeList = new List<Polyline>();
            var separateLine = separateSpace.OfType<Polyline>().ToList();

            holeList = holeList.SelectMany(x => x.Buffer(1).OfType<Polyline>()).ToList();

            var spaceObj = separateLine.ToCollection();
            var spaceIdx = new ThCADCoreNTSSpatialIndex(spaceObj);
            foreach (var hole in holeList)
            {
                var obj = spaceIdx.SelectWindowPolygon(hole);
                if (obj.Count > 0)
                {
                    removeList.AddRange(obj.OfType<Polyline>());

                }
            }

            foreach (var r in removeList)
            {
                separateSpace.Remove(r);
            }
        }

        private void CreateRoomSet(List<Entity> separateRoom)
        {
            var markExtractor = InputExtractors.Where(o => o is ThFloorHeatingRoomMarkExtractor).First() as ThFloorHeatingRoomMarkExtractor;

            var objSelect = new DBObjectCollection();
            Door.ForEach(x => objSelect.Add(x));
            WaterSeparator.ForEach(x => objSelect.Add(x.OBB));
            FurnitureObstacle.ForEach(x => objSelect.Add(x));
            separateRoom.ForEach(x => objSelect.Add(x));
            RoomSeparateLine.ForEach(x => objSelect.Add(x));

            var objIndex = new ThCADCoreNTSSpatialIndex(objSelect);

            foreach (var frame in RoomSetFrame)
            {
                var selectObj = objIndex.SelectCrossingPolygon(frame);
                var door = Door.Where(x => selectObj.Contains(x)).ToList();
                var waterSeparator = WaterSeparator.Where(x => selectObj.Contains(x.OBB)).FirstOrDefault();
                var obstacle = FurnitureObstacle.Where(x => selectObj.Contains(x));
                var room = separateRoom.Where(x => selectObj.Contains(x)).ToList();
                var roomSeparateline = RoomSeparateLine.Where(x => selectObj.Contains(x));

                var hasHoleRoom = CreateRoomModel(room, markExtractor, RoomSuggestDist, out var roomList);

                var roomset = new ThRoomSetModel();
                roomset.Frame = frame;
                roomset.Door.AddRange(door);
                roomset.WaterSeparator = waterSeparator;
                roomset.FurnitureObstacle.AddRange(obstacle);
                roomset.RoomSeparateLine.AddRange(roomSeparateline);
                roomset.Room.AddRange(roomList);

                if (hasHoleRoom == false)
                {
                    RoomSet.Add(roomset);
                }
                else
                {
                    WarningRoomSet.Add(roomset);
                }
            }
        }

        private static bool CreateRoomModel(List<Entity> room, ThFloorHeatingRoomMarkExtractor markExtractor, List<DBText> roomSuggestDist, out List<ThFloorHeatingRoom> RoomList)
        {
            var hasHoles = false;
            RoomList = new List<ThFloorHeatingRoom>();
            foreach (var r in room)
            {
                Polyline roomboundary = null;
                if (r is Polyline rpl)
                {
                    roomboundary = rpl;
                }
                if (r is MPolygon rmpl)
                {
                    roomboundary = rmpl.Shell();
                    if (rmpl.Holes().Count > 0)
                    {
                        hasHoles = true;
                    }
                }

                var roomModel = new ThFloorHeatingRoom(roomboundary);
                var markInRoom = markExtractor.Marks.Where(x => roomboundary.Contains(x.Geometry)).ToList();
                var markString = markInRoom.Select(x => x.Text).Distinct().ToList();
                roomModel.SetName(markString);

                //这里有问题，如果一个大厅被分割了但是suggest只有一个
                var suggestDist = GetSuggestDist(roomboundary, roomSuggestDist);
                roomModel.SetSuggestDist(suggestDist);
                RoomList.Add(roomModel);
            }
            return hasHoles;
        }

        public void ProjectOntoXYPlane()
        {

        }
        public void Print()
        {
            foreach (var roomset in RoomSet)
            {
                roomset.Door.ForEach(x => DrawUtils.ShowGeometry(x, "l1door", 3));
                if (roomset.WaterSeparator != null)
                {
                    roomset.WaterSeparator.StartPts.ForEach(x => DrawUtils.ShowGeometry(x, "l1waterSeparator", 1, r: 30));
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirLine, "l1waterDir", 1, l: 50);
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirStartPt, "l1waterDir", 1, l: 50);

                }
                roomset.FurnitureObstacle.ForEach(x => DrawUtils.ShowGeometry(x, "l1obstacle", 1));
                roomset.Room.ForEach(x => DrawUtils.ShowGeometry(x.RoomBoundary, "l1room", 30));
                roomset.RoomSeparateLine.ForEach(x => DrawUtils.ShowGeometry(x, "l1roomSeparator", 1));
            }

            foreach (var roomset in WarningRoomSet)
            {
                roomset.Door.ForEach(x => DrawUtils.ShowGeometry(x, "l1wdoor", 3));
                if (roomset.WaterSeparator != null)
                {
                    roomset.WaterSeparator.StartPts.ForEach(x => DrawUtils.ShowGeometry(x, "l1wwaterSeparator", 1, r: 30));
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirLine, "l1wwaterDir", 1, l: 50);
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirStartPt, "l1wwaterDir", 1, l: 50);
                }
                roomset.FurnitureObstacle.ForEach(x => DrawUtils.ShowGeometry(x, "l1wobstacle", 1));
                roomset.Room.ForEach(x => DrawUtils.ShowGeometry(x.RoomBoundary, "l1wroom", 30));
                roomset.RoomSeparateLine.ForEach(x => DrawUtils.ShowGeometry(x, "l1wroomSeparator", 1));

            }
        }
        public void Transform(ThMEPOriginTransformer transformer)
        {
        }
        public void Reset(ThMEPOriginTransformer transformer)
        {
        }


    }
}
