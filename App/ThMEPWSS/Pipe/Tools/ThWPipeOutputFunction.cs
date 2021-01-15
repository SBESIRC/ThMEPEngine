using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using DotNetARX;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Tools
{
    public class ThWPipeOutputFunction
    {
        public static DBText Taggingtext(Point3d tag, string s)
        {
            return new DBText()
            {
                Height = 175,
                Position = tag,
                TextString = s,//原来为{floor.Value}
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
            };
        }
        public bool Checkbucket(Point3d pipe, Point3d bucket, Polyline wboundary)
        {

            if (pipe.DistanceTo(bucket) < 10)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static List<Polyline> GetNewPipes(List<Polyline> rain_pipe)
        {
            var rain_pipes = new List<Polyline>();
            if (rain_pipe.Count > 0)
            {
                rain_pipes.Add(rain_pipe[0]);
                for (int i = 1; i < rain_pipe.Count; i++)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (rain_pipe[j].GetCenter() == rain_pipe[i].GetCenter())
                        {
                            break;
                        }
                        else
                        {
                            if (j > 0)
                            {
                                continue;
                            }
                            else
                            {
                                rain_pipes.Add(rain_pipe[i]);
                            }
                        }
                    }
                }
            }
            return rain_pipes;
        }
        public static double GetToilet_x(List<ThWCompositeRoom> room)
        {
            double x = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Toilet.Toilet.Boundary as Polyline;
                x += line.GetCenter().X;
            }
            return x;
        }
        public static double GetToilet_y(List<ThWCompositeRoom> room)
        {
            double y = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Toilet.Toilet.Boundary as Polyline;
                y += line.GetCenter().Y;
            }
            return y;
        }
        public static double GetBalconyRoom_x(List<ThWCompositeBalconyRoom> room)
        {
            double x = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Balcony.Balcony.Boundary as Polyline;
                x += line.GetCenter().X;
            }
            return x;
        }
        public static double GetBalconyRoom_y(List<ThWCompositeBalconyRoom> room)
        {
            double y = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Balcony.Balcony.Boundary as Polyline;
                y += line.GetCenter().Y;
            }
            return y;
        }
        public static DBObjectCollection GetObstacle()
        {
            DBObjectCollection obstacles = new DBObjectCollection();//定义障碍
            var poly = new Polyline();
            poly.CreatePolygon(new Point2d(698345.6372, 482936.8358), 4, 100);
            obstacles.Add(poly);
            return obstacles;
        }
        public static Point3d GetRadialPoint(Point3d Fpipeindex, ThCADCoreNTSSpatialIndex obstacle)
        {
            Point3d point = Point3d.Origin;
            double width = 175 * 7;
            Point3d dirPoint = new Point3d(Fpipeindex.X, Fpipeindex.Y - 1, 0);
            Vector3d normal = Fpipeindex.GetVectorTo(dirPoint);
            point = GetRadialPoint1(Fpipeindex, obstacle, width, normal);
            if (point == Point3d.Origin)
            {
                point = GetRadialPoint1(Fpipeindex, obstacle, -width, normal);
                if (point == Point3d.Origin)
                {
                    point = GetRadialPoint1(Fpipeindex, obstacle, width, -normal);
                    if (point == Point3d.Origin)
                    {
                        point = GetRadialPoint1(Fpipeindex, obstacle, -width, -normal);
                    }
                }
            }
            return point;
        }
        public static Point3d GetRadialPoint1(Point3d Fpipeindex, ThCADCoreNTSSpatialIndex obstacle, double width, Vector3d normal)
        {
            Point3d point = Point3d.Origin;
            for (int j = 0; j < 6; j++)
            {
                Point3d point1 = Fpipeindex + normal * 250 * (j + 2);
                var fontBox = obstacle.SelectCrossingPolygon(GetBoundary(width, point1));
                if (fontBox.Count > 0)
                {

                    continue;
                }
                else
                {
                    point = point1;
                    break;
                }
            }
            return point;
        }
        public static Polyline GetBoundary(double width, Point3d point)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(point.X, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(point.X + width, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(point.X + width, point.Y + 175), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(point.X, point.Y + 175), 0.0, 0.0, 0.0);
            return polyline;
        }
        public static double GetOffset(List<Point3dCollection> dublicatedPoints, Point3d indexPipe)
        {
            int num = 0;
            foreach (var points in dublicatedPoints)
            {
                if (points[0].X == indexPipe.X)//先确定哪一组
                {
                    for (int k = 0; k < points.Count; k++)
                    {
                        if (points[k].Y == indexPipe.Y)//再确定哪一行
                        {
                            num = k;
                        }
                    }
                }
            }
            return 250 * num;
        }
        public static Point3d GetdublicatePoint(List<Point3dCollection> dublicatedPoints, Point3d indexPipe)
        {
            Point3d point = Point3d.Origin;
            foreach (var points in dublicatedPoints)
            {
                if (points[0].X == indexPipe.X)
                {
                    for (int k = 0; k < points.Count; k++)
                    {
                        if (points[k].Y == indexPipe.Y)
                        {
                            point = points[0];
                        }
                    }
                }
            }
            return point;
        }
        public static Polyline GetCopyPipes(Entity polyline)
        {
            Circle circle = polyline as Circle;
            Polyline pipe = circle.Tessellate(50);
            return pipe;           
        }
        public static List<Polyline> GetListFpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('F'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListPpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('P'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListWpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('W'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListTpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('T'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListDpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('D'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListCopypipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('F') || toilet.Identifier.Contains('P') || toilet.Identifier.Contains('W'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListNormalCopypipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            return polylines;
        }
        public static List<Polyline> GetListFpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('F'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListPpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('P'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListWpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('W'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListTpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('T'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListDpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('D'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListCopypipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('F') || toilet.Identifier.Contains('P') || toilet.Identifier.Contains('W'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListNormalCopypipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            return polylines;
        }
        public static List<Polyline> GetPolyline(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            foreach (Entity item in toilet.Representation)
            {
                var offset = Matrix3d.Displacement(200 * ToiletPipes[0].Center.GetVectorTo(ToiletPipes[1].Center).GetNormal());
                Entity polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                polylines.Add(GetCopyPipes(polyline));
            }
            return polylines;
        }
        public static List<Entity> GetEntityPolyline(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var offset = Matrix3d.Displacement(200 * ToiletPipes[0].Center.GetVectorTo(ToiletPipes[1].Center).GetNormal());
                Entity polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                polylines.Add(polyline);
            }
            return polylines;
        }
        public static List<Polyline> GetPolyline1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Polyline>();
            foreach (Entity item in toilet.Representation)
            {
                var polyline = item.GetTransformedCopy(toilet.Matrix);
                polylines.Add(GetCopyPipes(polyline));
            }
            return polylines;
        }
        public static List<Entity> GetEntityPolyline1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var polyline = item.GetTransformedCopy(toilet.Matrix);
                polylines.Add(polyline);
            }
            return polylines;
        }
        public static Point3d GetTag(DBObjectCollection fontBox, Point3dCollection Pipeindex, int i, Matrix3d matrix1, Matrix3d Matrix)
        {
            Point3d tag = Point3d.Origin;

            if (fontBox.Count > 0)
            {
                tag = Pipeindex[i];

            }
            else
            {
                tag = Pipeindex[i].TransformBy(matrix1).TransformBy(Matrix);
            }
            return tag;
        }

    }
}
