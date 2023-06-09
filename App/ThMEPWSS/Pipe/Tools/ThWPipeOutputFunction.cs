﻿using System.Linq;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Geom;
using ThMEPWSS.Pipe.Model;
using static ThMEPWSS.Command.ThPipeCreateCmd;

namespace ThMEPWSS.Pipe.Tools
{
    public class ThWPipeOutputFunction
    {     
        public static List<Line> GetCreateLines(Point3dCollection points, Point3dCollection point1s, string W_RAIN_NOTE1)
        {
            var lines = new List<Line>();
            for (int i = 0; i < points.Count; i++)
            {
                Line s = new Line(points[i], point1s[4 * i]);
                s.Layer = W_RAIN_NOTE1;
                lines.Add(s);
            }
            return lines;
        }
        public static List<Line> GetCreateLines1(Point3dCollection points, Point3dCollection point1s, string W_RAIN_NOTE1,string name,int factor)
        {
            var lines = new List<Line>();
            for (int i = 0; i < points.Count; i++)
            {
                Line s = new Line(point1s[4 * i], point1s[4 * i]+ (70 +140* factor*(name.Length+1))*(point1s[4 * i].GetVectorTo(point1s[4 * i + 1]).GetNormal()));
                s.Layer = W_RAIN_NOTE1;
                lines.Add(s);
            }
            return lines;
        }
        public static Circle CreateCircle(Point3d point1)
        {
            return new Circle()
            {
                Radius = 50,
                Center = point1,
                Layer = ThWPipeCommon.W_RAIN_EQPM,
            };
        }

