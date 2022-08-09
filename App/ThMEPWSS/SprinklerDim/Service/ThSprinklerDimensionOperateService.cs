using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThSprinklerDimensionOperateService
    {
        public static List<int> DeleteIsDimed(List<int> tdim, List<List<int>> anotherCollineation, bool[] isDimensioned)
        {
            List<int> dims = new List<int>();
            for (int i = 0; i < anotherCollineation.Count; i++)
            {
                for (int j = 0; j < tdim.Count; j++)
                {
                    if (anotherCollineation[i].Contains(tdim[j]) && !isDimensioned[i]) dims.Add(tdim[j]);
                }
            }
            return dims;
        }

        public static void CheckDimensions(List<int> dim, List<List<int>> anotherCollineation, ref bool[] isDimensioned)
        {
            foreach (int idx in dim)
            {
                for (int i = 0; i < anotherCollineation.Count; i++)
                {
                    if (anotherCollineation[i].Contains(idx))
                    {
                        isDimensioned[i] = true;
                    }
                }
            }
        }

        public static List<int> GetLongestDimension(List<int> undimensionedLine, List<List<int>> collineation, List<List<int>> anoCollineation, bool[] isDimensioned)
        {
            int len = 0;
            List<int> Dim = new List<int>();
            foreach (int i in undimensionedLine)
            {
                List<int> line = collineation.Where(x => x.Contains(i)).ToList()[0];
                List<int> line1 = DeleteIsDimed(line, anoCollineation, isDimensioned);
                if (line1.Count > len)
                {
                    len = line1.Count;
                    Dim = line;
                }
            }

            return Dim;
        }

        public static List<int> GetLongestLine(List<List<int>> collineationList)
        {
            int longestLineIndex = 0;
            for (int i = 1; i < collineationList.Count; i++)
            {
                if (collineationList[i].Count > collineationList[longestLineIndex].Count)
                    longestLineIndex = i;
            }

            return collineationList[longestLineIndex];
        }

        public static bool IsConflicted(Point3d pts1, Point3d pts2, Matrix3d matrix, List<Polyline> walls)
        {
            List<Point3d> pts = new List<Point3d> { pts1, pts2 };
            pts = ThChangeCoordinateService.MakeTransformation(pts, matrix.Inverse());
            Line line = new Line(pts[0], pts[pts.Count - 1]);

            return ThSprinklerDimConflictService.IsConflicted(line, walls);
        }

        public static double GetNeareastDistance(List<Point3d> pts, List<int> dim, List<int> isNotDimensioned)
        {
            Line dimline = new Line(pts[dim[0]], pts[dim[dim.Count - 1]]);
            Point3d Dropfoot = dimline.GetClosestPointTo(pts[isNotDimensioned[0]], true);
            List<double> distance = new List<double>();
            foreach (int i in isNotDimensioned)
            {
                distance.Add(pts[i].DistanceTo(Dropfoot));
            }

            return distance.Min();
        }

        public static List<List<int>> SeperateLine(List<Point3d> pts, List<int> line, List<int> line2, bool isXAxis, double step)
        {
            line.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));
            List<List<int>> lines = new List<List<int>>();
            line2.Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));

            List<int> one = new List<int> { line[0] };
            for (int i = 1; i < line.Count; i++)
            {
                int iPtIndex = one[one.Count - 1];
                int jPtIndex = line[i];
                if (ThChangeCoordinateService.GetOriginalValue(pts[jPtIndex], isXAxis) - ThChangeCoordinateService.GetOriginalValue(pts[iPtIndex], isXAxis) > 1.5 * step || Math.Abs(line2.IndexOf(iPtIndex) - line2.IndexOf(jPtIndex)) != 1) 
                {
                    lines.Add(one);
                    one = new List<int> { jPtIndex };
                }
                else
                {
                    one.Add(jPtIndex);
                }
            }
            lines.Add(one);

            return lines;
        }

        public static int ChooseNearestPt(List<Point3d> pts, int pt1, int pt2, List<List<int>> dims)
        {
            List<double> d1 = new List<double>();
            List<double> d2 = new List<double>();
            foreach (List<int> dim in dims)
            {
                if (dim != null)
                {
                    if (dim.Count != 1 && dim.Count != 0)
                    {
                        Line line = new Line(pts[dim[0]], pts[dim[dim.Count - 1]]);
                        d1.Add(pts[pt1].DistanceTo(line.GetClosestPointTo(pts[pt1], true)));
                        d2.Add(pts[pt1].DistanceTo(line.GetClosestPointTo(pts[pt2], true)));
                    }
                }
            }
            d1.RemoveAll(p => p < 45);
            d2.RemoveAll(p => p < 45);
            if (d1.Min() > d2.Min()) return pt2;
            else return pt1;
        }

        //去重和去空
        public static List<List<List<int>>> DeletNullDimensions(List<Point3d> pts, List<List<List<int>>> dimensions, bool isXAxis)
        {
            List<List<List<int>>> Dimensions = new List<List<List<int>>>();
            foreach(List<List<int>> dims in dimensions)
            {
                List<List<int>> t = new List<List<int>>();
                for (int k = 0; k < dims.Count; k++) 
                {
                    if(dims[k] != null)
                    {
                        dims[k].Sort((x, y) => ThChangeCoordinateService.GetOriginalValue(pts[x], isXAxis).CompareTo(ThChangeCoordinateService.GetOriginalValue(pts[y], isXAxis)));
                        List<int> one = new List<int> { dims[k][0] };
                        for(int j = 1; j < dims[k].Count; j++)
                        {
                            if (pts[dims[k][j]].DistanceTo(pts[dims[k][j - 1]]) > 10)one.Add(dims[k][j]);
                        }
                        t.Add(one);
                    }
                }
                if (t.Count != 0) Dimensions.Add(t);
            }

            return Dimensions;
        }
    }
}
