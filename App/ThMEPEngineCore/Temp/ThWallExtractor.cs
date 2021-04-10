﻿using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Interface;
using ThMEPEngineCore.BuildRoom.Service;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;

namespace ThMEPEngineCore.Temp
{
    public class ThWallExtractor :ThExtractorBase, IExtract,IPrint, IBuildGeometry,IGroup
    {
        public List<Entity> Walls { get; private set; }
        public ThWallExtractor()
        {
            Walls = new List<Entity>();
            Category = "Wall";
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractWallService()
            {
                WallLayer = "墙",
            };
            instance.Extract(database, pts);

            IBuffer buffer = new ThNTSBufferService();
            var outlines = new List<Entity>();
            double offsetDis = 5.0;
            instance.Walls.ForEach(o =>
            {
                outlines.Add(buffer.Buffer(o, -offsetDis));
            });

            IBuildArea buildArea = new ThNTSBuildAreaService();
            var objs = buildArea.BuildArea(outlines.ToCollection());

            Walls = objs.Cast<Entity>().Select(o=> buffer.Buffer(o, offsetDis)).ToList();
            Walls = Walls.Where(o => o != null).ToList();
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        public void Print(Database database)
        {
            using (var db=AcadDatabase.Use(database))
            {
                var wallIds = new ObjectIdList();
                Walls.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    o.SetDatabaseDefaults();
                    wallIds.Add(db.ModelSpace.Add(o));
                });
                if (wallIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), wallIds);
                }
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            throw new NotImplementedException();
        }
    }
}