        public static DBText Taggingtext(Point3d tag, string s, int scaleFactor, Database db)
        {
            return new DBText()
            {
                Position = tag,
                TextString = s,    
                WidthFactor=0.7,
                Height = 175 * scaleFactor,
            };
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
                Polyline line = room[i].Toilet.Space.Boundary as Polyline;
                x += line.GetCenter().X;
            }
            return x;
        }
        public static double GetToilet_y(List<ThWCompositeRoom> room)
        {
            double y = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Toilet.Space.Boundary as Polyline;
                y += line.GetCenter().Y;
            }
            return y;
        }
        public static double GetBalconyRoom_x(List<ThWCompositeBalconyRoom> room)
        {
            double x = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Balcony.Space.Boundary as Polyline;
                x += line.GetCenter().X;
            }
            return x;
        }
        public static double GetBalconyRoom_y(List<ThWCompositeBalconyRoom> room)
        {
            double y = 0.0;
            for (int i = 0; i < room.Count; i++)
            {
                Polyline line = room[i].Balcony.Space.Boundary as Polyline;
                y += line.GetCenter().Y;
            }
            return y;
        }
        public static DBObjectCollection GetObstacle(List<Curve> objects)
        {
            DBObjectCollection obstacles = new DBObjectCollection();//定义障碍
            var poly = new Polyline();
            poly.CreatePolygon(new Point2d(698345.6372, 482936.8358), 4, 100);
            obstacles.Add(poly);
            objects.ForEach(o => obstacles.Add(o));
            return obstacles;
        }
        public static Point3d GetRadialPoint(Point3d Fpipeindex, ThCADCoreNTSSpatialIndex obstacle, int scaleFactor)
        {
            Point3d point = Point3d.Origin;
            double width = 175 * 7 * scaleFactor;
            Point3d dirPoint = new Point3d(Fpipeindex.X, Fpipeindex.Y - 1, 0);
            Vector3d normal = Fpipeindex.GetVectorTo(dirPoint);
            point = GetRadialPoint1(Fpipeindex, obstacle, width, normal, scaleFactor);
            if (point == Point3d.Origin)
            {
                point = GetRadialPoint1(Fpipeindex, obstacle, -width, normal, scaleFactor);
                if (point == Point3d.Origin)
                {
                    point = GetRadialPoint1(Fpipeindex, obstacle, width, -normal, scaleFactor);
                    if (point == Point3d.Origin)
                    {
                        point = GetRadialPoint1(Fpipeindex, obstacle, -width, -normal, scaleFactor);
                    }
                }
            }
            return point;
        }
        public static Point3d GetRadialPoint1(Point3d Fpipeindex, ThCADCoreNTSSpatialIndex obstacle, double width, Vector3d normal, int scaleFactor)
        {
            Point3d point = Point3d.Origin;
            for (int j = 0; j < 6; j++)
            {
                Point3d point1 = Fpipeindex + normal * 250 * (j + 2);
                var fontBox = obstacle.SelectCrossingPolygon(GetBoundary(width, point1, scaleFactor));
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
        public static Polyline GetBoundary(double width, Point3d point, int scaleFactor)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(point.X, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(point.X + width, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(point.X + width, point.Y + 200 * scaleFactor), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(point.X, point.Y + 200 * scaleFactor), 0.0, 0.0, 0.0);
            return polyline;
        }
        public static Polyline GetCircleBoundary(Circle circle)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(circle.Center.X + circle.Radius, circle.Center.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(circle.Center.X, circle.Center.Y + circle.Radius), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(circle.Center.X - circle.Radius, circle.Center.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(circle.Center.X, circle.Center.Y - circle.Radius), 0.0, 0.0, 0.0);
            return polyline;
        }
        public static Polyline GetPolylineBoundary(Curve curve)
        {
            if (curve != null)
            {
                Polyline polyline = new Polyline()
                {
                    Closed = true
                };
                Point3d minpt = curve.GeometricExtents.MinPoint;
                Point3d maxpt = curve.GeometricExtents.MaxPoint;
                polyline.AddVertexAt(0, new Point2d(minpt.X - 1, minpt.Y - 1), 0.0, 0.0, 0.0);
                polyline.AddVertexAt(1, new Point2d(maxpt.X + 1, minpt.Y - 1), 0.0, 0.0, 0.0);
                polyline.AddVertexAt(2, new Point2d(maxpt.X + 1, maxpt.Y + 1), 0.0, 0.0, 0.0);
                polyline.AddVertexAt(3, new Point2d(minpt.X - 1, maxpt.Y + 1), 0.0, 0.0, 0.0);
                return polyline;
            }
            else
            {
                return new Polyline();
            }
        }
        public static Polyline GetBlockBoundary(BlockReference curve)
        {
            if (curve != null)
            {
                Polyline polyline = new Polyline()
                {
                    Closed = true
                };
                Point3d minpt = curve.GeometricExtents.MinPoint;
                Point3d maxpt = curve.GeometricExtents.MaxPoint;
                polyline.AddVertexAt(0, new Point2d(minpt.X - 1, minpt.Y - 1), 0.0, 0.0, 0.0);
                polyline.AddVertexAt(1, new Point2d(maxpt.X + 1, minpt.Y - 1), 0.0, 0.0, 0.0);
                polyline.AddVertexAt(2, new Point2d(maxpt.X + 1, maxpt.Y + 1), 0.0, 0.0, 0.0);
                polyline.AddVertexAt(3, new Point2d(minpt.X - 1, maxpt.Y + 1), 0.0, 0.0, 0.0);
                return polyline;
            }
            else
            {
                return new Polyline();
            }
        }
        public static Polyline GetTextBoundary(double width, double height, Point3d point)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(point.X, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(point.X + width, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(point.X + width, point.Y + height), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(point.X, point.Y + height), 0.0, 0.0, 0.0);
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
        public static List<Polyline> GetListFpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('F'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListPpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('P'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => o.Layer =W_DRAI_EQPM);
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListWpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('W'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListTpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes, string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('T'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => o.Layer= W_DRAI_EQPM);
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }

            return polylines;
        }
        public static List<Polyline> GetListDpipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('D'))
            {
                GetPolyline(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Entity> GetListCopypipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            if (toilet.Identifier.Contains('F') || toilet.Identifier.Contains('P') || toilet.Identifier.Contains('W'))
            {             
                GetEntity(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Entity> GetListNormalCopypipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            if (toilet.Identifier.Contains('T'))
            {
                GetEntity(toilet, ToiletPipes,W_DRAI_EQPM).ForEach(o => o.Layer = W_DRAI_EQPM);
            }
            else
            {
                GetEntity(toilet, ToiletPipes,W_DRAI_EQPM).ForEach(o => o.Layer = W_DRAI_EQPM);
            }
            GetEntity(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => polylines.Add(o));
            return polylines;
        }
        public static List<Polyline> GetListFpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('F'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListPpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('P'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListWpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('W'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListTpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('T'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Polyline> GetListDpipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Polyline>();
            if (toilet.Identifier.Contains('D'))
            {
                GetPolyline1(toilet, ToiletPipes).ForEach(o => o.Layer =W_DRAI_EQPM);
                GetPolyline1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Entity> GetListCopypipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            if (toilet.Identifier.Contains('F') || toilet.Identifier.Contains('P') || toilet.Identifier.Contains('W'))
            {
                GetEntity1(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => o.Layer = W_DRAI_EQPM);
                GetEntity1(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Entity> GetListNormalCopypipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            if (toilet.Identifier.Contains('T'))
            {
                GetEntity1(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => o.Layer = W_DRAI_EQPM);
            }
            else
            {
                GetEntity1(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => o.Layer = W_DRAI_EQPM);
            }
            GetEntity1(toilet, ToiletPipes, W_DRAI_EQPM).ForEach(o => polylines.Add(o));
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
        public static List<Entity> GetEntity(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var offset = Matrix3d.Displacement(200 * ToiletPipes[0].Center.GetVectorTo(ToiletPipes[1].Center).GetNormal());
                Entity polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                polyline.Layer = W_DRAI_EQPM;
                polylines.Add(polyline);
            }
            return polylines;
        }
        public static List<Entity> GetEntityPolyline(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var offset = Matrix3d.Displacement(200 * ToiletPipes[0].Center.GetVectorTo(ToiletPipes[1].Center).GetNormal());
                Entity polyline = item.GetTransformedCopy(toilet.Matrix.PostMultiplyBy(offset));
                polyline.Layer = W_DRAI_EQPM;
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
        public static List<Entity> GetEntity1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var polyline = item.GetTransformedCopy(toilet.Matrix);
                polyline.Layer = W_DRAI_EQPM;
                polylines.Add(polyline);
            }
            return polylines;
        }
        public static List<Entity> GetEntityPolyline1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes,string W_DRAI_EQPM)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var polyline = item.GetTransformedCopy(toilet.Matrix);
                polyline.Layer = W_DRAI_EQPM;
                polylines.Add(polyline);
            }
            return polylines;
        }
        public static Point3d GetTag(DBObjectCollection fontBox, Point3dCollection Pipeindex, int i, Matrix3d matrix1, Matrix3d Matrix, ThCADCoreNTSSpatialIndex obstacle, int scaleFactor,Point3dCollection originPoint)
        {
            Point3d tag = Point3d.Origin;

            if (fontBox.Count > 0)
            {
                if (obstacle.SelectCrossingPolygon(GetBoundary(175 * 7 * scaleFactor, Pipeindex[i], scaleFactor)).Count > 0)
                {
                    tag = GetRadialFontPoint(Pipeindex[i], obstacle, Pipeindex[i], scaleFactor, originPoint[i / 3]);
                }
                else 
                {
                    tag = Pipeindex[i];
                }
            }
            else
            {
                if (obstacle.SelectCrossingPolygon(GetBoundary(175 * 7 * scaleFactor, Pipeindex[i + 2], scaleFactor)).Count > 0)
                {
                    tag = GetRadialFontPoint(Pipeindex[i], obstacle, Pipeindex[i], scaleFactor, originPoint[i/3]);
                }
                else
                {
                    if (Matrix.Translation == new Vector3d(0, 0, 0))
                    { tag = Pipeindex[i]; }
                    else
                    {
                        tag = Pipeindex[i].TransformBy(matrix1).TransformBy(Matrix);
                    }
                }
            }
            return tag;
        }
        public static Point3d GetTag1(Point3dCollection Pipeindex, int i, ThCADCoreNTSSpatialIndex obstacle, int scaleFactor,Point3dCollection originPoint)

        {
            Point3d tag = Point3d.Origin;
            if (i + 2 < Pipeindex.Count)
            {
                if (obstacle.SelectCrossingPolygon(GetBoundary(175 * 7 * scaleFactor, Pipeindex[i + 2], scaleFactor)).Count > 0)
                {
                    tag = GetRadialFontPoint(Pipeindex[i], obstacle, Pipeindex[i], scaleFactor, originPoint[i / 3]);
                }
                else
                {
                    tag = Pipeindex[i];
                }
            }
            else
            {
                tag = Pipeindex[i];
            }

            return tag;
        }
        public static Point3d GetRadialFontPoint(Point3d Fpipeindex, ThCADCoreNTSSpatialIndex obstacle, Point3d Fpipeindex1, int scaleFactor,Point3d originPoint)
        {
            Point3d point = Point3d.Origin;
            for (int j = 0; j < 6; j++)
            {
                Point3d point1 = Fpipeindex1 - 250 * (j) * Vector3d.YAxis.GetNormal();
                Point3d point2 = Fpipeindex1 + 250 * (j) * Vector3d.YAxis.GetNormal();
                var fontBox = obstacle.SelectCrossingPolygon(GetBoundary(175 * 7 * scaleFactor, point1, scaleFactor));
                var fontBox1 = obstacle.SelectCrossingPolygon(GetBoundary(175 * 7 * scaleFactor, point2, scaleFactor));
                if (fontBox.Count > 0)
                {
                    if (fontBox1.Count > 0)
                    {
                        continue;
                    }
                    else if(fontBox1.Count ==0&& point2.DistanceTo(originPoint) >10)
                    {
                        point = point2;
                        break;
                    }
                }
                else if(fontBox.Count==0&&point1.DistanceTo(originPoint) > 10)
                {
                    point = point1;
                    break;
                }
            }
            return Fpipeindex + Fpipeindex1.GetVectorTo(point);
        }
        public static List<BlockReference> GetGravityWaterBuckets(List<ThWGravityWaterBucket> GravityWaterBuckets)
        {
            var gravityWaterBucket = new List<BlockReference>();
            foreach (var gravity in GravityWaterBuckets)
            {
                BlockReference block = null;
                block = gravity.Outline as BlockReference;
                gravityWaterBucket.Add(block);
            }
            return gravityWaterBucket;
        }
        public static List<BlockReference> GetSideWaterBuckets(List<ThWSideEntryWaterBucket> GravityWaterBuckets)
        {
            var gravityWaterBucket = new List<BlockReference>();
            foreach (var gravity in GravityWaterBuckets)
            {
                BlockReference block = null;
                block = gravity.Outline as BlockReference;
                gravityWaterBucket.Add(block);
            }
            return gravityWaterBucket;
        }
        public static List<Polyline> GetroofRainPipe(List<ThWRoofRainPipe> RoofRainPipes)
        {
            var roofRainPipe = new List<Polyline>();
            foreach (var pipe in RoofRainPipes)
            {
                Polyline block = null;
                block = pipe.Outline as Polyline;
                roofRainPipe.Add(block);
            }
            return roofRainPipe;
        }
        public static List<DBText> GetListText(Point3dCollection points, Point3dCollection points1, string s,int scaleFactor, string W_RAIN_NOTE1, Database db)
        {
            var texts = new List<DBText>();
            for (int i = 0; i < points.Count; i++)
            {
                texts.Add(TaggingBuckettext(points1[4 * i + 2], s, scaleFactor, W_RAIN_NOTE1, db));
            }
            return texts;
        }
        public static List<DBText> GetListText1(Point3dCollection points, Point3dCollection points1, string s,int scaleFactor, string W_RAIN_NOTE1, Database db)
        {
            var texts = new List<DBText>();
            for (int i = 0; i < points.Count; i++)
            {
                texts.Add(TaggingBuckettext(points1[4 * i + 3], s, scaleFactor, W_RAIN_NOTE1, db));
            }
            return texts;
        }
        public static DBText TaggingBuckettext(Point3d tag, string s, int scaleFactor, string W_RAIN_NOTE1, Database db)
        {
            return new DBText()
            {
                Position = tag,
                TextString = s,
                WidthFactor = 0.7,
                Layer = W_RAIN_NOTE1,
                Height = 200 * scaleFactor,
            };
        }
        public static Polyline CreatePolyline(Point3d point1, Point3d point2, List<string> strings, string pipeLayer)
        {
            Polyline ent_line1 = new Polyline();
            ent_line1.AddVertexAt(0, point1.ToPoint2d(), 0, 35, 35);
            ent_line1.AddVertexAt(1, point2.ToPoint2d(), 0, 35, 35);
            //ent_line1.Linetype = "DASHDED";   
            ent_line1.Layer = Get_Layers(strings, pipeLayer);
            //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
            return ent_line1;
        }
        public static string Get_Layers(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if(s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_DRAI_SEWA_PIPE1))
                {
                    return ThWPipeCommon.W_DRAI_SEWA_PIPE1;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_DRAI_NOTE))
                {
                    return ThWPipeCommon.W_DRAI_NOTE;
                }
            }
            return (null);
        }
        public static string Get_Layers6(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if (s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_RAIN_NOTE))
                {
                    return ThWPipeCommon.W_RAIN_NOTE;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(ThWPipeCommon.W_RAIN_NOTE))
                {
                    return s;
                }
            }
            return "";
        }
        public static string Get_Layers1(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if (s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_DRAI_NOTE))
                {
                    return ThWPipeCommon.W_DRAI_NOTE;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(ThWPipeCommon.W_DRAI_NOTE))
                {
                    return s;
                }
            }
            return "";
        }
        public static string Get_Layers2(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if (s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_RAIN_EQPM))
                {
                    return ThWPipeCommon.W_RAIN_EQPM;
                }              
            }
            foreach (string s in strings)
            {
                if (s.Contains(ThWPipeCommon.W_RAIN_EQPM))
                {
                   return s;
                }
            }
                return (null);
        }
        public static string Get_Layers3(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if (s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_DRAI_NOTE))
                {
                    return ThWPipeCommon.W_DRAI_NOTE;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(ThWPipeCommon.W_DRAI_NOTE))
                {
                   return s;
                }
            }
            return (ThWPipeCommon.W_DRAI_EQPM);
        }
        public static string Get_Layers4(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if (s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_RAIN_EQPM))
                {
                    return ThWPipeCommon.W_RAIN_EQPM;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(ThWPipeCommon.W_RAIN_EQPM))
                {
                    return s;
                }
            }
            return (null);
        }
        public static string Get_Layers5(List<string> strings, string pipeLayer)
        {
            foreach (string s in strings)
            {
                if (s.Equals(pipeLayer))
                {
                    return pipeLayer;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(pipeLayer))
                {
                    return s;
                }
            }
            foreach (string s in strings)
            {
                if (s.Equals(ThWPipeCommon.W_DRAI_EQPM))
                {
                    return ThWPipeCommon.W_DRAI_EQPM;
                }
            }
            foreach (string s in strings)
            {
                if (s.Contains(ThWPipeCommon.W_DRAI_EQPM))
                {
                    return s;
                }
            }
            return "";       
        }
        public static List<BlockReference> GetListFloorDrain(ThWCompositeBalconyRoom compositeBalcony, ThWTopBalconyParameters parameters)
        {
            var floodrains = new List<BlockReference>();
            foreach (var FloorDrain in compositeBalcony.Balcony.FloorDrains)
            {
                parameters.floordrain = FloorDrain.Outline as BlockReference;
                floodrains.Add(parameters.floordrain);
            }
            return floodrains;
        }
        public static Polyline CreateRainline(Point3d point1, Point3d point2,string W_RAIN_PIPE)
        {
            Polyline ent_line1 = new Polyline();
            ent_line1.AddVertexAt(0, point1.ToPoint2d(), 0, 35, 35);
            ent_line1.AddVertexAt(1, point2.ToPoint2d(), 0, 35, 35);   
            ent_line1.Layer = W_RAIN_PIPE;
            return ent_line1;
        }
        public void InputKitchenParameters(ThWCompositeRoom composite, ThWTopCompositeParameters parameters, ThWTopParameters parameters0)
        {
            if (composite.Kitchen.Space!=null)
            {
                parameters.boundary = composite.Kitchen.Space.Boundary as Polyline;
                if (composite.Kitchen.DrainageWells.Count > 1)
                {
                    var kitchenWell = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                    var kitchenWell1 = composite.Kitchen.DrainageWells[1].Boundary as Polyline;
                    if (composite.Toilet.DrainageWells.Count>0)
                    {
                        var toiletWell= composite.Toilet.DrainageWells[0].Boundary as Polyline;
                        if (kitchenWell.GetCenter().DistanceTo(toiletWell.GetCenter())< kitchenWell1.GetCenter().DistanceTo(toiletWell.GetCenter()))
                        {
                            parameters.outline = kitchenWell;
                        }
                        else
                        {
                            parameters.outline = kitchenWell1;
                        }
                    }
                }
                else if(composite.Kitchen.DrainageWells.Count==1)
                {
                    var boundary = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                    if (boundary.Area < 100000)
                    {
                        parameters.outline = boundary;
                    }
                    else
                    {
                        if (composite.Toilet.DrainageWells.Count > 1)
                        {
                            if((composite.Toilet.DrainageWells[0].Boundary as Polyline).GetCenter().DistanceTo((composite.Kitchen.DrainageWells[0].Boundary as Polyline).GetCenter())>10)
                            {
                                parameters.outline = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                            }
                            else
                            {
                                parameters.outline = composite.Toilet.DrainageWells[1].Boundary as Polyline;
                            }
                        }
                        else
                        {
                            parameters.outline = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                        }
                    }
                }
                else
                {
                     if (composite.Toilet.DrainageWells.Count > 1)
                        {
                            if((composite.Toilet.DrainageWells[0].Boundary as Polyline).GetCenter().DistanceTo(parameters.boundary.GetCenter()) <
                                     (composite.Toilet.DrainageWells[1].Boundary as Polyline).GetCenter().DistanceTo(parameters.boundary.GetCenter()))
                            {
                                parameters.outline = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                            }
                            else
                            {
                                parameters.outline = composite.Toilet.DrainageWells[1].Boundary as Polyline;
                            }
                        }
                        else
                        {
                            parameters.outline = composite.Toilet.DrainageWells[0].Boundary as Polyline;
                        }
                }
                if (!(GeomUtils.PtInLoop(parameters.boundary, parameters.outline.GetCenter())) && !(GeomUtils.PtInLoop(composite.Toilet.Space.Boundary as Polyline, parameters.outline.GetCenter()))&& composite.Toilet.DrainageWells.Count>0)
                {
                    if (parameters.outline.GetCenter().DistanceTo((composite.Toilet.DrainageWells[0].Boundary as Polyline).GetCenter()) > 10)
                    {
                        parameters.boundary = GetkitchenBoundary(parameters.boundary, parameters.outline);
                    }
                }
                if(composite.Kitchen.DrainageWells.Count>1)
                {
                    foreach(var drainWell in composite.Kitchen.DrainageWells)
                    {
                        Polyline wellOutline = drainWell.Boundary as Polyline;
                        if (GeomUtils.PtInLoop(parameters.boundary, wellOutline.GetCenter()))
                        {
                            parameters.outline = wellOutline;
                            break;
                        }
                    }
                }
                if (composite.Kitchen.BasinTools.Count > 0)
                {
                    parameters.basinline = composite.Kitchen.BasinTools[0].Outline as BlockReference;
                }
                if (composite.Kitchen.Pypes.Count > 0)
                {
                    parameters.pype = composite.Kitchen.Pypes[0].Boundary as Polyline;
                }
                else
                {
                    if (composite.Kitchen.DrainageWells.Count > 1)
                    {
                        if ((composite.Kitchen.DrainageWells[0].Boundary as Polyline).GetCenter().DistanceTo(parameters.outline.GetCenter()) > 10)
                        {
                            parameters.pype = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                        }
                        else
                        {
                            parameters.pype = composite.Kitchen.DrainageWells[1].Boundary as Polyline;
                        }
                        parameters.boundary = GetkitchenBoundary(parameters.boundary, parameters.pype);
                    }
                    else
                    {
                        if(parameters.outline.GetCenter().DistanceTo((composite.Kitchen.DrainageWells[0].Boundary as Polyline).GetCenter())>10)
                        {
                            parameters.pype = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                            parameters.boundary = GetkitchenBoundary(parameters.boundary, parameters.pype);
                        }
                    }
                }
                if (composite.Kitchen.RainPipes.Count > 0)
                {
                    parameters0.rain_pipe.Add(composite.Kitchen.RainPipes[0].Outline as Polyline);
                }
                if (composite.Kitchen.RoofRainPipes.Count > 0)
                {
                    foreach (var rpipe in composite.Kitchen.RoofRainPipes)
                    {
                        Polyline s = rpipe.Outline as Polyline;
                        parameters0.roofrain_pipe.Add(s);
                        parameters0.copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 38.5 });
                        parameters0.copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 55.0 });
                    }
                }
                if (composite.Kitchen.CondensePipes.Count > 0)
                {
                    foreach (var pipe in composite.Kitchen.CondensePipes)
                    {
                        parameters0.npipe.Add(pipe.Outline as Polyline);
                    }
                }
            }
        }
        public static Polyline GetkitchenBoundary(Polyline roofSpaces, Polyline StandardSpaces)
        {
            var pts = new Point3dCollection();
            pts.Add(roofSpaces.GeometricExtents.MinPoint);
            pts.Add(roofSpaces.GeometricExtents.MaxPoint);
            pts.Add(roofSpaces.GeometricExtents.MinPoint);
            pts.Add(roofSpaces.GeometricExtents.MaxPoint);
            double minpt_x = double.MinValue;
            double minpt_y = double.MinValue;
            double maxpt_x = double.MaxValue;
            double maxpt_y = double.MaxValue;
            for(int i=0;i< pts.Count;i++)
            {
                if (pts[i].X > minpt_x)
                {
                    minpt_x = pts[i].X;
                }
                if (pts[i].Y > minpt_y)
                {
                    minpt_y = pts[i].Y;
                }
                if (pts[i].X < maxpt_x)
                {
                    maxpt_x = pts[i].X;
                }
                if (pts[i].Y < maxpt_y)
                {
                    maxpt_y = pts[i].Y;
                }
            }       
            return GetNewPolyline(maxpt_x, maxpt_y, minpt_x, minpt_y);
        }
        private static Polyline GetNewPolyline(double x1, double y1, double x2, double y2)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(x1, y1), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(x2, y1), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(x2, y2), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(x1, y2), 0.0, 0.0, 0.0);
            return polyline;
        }
        public bool IsValidToiletContainer(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Space != null &&
                toiletContainer.DrainageWells.Count >0 &&
                toiletContainer.Closestools.Count == 1 
               ;
        }
        public bool IsValidToiletContainerForFloorDrain(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Space != null &&
                toiletContainer.FloorDrains.Count > 0;
        }
        public bool IsValidBalconyForFloorDrain(ThWBalconyRoom balconyContainer)
        {
            return balconyContainer.FloorDrains.Count > 0 ;
        }
        public static List<Polyline> GetListRainPipes(ThWCompositeBalconyRoom compositeBalcony)
        {
            var rainpipes = new List<Polyline>();
            foreach (var RainPipe in compositeBalcony.Balcony.RainPipes)
            {
                Polyline ent = RainPipe.Outline as Polyline;
                ent.Closed = true;
                rainpipes.Add(ent);
            }
            return rainpipes;
        }
        public static DBObjectCollection GetFont(List<Polyline> polylines)
        {
            var font = new DBObjectCollection();
            foreach (Polyline polyline in polylines)
            {
                font.Add(polyline);
        }
            return font;
        }
   
    }
}
