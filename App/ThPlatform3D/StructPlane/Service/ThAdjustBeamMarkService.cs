using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThAdjustBeamMarkService
    {
        private DBObjectCollection BeamLines { get; set; }
        // 梁标注距离梁线间隔
        private double BeamMarkInterval = 70;
        /// <summary>
        /// 文字，及文字移动的方向
        /// </summary>
        private Dictionary<DBText, Vector3d> BeamTexts { get; set; }
        private ThCADCoreNTSSpatialIndex BeamLineSpatialIndex { get; set; }

        public List<Tuple<DBText, DBText, DBText>> DoubleRowTexts { get; set; }

        public ThAdjustBeamMarkService(
            DBObjectCollection beamLines,
            Dictionary<DBText, Vector3d> beamTexts)
        {
            BeamLines = beamLines;
            BeamTexts = beamTexts;
            DoubleRowTexts = new List<Tuple<DBText, DBText, DBText>>();
            BeamLineSpatialIndex = new ThCADCoreNTSSpatialIndex(BeamLines);
        }
        public void Adjust(AcadDatabase acadDb)
        {
            // 要把单行文字拆成两行
            BeamTexts.ForEach(o =>
            {
                var text = acadDb.Element<DBText>(o.Key.ObjectId, true);
                if (text.TextString.IsBeamSpec())
                {
                    Move(text, o.Value);
                }
                else if (text.TextString.IsBeamBgMark())
                {
                    var texts = Split(text); // spec,bg
                    if (texts != null)
                    {
                        Move(text, texts.Item1, texts.Item2, o.Value);
                        DoubleRowTexts.Add(Tuple.Create(text, texts.Item1, texts.Item2));
                    }
                    else
                    {
                        Move(text, o.Value);
                    }
                }
            });
        }

        private void Move(DBText beamMark,Vector3d moveVec)
        {
            // 让梁标注外包框距离梁线 eg.50mm
            var textCenter = GetTextCenter(beamMark);
            if(!textCenter.HasValue)
            {
                return;
            }
            var beamWidth = GetBeamWidth(beamMark);
            var geoH = CalculateTextHeight(beamMark);
            var newTextCenter = textCenter.Value + moveVec.GetNormal().MultiplyBy(
                beamWidth / 2.0 + BeamMarkInterval + geoH / 2.0);
            var mt = Matrix3d.Displacement(newTextCenter - textCenter.Value);
            beamMark.TransformBy(mt);
        }

        private void Move(DBText beamMark, DBText spec,DBText bg,Vector3d moveVec)
        {
            // 让梁标注外包框距离梁线 eg.50mm
            var textCenter = GetTextCenter(beamMark);
            if (!textCenter.HasValue)
            {
                return;
            }
            // 先把标高文字移动到规格文字一边
            var movePt = beamMark.Position.GetExtentPoint(moveVec, beamMark.Height);
            var mt1 = Matrix3d.Displacement(movePt - beamMark.Position);
            bg.TransformBy(mt1);

            var beamWidth = GetBeamWidth(beamMark);
            var geoH = CalculateTextHeight(spec);
            var newTextCenter = textCenter.Value + moveVec.GetNormal().MultiplyBy(
                beamWidth / 2.0 + BeamMarkInterval + geoH / 2.0);
            var mt2 = Matrix3d.Displacement(newTextCenter - textCenter.Value);
            bg.TransformBy(mt2);
            spec.TransformBy(mt2);
        }

        private double GetBeamWidth(DBText beamMark)
        {
            // 暂时解析文字中的宽度
            // beamWidth后期通过两边的梁线来获取，暂时取梁规格中的宽度
            return GetBeamWidth(beamMark.TextString).Item1;
        }

        private double CalculateTextHeight(DBText text)
        {
            double geoH = text.Height;
            var clone = text.Clone() as DBText;
            var mt = Matrix3d.Rotation(clone.Rotation * -1.0, clone.Normal, clone.Position);
            clone.TransformBy(mt);
            if(clone.Bounds.HasValue)
            {
                geoH = clone.GeometricExtents.MaxPoint.Y - clone.GeometricExtents.MinPoint.Y;
            }
            clone.Dispose();
            return geoH;
        }

        private Tuple<DBText, DBText> Split(DBText beamMark)
        {
            // 把单行文字拆成两行文字
            var texts = SplitBeamMarkTexts(beamMark.TextString);
            if (texts.Count != 2)
            {
                return null;
            }
            var specText = CreateText(beamMark, texts[0]);
            var bgText = CreateText(beamMark, texts[1]);
            return Tuple.Create(specText, bgText);
        }

        private Tuple<double, double> GetBeamWidth(string textstring)
        {
            // 此规格在外部已检查
            var values = textstring.GetDoubles();
            return Tuple.Create(values[0], values[1]);
        }

        private Point3d? GetTextCenter(DBText text)
        {
            var obb = GetTextObb(text);
            if (obb == null || obb.Area <= 1.0 || obb.NumberOfVertices < 4)
            {
                return null;
            }
            return obb.GetPoint3dAt(0).GetMidPt(obb.GetPoint3dAt(2));
        }
 
        private DBText CreateText(DBText beamMark, string content)
        {
            var clone = beamMark.Clone() as DBText;
            clone.TextString = content;
            //clone.AlignmentPoint = beamMark.AlignmentPoint;
            //clone.HorizontalMode = TextHorizontalMode.TextCenter;
            //clone.VerticalMode = TextVerticalMode.TextVerticalMid;
            return clone;
        }

        private DBObjectCollection Filter(DBObjectCollection beamLines,double rad)
        {
            var radTolerance = ThAuxiliaryUtils.AngToRad(1.0);
            return beamLines
                .OfType<Line>()
                .Where(o => IsParallel(o.Angle, rad, radTolerance))
                .ToCollection();
        }

        private bool IsParallel(double firstRad,double secondRad,double tolerance)
        {
            var minus = Math.Abs(firstRad - secondRad) % Math.PI;
            return minus <= tolerance || Math.Abs(minus - Math.PI) <= tolerance;
        }

        private DBObjectCollection QueryBeamLines(Point3d startPt,Point3d endPt,double width)
        {
            var outline = ThDrawTool.ToRectangle(startPt, endPt, width);
            var results = QueryBeamLines(outline);
            outline.Dispose();
            return results;
        }

        private DBObjectCollection QueryBeamLines(Polyline outline) 
        {
            return BeamLineSpatialIndex.SelectCrossingPolygon(outline);
        }

        private Vector3d GetTextPerpendVector(double textRotation)
        {
            return GetTextVector(textRotation).GetPerpendicularVector();
        }

        private Vector3d GetTextVector(double textRotation)
        {
            return Vector3d.XAxis.RotateBy(textRotation, Vector3d.ZAxis);
        }

        private Polyline GetTextObb(DBText text)
        {
            return text.TextOBB();
        }

        private List<string> SplitBeamMarkTexts(string beamMark)
        {
            // 600x400(BG)
            var results = new List<string>();
            var index = beamMark.IndexOf('(');
            if (index == -1)
            {
                return results;
            }
            var spec = beamMark.Substring(0, index);
            var elevation = beamMark.Substring(index);
            if (!string.IsNullOrEmpty(spec) && !string.IsNullOrEmpty(elevation))
            {
                results.Add(spec);
                results.Add(elevation);
            }
            return results;
        }
    }
}
