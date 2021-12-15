using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.BuildSecondaryBeam
{
    class BuildSecondaryBeam
    {
        private List<Line> Lines { get; set; }
        private DBObjectCollection Outlines { get; set; }
        public BuildSecondaryBeam(List<Line> lines, DBObjectCollection outlines)
        {
            Lines = lines;
            Outlines = outlines;
        }
        public List<Entity> Build()
        {
            List<Entity> result = new List<Entity>();
            Lines.ForEach(o =>
            {
                int B = Calculate(o).Item1;
                int H = Calculate(o).Item2;
                var outline = BuildLinearBeam(o.StartPoint, o.EndPoint, B);
                var beam = Difference(outline, Outlines);
                if (beam != null)
                {
                    result.Add(beam);
                }
            });
            return result;
        }
        private Entity Difference(Polyline outline, DBObjectCollection columns)
        {
            var objs = outline.Difference(columns);
            return objs.OfType<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
        }

        private Polyline BuildLinearBeam(Point3d start, Point3d end, int B)
        {
            return ThDrawTool.ToRectangle(start, end, B);
        }

        private Tuple<int, int> Calculate(Line SingleBeam)
        {
            double L = SingleBeam.Length;
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
    }
}
