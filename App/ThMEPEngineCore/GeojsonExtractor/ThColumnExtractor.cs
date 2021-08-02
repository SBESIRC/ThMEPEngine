﻿using NFox.Cad;
using DotNetARX;
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

namespace ThMEPEngineCore.GeojsonExtractor
{
    public class ThColumnExtractor : ThExtractorBase, IPrint
    {
        public List<Polyline> Columns { get; protected set; }
        protected List<ThIfcRoom> Rooms { get; set; }
        public ThColumnExtractor()
        {
            Category = BuiltInCategory.Column.ToString();
            Columns = new List<Polyline>();
            Rooms = new List<ThIfcRoom>();
            TesslateLength = 5.0;
        }

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var isolateColumns = ThElementIsolateFilterService.Filter(Columns.Cast<Entity>().ToList(), Rooms);
            Columns.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var isolate = isolateColumns.Contains(o);
                geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, isolate);
                geometry.Boundary = o;
                if (IsolateSwitch) // 表示只传入孤立的柱
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
                var columnBuilder = new ThColumnBuilderEngine();
                Columns = columnBuilder
                    .Build(database, pts)
                    .Select(o=>o.Outline as Polyline)
                    .ToList();
            }
            else
            {
                var instance = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer,
                };
                instance.Extract(database, pts);
                Columns = instance.Polys.Select(o=>o.TessellatePolylineWithArc(TesslateLength)).ToList();
            }
            if (FilterMode == FilterMode.Window)
            {
                Columns = FilterWindowPolygon(pts, Columns.Cast<Entity>().ToList()).Cast<Polyline>().ToList();
            }
        }
        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }

        public void Print(Database database)
        {
            Columns.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }
    }
}
