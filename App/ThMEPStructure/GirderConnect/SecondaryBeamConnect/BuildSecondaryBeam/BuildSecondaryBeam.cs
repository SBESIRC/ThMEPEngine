using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;

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
            List<Entity> tiltBeams = new List<Entity>();
            Lines.ForEach(o =>
            {
                int B = Calculate(o).Item1;
                int H = Calculate(o).Item2;
                var outline = BuildLinearBeam(o, B);
                var beam = Difference(outline, Outlines);
                beam.ForEach(i => i.Layer = o.Layer);
                if (beam != null)
                {
                    if (Math.Abs(o.Angle) % 90 < 0.01)
                    {
                        result.AddRange(beam);
                    }
                    else
                    {
                        tiltBeams.AddRange(beam);
                    }
                }
            });
            DBObjectCollection beams = new DBObjectCollection();
            result.ForEach(o => beams.Add(o));
            tiltBeams.ForEach(o =>
            {
                var beam = Difference(o as Polyline, beams);
                if (beam != null)
                {
                    result.Add(beam);
                }
            });
            return result;
        }
        private List<Polyline> Difference(List<Polyline> outline, DBObjectCollection columns)
        {
            List<Polyline> objs = new List<Polyline>();
            outline.ForEach(o =>
            {
                objs.Add(o.Difference(columns).OfType<Polyline>().OrderByDescending(i => i.Area).FirstOrDefault());
            });
            return objs;
        }
        private Polyline Difference(Polyline outline, DBObjectCollection columns)
        {
            Polyline objs = new Polyline();
            objs = outline.Difference(columns).OfType<Polyline>().OrderByDescending(i => i.Area).FirstOrDefault();
            return objs;
        }
        private List<Polyline> BuildLinearBeam(Line line, int B)
        {
            List<Polyline> result = new List<Polyline>();
            result.Add(line.GetOffsetCurves(B / 2).Cast<Polyline>().First());
            result.Add(line.GetOffsetCurves(-B / 2).Cast<Polyline>().First());

            return result;
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
