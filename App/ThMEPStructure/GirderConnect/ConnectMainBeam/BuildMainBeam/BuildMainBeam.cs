using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.BuildMainBeam
{
    public class BuildMainBeam
    {
        private Dictionary<Line, DBObjectCollection> LineDict { get; set; }
        public BuildMainBeam(Dictionary<Line, DBObjectCollection> lineDict)
        {
            LineDict = lineDict;
        }
        public List<Entity> Build(string Switch)
        {
            List<Entity> result = new List<Entity>();
            LineDict.ForEach(o =>
            {
                int B = Calculate(o.Key, Switch).Item1;
                int H = Calculate(o.Key, Switch).Item2;
                var outline = BuildLinearBeam(o.Key.StartPoint, o.Key.EndPoint, B);
                var beam = Difference(outline, o.Value);
                if(beam != null)
                {
                    result.Add(beam);
                }
            });
            return result;
        }
        private Entity Difference(Polyline outline, DBObjectCollection columns)
        {
            var objs = outline.Difference(columns);
            return objs.OfType<Polyline>().OrderByDescending(o=>o.Area).FirstOrDefault();
        }

        private Polyline BuildLinearBeam(Point3d start,Point3d end,int B)
        {
            return ThDrawTool.ToRectangle(start, end, B);
        }
 
        private Tuple<int, int> Calculate(Line SingleBeam, string Switch)
        {
            double L = SingleBeam.Length;
            if (Switch is "地下室顶板")
            {
                int H = Math.Max(500, Convert.ToInt32(L / 500) * 50);
                int B = Math.Max(200, Convert.ToInt32(H / 100) * 50);
                return (B, H).ToTuple();
            }
            else if (Switch is "地下室中板")
            {
                int H = Math.Max(300, Convert.ToInt32(L / 750) * 50);
                int B = H / 3;
                if (B % 50 == 0)
                {
                    B = Math.Max(200, B);
                }
                else
                {
                    B = Math.Max(200, Convert.ToInt32(B / 50) * 50 + 50);
                }
                return (B, H).ToTuple();
            }
            return null;
        }
        private Tuple<int, int> CalculateSecond(Line SingleBeam, string Switch)
        {
            double L = SingleBeam.Length;
            if (Switch is "地下室顶板")
            {
                int H = Math.Max(500, Convert.ToInt32(L / 500) * 50);
                int B = Math.Max(200, Convert.ToInt32(H / 100) * 50);
                return (B, H).ToTuple();
            }
            else if(Switch is "地下室中板")
            {
                int H = Math.Max(300, Convert.ToInt32(L / 500) * 50);
                int B = Math.Max(200, Convert.ToInt32(H / 100) * 50);
                return (B, H).ToTuple();
            }

            return null;
        }
    }
}