using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public abstract class ThExtractorBase
    {
        public string Category { get; set; }
        public short ColorIndex { get; set; }
        protected Dictionary<Curve, List<string>> GroupOwner { get; set; }
        protected string IdPropertyName = "Id";
        protected string CodePropertyName = "Code";
        protected string NamePropertyName = "Name";
        protected string CategoryPropertyName = "Category";
        protected string GroupOwnerPropertyName = "GroupOwner";
        public ThExtractorBase()
        {
            Category = "";
            GroupOwner = new Dictionary<Curve, List<string>>();
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

        protected virtual string BuildString(Dictionary<Curve, List<string>> owners, Curve curve)
        {
            if (owners.ContainsKey(curve))
            {
                return string.Join(";", owners[curve]);
            }
            return "";
        }

        protected List<string> FindCurveGroupIds(Dictionary<Polyline, string> groupId,Curve curve)
        {
            var ids = new List<string>();
            var groups = groupId.Select(g => g.Key).ToList().Where(g => g.Contains(curve)).ToList();
            groups.ForEach(g => ids.Add(groupId[g]));
            return ids;
        }
    }
}
