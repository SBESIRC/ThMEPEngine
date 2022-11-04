using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPEngineCore.CAD;

namespace ThMEPArchitecture.PartitionLayout
{
    public static class DisplayTools
    {
        public static void Display(LineSegment line, int colorindex = 0, string layer = "0")
        {
            var l = line.ToDbLine();
            l.Layer = layer;
            l.ColorIndex = colorindex;
            l.AddToCurrentSpace();
        }
        public static void Display(IEnumerable<LineSegment> lines, int colorindex = 0, string layer = "0")
        {
            lines.Select(e =>
            {
                var l = e.ToDbLine();
                l.Layer = layer;
                l.ColorIndex = colorindex;
                return l;
            }).AddToCurrentSpace();
        }
        public static void Display(List<LineSegment> lines, int colorindex = 0, string layer = "0")
        {
            lines.Select(e =>
            {
                var l = e.ToDbLine();
                l.Layer = layer;
                l.ColorIndex = colorindex;
                return l;
            }).AddToCurrentSpace();
        }
        public static void Display(Polygon polygon, int colorindex = 0, string layer = "0")
        {
            var pl = polygon.Shell.ToDbPolyline();
            pl.Layer = layer;
            pl.ColorIndex = colorindex;
            pl.AddToCurrentSpace();
            DisplayParkingStall.Add(pl);
        }
        public static void Display(IEnumerable<Polygon> polygons, int colorindex = 0, string layer = "0")
        {
            polygons.Select(polygon =>
            {
                var pl = polygon.Shell.ToDbPolyline();
                pl.Layer = layer;
                pl.ColorIndex = colorindex;
                return pl;
            }).AddToCurrentSpace();
        }
        public static void Display(List<Polygon> polygons, int colorindex = 0, string layer = "0")
        {
            polygons.Select(polygon =>
            {
                var pl = polygon.Shell.ToDbPolyline();
                pl.Layer = layer;
                pl.ColorIndex = colorindex;
                return pl;
            }).AddToCurrentSpace();
        }
        public static void Display(LineString line, int colorindex = 0, string layer = "0")
        {
            var pl = line.ToDbPolyline();
            pl.Layer = layer;
            pl.ColorIndex = colorindex;
            pl.AddToCurrentSpace();
        }
        public static void Display(IEnumerable<LineString> lines, int colorindex = 0, string layer = "0")
        {
            lines.Select(line =>
            {
                var pl = line.ToDbPolyline();
                pl.Layer = layer;
                pl.ColorIndex = colorindex;
                return pl;
            }).AddToCurrentSpace();
        }
        public static void Display(List<LineString> lines, int colorindex = 0, string layer = "0")
        {
            lines.Select(line =>
            {
                var pl = line.ToDbPolyline();
                pl.Layer = layer;
                pl.ColorIndex = colorindex;
                return pl;
            }).AddToCurrentSpace();
        }


    }
}
