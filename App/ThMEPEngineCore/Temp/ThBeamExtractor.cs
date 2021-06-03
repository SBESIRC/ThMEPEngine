﻿using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThBeamExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> Beams { get; set; }
        public ThBeamExtractor()
        {
            Category = BuiltInCategory.Beam.ToString();
            Beams = new List<Polyline>();
            ElementLayer = "梁";
            UseDb3Engine = true;
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            if(UseDb3Engine)
            {
                var engine = new ThBeamRecognitionEngine();
                engine.Recognize(database, pts);
                Beams = engine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            }
            else
            {
                var service = new ThExtractPolylineService()
                {
                    ElementLayer = this.ElementLayer
                };
                service.Extract(database, pts);
                Beams = service.Polys;
            }
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Beams.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if(GroupSwitch)
                {
                    geometry.Properties.Add(AreaOwnerPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }   

        public void Print(Database database)
        {
            Beams.Cast<Entity>().ToList().CreateGroup(database,ColorIndex);
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                Beams.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
