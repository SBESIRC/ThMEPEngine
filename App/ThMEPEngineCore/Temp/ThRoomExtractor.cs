using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;

namespace ThMEPEngineCore.Temp
{
    public class ThRoomExtractor : ThExtractorBase, IExtract, IBuildGeometry, IPrint, IGroup
    {
        public List<ThIfcRoom> Rooms { get; set; }
        public ThRoomExtractor()
        {
            Rooms = new List<ThIfcRoom>();           
        }
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Rooms.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o.Boundary));
                }
                if(Group2Switch)
                {
                    geometry.Properties.Add(Group2IdPropertyName, BuildString(Group2Owner, o.Boundary));
                }
                geometry.Boundary = o.Boundary;
                geos.Add(geometry);
            });

            return geos;
        }

        public void Extract(Database database, Point3dCollection pts)
        {
            if (UseDb3Engine)
            {
                throw new NotSupportedException();
            }
            else
            {
                var roomBuidler = new ThRoomBuilderEngine();
                Rooms = roomBuidler.BuildFromMS(database, pts);               
            }
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                Rooms.ForEach(o => GroupOwner.Add(o.Boundary, FindCurveGroupIds(groupId, o.Boundary)));
            }
        }

        public override void Group2(Dictionary<Entity, string> groupId)
        {
            if (Group2Switch)
            {
                Rooms.ForEach(o => Group2Owner.Add(o.Boundary, FindCurveGroupIds(groupId, o.Boundary)));
            }
        }

        public void Print(Database database)
        {
            Rooms.Select(o=>o.Boundary).ToList().CreateGroup(database, ColorIndex);
        }
    }
}
