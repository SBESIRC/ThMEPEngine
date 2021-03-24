using System;
using System.Linq;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Temp
{
    public class ThSpaceExtractor :ThExtractorBase,IExtract,IPrint,IBuildGeometry,IGroup
    {
        public List<ThIfcRoom> Rooms { get; private set; }
        public Dictionary<Polyline, List<Polyline>> Obstacles { get; set; }
        /// <summary>
        /// 障碍物颜色
        /// </summary>
        public short ObstacleColorIndex { get; set; }
        /// <summary>
        /// 是否要创建阻碍物
        /// </summary>
        public bool IsBuildObstacle { get; set; }
        /// <summary>
        /// 空间轮廓图层名
        /// </summary>
        public string RoomLayer { get; set; }
        /// <summary>
        /// 空间标识名称
        /// </summary>
        public string NameLayer { get; set; }
        /// <summary>
        /// 障碍物种类
        /// </summary>
        public string ObstacleCategory { get; set; }

        public ThSpaceExtractor()
        {
            Rooms = new List<ThIfcRoom>();
            Obstacles = new Dictionary<Polyline, List<Polyline>>();
            IsBuildObstacle = false;
            Category = "Space";
            RoomLayer = "AD-AREA-OUTL";
            NameLayer = "AD-NAME-ROOM";
            ObstacleColorIndex = 211;
            ObstacleCategory = "Obstacle";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var roomEngine = new ThExtractRoomRecognitionEngine())
            {
                roomEngine.RoomLayer = RoomLayer;
                roomEngine.NameLayer = NameLayer;

                roomEngine.Recognize(database, pts);
                Rooms = roomEngine.Rooms;

                if(IsBuildObstacle)
                {
                    BuildObstacles();
                }
            }
        }

        private void BuildObstacles()
        {
            //在停车区域空间
            var rooms = Rooms
                 .Where(o => o.Tags.Where(g => g.Contains("停车区域")).Any())
                 .Select(o => o.Boundary).ToList();
            rooms.ForEach(o => Obstacles.Add(o.Clone() as Polyline, ThArrangeObstacleService.Arrange(o as Polyline)));
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            geos.AddRange(BuildRoomGeometries());
            geos.AddRange(BuildObstacleGeometries());
            return geos;
        }

        private List<ThGeometry> BuildRoomGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, string.Join(";", o.Tags.ToArray()));
                geometry.Properties.Add(GroupOwnerPropertyName, BuildString(GroupOwner,o.Boundary));
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildObstacleGeometries()
        {
            var geos = new List<ThGeometry>();
            Obstacles.ForEach(o =>
            {
                o.Value.ForEach(v =>
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(CategoryPropertyName, ObstacleCategory);
                    geometry.Boundary = v;
                    geos.Add(geometry);
                });                
            });
            return geos;
        }

        public void Print(Database database)
        {
            PrintRoom(database);
            PrintObstacles(database);
        }

        private void PrintRoom(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var roomIds = new ObjectIdList();
                Rooms.ForEach(o =>
                {
                    o.Boundary.ColorIndex = ColorIndex;
                    o.Boundary.SetDatabaseDefaults();
                    roomIds.Add(db.ModelSpace.Add(o.Boundary));
                });
                if (roomIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), roomIds);
                }
            }
        }

        private void PrintObstacles(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                Obstacles.ForEach(o =>
                {
                    var obstructIds = new ObjectIdList();
                    o.Key.ColorIndex = ColorIndex;
                    o.Key.SetDatabaseDefaults();
                    obstructIds.Add(db.ModelSpace.Add(o.Key));
                    o.Value.ForEach(v =>
                    {
                        v.ColorIndex = ObstacleColorIndex;
                        v.SetDatabaseDefaults();
                        obstructIds.Add(db.ModelSpace.Add(v));
                    });
                    if (obstructIds.Count > 0)
                    {
                        GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), obstructIds);
                    }
                });

            }
        }

        public void Group(Dictionary<Polyline, string> groupId)
        {
            Rooms.ForEach(o => GroupOwner.Add(o.Boundary, FindCurveGroupIds(groupId, o.Boundary)));
        }
    }
}
