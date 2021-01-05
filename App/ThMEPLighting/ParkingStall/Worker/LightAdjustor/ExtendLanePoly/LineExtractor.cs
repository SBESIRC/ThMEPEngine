using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Geometry;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class LineExtractor
    {
        private List<Curve> m_curves;

        public List<Line> Lines
        {
            get;
            set;
        } = new List<Line>();


        public LineExtractor(List<Curve> curves)
        {
            m_curves = curves;
        }

        public static List<Line> MakeLinesExtractor(List<Curve> curves)
        {
            var lineExtractor = new LineExtractor(curves);
            lineExtractor.Do();
            return lineExtractor.Lines;
        }

        public void Do()
        {
            foreach (var curve in m_curves)
            {
                if (curve is Line line)
                {
                    Lines.Add(line);
                }
                else if (curve is Polyline polyline)
                {
                    Lines.AddRange(Polyline2dLines(polyline));
                }
            }
        }

        public static List<Line> Polyline2dLines(Polyline polyline)
        {
            if (polyline == null)
                return null;

            var lines = new List<Line>();
            if (polyline.Closed)
            {
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    var bulge = polyline.GetBulgeAt(i);
                    if (GeomUtils.IsAlmostNearZero(bulge))
                    {
                        var line3d = polyline.GetLineSegmentAt(i);
                        lines.Add(new Line(line3d.StartPoint, line3d.EndPoint));
                    }
                }
            }
            else
            {
                for (int j = 0; j < polyline.NumberOfVertices - 1; j++)
                {
                    var bulge = polyline.GetBulgeAt(j);
                    if (GeomUtils.IsAlmostNearZero(bulge))
                    {
                        var line3d = polyline.GetLineSegmentAt(j);
                        lines.Add(new Line(line3d.StartPoint, line3d.EndPoint));
                    }
                }
            }

            return lines;
        }


    }
}
