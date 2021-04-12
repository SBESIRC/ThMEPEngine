﻿using System;
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
        public List<ThTempSpace> Spaces { get; private set; }
        public Dictionary<Entity, List<Polyline>> Obstacles { get; set; }
        /// <summary>
        /// 障碍物颜色
        /// </summary>
        public short ObstacleColorIndex { get; set; }
        /// <summary>
        /// 是否要创建阻碍物
        /// </summary>
        public bool IsBuildObstacle { get; set; }
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
            Spaces = new List<ThTempSpace>();
            Obstacles = new Dictionary<Entity, List<Polyline>>();
            IsBuildObstacle = false;
            Category = "Space";
            ElementLayer = "AD-AREA-OUTL";
            NameLayer = "AD-NAME-ROOM";
            ObstacleColorIndex = 211;
            ObstacleCategory = "Obstacle";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var spaceEngine = new ThExtractSpaceRecognitionEngine())
            {
                spaceEngine.SpaceLayer = ElementLayer;
                spaceEngine.NameLayer = NameLayer;

                spaceEngine.Recognize(database, pts);
                Spaces = spaceEngine.TempSpaces;               
                if(IsBuildObstacle)
                {
                    BuildObstacles();
                }
            }
        }

        private void BuildObstacles()
        {
            //在停车区域空间
            var spaces = Spaces
                 .Where(o => o.Tags.Where(g => g.Contains("停车区域")).Any())
                 .Select(o => o.Outline).ToList();
            spaces.ForEach(o => Obstacles.Add(o.Clone() as Entity, ThArrangeObstacleService.Arrange(o)));
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            geos.AddRange(BuildSpaceGeometries());
            geos.AddRange(BuildObstacleGeometries());
            return geos;
        }

        private List<ThGeometry> BuildSpaceGeometries()
        {
            var geos = new List<ThGeometry>();
            Spaces.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, string.Join(";", o.Tags.ToArray()));
                geometry.Properties.Add(GroupOwnerPropertyName, BuildString(GroupOwner,o.Outline));
                geometry.Boundary = o.Outline;
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
            PrintSpace(database);
            PrintObstacles(database);
        }

        private void PrintSpace(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var spaceIds = new ObjectIdList();
                Spaces.ForEach(o =>
                {
                    o.Outline.ColorIndex = ColorIndex;
                    o.Outline.SetDatabaseDefaults();
                    spaceIds.Add(db.ModelSpace.Add(o.Outline));
                });
                if (spaceIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), spaceIds);
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

        public void Group(Dictionary<Entity, string> groupId)
        {
            Spaces.ForEach(o => GroupOwner.Add(o.Boundary, FindCurveGroupIds(groupId, o.Boundary)));
        }
    }
}
