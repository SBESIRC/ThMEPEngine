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
        private DBObjectCollection BeamTexts { get; set; }
        private ThCADCoreNTSSpatialIndex BeamLineSpatialIndex { get; set; }

        public ThAdjustBeamMarkService(
            DBObjectCollection beamLines,
            DBObjectCollection beamTexts)
        {
            BeamLines = beamLines;
            BeamTexts = beamTexts;
            BeamLineSpatialIndex = new ThCADCoreNTSSpatialIndex(BeamLines);
        }
        public List<Tuple<DBText, DBText, DBText>> Adjust()
        {
            var results = new List<Tuple<DBText, DBText, DBText>>();
            BeamTexts.OfType<DBText>()
                .Where(o => IsBeamBgMark(o.TextString))
                .ForEach(o=>
                {
                    var texts = Ajust(o);
                    if (texts!=null)
                    {
                        results.Add(Tuple.Create(o, texts.Item1, texts.Item2));
                    }
                });            
            return results;
        }

        private Tuple<DBText, DBText> Ajust(DBText beamMark)
        {
            var texts = SplitBeamMarkTexts(beamMark.TextString);
            if(texts.Count!=2)
            {
                return null;
            }
            var center = GetTextCenter(beamMark);
            if(!center.HasValue)
            {
                return null;
            }
            var perpendVec = GetTextPerpendVector(beamMark.Rotation);
            var detectLength = 2.0 * beamMark.Height;

            var oneSideBeamLines = QueryBeamLines(center.Value,
                center.Value.GetExtentPoint(perpendVec, detectLength), 2.0);
            var otherSideBeamLines = QueryBeamLines(center.Value,
                center.Value.GetExtentPoint(perpendVec.Negate(), detectLength), 2.0);

            // 创建文字
            var specText = CreateText(beamMark, texts[0]);
            var bgText = CreateText(beamMark, texts[1]);

            // 移动
            if (oneSideBeamLines.Count>0 && otherSideBeamLines.Count==0)
            {
                Move(beamMark, specText, bgText, perpendVec.Negate());
            }
            else if(oneSideBeamLines.Count == 0 && otherSideBeamLines.Count > 0)
            {
                Move(beamMark, specText, bgText, perpendVec);
            }
            else if(oneSideBeamLines.Count > 0 && otherSideBeamLines.Count > 0)
            {
                return null; // 不调整
            }
            else
            {
                Move(beamMark, specText, bgText, perpendVec);               
            }
            return Tuple.Create(specText, bgText);
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
            // 文字中心与beamMark中心对齐
            var specCenter = GetTextCenter(specText); 
            var bgCenter = GetTextCenter(bgText);
            var beamMarkCenter = GetTextCenter(beamMark);
            if (beamMarkCenter.HasValue && specCenter.HasValue && bgCenter.HasValue)
            {
                var specCenterProjectionPt = specCenter.Value.GetProjectPtOnLine(beamMarkCenter.Value,
                     beamMarkCenter.Value.GetExtentPoint(towardVec, 1000.0));
                var bgCenterProjectionPt = bgCenter.Value.GetProjectPtOnLine(beamMarkCenter.Value,
                    beamMarkCenter.Value.GetExtentPoint(towardVec, 1000.0));
                var mt1 = Matrix3d.Displacement(specCenterProjectionPt- specCenter.Value);
                var mt2 = Matrix3d.Displacement(bgCenterProjectionPt - bgCenter.Value);
                specText.TransformBy(mt1);
                bgText.TransformBy(mt2);
            }
        }

        private DBText CreateText(DBText beamMark,string content)
        {
            return new DBText()
            {
                TextString = content,
                Position = beamMark.Position,
                Rotation = beamMark.Rotation,
                WidthFactor = beamMark.WidthFactor,
                Height = beamMark.Height,
                TextStyleId= beamMark.TextStyleId,
            };
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
            string pattern = @"^\d+\s*[Xx]{1}\s*\d+\s*[(（]{1}[\S\s]*[）)]{1}$";
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
