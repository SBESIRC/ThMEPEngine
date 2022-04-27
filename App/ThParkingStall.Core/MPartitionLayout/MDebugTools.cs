using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.MPartitionLayout
{
    public static class MDebugTools
    {
        public static string AnalysisLineString(LineString line)
        {
            string s = "";
            foreach (var co in line.Coordinates)
            {
                s += co.X.ToString() + "," + co.Y.ToString() + ",";
            }
            s = s.Remove(s.Length - 1, 1);
            return s;
        }
        public static string AnalysisLineStringLIST(List<LineString> lines)
        {
            string s = "";
            foreach (var line in lines)
            {
                s += AnalysisLineString(line) + ",";
            }
            s = s.Remove(s.Length - 1, 1);
            return s;
        }
        public static string AnalysisLineSegment(LineSegment line)
        {
            return AnalysisLineString(new LineString(new Coordinate[] { line.P0, line.P1 }));
        }
        public static string AnalysisLineSegmentLIST(List<LineSegment> lines)
        {
            string s = "";
            foreach (var line in lines)
            {
                s += AnalysisLineSegment(line) + ",";
            }
            s = s.Remove(s.Length - 1, 1);
            return s;
        }
        public static string AnalysisPolygon(Polygon polygon)
        {
            var lines = new LineString(polygon.Coordinates);
            return AnalysisLineString(lines);
        }
        public static string AnalysisPolygonList(List<Polygon> polygons)
        {
            return AnalysisLineStringLIST(polygons.Select(e =>
            new LineString(e.Coordinates)).ToList());
        }
        public static string AnalysisCoordinateList(List<Coordinate> coords)
        {
            string s = "";
            foreach (var co in coords)
            {
                s += co.X.ToString() + "," + co.Y.ToString() + ",";
            }
            s = s.Remove(s.Length - 1, 1);
            return s;
        }

    }
}