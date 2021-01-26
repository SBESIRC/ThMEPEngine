﻿using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using DotNetARX;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Plumbing;
using static ThMEPWSS.ThPipeCmds;

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
        public static DBObjectCollection GetObstacle(List<Curve> objects)
        {
            DBObjectCollection obstacles = new DBObjectCollection();//定义障碍
            var poly = new Polyline();
            poly.CreatePolygon(new Point2d(698345.6372, 482936.8358), 4, 100);
            obstacles.Add(poly);
            objects.ForEach(o => obstacles.Add(o));
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
            polyline.AddVertexAt(2, new Point2d(point.X + width, point.Y + 200), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(point.X, point.Y + 200), 0.0, 0.0, 0.0);
            return polyline;
        }
        public static Polyline GetCircleBoundary(Circle circle)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(circle.Center.X+ circle.Radius, circle.Center.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(circle.Center.X, circle.Center.Y+ circle.Radius), 0.0, 0.0, 0.0);
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
        public static List<Entity> GetListCopypipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            if (toilet.Identifier.Contains('F') || toilet.Identifier.Contains('P') || toilet.Identifier.Contains('W'))
            {
                GetEntity(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Entity> GetListNormalCopypipes(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            GetEntity(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
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
        public static List<Entity> GetListCopypipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            if (toilet.Identifier.Contains('F') || toilet.Identifier.Contains('P') || toilet.Identifier.Contains('W'))
            {
                GetEntity1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
            }
            return polylines;
        }
        public static List<Entity> GetListNormalCopypipes1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            GetEntity1(toilet, ToiletPipes).ForEach(o => polylines.Add(o));
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
        public static List<Entity> GetEntity(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
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
        public static List<Entity> GetEntity1(ThWToiletPipe toilet, List<ThWToiletPipe> ToiletPipes)
        {
            var polylines = new List<Entity>();
            foreach (Entity item in toilet.Representation)
            {
                var polyline = item.GetTransformedCopy(toilet.Matrix);
                polylines.Add(polyline);
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
        public static Point3d GetTag(DBObjectCollection fontBox, Point3dCollection Pipeindex, int i, Matrix3d matrix1, Matrix3d Matrix, ThCADCoreNTSSpatialIndex obstacle)
        {
            Point3d tag = Point3d.Origin;

            if (fontBox.Count > 0)
            {
                if (obstacle.SelectCrossingPolygon(GetBoundary(175 * 7, Pipeindex[i])).Count>0)
                {
                    tag = GetRadialFontPoint(Pipeindex[i], obstacle, Pipeindex[i/3*3]);
                }
                else
                {
                    tag = Pipeindex[i];
                }
            }
            else
            {
                if (obstacle.SelectCrossingPolygon(GetBoundary(175 * 7, Pipeindex[i+2])).Count > 0)
                {
                    tag = GetRadialFontPoint(Pipeindex[i], obstacle, Pipeindex[i]);
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
        public static Point3d GetTag1(Point3dCollection Pipeindex, int i,ThCADCoreNTSSpatialIndex obstacle)

        {
                Point3d tag = Point3d.Origin;                 
                if (obstacle.SelectCrossingPolygon(GetBoundary(175 * 7, Pipeindex[i+2])).Count > 0)
                {
                    tag = GetRadialFontPoint(Pipeindex[i], obstacle, Pipeindex[i]);
                }
                else
                {
                    tag = Pipeindex[i];
                }
           
            return tag;
        }
        public static Point3d GetRadialFontPoint(Point3d Fpipeindex, ThCADCoreNTSSpatialIndex obstacle, Point3d Fpipeindex1)
        {
            Point3d point = Point3d.Origin;
            for (int j = 0; j < 6; j++)
            {
                Point3d point1 = Fpipeindex1 - 250 * (j )*Vector3d.YAxis.GetNormal();
                Point3d point2 = Fpipeindex1 + 250 * (j) * Vector3d.YAxis.GetNormal();
                var fontBox = obstacle.SelectCrossingPolygon(GetBoundary(175*7, point1));
                var fontBox1 = obstacle.SelectCrossingPolygon(GetBoundary(175 * 7, point2));
                if (fontBox.Count > 0)
                {
                    if (fontBox1.Count > 0)
                    {
                        continue;
                    }
                    else
                    {
                        point = point2;
                        break;
                    }
                }
                else
                {
                    point = point1;
                    break;
                }
            }
            return Fpipeindex+ Fpipeindex1.GetVectorTo(point);
        }
        public static List<BlockReference> GetGravityWaterBuckets(List<ThIfcGravityWaterBucket> GravityWaterBuckets)
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
        public static List<BlockReference> GetSideWaterBuckets(List<ThIfcSideEntryWaterBucket> GravityWaterBuckets)
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
        public static List<Polyline> GetroofRainPipe(List<ThIfcRoofRainPipe> RoofRainPipes)
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
        public static List<DBText> GetListText(Point3dCollection points, Point3dCollection points1, string s)
        {
            var texts = new List<DBText>();
            for (int i = 0; i < points.Count; i++)
            {
                texts.Add(TaggingBuckettext(points1[4 * i + 2], s));
            }
            return texts;
        }
        public static List<DBText> GetListText1(Point3dCollection points, Point3dCollection points1, string s)
        {
            var texts = new List<DBText>();
            for (int i = 0; i < points.Count; i++)
            {
                texts.Add(TaggingBuckettext(points1[4 * i + 3], s));
            }
            return texts;
        }
        public static DBText TaggingBuckettext(Point3d tag, string s)
        {
            return new DBText()
            {
                Height = 200,
                Position = tag,
                TextString = s,
                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255),
            };
        }
        public static Polyline CreatePolyline(Point3d point1, Point3d point2)
        {
            Polyline ent_line1 = new Polyline();
            ent_line1.AddVertexAt(0, point1.ToPoint2d(), 0, 35, 35);
            ent_line1.AddVertexAt(1, point2.ToPoint2d(), 0, 35, 35);
            //ent_line1.Linetype = "DASHDED";
            //ent_line1.Layer = "W-DRAI-DOME-PIPE";
            //ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
            return ent_line1;
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
        public static Polyline CreateRainline(Point3d point1, Point3d point2)
        {
            Polyline ent_line1 = new Polyline();
            ent_line1.AddVertexAt(0, point1.ToPoint2d(), 0, 35, 35);
            ent_line1.AddVertexAt(1, point2.ToPoint2d(), 0, 35, 35);
            //ent_line1.Linetype = "DASHDOT";
            //ent_line1.Layer = "W-RAIN-PIPE";
            ent_line1.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 255);
            return ent_line1;
        }
        public void InputKitchenParameters(ThWCompositeRoom composite, ThWTopCompositeParameters parameters, ThWTopParameters parameters0)
        {
            if (IsValidKitchenContainer(composite.Kitchen))
            {
                parameters.boundary = composite.Kitchen.Kitchen.Boundary as Polyline;
                parameters.outline = composite.Kitchen.DrainageWells[0].Boundary as Polyline;
                parameters.basinline = composite.Kitchen.BasinTools[0].Outline as BlockReference;
                if (composite.Kitchen.Pypes.Count > 0)
                {
                    parameters.pype = composite.Kitchen.Pypes[0].Boundary as Polyline;
                }
                else
                {
                    parameters.pype = new Polyline();
                }
                if (composite.Kitchen.RainPipes.Count > 0)
                {
                    parameters0.rain_pipe.Add(composite.Kitchen.RainPipes[0].Outline as Polyline);
                }
                if (composite.Kitchen.RoofRainPipes.Count > 0)
                {
                    Polyline s = composite.Kitchen.RoofRainPipes[0].Outline as Polyline;
                    parameters0.roofrain_pipe.Add(s);
                    parameters0.copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 38.5 });
                    parameters0.copyroofpipes.Add(new Circle() { Center = s.GetCenter(), Radius = 55.0 });
                }
            }
        }
        public bool IsValidKitchenContainer(ThWKitchenRoom kitchenContainer)
        {
            return (kitchenContainer.Kitchen != null && kitchenContainer.DrainageWells.Count == 1);
        }
        public bool IsValidToiletContainer(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Toilet != null &&
                toiletContainer.DrainageWells.Count == 1 &&
                toiletContainer.Closestools.Count == 1 &&
                toiletContainer.FloorDrains.Count > 0;
        }
        public bool IsValidToiletContainerForFloorDrain(ThWToiletRoom toiletContainer)
        {
            return toiletContainer.Toilet != null &&
                toiletContainer.FloorDrains.Count > 0;
        }
        public bool IsValidBalconyForFloorDrain(ThWBalconyRoom balconyContainer)
        {
            return balconyContainer.FloorDrains.Count > 0 && balconyContainer.Washmachines.Count > 0;
        }
        public static List<Polyline> GetListRainPipes(ThWCompositeBalconyRoom compositeBalcony)
        {
            var rainpipes = new List<Polyline>();
            foreach (var RainPipe in compositeBalcony.Balcony.RainPipes)
            {
                var ent = RainPipe.Outline as Polyline;
                rainpipes.Add(ent);
            }
            return rainpipes;
        }
        public static DBObjectCollection GetFont(List<Polyline> polylines)
        {
            var font = new DBObjectCollection();
            foreach(Polyline polyline in polylines)
            {
                font.Add(polyline); 
            }
            return font;
        }
    }
}
