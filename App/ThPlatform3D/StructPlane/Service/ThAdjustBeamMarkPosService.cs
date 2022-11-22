using System;
using System.Linq;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThAdjustBeamMarkPosService
    {
        // 梁标注距离梁线间隔
        private double firstBeamMarkInterval= 50; // 第一个梁标注边界距离梁边界的间隙
        private double beamMarkInterval = 70; // 梁标注边界的Gap距离
        private ThCADCoreNTSSpatialIndex beamLineSpatialIndex;
        public ThAdjustBeamMarkPosService(DBObjectCollection beamLines, 
            double firstBeamMarkInterval, double beamMarkInterval)
        {
            this.firstBeamMarkInterval = firstBeamMarkInterval;
            this.beamMarkInterval = beamMarkInterval;
            beamLineSpatialIndex = new ThCADCoreNTSSpatialIndex(beamLines);
        }
        public void Adjust(DBObjectCollection beamMarks, Vector3d moveDir)
        {
            if (beamMarks.Count == 0)
            {
                return;
            }
            var firstText = beamMarks.OfType<DBText>().First();
            var maximumBeamWidth = GetBeamWidth(beamMarks.OfType<DBText>().OrderByDescending(o => GetBeamWidth(o)).First());
            if(maximumBeamWidth == 0.0)
            {
                maximumBeamWidth = ThStructurePlaneCommon.BeamDefaultCalculateWidth;
            }
            var geoTextHeights = beamMarks.OfType<DBText>().Select(o => CalculateTextHeight(o)).ToList();
            var baseOffsetDistance = maximumBeamWidth / 2.0 + firstBeamMarkInterval;
            for (int i = 0; i < beamMarks.Count; i++)
            {
                var current = beamMarks[i] as DBText;
                var currentGeoHeight = geoTextHeights[i];
                double distance = 0.0;
                if (i == 0)
                {
                    distance = baseOffsetDistance + currentGeoHeight / 2.0;
                }
                else
                {
                    var heights = geoTextHeights.Take(i).Sum();
                    distance = baseOffsetDistance + heights + i * beamMarkInterval + currentGeoHeight / 2.0;
                }
                var mt = Matrix3d.Displacement(moveDir.GetNormal().MultiplyBy(distance));
                current.TransformBy(mt);
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
