using System;
using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThAdjustBeamMarkPosService
    {
        // 梁标注距离梁线间隔
        private double beamMarkInterval= 70; // 第一个梁标注的边界距离梁边界的间隙
        private double textBoundaryInterval = 70; // 梁标注的边界距离梁标注的间隙
        private ThCADCoreNTSSpatialIndex beamLineSpatialIndex;
        public ThAdjustBeamMarkPosService(DBObjectCollection beamLines, 
            double beamMarkInterval, double textBoundaryInterval)
        {
            this.beamMarkInterval = beamMarkInterval;
            this.textBoundaryInterval = textBoundaryInterval;
            beamLineSpatialIndex = new ThCADCoreNTSSpatialIndex(beamLines);
        }
        public void Adjust(DBObjectCollection beamMarks, Vector3d moveDir)
        {
            if (beamMarks.Count == 0)
            {
                return;
            }
            var firstText = beamMarks.OfType<DBText>().First();
            var textCenter = firstText.GetCenterPointByOBB();
            var maximumBeamWidth = GetBeamWidth(beamMarks.OfType<DBText>().OrderByDescending(o => GetBeamWidth(o)).First());
            // 获取firstText中心距离梁线的长度
            var distance = GetTextMoveDisToBeam(textCenter, firstText.Rotation, moveDir, maximumBeamWidth);
            if (distance == 0)
            {
                distance = maximumBeamWidth / 2.0;
            }
            var geoHeights = beamMarks.OfType<DBText>().Select(o => CalculateTextHeight(o)).ToList();
            var newTextCenter = textCenter + moveDir.GetNormal().MultiplyBy(
               distance + beamMarkInterval + geoHeights[0] / 2.0);
            var mt1 = Matrix3d.Displacement(newTextCenter - textCenter);
            firstText.TransformBy(mt1);
            for (int i = 1; i < beamMarks.Count; i++)
            {
                var secondText = beamMarks[i] as DBText;
                double middleTextHeights = 0.0;
                for (int j = 2; j < i; j++)
                {
                    middleTextHeights += geoHeights[j];
                }
                var textInterval = geoHeights[0] / 2.0 + middleTextHeights + geoHeights[i] / 2.0 + i * textBoundaryInterval;
                var oldSecondTextCenter = secondText.GetCenterPointByOBB();
                var newSecondTextCenter = newTextCenter.GetExtentPoint(moveDir, textInterval);
                var mt2 = Matrix3d.Displacement(newSecondTextCenter - oldSecondTextCenter);
                secondText.TransformBy(mt2);
            }
        }

        private double GetTextMoveDisToBeam(Point3d textCenter,double textRotation,Vector3d moveDir,double queryLength)
        {
            var textAng = textRotation.RadToAng();
            var extendPt = textCenter.GetExtentPoint(moveDir, queryLength);
            var beamLines = QueryBeamLines(textCenter, extendPt, 2.0);            
            return beamLines.OfType<Line>()
                .Where(o => textAng.IsAngleParallel(o.Angle.RadToAng(), 1.0))
                .Select(o =>
                {
                    var projectionPt = textCenter.GetProjectPtOnLine(o.StartPoint, o.EndPoint);
                    return textCenter.DistanceTo(projectionPt);
                })
                .OrderByDescending(o => o)
                .FirstOrDefault();
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

        private Tuple<double, double> GetBeamWidth(string textstring)
        {
            // 此规格在外部已检查
            var values = textstring.GetDoubles();
            return Tuple.Create(values[0], values[1]);
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
            return beamLineSpatialIndex.SelectCrossingPolygon(outline);
        }
    }
}
