using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service
{
    public class ThFindLightBlockNumberService
    {
        private Point3d Position { get; set; }
        private List<Line> Lines { get; set; }
        private List<DBText> Texts { get; set; }
        private double FindLength { get; set; }

        private string LightNumber { get; set; }

        public ThFindLightBlockNumberService(Point3d position,List<Line> lines,List<DBText> texts,double findLength=500.0) 
        {
            Position = position;
            Lines = lines;
            Texts = texts;
            FindLength = findLength;
            LightNumber = "";
        }

        public static string Find(Point3d position, List<Line> lines, List<DBText> texts, double findLength = 500.0)
        {
            var instance = new ThFindLightBlockNumberService(position, lines, texts);
            instance.Find();
            return instance.LightNumber;
        }

        private void Find()
        {
            double squareLength = 2.5;
            var lineSpatialIndex = ThGarageLightUtils.BuildSpatialIndex(Lines);
            var textSpatialIndex = new ThCADCoreNTSSpatialIndex(Texts.ToCollection());
            var square = ThDrawTool.CreateSquare(Position, squareLength);
            var lineObjs = lineSpatialIndex.SelectCrossingPolygon(square);

            var onLines = lineObjs.Cast<Line>().Where(o=>Position.IsPointOnLine(o, squareLength)).ToList();
            if(onLines.Count>0)
            {
                var first = onLines.First();               
                var normalLine = ThGarageLightUtils.NormalizeLaneLine(first);
                var lineDir = normalLine.StartPoint.GetVectorTo(normalLine.EndPoint).GetNormal();
                var perpendVec = lineDir.GetPerpendicularVector();
                var startPt = Position + perpendVec.MultiplyBy(FindLength);
                var findArea = ThDrawTool.ToRectangle(Position, startPt, squareLength * 2.0);
                var findTexts = textSpatialIndex.SelectCrossingPolygon(findArea);

                var resutls = findTexts
                    .Cast<DBText>()
                    .Where(o=>ThGarageLightUtils.IsLightNumber(o.TextString))
                    .Where(o=> IsApproximateParallel(o.Rotation,normalLine.Angle))
                    .OrderBy(o=>o.Position.DistanceTo(Position));

                if(resutls.Count()>0)
                {
                    LightNumber = resutls.First().TextString;
                }
            }
        }
        private bool IsApproximateParallel(double textRotation,double lineAngle,double tolerance=5.0)
        {
            double textAng = textRotation / Math.PI * 180.0;
            double lineAng = lineAngle / Math.PI * 180.0;

            textAng =(textAng+ tolerance) % 180.0;
            lineAng = (lineAng + tolerance) % 180.0;

            return Math.Abs(textAng - lineAng) <= tolerance;
        }
    }
}
