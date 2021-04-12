using System.Linq;
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
        public string Category { get; set; }
        public short ColorIndex { get; set; }
        public string ElementLayer { get; set; }
        protected Dictionary<Entity, List<string>> GroupOwner { get; set; }
        protected string IdPropertyName = "Id";
        protected string CodePropertyName = "Code";
        protected string NamePropertyName = "Name";
        protected string CategoryPropertyName = "Category";
        protected string GroupOwnerPropertyName = "GroupId";
        protected string IsolatePropertyName = "Isolated";
        public ThExtractorBase()
        {
            Category = "";
            ElementLayer = "";
            GroupOwner = new Dictionary<Entity, List<string>>();
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
