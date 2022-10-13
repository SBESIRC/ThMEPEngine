using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using Linq2Acad;
using AcHelper;
using DotNetARX;
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
using ThMEPEngineCore.Service;

using ThMEPHVAC.FloorHeatingCoil.Model;
using ThMEPHVAC.FloorHeatingCoil.Service;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThFloorHeatingDataProcessService
    {
        //input
        public bool WithUI = false;
        public ThMEPOriginTransformer Transformer { get; set; } = new ThMEPOriginTransformer();
        public List<ThExtractorBase> InputExtractors { get; set; }
        public List<Polyline> FurnitureObstacle { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        public List<BlockReference> WaterSeparatorData { get; set; } = new List<BlockReference>();
        public List<BlockReference> BathRadiatorData { get; set; } = new List<BlockReference>();

        //processed
        private List<Polyline> Door { get; set; } = new List<Polyline>();
        private List<MPolygon> RoomBoundary { get; set; } = new List<MPolygon>();
        private List<ThFloorHeatingWaterSeparator> WaterSeparator { get; set; } = new List<ThFloorHeatingWaterSeparator>();
        private List<ThFloorHeatingBathRadiator> BathRadiator { get; set; } = new List<ThFloorHeatingBathRadiator>();

        //----output

        public List<ThRoomSetModel> RoomSet = new List<ThRoomSetModel>();
        public List<ThRoomSetModel> WarningRoomSet = new List<ThRoomSetModel>();

        public ThFloorHeatingDataProcessService()
        {

        }

        //public void ProcessDataWithFrame()
        //{
        //    ProcessDoorData();
        //    ProcessRoomData();
        //    ProcessFurnitureObstacle();
        //    ProcessWaterSeparator();
        //    ProcessBathRadiator();
        //    CleanSeparateLine();
        //    GenerateLineToRoomBoundary();
        //    var separateRooms = SeparateRoomWithLine();
        //    CreateRoomSet(separateRooms);
        //}

        public void ProcessDataWithRoom(List<Polyline> selectFrames)
        {
            ProcessedRowData();
            Transform(ref selectFrames);
            ProjectOntoXYPlane();
            SelectObjInRoom(selectFrames);
            CleanRoomBoundary();
            CleanSeparateLine();
            GenerateLineToRoomBoundary();
            var separateRooms = SeparateRoomWithLine();
            CreateRoomSetInRoomFrames(separateRooms);
            PairRoomSetWithOriginalRoom();
            selectFrames.ForEach(x => Transformer.Reset(x));
        }

        private void ProcessedRowData()
        {
            ProcessDoorData();
            ProcessRoomData();
            ProcessWaterSeparator();
            ProcessBathRadiator();
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
        private void ProcessBathRadiator()
        {
            foreach (var bathRadiator in BathRadiatorData)
            {
                var b = new ThFloorHeatingBathRadiator(bathRadiator);
                BathRadiator.Add(b);
            }
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

        private List<Entity> SeparateRoomWithLine()
        {
            double Tol_Area = 1000;
            var bufferValue = 200;

            var obj = new DBObjectCollection();
            RoomBoundary.ForEach(x => x.Holes().ForEach(h => obj.Add(h)));
            RoomBoundary.ForEach(x => obj.Add(x.Shell()));
            FurnitureObstacle.ForEach(x => obj.Add(x));
            RoomSeparateLine.ForEach(x => obj.Add(x));

            //RoomBoundary.ForEach(x => DrawUtils.ShowGeometry(x, "l0RoomBoundary", 1));
            //FurnitureObstacle.ForEach(x => DrawUtils.ShowGeometry(x, "l0FurnitureObstacle", 1));
            //RoomSeparateLine.ForEach(x => DrawUtils.ShowGeometry(x, "l0RoomSeparateLine", 1));

            var sbService = new ThRoomSuperBoundaryProcessService();
            var separateSpaceObj = sbService.ProcessBoundary(obj, WithUI);
            var separateSpace = separateSpaceObj.OfType<Polyline>().ToList();
            //DrawUtils.ShowGeometry(separateSpace, "l0separateSpace", 1);

            //清除太小的面
            separateSpace = separateSpace.Where(x => x.Area > Tol_Area).ToList();
            var spaceObj = separateSpace.ToCollection();

            //清除重复
            var spIdx = new ThCADCoreNTSSpatialIndex(spaceObj);
            var areaObj = spIdx.SelectAll();

            //造空间
            var bufferService = new ThNTSBufferService();
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
            var separateRoomTemp = separateSpaceM.OfType<Entity>().ToList();
            //separateRoomTemp.ForEach(x => DrawUtils.ShowGeometry(x, "l0shrinkRoom", 11));

            var simplifier = new ThPolygonalElementSimplifier()
            {
                OFFSETDISTANCE = 10,
            };
            separateSpaceM = simplifier.MakeValid(separateSpaceM);
            separateSpaceM = simplifier.Normalize(separateSpaceM);
            //separateRoomTemp.ForEach(x => DrawUtils.ShowGeometry(x, "l0shrinkRoom2", 14));

            //放大
            for (int i = 0; i < separateSpaceM.Count; i++)
            {
                separateSpaceM[i] = bufferService.Buffer(separateSpaceM[i] as Entity, bufferValue);
            }
            var separateRoom = separateSpaceM.OfType<Entity>().ToList();
            //separateRoom.ForEach(x => DrawUtils.ShowGeometry(x, "l0enlargeRoom", 171));

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

        private void CreateRoomSetInRoomFrames(List<Entity> separateRoom)
        {
            //var markExtractor = InputExtractors.Where(o => o is ThFloorHeatingRoomMarkExtractor).First() as ThFloorHeatingRoomMarkExtractor;

            //var hasHoleRoom = CreateRoomModel(separateRoom, markExtractor, RoomSuggestDist, out var roomList);
            var hasHoleRoom = CreateRoomModel(separateRoom, out var roomList);
            var roomset = new ThRoomSetModel();
            roomset.Door.AddRange(Door);
            roomset.WaterSeparator = WaterSeparator.FirstOrDefault();
            roomset.BathRadiators.AddRange(BathRadiator);
            roomset.FurnitureObstacle.AddRange(FurnitureObstacle);
            roomset.RoomSeparateLine.AddRange(RoomSeparateLine);
            roomset.Room.AddRange(roomList);
            roomset.HasHoleRoom = hasHoleRoom;

            if (roomset.HasHoleRoom == false)
            {
                RoomSet.Add(roomset);
            }
            else
            {
                WarningRoomSet.Add(roomset);
            }

        }

        private static bool CreateRoomModel(List<Entity> room, out List<ThFloorHeatingRoom> RoomList)
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
                if (roomboundary == null)
                {
                    continue;
                }

                var roomModel = new ThFloorHeatingRoom(roomboundary);
                RoomList.Add(roomModel);
            }
            return hasHoles;
        }

        /// <summary>
        /// 先根据所选房间选择门，集分水器，散热器，障碍物，分割线，房间
        /// 不包括房间tag和房间盘管间距建议
        /// </summary>
        /// <param name="selectFrames"></param>
        private void SelectObjInRoom(List<Polyline> selectFrames)
        {
            //var markExtractor = InputExtractors.Where(o => o is ThFloorHeatingRoomMarkExtractor).First() as ThFloorHeatingRoomMarkExtractor;

            var objSelect = new DBObjectCollection();
            Door.ForEach(x => objSelect.Add(x));
            WaterSeparator.ForEach(x => objSelect.Add(x.OBB));
            BathRadiator.ForEach(x => objSelect.Add(x.OBB));
            FurnitureObstacle.ForEach(x => objSelect.Add(x));
            RoomSeparateLine.ForEach(x => objSelect.Add(x));
            RoomBoundary.ForEach(x => objSelect.Add(x));

            var objIndex = new ThCADCoreNTSSpatialIndex(objSelect);

            var tempDoor = new List<Polyline>();
            var tempWaterSeparator = new List<ThFloorHeatingWaterSeparator>();
            var tempBathRadiator = new List<ThFloorHeatingBathRadiator>();
            var tempFurnitureObstacle = new List<Polyline>();
            var tempRoomSeparatorLine = new List<Line>();
            var tempRoomBoundary = new List<MPolygon>();

            foreach (var frame in selectFrames)
            {
                var selectObj = objIndex.SelectCrossingPolygon(frame);
                tempDoor.AddRange(Door.Where(x => selectObj.Contains(x)));
                tempWaterSeparator.AddRange(WaterSeparator.Where(x => selectObj.Contains(x.OBB)));
                tempBathRadiator.AddRange(BathRadiator.Where(x => selectObj.Contains(x.OBB)));
                tempFurnitureObstacle.AddRange(FurnitureObstacle.Where(x => selectObj.Contains(x)));
                tempRoomSeparatorLine.AddRange(RoomSeparateLine.Where(x => selectObj.Contains(x)));
                tempRoomBoundary.AddRange(RoomBoundary.Where(x => selectObj.Contains(x)));
            }


            Door.Clear();
            WaterSeparator.Clear();
            BathRadiator.Clear();
            FurnitureObstacle.Clear();
            RoomSeparateLine.Clear();
            RoomBoundary.Clear();

            Door.AddRange(tempDoor.Distinct());
            WaterSeparator.AddRange(tempWaterSeparator.Distinct());
            BathRadiator.AddRange(tempBathRadiator.Distinct());
            FurnitureObstacle.AddRange(tempFurnitureObstacle.Distinct());
            RoomSeparateLine.AddRange(tempRoomSeparatorLine.Distinct());
            RoomBoundary.AddRange(tempRoomBoundary.Distinct());
        }

        private void CleanRoomBoundary()
        {
            //RoomBoundary.ForEach(x => DrawUtils.ShowGeometry(x, "l0originalRoomBoundary"));
            var roomBoundarySimplify = RoomBoundary.Select(x => ThGeomUtil.ProcessMpoly(x, 20)).ToList();
            RoomBoundary.Clear();
            RoomBoundary.AddRange(roomBoundarySimplify);

        }

        private void PairRoomSetWithOriginalRoom()
        {
            foreach (var rooms in RoomSet)
            {
                foreach (var room in rooms.Room)
                {
                    var pl = room.RoomBoundary;
                    var oriPls = RoomBoundary.Where(x => x.Contains(pl) || x.IsIntersects(pl));
                    if (oriPls.Any())
                    {
                        var arearadioMax = 0.0;
                        Polyline pairOriPl = null;

                        foreach (var oriPl in oriPls)
                        {
                            var obj = new DBObjectCollection();
                            obj.Add(oriPl);
                            var intersect = pl.Intersection(obj);

                            foreach (var inter in intersect)
                            {
                                var arearadio = 0.0;
                                if (inter is Polyline interPl)
                                {
                                    arearadio = interPl.Area / pl.Area;
                                    //DrawUtils.ShowGeometry(interPl, "l0test");
                                }
                                else if (inter is MPolygon interMpl)
                                {
                                    arearadio = interMpl.Area / pl.Area;
                                    //DrawUtils.ShowGeometry(interMpl, "l0test");
                                }
                                if (arearadio > arearadioMax && arearadio > 0.9)
                                {
                                    arearadioMax = arearadio;
                                    pairOriPl = oriPl.Shell();
                                }

                            }
                            if (pairOriPl != null)
                            {
                                room.SetOriginalBoundary(pairOriPl);
                            }
                        }
                    }
                }
            }
        }

        public static void GetSuggestData(BlockReference blk, out double route, out double suggestDist, out double totalLength)
        {
            var rValue = blk.ObjectId.GetAttributeInBlockReference(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Route);
            var dValue = blk.ObjectId.GetAttributeInBlockReference(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Dist);
            var lValue = blk.ObjectId.GetAttributeInBlockReference(ThFloorHeatingCommon.BlkSettingAttrName_RoomSuggest_Length);

            route = ThFloorHeatingCoilUtilServices.GetNumberFromString(rValue);
            suggestDist = ThFloorHeatingCoilUtilServices.GetNumberFromString(dValue);
            totalLength = ThFloorHeatingCoilUtilServices.GetNumberFromString(lValue);

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
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirLine, "l1waterSeparator", 1, l: 50);
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirStartPt, "l1waterSeparator", 1, l: 50);
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.OBB, "l1waterSeparator", 1);
                }

                roomset.BathRadiators.ForEach(b => b.StartPts.ForEach(x => DrawUtils.ShowGeometry(x, "l1radiator", 1, r: 30)));

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
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirLine, "l1wwaterSeparator", 1, l: 50);
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.StartPts[0], roomset.WaterSeparator.DirStartPt, "l1wwaterSeparator", 1, l: 50);
                    DrawUtils.ShowGeometry(roomset.WaterSeparator.OBB, "l1wwaterSeparator", 1);
                }
                roomset.BathRadiators.ForEach(b => b.StartPts.ForEach(x => DrawUtils.ShowGeometry(x, "l1wradiator", 1, r: 30)));

                roomset.FurnitureObstacle.ForEach(x => DrawUtils.ShowGeometry(x, "l1wobstacle", 1));
                roomset.Room.ForEach(x => DrawUtils.ShowGeometry(x.RoomBoundary, "l1wroom", 30));
                roomset.RoomSeparateLine.ForEach(x => DrawUtils.ShowGeometry(x, "l1wroomSeparator", 1));

            }
        }

        private void Transform(ref List<Polyline> selectFrames)
        {
            selectFrames.ForEach(x => Transformer.Transform(x));
            Door.ForEach(x => Transformer.Transform(x));
            RoomBoundary.ForEach(x => Transformer.Transform(x));
            RoomSeparateLine.ForEach(x => Transformer.Transform(x));
            WaterSeparator.ForEach(x => x.Transform(Transformer));
            BathRadiator.ForEach(x => x.Transform(Transformer));
            FurnitureObstacle.ForEach(x => Transformer.Transform(x));
        }

        public void Reset()
        {


        }


    }
}
