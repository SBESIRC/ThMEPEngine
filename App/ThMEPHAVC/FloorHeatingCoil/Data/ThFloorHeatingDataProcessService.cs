﻿using System;
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

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThFloorHeatingDataProcessService
    {
        //input
        public List<ThExtractorBase> InputExtractors { get; set; }
        public List<ThIfcDistributionFlowElement> FurnitureObstacleData { get; set; } = new List<ThIfcDistributionFlowElement>();
        public List<Polyline> FurnitureObstacleDataTemp { get; set; } = new List<Polyline>();
        public List<Line> RoomSeparateLine { get; set; } = new List<Line>();
        public List<BlockReference> WaterSeparatorData { get; set; } = new List<BlockReference>();
        public List<DBText> RoomSuggestDist { get; set; } = new List<DBText>();
        public List<Polyline> RoomSetFrame { get; set; } = new List<Polyline>();

        //----private
        private List<Polyline> Door { get; set; } = new List<Polyline>();
        private List<ThFloorHeatingRoom> Room { get; set; } = new List<ThFloorHeatingRoom>();
        private List<ThFloorHeatingWaterSeparator> WaterSeparator { get; set; } = new List<ThFloorHeatingWaterSeparator>();
        private List<Polyline> FurnitureObstacle { get; set; } = new List<Polyline>();

        //----output

        public List<ThRoomSetModel> RoomSet = new List<ThRoomSetModel>();

        public ThFloorHeatingDataProcessService()
        {

        }

        public void ProcessDoorData()
        {
            var doorExtractor = InputExtractors.Where(o => o is ThFloorHeatingDoorExtractor).First() as ThFloorHeatingDoorExtractor;
            doorExtractor.Doors.ForEach(x => Door.Add(x.Outline as Polyline));
        }

        public void ProcessWaterSeparator()
        {
            foreach (var waterSeparator in WaterSeparatorData)
            {
                var w = new ThFloorHeatingWaterSeparator(waterSeparator);
                WaterSeparator.Add(w);
            }
        }

        public void CraeteRoomSapceModel()
        {
            var separateRooms = SeparateRoomWithLine();

            var markExtractor = InputExtractors.Where(o => o is ThFloorHeatingRoomMarkExtractor).First() as ThFloorHeatingRoomMarkExtractor;

            foreach (var room in separateRooms)
            {
                var newRoom = new ThFloorHeatingRoom(room);

                var markInRoom = markExtractor.Marks.Where(x => room.Contains(x.Geometry)).ToList();
                var markString = markInRoom.Select(x => x.Text).Distinct().ToList();
                newRoom.SetName(markString);

                var suggestDist = GetSuggestDist(room);
                newRoom.SetSuggestDist(suggestDist);


                Room.Add(newRoom);
            }
        }

        private double GetSuggestDist(MPolygon room)
        {
            var dist = 0;
            var text = RoomSuggestDist.Where(x => room.Contains(x.Position)).FirstOrDefault();
            dist = ThGeomUtil.GetNumberInText(text);
            return dist;
        }


        private List<MPolygon> SeparateRoomWithLine()
        {
            var boundary = new List<LineString>();
            var roomExtractor = InputExtractors.Where(o => o is ThFloorHeatingRoomExtractor).First() as ThFloorHeatingRoomExtractor;
            foreach (var room in roomExtractor.Rooms)
            {
                if (room.Boundary is Polyline pl)
                {
                    var bound = pl.ToNTSLineString();
                    boundary.Add(bound);
                }
                else if (room.Boundary is MPolygon mpl)
                {
                    var bound = mpl.Shell().ToNTSLineString();
                    var holes = mpl.Holes().Select(x => x.ToNTSLineString());
                    boundary.Add(bound);
                    boundary.AddRange(holes);
                }
            }

            var separate = RoomSeparateLine.Select(x => x.ExtendLine(50)).ToList();
            DrawUtils.ShowGeometry(separate, "l0sepExtend", 1);
            boundary.AddRange(separate.Select(x => x.ToNTSLineString()));
            var separateRoom = boundary.GetPolygons();
            var rooms = separateRoom.Select(x => x.ToDbEntity()).OfType<Entity>().ToList();

            var newRoom = new List<MPolygon>();
            newRoom.AddRange(rooms.OfType<MPolygon>());
            newRoom.AddRange(rooms.OfType<Polyline>().Select(x => ThMPolygonTool.CreateMPolygon(x)));

            return newRoom;
        }

        public void CreateFurnitureObstacle()
        {
            foreach (var obstacle in FurnitureObstacleData)
            {
                var blk = obstacle.Outline as BlockReference;
                var pl = ThGeomUtil.GetVisibleOBB(blk);
                FurnitureObstacle.Add(pl);
            }

            FurnitureObstacle.AddRange(FurnitureObstacleDataTemp);
        }

        public void CreateRoomSet()
        {
            foreach (var frame in RoomSetFrame)
            {
                var door = Door.Where(x => frame.Contains(x)).ToList();
                var room = Room.Where(x => frame.Contains(x.RoomBoundary)).ToList();
                var waterSeparator = WaterSeparator.Where(x => frame.Contains(x.OBB)).FirstOrDefault();
                var roomSeparateline = RoomSeparateLine.Where(x => frame.Contains(x)).ToList();
                var obstacle = FurnitureObstacle.Where(x => frame.Contains(x)).ToList();

                var roomset = new ThRoomSetModel();
                roomset.Frame = frame;
                roomset.Door.AddRange(door);
                roomset.Room.AddRange(room);
                roomset.WaterSeparator = waterSeparator;
                roomset.RoomSeparateLine.AddRange(roomSeparateline);
                roomset.FurnitureObstacle.AddRange(obstacle);
                RoomSet.Add(roomset);
            }
        }




        public void ProjectOntoXYPlane()
        {

        }
        public void Print()
        {
            foreach (var roomset in RoomSet)
            {
                roomset.Door.ForEach(x => DrawUtils.ShowGeometry(x, "l0door", 3));
                roomset.Room.ForEach(x => DrawUtils.ShowGeometry(x.RoomBoundary, "l0room", 30));
                roomset.Room.ForEach(x => DrawUtils.ShowGeometry(x.RoomBoundary.Shell().GetCentroidPoint(), String.Format("{0},{1}", string.Join(";", x.Name.ToArray()), x.SuggestDist), "l0roomName", 30, hight: 150));
                roomset.RoomSeparateLine.ForEach(x => DrawUtils.ShowGeometry(x, "l0roomSeparator", 1));
                if (roomset.WaterSeparator != null)
                {
                    roomset.WaterSeparator.StartPts.ForEach(x => DrawUtils.ShowGeometry(x, "l0waterSeparator", 1, r: 30));
                }
                roomset.FurnitureObstacle.ForEach(x => DrawUtils.ShowGeometry(x, "l0obstacle", 1));

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
