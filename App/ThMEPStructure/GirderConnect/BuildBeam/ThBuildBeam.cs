using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPStructure.GirderConnect.Data;

namespace ThMEPStructure.GirderConnect.BuildBeam
{
    class ThBuildBeam
    {
        private List<Line> MainBeams { get; set; }            //无次梁搁置主梁
        private List<Line> SecondaryMainBeams { get; set; }   //有次梁搁置主梁
        private List<Line> SecondaryBeams { get; set; }       //次梁
        private DBObjectCollection Outlines { get; set; }     //障碍物
        private const int LMax = 9000;
        public ThBuildBeam(List<Line> mainBeams, List<Line> secondaryMainBeams, List<Line> secondaryBeams, DBObjectCollection outlines)
        {
            MainBeams = mainBeams;
            SecondaryMainBeams = secondaryMainBeams;
            SecondaryBeams = secondaryBeams;
            Outlines = outlines;
        }
        public Dictionary<Tuple<Line, Line>, DBText> build(string Switch)
        {
            Dictionary<Tuple<Line, Line>, DBText> results = new Dictionary<Tuple<Line, Line>, DBText>();
            var code = new DBText();
            MainBeams.ForEach(o =>
            {
                int B = CalculateMainBeams(o, Switch).Item1;
                int H = CalculateMainBeams(o, Switch).Item2;
                code = AddText(o, B, H);
                var outline = BuildLinearBeam(o, B);
                var beam = Difference(outline, Outlines);
                if (!beam.Item1.IsNull())
                {
                    beam.Item1.Layer = BeamConfig.MainBeamLayerName;
                    beam.Item2.Layer = BeamConfig.MainBeamLayerName;
                    beam.Item1.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item2.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item1.Linetype = "ByLayer";
                    beam.Item2.Linetype = "ByLayer";
                    code.Layer = BeamConfig.MainBeamTextLayerName;
                    code.ColorIndex = (int)ColorIndex.BYLAYER;
                    code.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    results.Add(beam, code);
                }
            });
            var SecondaryBeamsData = getSecondaryMainBeams();
            var objs = new DBObjectCollection();
            SecondaryBeams.ForEach(o => objs.Add(o));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            SecondaryMainBeams.ForEach(o =>
            {
                int B = CalculateSecondaryMainBeams(o, Switch).Item1;
                int H = CalculateSecondaryMainBeams(o, Switch).Item2;
                var query = spatialIndex.SelectFence(o);
                query.Cast<Entity>().ToList().ForEach(i =>
                {
                    if (SecondaryBeamsData[i as Line].Item2 +50 > H)
                    {
                        H = SecondaryBeamsData[i as Line].Item2 + 50;
                    }
                });
                if (B < H / 4)
                {
                    B = H / 4;
                    if (B % 50 != 0)
                    {
                        B = Convert.ToInt32(B / 50) * 50 + 50;
                    }
                }
                code = AddText(o, B, H);
                var outline = BuildLinearBeam(o, B);
                var beam = Difference(outline, Outlines);
                if (!beam.Item1.IsNull())
                {
                    beam.Item1.Layer = BeamConfig.MainBeamLayerName;
                    beam.Item2.Layer = BeamConfig.MainBeamLayerName;
                    beam.Item1.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item2.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item1.Linetype = "ByLayer";
                    beam.Item2.Linetype = "ByLayer";
                    code.Layer = BeamConfig.MainBeamTextLayerName;
                    code.ColorIndex = (int)ColorIndex.BYLAYER;
                    code.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    results.Add(beam, code);
                }
            });
            results.ToList().ForEach(o =>
            {
                Outlines.Add(o.Key.Item1);
                Outlines.Add(o.Key.Item2);
            });
            SecondaryBeams.ForEach(o =>
            {
                code = AddText(o, SecondaryBeamsData[o].Item1, SecondaryBeamsData[o].Item2);
                var outline = BuildLinearBeam(o, SecondaryBeamsData[o].Item1);
                var beam = Difference(outline, Outlines);
                if (!beam.Item1.IsNull())
                {
                    beam.Item1.Layer = BeamConfig.SecondaryBeamLayerName;
                    beam.Item2.Layer = BeamConfig.SecondaryBeamLayerName;
                    beam.Item1.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item2.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item1.Linetype = "ByLayer";
                    beam.Item2.Linetype = "ByLayer";
                    code.Layer = BeamConfig.SecondaryBeamTextLayerName;
                    code.ColorIndex = (int)ColorIndex.BYLAYER;
                    code.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
                    results.Add(beam, code);
                }
            });

            return results;
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
            Point3d centerPt = outline.Item1.GetMidpoint();
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
            centerPt = outline.Item2.GetMidpoint();
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
        //计算无次梁搁置主梁BH
        private Tuple<int, int> CalculateMainBeams(Line SingleBeam, string Switch)
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
        //计算有次梁搁置主梁
        private Tuple<int, int> CalculateSecondaryMainBeams(Line SingleBeam, string Switch)
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
                int H = Math.Max(300, Convert.ToInt32(L / 500) * 50);
                int B = Math.Max(200, Convert.ToInt32(H / 100) * 50);
                return (B, H).ToTuple();
            }

            return null;
        }
        //计算次梁BH
        private Tuple<int, int> CalculateSecondaryBeams(Line SingleBeam)
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
        private Dictionary<Line, Tuple<int, int>> getSecondaryMainBeams()
        {
            var results = new Dictionary<Line, Tuple<int, int>>();
            SecondaryBeams.ForEach(o =>
            {
                int B = CalculateSecondaryBeams(o).Item1;
                int H = CalculateSecondaryBeams(o).Item2;
                results.Add(o, (B, H).ToTuple());
            });

            return results;
        }
    }
}
