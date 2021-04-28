﻿using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Temp
{
    public abstract class ThExtractorBase
    {
        public IElevationQuery IEleQuery { get; set; }
        public string Category { get; set; }
        public short ColorIndex { get; set; }
        public string ElementLayer { get; set; }

        public List<System.Type> Types { get; set; }
        protected Dictionary<Entity, List<string>> GroupOwner { get; set; }
        protected string IdPropertyName = "Id";
        protected string GroupIdPropertyName = "GroupId";
        protected string CodePropertyName = "Code";
        protected string NamePropertyName = "Name";
        protected string CategoryPropertyName = "Category";
        protected string AreaOwnerPropertyName = "AreaId";
        protected string IsolatePropertyName = "Isolated";
        protected string ElevationPropertyName = "Elevation";

        public ThExtractorBase()
        {
            Category = "";
            ElementLayer = "";
            GroupOwner = new Dictionary<Entity, List<string>>();
            Types = new List<System.Type>() { typeof(Polyline)};
        }
        protected string ToString(Polyline poly)
        {
            var pts = poly.Vertices();
            var str = "";
            for(int i= 0;i<pts.Count;i++)
            {
                str += ToString(pts[i]);
                if(i!= pts.Count-1)
                {
                    str += ",";
                }
            }
            return str;
        }
        protected string ToString(Point3d pt)
        {
            return pt.X + "," + pt.Y + "," + pt.Z;
        }

        protected virtual string BuildString(Dictionary<Entity, List<string>> owners, Entity curve)
        {
            if (owners.ContainsKey(curve))
            {
                return string.Join(";", owners[curve]);
            }
            return "";
        }

        protected List<string> FindCurveGroupIds(Dictionary<Entity, string> groupId, Entity curve)
        {
            var ids = new List<string>();
            var groups = groupId.Select(g => g.Key).ToList().Where(g => g.IsContains(curve)).ToList();
            groups.ForEach(g => ids.Add(groupId[g]));
            return ids;
        }     
        
        protected bool IsIsolate(List<ThTempSpace> spaces , Entity o)
        {
            foreach(var space in spaces)
            {
                if(space.Outline.IsFullContains(o))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
