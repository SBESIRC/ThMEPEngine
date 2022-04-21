using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThMultipleMarkFilter
    {
        private DBObjectCollection BeamLines { get; set; } 
        private DBObjectCollection BeamTexts { get; set; }
        private ThCADCoreNTSSpatialIndex BeamLineSpatialIndex { get; set; }
        public DBObjectCollection Results { get; set; }

        public ThMultipleMarkFilter(
            DBObjectCollection beamLines, 
            DBObjectCollection beamTexts)
        {
            BeamLines = beamLines;
            BeamTexts = beamTexts;
            Results= new DBObjectCollection();
            BeamLineSpatialIndex = new ThCADCoreNTSSpatialIndex(beamLines);
        }
        public void Filter()
        {
            var filter1 = FilterHasCommonOppositeBeamLines(BeamTexts);
            var filter2 = FilterHasCommonNeibourBeamLines(BeamTexts.Difference(filter1));
            Results = Results.Union(filter1);
            Results = Results.Union(filter2);
        }
        private DBObjectCollection FilterHasCommonNeibourBeamLines(
           DBObjectCollection beamTexts)
        {
            var results = new DBObjectCollection();
            // 找到文字相邻的线
            var beamMarkNeibours = GetBeamMarkNeibourLines(beamTexts);

            // 把具有相邻线的文字找出来
            var beamLineNeibourTexts = GetMatchedDbTexts(beamMarkNeibours);

            // 移除多余的文字(对于梁线大于12000mm时，不合并文字)
            beamLineNeibourTexts
                .Where(o => o.Value.Count > 1)
                .Where(o=>o.Key.Length<= 12000.0)
                .ForEach(o =>
            {
                var res = Normalize(o.Key.StartPoint, o.Key.EndPoint);
                var sorts = o.Value
                .OfType<DBText>()
                .OrderBy(d => d.Position
                .GetProjectPtOnLine(res.Item1, res.Item2)
                .DistanceTo(res.Item1)).ToCollection();
                // 先保留第一个
                for (int i = 1; i < sorts.Count; i++)
                {
                    results.Add(sorts[i]);
                }
            });
            return results;
        }
        private DBObjectCollection FilterHasCommonOppositeBeamLines(
            DBObjectCollection beamTexts)
        {
            var results = new DBObjectCollection();
            // 找到文字对面的线
            var beamMarkOpposites = GetBeamMarkOppositeLines(BeamTexts);

            // 把具有相同对面线的文字找出来
            var beamLineOppositeTexts = GetMatchedDbTexts(beamMarkOpposites);

            // 移除多余的文字(对于梁线大于12000mm时，不合并文字)
            beamLineOppositeTexts
                .Where(o => o.Value.Count > 1)
                .Where(o => o.Key.Length <= 12000.0)
                .ForEach(o =>
            {
                var res = Normalize(o.Key.StartPoint, o.Key.EndPoint);
                var sorts = o.Value
                .OfType<DBText>()
                .OrderBy(d => d.Position
                .GetProjectPtOnLine(res.Item1, res.Item2)
                .DistanceTo(res.Item1)).ToCollection();
                // 先保留第一个
                for (int i = 1; i < sorts.Count; i++)
                {
                    results.Add(sorts[i]);
                }
            });
            return results;
        }

        private Tuple<Point3d, Point3d> Normalize(Point3d lineSp,Point3d lineEp)
        {
            if(lineSp.X< lineEp.X)
            {
                return Tuple.Create(lineSp, lineEp);
            }
            else if(lineEp.X < lineSp.X)
            {
                return Tuple.Create(lineEp, lineSp);
            }
            else
            {
                if(lineSp.Y< lineEp.Y)
                {
                    return Tuple.Create(lineSp, lineEp);
                }
                else
                {
                    return Tuple.Create(lineEp, lineSp);
                }
            }
        }

        private Dictionary<Line,DBObjectCollection> GetMatchedDbTexts(
            Dictionary<DBText, DBObjectCollection> beamMarkOpposites)
        {
            var results = new Dictionary<Line, DBObjectCollection>();
            var totalBeamLines = new DBObjectCollection();
            beamMarkOpposites.ForEach(o => totalBeamLines = totalBeamLines.Union(o.Value));
            totalBeamLines = totalBeamLines.Distinct();
            totalBeamLines.OfType<Line>().ForEach(l =>
            {
                var texts = new DBObjectCollection();
                foreach(var item in beamMarkOpposites)
                {
                    if(item.Value.Contains(l))
                    {
                        texts.Add(item.Key);
                    }
                }
                results.Add(l, texts);
            });
            return results;
        }
        private Dictionary<DBText, DBObjectCollection> GetBeamMarkNeibourLines(DBObjectCollection texts)
        {
            /*
             *  400x200     400x200     400x200
             * ----------------------------------         
             */
            var results = new Dictionary<DBText, DBObjectCollection>();
            texts
                .OfType<DBText>()
                .Where(o => IsValidSpec(o.TextString))
                .ForEach(e =>
                {
                    var outline = e.TextOBB();
                    var center = outline.GetPoint3dAt(0).GetMidPt(outline.GetPoint3dAt(2));
                    var textDir = Vector3d.XAxis.RotateBy(e.Rotation, Vector3d.ZAxis);
                    var perpendVec = textDir.GetPerpendicularVector();
                    var height = GetMinWidth(outline);
                    if (height < e.Height)
                    {
                        height = e.Height;
                    }
                    var lineSp = center + perpendVec.MultiplyBy(height * 0.6);
                    var lineEp = center - perpendVec.MultiplyBy(height * 0.6);
                    var envelop1 = ThDrawTool.ToRectangle(lineSp, lineEp, 1.0);
                    var lines = QueryBeams(envelop1);
                    var parallels = FindParallelLines(lines, e.Rotation);
                    results.Add(e, parallels);
                    outline.Dispose();
                    envelop1.Dispose();
                });
            return results;
        }
        private Dictionary<DBText, DBObjectCollection> GetBeamMarkOppositeLines(DBObjectCollection texts)
        {
            /*
             *  400x200     400x200     400x200
             * ---------- ----------- -----------
             * 
             * ----------------------------------          
             */
            var results = new Dictionary<DBText, DBObjectCollection>();
            texts
                .OfType<DBText>()
                .Where(o => IsValidSpec(o.TextString))
                .ForEach(e =>
                {
                    var outline = e.TextOBB();
                    var center = outline.GetPoint3dAt(0).GetMidPt(outline.GetPoint3dAt(2));
                    var textDir = Vector3d.XAxis.RotateBy(e.Rotation, Vector3d.ZAxis);
                    var perpendVec = textDir.GetPerpendicularVector();
                    var height = GetMinWidth(outline);
                    if(height< e.Height)
                    {
                        height = e.Height;
                    }
                    var lineSp = center + perpendVec.MultiplyBy(height * 0.6);
                    var lineEp = center - perpendVec.MultiplyBy(height * 0.6);
                    var envelop1 = ThDrawTool.ToRectangle(lineSp, lineEp, 1.0);
                    var lines = QueryBeams(envelop1);
                    var parallels = FindParallelLines(lines, e.Rotation);
                    if (parallels.Count > 0)
                    {
                        var closest = parallels.OfType<Line>()
                        .OrderBy(o => center.DistanceTo(o.GetClosestPointTo(center, false))).First();
                        var beamWidth = GetBeamWidth(e.TextString);
                        var closePt = closest.GetClosestPointTo(center, false);
                        var direction = center.GetVectorTo(closePt).GetNormal();
                        var oppositePt = closePt + direction.MultiplyBy(beamWidth);
                        var envelop2 = ThDrawTool.CreateSquare(oppositePt, 1.0);
                        var oppositeLines = QueryBeams(envelop2);
                        results.Add(e,oppositeLines.OfType<Line>()
                            .Where(o => closest.IsParallelToEx(o)).ToCollection());
                        envelop2.Dispose();
                    }
                    else
                    {
                        // for test
                    }
                    outline.Dispose();
                    envelop1.Dispose();
                });
            return results;
        }
        private double GetMinWidth(Polyline rectangle)
        {
            if(rectangle.NumberOfVertices>2)
            {
                return Math.Min(rectangle.GetLineSegmentAt(0).Length, rectangle.GetLineSegmentAt(1).Length);
            }
            else
            {
                return 0.0;
            }
        }
        private bool IsValidSpec(string beamSpec)
        {
            var newSpec = beamSpec.Replace(" ", "");
            var pattern = @"^\d+[x]{1}\d+";
            return Regex.IsMatch(newSpec, pattern);
        }

        private double GetBeamWidth(string beamSpec)
        {
            string[] values = beamSpec.Split('x');
            if(values.Length==2)
            {
                return double.Parse(values[0]);
            }
            else
            {
                return 0.0;
            }
        }

        private DBObjectCollection FindParallelLines(DBObjectCollection lines,double rad)
        {
            var ang = rad.RadToAng();
            return lines
                .OfType<Line>()
                .Where(o => IsCloseParallel(o.Angle.RadToAng(), ang))
                .ToCollection();
        }

        private bool IsCloseParallel(double firstAng,double secondAng, double angTolerance=1.0)
        {
            firstAng = firstAng % 180.0;
            secondAng = secondAng % 180.0;
            var minus = Math.Abs(firstAng - secondAng);
            return minus <= angTolerance || Math.Abs(180.0 - minus) <= angTolerance;
        }

        private DBObjectCollection QueryBeams(Polyline outline)
        {
            return BeamLineSpatialIndex.SelectCrossingPolygon(outline);
        }
             
        private Polyline Buffer(Polyline outline,double length)
        {
            var res = outline
                .Buffer(length)
                .OfType<Polyline>()
                .OrderByDescending(p => p.Area);
            if(res.Count()>0)
            {
                return res.First();
            }
            else
            {
                return new Polyline();
            }
        }
    }
}
