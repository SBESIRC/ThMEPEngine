using System;
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

        private List<ThTempSpace> Spaces { get; set; }
        public bool BuildAreaSwitch { get; set; }
        public bool CheckIsolated { get; set; }
        public ThWallExtractor()
        {
            Walls = new List<Entity>();
            Category = "Wall";
            ElementLayer = "墙";
            BuildAreaSwitch = true;
            CheckIsolated = true;
            Spaces = new List<ThTempSpace>();
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var instance = new ThExtractWallService()
            {
                ElementLayer = this.ElementLayer,
                Types = this.Types
            };
            instance.Extract(database, pts);
            if(BuildAreaSwitch)
            {
                IBuffer buffer = new ThNTSBufferService();
                var outlines = new List<Entity>();
                double offsetDis = 5.0;
                instance.Walls.ForEach(o =>
                {
                    outlines.Add(buffer.Buffer(o, -offsetDis));
                });
                IBuildArea buildArea = new ThNTSBuildAreaService();
                var objs = buildArea.BuildArea(outlines.ToCollection());
                Walls = objs.Cast<Entity>().Select(o => buffer.Buffer(o, offsetDis)).ToList();
                Walls = Walls.Where(o => o != null).ToList();
            }
            else
            {
                Walls = instance.Walls;
            }
        }

        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Walls.ForEach(o =>
            {
                if(CheckIsolated)
                {
                    var isolate = IsIsolate(Spaces, o);
                    if (isolate)
                    {
                        var geometry = new ThGeometry();
                        geometry.Properties.Add(CategoryPropertyName, Category);
                        geometry.Properties.Add(IsolatePropertyName, isolate);
                        geometry.Boundary = o;
                        geos.Add(geometry);
                    }
                }
                else
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(CategoryPropertyName, Category);
                    geometry.Properties.Add(IsolatePropertyName, false);
                    var eles = IEleQuery.Query(o);
                    if(eles.Count>0)
                    {
                        geometry.Properties.Add(ElevationPropertyName, eles);
                    }
                    geometry.Boundary = o;
                    geos.Add(geometry);
                }
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

        public void SetSpaces(List<ThTempSpace> spaces)
        {
            Spaces = spaces;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            throw new NotImplementedException();
        }
    }
}
