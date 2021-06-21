using System;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Temp
{
    public class ThExternalSpaceExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {
        public List<Entity> ExternalSpaces { get; set; }
        public double OffsetDis { get; set; }
        public ThExternalSpaceExtractor()
        {
            OffsetDis = 100000;
            Category = "ExternalSpace";
            ExternalSpaces = new List<Entity>();
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ExternalSpaces.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            var service = new ThExtractOuterBoundaryService()
            {
                ElementLayer=this.ElementLayer,
            };

            service.Extract(database, pts);
            service.OuterBoundaries.ForEach(o =>
            {
                var objs = o.GetOffsetCurves(OffsetDis);
                if(objs.Count == 0)
                {
                    objs = o.Buffer(OffsetDis);
                }
                if(objs.Count>0)
                {
                    var shell = objs.Cast<Polyline>().OrderByDescending(e => e.Area).First();
                    var newShell = ThMEPFrameService.Normalize(shell);
                    var hole = ThMEPFrameService.Normalize(o.Clone() as Polyline);
                    var mPolygon = ThMPolygonTool.CreateMPolygon(shell, new List<Curve> { hole });
                    if(mPolygon.Area>0.0)
                    {
                        ExternalSpaces.Add(mPolygon);
                    }
                }
            });
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            throw new NotImplementedException();
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var doorIds = new ObjectIdList();
                ExternalSpaces.ForEach(o =>
                {
                    o.ColorIndex = ColorIndex;
                    doorIds.Add(db.ModelSpace.Add(o));
                    o.SetDatabaseDefaults();                    
                });
                if (doorIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), doorIds);
                }
            }
        }
    }
}
