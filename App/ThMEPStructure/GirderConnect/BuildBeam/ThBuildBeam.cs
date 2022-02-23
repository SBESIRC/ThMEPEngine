using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
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
            SecondaryBeams = secondaryBeams.Select(o => o.ExtendLine(10)).ToList();
            Outlines = outlines;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Switch">1.地下室顶板 2.地下室中板</param>
        /// <returns></returns>
        public Dictionary<Tuple<Line, Line>, DBText> build(int Switch)
        {
            Dictionary<Tuple<Line, Line>, DBText> results = new Dictionary<Tuple<Line, Line>, DBText>();
            //構建单梁线和高度的字典
            Dictionary<Line, int> LineDic = new Dictionary<Line, int>();
            var code = new DBText();
            MainBeams.ForEach(o =>
            {
                var MainBeamsInfo = CalculateMainBeams(o, Switch);
                int B = MainBeamsInfo.Item1;
                int H = MainBeamsInfo.Item2;
                code = AddText(o, B, H);
                var outline = BuildLinearBeam(o, B);
                var beam = Difference(outline, Outlines);
                if (!beam.Item1.IsNull())
                {
                    results.Add(beam, code);
                    LineDic.Add(beam.Item1, H);
                    LineDic.Add(beam.Item2, H);
                }
            });
            var SecondaryBeamsData = getSecondaryMainBeams();
            var objs = new DBObjectCollection();
            SecondaryBeams.ForEach(o => objs.Add(o));
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            SecondaryMainBeams.ForEach(o =>
            {
                var SecondaryMainBeamsInfo = CalculateSecondaryMainBeams(o);
                int B = SecondaryMainBeamsInfo.Item1;
                int H = SecondaryMainBeamsInfo.Item2;
                var query = spatialIndex.SelectFence(o);
                if (BuildBeamLayoutConfig.BeamCheckSelection == 1)
                {
                    query.Cast<Entity>().ToList().ForEach(i =>
                    {
                        if (SecondaryBeamsData[i as Line].Item2 + BuildBeamLayoutConfig.BeamCheck > H)
                        {
                            H = SecondaryBeamsData[i as Line].Item2 + 50;
                        }
                    });
                    if (B < H / 4)
                    {
                        B = (int)Math.Floor(1.0 * H / 4 / 50) * 50;
                    }
                }
                code = AddText(o, B, H);
                var outline = BuildLinearBeam(o, B);
                var beam = Difference(outline, Outlines);
                if (!beam.Item1.IsNull())
                {
                    beam.Item1.Layer = BeamConfig.UniteBeamLayerName;
                    beam.Item2.Layer = BeamConfig.UniteBeamLayerName;
                    beam.Item1.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item2.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item1.Linetype = "ByLayer";
                    beam.Item2.Linetype = "ByLayer";
                    code.Layer = BeamConfig.UniteBeamTextLayerName;
                    code.ColorIndex = (int)ColorIndex.BYLAYER;
                    results.Add(beam, code);
                    LineDic.Add(beam.Item1, H);
                    LineDic.Add(beam.Item2, H);
                }
            });
            results = handleConflict(results, LineDic);

            results.ForEach(o =>
            {
                o.Key.Item1.Layer = BeamConfig.UniteBeamLayerName;
                o.Key.Item2.Layer = BeamConfig.UniteBeamLayerName;
                o.Key.Item1.ColorIndex = (int)ColorIndex.BYLAYER;
                o.Key.Item2.ColorIndex = (int)ColorIndex.BYLAYER;
                o.Key.Item1.Linetype = "ByLayer";
                o.Key.Item2.Linetype = "ByLayer";
                o.Value.Layer = BeamConfig.UniteBeamTextLayerName;
                o.Value.ColorIndex = (int)ColorIndex.BYLAYER;
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
                    beam.Item1.Layer = BeamConfig.UniteBeamLayerName;
                    beam.Item2.Layer = BeamConfig.UniteBeamLayerName;
                    beam.Item1.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item2.ColorIndex = (int)ColorIndex.BYLAYER;
                    beam.Item1.Linetype = "ByLayer";
                    beam.Item2.Linetype = "ByLayer";
                    code.Layer = BeamConfig.UniteBeamTextLayerName;
                    code.ColorIndex = (int)ColorIndex.BYLAYER;
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
        private Tuple<int, int> CalculateMainBeams(Line SingleBeam, int Switch)
        {
            double L = SingleBeam.Length;
            int H = 0, B = 0;
            if (Switch == 1)
            {
                //经验公式估算
                if (BuildBeamLayoutConfig.EstimateSelection == 1)
                {
                    H = Math.Max(BuildBeamLayoutConfig.FormulaTop.Hmin, (int)Math.Floor(L / BuildBeamLayoutConfig.FormulaTop.LDividesH / 50) * 50);
                    B = Math.Max(BuildBeamLayoutConfig.FormulaTop.Bmin, (int)Math.Floor(1.0 * H / BuildBeamLayoutConfig.FormulaTop.HDividesB / 50) * 50);
                }
                //经验表格估算
                else
                {
                    if(L <= 4000)
                    {
                        H = BuildBeamLayoutConfig.TableTop1.H;
                        B = BuildBeamLayoutConfig.TableTop1.B;
                    }
                    else if(L <= 6000)
                    {
                        H = BuildBeamLayoutConfig.TableTop2.H;
                        B = BuildBeamLayoutConfig.TableTop2.B;
                    }
                    else if(L <= 7000)
                    {
                        H = BuildBeamLayoutConfig.TableTop3.H;
                        B = BuildBeamLayoutConfig.TableTop3.B;
                    }
                    else if (L <= 8000)
                    {
                        H = BuildBeamLayoutConfig.TableTop4.H;
                        B = BuildBeamLayoutConfig.TableTop4.B;
                    }
                    else if (L <= 9000)
                    {
                        H = BuildBeamLayoutConfig.TableTop5.H;
                        B = BuildBeamLayoutConfig.TableTop5.B;
                    }
                    else if (L <= 10000)
                    {
                        H = BuildBeamLayoutConfig.TableTop6.H;
                        B = BuildBeamLayoutConfig.TableTop6.B;
                    }
                    else
                    {
                        H = BuildBeamLayoutConfig.TableTop7.H;
                        B = BuildBeamLayoutConfig.TableTop7.B;
                    }
                }
            }
            else if (Switch == 2)
            {
                if (BuildBeamLayoutConfig.EstimateSelection == 1)
                {
                    H = Math.Max(BuildBeamLayoutConfig.FormulaMiddleB.Hmin, (int)Math.Floor(L / BuildBeamLayoutConfig.FormulaMiddleB.LDividesH / 50) * 50);
                    B = Math.Max(BuildBeamLayoutConfig.FormulaMiddleB.Bmin, (int)Math.Floor(1.0 * H / BuildBeamLayoutConfig.FormulaMiddleB.HDividesB / 50) * 50);
                }
                else
                {
                    if (L <= 6000)
                    {
                        H = BuildBeamLayoutConfig.TableMiddleB1.H;
                        B = BuildBeamLayoutConfig.TableMiddleB1.B;
                    }
                    else if (L <= 7000)
                    {
                        H = BuildBeamLayoutConfig.TableMiddleB2.H;
                        B = BuildBeamLayoutConfig.TableMiddleB2.B;
                    }
                    else if (L <= 8000)
                    {
                        H = BuildBeamLayoutConfig.TableMiddleB3.H;
                        B = BuildBeamLayoutConfig.TableMiddleB3.B;
                    }
                    else if (L <= 9000)
                    {
                        H = BuildBeamLayoutConfig.TableMiddleB4.H;
                        B = BuildBeamLayoutConfig.TableMiddleB4.B;
                    }
                    else if (L <= 10000)
                    {
                        H = BuildBeamLayoutConfig.TableMiddleB5.H;
                        B = BuildBeamLayoutConfig.TableMiddleB5.B;
                    }
                    else
                    {
                        H = BuildBeamLayoutConfig.TableMiddleB6.H;
                        B = BuildBeamLayoutConfig.TableMiddleB6.B;
                    }
                }
            }
            return (B, H).ToTuple();
        }
        //计算有次梁搁置主梁
        private Tuple<int, int> CalculateSecondaryMainBeams(Line SingleBeam)
        {
            double L = SingleBeam.Length;
            int H = 0, B = 0;
            //经验公式估算
            if (BuildBeamLayoutConfig.EstimateSelection == 1)
            {
                H = Math.Max(BuildBeamLayoutConfig.FormulaMiddleA.Hmin, (int)Math.Floor(L / BuildBeamLayoutConfig.FormulaMiddleA.LDividesH / 50) * 50);
                B = Math.Max(BuildBeamLayoutConfig.FormulaMiddleA.Bmin, (int)Math.Floor(1.0 * H / BuildBeamLayoutConfig.FormulaMiddleA.HDividesB / 50) * 50);
            }
            //经验表格估算
            else
            {
                if (L <= 6000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleA1.H;
                    B = BuildBeamLayoutConfig.TableMiddleA1.B;
                }
                else if (L <= 7000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleA2.H;
                    B = BuildBeamLayoutConfig.TableMiddleA2.B;
                }
                else if (L <= 8000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleA3.H;
                    B = BuildBeamLayoutConfig.TableMiddleA3.B;
                }
                else if (L <= 9000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleA4.H;
                    B = BuildBeamLayoutConfig.TableMiddleA4.B;
                }
                else if (L <= 10000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleA5.H;
                    B = BuildBeamLayoutConfig.TableMiddleA5.B;
                }
                else
                {
                    H = BuildBeamLayoutConfig.TableMiddleA6.H;
                    B = BuildBeamLayoutConfig.TableMiddleA6.B;
                }
            }
            return (B, H).ToTuple();
        }
        //计算次梁BH
        private Tuple<int, int> CalculateSecondaryBeams(Line SingleBeam)
        {
            double L = SingleBeam.Length;
            int H = 0, B = 0;
            if (BuildBeamLayoutConfig.EstimateSelection == 1)
            {
                H = Math.Max(BuildBeamLayoutConfig.FormulaMiddleSecondary.Hmin, (int)Math.Floor(L / BuildBeamLayoutConfig.FormulaMiddleSecondary.LDividesH / 50) * 50);
                B = Math.Max(BuildBeamLayoutConfig.FormulaMiddleSecondary.Bmin, (int)Math.Floor(1.0 * H / BuildBeamLayoutConfig.FormulaMiddleSecondary.HDividesB / 50) * 50);
            }
            else
            {
                if (L <= 6000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleB1.H;
                    B = BuildBeamLayoutConfig.TableMiddleB1.B;
                }
                else if (L <= 7000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleB2.H;
                    B = BuildBeamLayoutConfig.TableMiddleB2.B;
                }
                else if (L <= 8000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleB3.H;
                    B = BuildBeamLayoutConfig.TableMiddleB3.B;
                }
                else if (L <= 9000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleB4.H;
                    B = BuildBeamLayoutConfig.TableMiddleB4.B;
                }
                else if (L <= 10000)
                {
                    H = BuildBeamLayoutConfig.TableMiddleB5.H;
                    B = BuildBeamLayoutConfig.TableMiddleB5.B;
                }
                else
                {
                    H = BuildBeamLayoutConfig.TableMiddleB6.H;
                    B = BuildBeamLayoutConfig.TableMiddleB6.B;
                }
            }
            return (B, H).ToTuple();
        }
        private Dictionary<Line, Tuple<int, int>> getSecondaryMainBeams()
        {
            var results = new Dictionary<Line, Tuple<int, int>>();
            SecondaryBeams.ForEach(o =>
            {
                var SecondaryBeamsInfo = CalculateSecondaryBeams(o);
                int B = SecondaryBeamsInfo.Item1;
                int H = SecondaryBeamsInfo.Item2;
                results.Add(o, (B, H).ToTuple());
            });

            return results;
        }
        private Dictionary<Tuple<Line, Line>, DBText> handleConflict(Dictionary<Tuple<Line, Line>, DBText> mainBeams, Dictionary<Line, int> LineDic)
        {
            var results = new Dictionary<Tuple<Line, Line>, DBText>();
            var objs = new DBObjectCollection();
            var objs2 = new DBObjectCollection();
            mainBeams.ForEach(o =>
            {
                objs.Add(o.Key.Item1);
                objs.Add(o.Key.Item2);
            });
            Outlines.Cast<Entity>().ForEach(o =>
            {
                objs2.Add(o);
            });
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var spatialIndex2 = new ThCADCoreNTSSpatialIndex(objs2);
            mainBeams.ForEach(o =>
            {
                var querysItem1 = spatialIndex.SelectFence(o.Key.Item1);
                querysItem1.Remove(o.Key.Item1);
                querysItem1.Cast<Line>().ToList().ForEach(i => 
                {
                    if (LineDic[o.Key.Item1] > LineDic[i])
                    {
                        querysItem1.Remove(i);
                    }
                    else if (LineDic[o.Key.Item1] == LineDic[i])
                    {
                        if (IsMainBeam(i, spatialIndex2))
                        {
                            querysItem1.Remove(i);
                        }
                        else if (!IsMainBeam(i, spatialIndex2) && !IsMainBeam(o.Key.Item1, spatialIndex2))
                        {
                            querysItem1.Clear();
                        }
                    }
                });
                var querysItem2 = spatialIndex.SelectFence(o.Key.Item2);
                querysItem2.Remove(o.Key.Item2);
                querysItem2.Cast<Line>().ToList().ForEach(i =>
                {
                    if (LineDic[o.Key.Item2] > LineDic[i])
                    {
                        querysItem2.Remove(i);
                    }
                    else if (LineDic[o.Key.Item2] == LineDic[i])
                    {
                        if (IsMainBeam(i, spatialIndex2))
                        {
                            querysItem2.Remove(i);
                        }
                        else if (!IsMainBeam(i, spatialIndex2) && !IsMainBeam(o.Key.Item2, spatialIndex2))
                        {
                            querysItem2.Clear();
                        }
                    }
                });
                var Item1 = o.Key.Item1;
                var Item2 = o.Key.Item2;
                var ptsCollection = new List<Point3d>();
                querysItem1.Cast<Entity>().ToList().ForEach(i =>
                {
                    var pts = new Point3dCollection();
                    o.Key.Item1.IntersectWith(i, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0)
                    {
                        ptsCollection.Add(pts.Cast<Point3d>().First());
                    }
                });
                ptsCollection.Add(o.Key.Item1.StartPoint);
                ptsCollection.Add(o.Key.Item1.EndPoint);
                var Spts = ptsCollection.Where(i => i.DistanceTo(o.Key.Item1.StartPoint) < i.DistanceTo(o.Key.Item1.EndPoint));
                var Epts = ptsCollection.Except(Spts);
                Point3d centerPt = o.Key.Item1.GetMidpoint();
                Item1 = new Line(Spts.OrderBy(i => i.DistanceTo(centerPt)).First(), Epts.OrderBy(i => i.DistanceTo(centerPt)).First());
                ptsCollection.Clear();
                querysItem2.Cast<Entity>().ToList().ForEach(i =>
                {
                    var pts = new Point3dCollection();
                    o.Key.Item2.IntersectWith(i, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0)
                    {
                        ptsCollection.Add(pts.Cast<Point3d>().First());
                    }
                });
                ptsCollection.Add(o.Key.Item2.StartPoint);
                ptsCollection.Add(o.Key.Item2.EndPoint);
                Spts = ptsCollection.Where(i => i.DistanceTo(o.Key.Item2.StartPoint) < i.DistanceTo(o.Key.Item2.EndPoint));
                Epts = ptsCollection.Except(Spts);
                centerPt = o.Key.Item2.GetMidpoint();
                Item2 = new Line(Spts.OrderBy(i => i.DistanceTo(centerPt)).First(), Epts.OrderBy(i => i.DistanceTo(centerPt)).First());
                results.Add((Item1, Item2).ToTuple(), o.Value);
            });

            return results;
        }
        private bool IsMainBeam(Line line, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            bool result = false;
            var querys = spatialIndex.SelectFence(line.ExtendLine(10));
            var dir = line.LineDirection();
            querys.Cast<Entity>().OfType<Polyline>().ForEach(o =>
            {
                var lines = o.ToLines();
                for (int i = 0; i < lines.Count; i++)
                {
                    if (result)
                    {
                        break;
                    }
                    if (ThGeometryTool.IsPerpendicular(dir, lines[i].LineDirection()))
                    {
                        result = true;
                        break;
                    }
                }
            });

            return result;
        }


    }
}