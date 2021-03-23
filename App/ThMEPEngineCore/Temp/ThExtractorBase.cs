using System.Linq;
using System.Text;
using System.Collections.Generic;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public abstract class ThExtractorBase
    {
        public string Category { get; set; }
        public short ColorIndex { get; set; }
        public ThExtractorBase()
        {
            Category = "";
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
        protected virtual string BuildString(Dictionary<Curve,List<Polyline>> owners,Curve curve)
        {
            if(owners.ContainsKey(curve))
            {
                var points = new List<string>();
                foreach(Polyline poly in owners[curve])
                {
                    points.Add(ToString(poly));
                }
                if(points.Count>0)
                {
                    return string.Join(";", points.ToArray());
                }
            }
            return "";
        }
    }
}
