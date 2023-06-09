﻿using NFox.Cad;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThArchitectureExtractor : ThExtractorBase, IPrint
    {
        public List<Entity> Walls { get; set; }
        protected List<ThIfcRoom> Rooms { get; set; }
        public ThArchitectureExtractor()
        {
            Category = BuiltInCategory.ArchitectureWall.ToString();
            Walls = new List<Entity>();
            Rooms = new List<ThIfcRoom>();
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateShearwalls = ThElementIsolateFilterService.Filter(Walls, Rooms);
            Walls.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var isolate = isolateShearwalls.Contains(o);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, isolate);
                geometry.Boundary = o;
                if (IsolateSwitch) //表示只传入孤立建筑墙
                {
                    if (isolate)
                    {
                        geos.Add(geometry);
                    }
                }
                else
                {
                    geos.Add(geometry);
                }
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {                
                var extractEngine = new ThDB3ArchWallExtractionEngine();
                extractEngine.Extract(database);

                var center = pts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(center);
                var newPts = transformer.Transform(pts);
                extractEngine.Results.ForEach(e => transformer.Transform(e.Geometry));

                var recogEngine = new ThDB3ArchWallRecognitionEngine();
                recogEngine.Recognize(extractEngine.Results, newPts);

                recogEngine.Elements.ForEach(o =>
                {
                    transformer.Reset(o.Outline);
                    Walls.Add(o.Outline);
                });
            }
            else
            {
                var instance = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);
                Walls = instance.Polys.Cast<Entity>().ToList();
            }
            if (FilterMode == FilterMode.Window)
            {
                Walls = FilterWindowPolygon(pts, Walls);
            }
        }

        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }

        public void Print(Database database)
        {
            Walls.CreateGroup(database, ColorIndex);
        }
        public override List<Entity> GetEntities()
        {
            return Walls;
        }
    }
}
