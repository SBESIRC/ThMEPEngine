using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace mt2
{
    internal class Program
    {
        static private void InsectMore(LineString lstr1, LineString lstr2, int n = 1000000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.Intersects(lstr2);
            }
        }
        static  private LineString GetLineString(List<Coordinate> pts)
        {
            //var spt = new Point3d(General.Utils.RandDouble() * Lim, General.Utils.RandDouble() * Lim, 0);
            //var ept = new Point3d(General.Utils.RandDouble() * Lim, General.Utils.RandDouble() * Lim, 0);
            //return new Line(spt, ept);
            //var rand = new Random();
            //var points = new List<Coordinate>
            //{
            //    new Coordinate(0,0),

            //    //new Coordinate(rand.NextDouble() * 10000, rand.NextDouble() * 10000),
            //    //new Coordinate(rand.NextDouble() * 10000, rand.NextDouble() * 10000)
            //};

            //return ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
            return new LineString(pts.ToArray());
        }
        static void Main(string[] args)
        {
            Stopwatch _stopwatch = new Stopwatch();
            _stopwatch.Start();

            var line1Pts = new List<Coordinate> {
                new Coordinate(0,0),
                new Coordinate(0,10000)
            };
            var line2Pts = new List<Coordinate> { new Coordinate(4500, 4500), new Coordinate(-4500, -4500) };

            InsectMore(GetLineString(line1Pts), GetLineString(line2Pts));
            //Console.Write(_stopwatch.Elapsed.TotalSeconds);
            //Console.ReadKey(); 
        }
    }
}
