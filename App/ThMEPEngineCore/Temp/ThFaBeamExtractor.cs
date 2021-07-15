using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using System.Linq;

namespace ThMEPEngineCore.Temp
{
    class ThFaBeamExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry, IGroup
    {
        public List<ThIfcBeam> Beams { get; private set; }
        private const string SwitchPropertyName = "Switch";
        private const string UsePropertyName = "Use";
        private const string DistanceToFlorPropertyName = "BottomDistanceToFloor";
        public ThFaBeamExtractor()
        {
            Beams = new List<ThIfcBeam>();
            Category = BuiltInCategory.Beam.ToString();
            ElementLayer = "梁";
            UseDb3Engine = true;
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Beams.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o.Outline));
                }
                if (Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o.Outline));
                }
                geometry.Properties.Add(DistanceToFlorPropertyName, o.DistanceToFloor);
                geometry.Boundary = o.Outline;
                geos.Add(geometry);
            });

            return geos;
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                var beamEngine = new ThBeamRecognitionEngine();
                beamEngine.Recognize(database, pts);
                Beams=beamEngine.Elements.Cast<ThIfcBeam>().ToList();
            }
            else
            {
                //
                throw new NotSupportedException();
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                Beams.ForEach(o => GroupOwner.Add(o.Outline, FindCurveGroupIds(groupId, o.Outline)));
            }
        }

        public override void Group2(Dictionary<Entity, string> groupId)
        {
            if (Group2Switch)
            {
                Beams.ForEach(o => Group2Owner.Add(o.Outline, FindCurveGroupIds(groupId, o.Outline)));
            }
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var beamIds = new ObjectIdList();
                Beams.ForEach(o =>
                {
                    o.Outline.ColorIndex = ColorIndex;
                    o.Outline.SetDatabaseDefaults();
                    beamIds.Add(db.ModelSpace.Add(o.Outline));
                });
                if (beamIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), beamIds);
                }
            }
        }
    }
}
