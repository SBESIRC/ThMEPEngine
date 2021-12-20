using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Method;
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
        public Dictionary<Tuple<Line, Line>, DBText> Build()
        {
            Dictionary<Tuple<Line, Line>, DBText> result = new Dictionary<Tuple<Line, Line>, DBText>();
            var code = new DBText();
            Lines.ForEach(o =>
            {
                int B = Calculate(o).Item1;
                int H = Calculate(o).Item2;
                code = AddText(o, B, H);
                var outline = BuildLinearBeam(o, B);
                var beam = Difference(outline, Outlines);
                if (!beam.Item1.IsNull())
                {
                    result.Add(beam, code);
                }
            });
            
            return result;
        }
        private Tuple<Line, Line> Difference(Tuple<Line, Line> outline, DBObjectCollection columns)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(columns);
            var querysItem1 = spatialIndex.SelectFence(outline.Item1);
            var querysItem2 = spatialIndex.SelectFence(outline.Item2);
            var ptsCollection = new List<Point3d>();
            querysItem1.Cast<Entity>().ToList().ForEach(o =>
            {
                var pts = new Point3dCollection();
                outline.Item1.IntersectWith(o, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    ptsCollection.Add(pts.Cast<Point3d>().First());
                }
            });
            Line Item1 = new Line();
            ptsCollection.Add(outline.Item1.StartPoint);
            ptsCollection.Add(outline.Item1.EndPoint);
            var Spts = ptsCollection.Where(o => o.DistanceTo(outline.Item1.StartPoint) < o.DistanceTo(outline.Item1.EndPoint));
            var Epts = ptsCollection.Except(Spts);
            Point3d centerPt = outline.Item1.GetMiddlePt();
            Item1 = new Line(Spts.OrderBy(o => o.DistanceTo(centerPt)).First(), Epts.OrderBy(o => o.DistanceTo(centerPt)).First());
            ptsCollection.Clear();
            querysItem2.Cast<Entity>().ToList().ForEach(o =>
            {
                var pts = new Point3dCollection();
                outline.Item2.IntersectWith(o, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    ptsCollection.Add(pts.Cast<Point3d>().First());
                }
            });
            Line Item2 = new Line();
            ptsCollection.Add(outline.Item2.StartPoint);
            ptsCollection.Add(outline.Item2.EndPoint);
            Spts = ptsCollection.Where(o => o.DistanceTo(outline.Item2.StartPoint) < o.DistanceTo(outline.Item2.EndPoint));
            Epts = ptsCollection.Except(Spts);
            centerPt = outline.Item2.GetMiddlePt();
            Item2 = new Line(Spts.OrderBy(o => o.DistanceTo(centerPt)).First(), Epts.OrderBy(o => o.DistanceTo(centerPt)).First());

            return (Item1, Item2).ToTuple();
        }
        private Tuple<Line, Line> BuildLinearBeam(Line line, int B)
        {
            return (line.GetOffsetCurves(B / 2).OfType<Line>().First(),
                    line.GetOffsetCurves(-B / 2).OfType<Line>().First()).ToTuple();
        }
        private DBText AddText(Line line, int B, int H)
        {
            var newLine = line.Normalize();
            DBText code = new DBText();
            code.TextString = B + "×" + H;
            var basePt = newLine.StartPoint.GetMidPt(newLine.EndPoint);
            double angle = newLine.Angle / Math.PI * 180.0; //rad->ang
            var alignPt = new Point3d();
            if (Math.Abs(angle - 90) <= 1.0 || Math.Abs(angle - 270) <= 1.0)
            {
                if (newLine.StartPoint.Y < newLine.EndPoint.Y)
                {
                    alignPt = basePt + newLine.StartPoint.GetVectorTo(newLine.EndPoint)
                    .GetPerpendicularVector()
                    .GetNormal()
                    .MultiplyBy(B / 2 + 50 + 375 / 2.0);
                }
                else
                {
                    alignPt = basePt + newLine.EndPoint.GetVectorTo(newLine.StartPoint)
                    .GetPerpendicularVector()
                    .GetNormal()
                    .MultiplyBy(B / 2 + 50 + 375 / 2.0);
                }
            }
            else
            {
                alignPt = basePt + newLine.StartPoint.GetVectorTo(newLine.EndPoint)
                .GetPerpendicularVector()
                .GetNormal()
                .MultiplyBy(B / 2 + 50 + 375 / 2.0);
            }
            code.Height = 375;
            code.WidthFactor = 0.65;
            code.Position = alignPt;
            angle = AdjustAngle(angle);
            angle = angle / 180.0 * Math.PI;
            code.Rotation = angle;
            code.HorizontalMode = TextHorizontalMode.TextCenter;
            code.VerticalMode = TextVerticalMode.TextVerticalMid;
            code.AlignmentPoint = code.Position;

            return code;
        }
        private double AdjustAngle(double angle)
        {
            var result = angle;
            angle = angle % 180.0;
            if (angle <= 1.0)
            {
                result = 0.0;
            }
            else if (Math.Abs(angle - 180.0) <= 1.0)
            {
                result = 0.0;
            }
            else if (Math.Abs(angle - 90.0) <= 1.0)
            {
                result = 90.0;
            }
            else if (Math.Abs(angle - 270.0) <= 1.0)
            {
                result = 90.0;
            }
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
