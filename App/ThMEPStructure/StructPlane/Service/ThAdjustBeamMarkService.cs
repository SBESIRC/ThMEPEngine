using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.StructPlane.Service
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
        private Database database;

        public ThAdjustBeamMarkService(
            Database db,
            DBObjectCollection beamLines,
            Dictionary<DBText, Vector3d> beamTexts)
        {
            database = db;
            BeamLines = beamLines;
            BeamTexts = beamTexts;
            DoubleRowTexts = new List<Tuple<DBText, DBText, DBText>>();
            BeamLineSpatialIndex = new ThCADCoreNTSSpatialIndex(BeamLines);
        }
        public void Adjust()
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(database))
            {
                // 要把单行文字拆成两行
                BeamTexts.ForEach(o =>
                {
                    var text = acadDb.Element<DBText>(o.Key.ObjectId, true);
                    if (IsBeamBgMark(text.TextString) || IsBeamSpec(text.TextString))
                    {
                        Move(text, o.Value);
                    }
                    if (IsBeamBgMark(text.TextString))
                    {
                        var texts = Split(text, o.Value);
                        if (texts != null)
                        {
                            DoubleRowTexts.Add(Tuple.Create(text, texts.Item1, texts.Item2));
                        }
                    }
                });
            }     
        }

        private void Move(DBText beamMark,Vector3d moveVec)
        {
            // 让梁标注外包框距离梁线 eg.50mm
            var textCenter = GetTextCenter(beamMark);
            if(!textCenter.HasValue)
            {
                return;
            }
            var beamSpec = GetBeamWidth(beamMark.TextString);
            var beamWidth = beamSpec.Item1;
            var newTextCenter = textCenter.Value + moveVec.GetNormal().MultiplyBy(
                beamWidth / 2.0 + BeamMarkInterval + beamMark.Height / 2.0);
            var mt = Matrix3d.Displacement(newTextCenter - textCenter.Value);
            beamMark.TransformBy(mt);
        }

        private Tuple<DBText, DBText> Split(DBText beamMark,Vector3d direction)
        {
            // 把单行文字拆成两行文字
            // 往direction方向调整
            var center = GetTextCenter(beamMark);
            if (!center.HasValue)
            {
                return null;
            }
            var texts = SplitBeamMarkTexts(beamMark.TextString);
            if (texts.Count != 2)
            {
                return null;
            }
            var specText = CreateText(beamMark, texts[0]);
            var bgText = CreateText(beamMark, texts[1]);            
            Move(beamMark, specText, bgText, direction);
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
        private void Move(DBText beamMark,DBText specText,DBText bgText, Vector3d towardVec)
        {
            var radTolerance = ThAuxiliaryUtils.AngToRad(1.0);
            var movePt = beamMark.Position.GetExtentPoint(towardVec, beamMark.Height);
            var mt = Matrix3d.Displacement(movePt - beamMark.Position);
            if (IsParallel(beamMark.Rotation,Math.PI*0.5, radTolerance) ||
                IsParallel(beamMark.Rotation, Math.PI * 1.5, radTolerance))
            {
                // 垂直,specText在左边，gbText在右边               
                if(movePt.X< beamMark.Position.X)
                {
                    specText.TransformBy(mt);
                }
                else
                {
                    bgText.TransformBy(mt);
                }
            }
            else
            {
                // 水平,specText在上方，gbText在下方
                // 二四象限，specText在上方，gbText在下方
                // 一三象限，specText在上方，gbText在下方
                if (movePt.Y > beamMark.Position.Y)
                {
                    specText.TransformBy(mt);
                }
                else
                {
                    bgText.TransformBy(mt);
                }
            }            
        }

        private DBText CreateText(DBText beamMark, string content)
        {
            var clone = beamMark.Clone() as DBText;
            clone.TextString = content;
            clone.AlignmentPoint = beamMark.AlignmentPoint;
            clone.HorizontalMode = TextHorizontalMode.TextCenter;
            clone.VerticalMode = TextVerticalMode.TextVerticalMid;
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

        private bool IsBeamBgMark(string content)
        {
            // 400x200(Bg-2.5)
            string pattern = @"^\d+\s*[Xx]{1}\s*\d+\s*[(（]{1}[\S\s]*[）)]{1}$";
            return Regex.IsMatch(content.Trim(), pattern);
        }
        private bool IsBeamSpec(string content)
        {
            // 400x200
            string pattern = @"^\d+\s*[Xx]{1}\s*\d+$";
            return Regex.IsMatch(content.Trim(), pattern);
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
