using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineBuilderEngine
    {
        private const double AreaTolerance = 1.0;
        private const double AngleTolerance = 1.0;
        private const double BufferDistance = 50.0; //用于处理墙、门、窗、柱等元素之间不相接的Case
        private const double SmallLineLengthTolerance = 1.0;
        private const double Colliear_Gap_Distance = 2.0;
        public double LineExtendDistance { get; set; } = 10.0;
        public double ArcTessellateLength { get; set; } = 100.0;
        private Matrix3d WcsToUcs { get; set; }
        public DBObjectCollection Areas { get; set; }
        //创建数据
        public ThRoomOutlineBuilderEngine()
        {
            Areas = new DBObjectCollection();
            WcsToUcs = AcHelper.Active.Editor.GetMatrixFromWcsToUcs();
        }
        public void Build(DBObjectCollection objs)
        {
            // 转成线 + 对线进行合并处理
            var lines = objs.ToLines(ArcTessellateLength);
            lines = FilterSmallLines(lines, SmallLineLengthTolerance);
            lines = Extend(lines, LineExtendDistance);
            lines = Merge(lines);
            // 造面
            Areas = lines.PolygonsEx();

            // 后处理
            Areas = PostProcess(Areas);
        }  
        
        public DBObjectCollection PostProcess(DBObjectCollection objs)
        {
            var results = objs.FilterSmallArea(AreaTolerance);
            var roomSimplifier = new ThRoomOutlineSimplifier();
            results = roomSimplifier.Normalize(results);
            results = results.FilterSmallArea(AreaTolerance);
            results = roomSimplifier.MakeValid(results);
            results = results.FilterSmallArea(AreaTolerance);
            results = roomSimplifier.Simplify(results);
            results = results.FilterSmallArea(AreaTolerance);
            return results;
        }

        private DBObjectCollection Merge(DBObjectCollection lines)
        {
            // 先按角度分度
            var angleGroups = GroupByAngle(lines);

            // 在对角度是0和90度继续分组
            var groups = new List<DBObjectCollection>();
            foreach (var item in angleGroups)
            {
                if (item.Key == 0.0)
                {
                    groups.AddRange(GroupByYCoordinate(item.Value));
                }
                else if (item.Key == 90.0)
                {
                    groups.AddRange(GroupByXCoordinate(item.Value));
                }
                else
                {
                    groups.Add(item.Value);
                }
            }
            var lineResults = new DBObjectCollection();
            groups.ForEach(g =>
            {
                var results = GroupLines(g);
                results.ForEach(o => lineResults.Add(ToLine(o)));
            });
            return lineResults;
        }

        private Dictionary<double, DBObjectCollection> GroupByAngle(DBObjectCollection lines)
        {
            var results = new Dictionary<double, DBObjectCollection>();
            var groups = lines.OfType<Line>().GroupBy(o => ByAngle(GetUcsAngle(o.StartPoint.GetVectorTo(o.EndPoint))));
            foreach (var item in groups)
            {
                results.Add(item.Key, item.ToCollection());
            }
            return results;
        }

        private double GetUcsAngle(Vector3d wcsVec)
        {
            var newVec = wcsVec.TransformBy(WcsToUcs);
            return newVec.GetAngleTo(Vector3d.XAxis).RadToAng();
        }

        private Point3d GetUcsPoint(Point3d wcsPt)
        {
            var newPt = wcsPt.TransformBy(WcsToUcs);
            return newPt;
        }

        private double ByAngle(double ang)
        {
            ang %= 180.0;
            if (ang < AngleTolerance || Math.Abs(ang - 180.0) < AngleTolerance)
            {
                return 0.0;
            }
            if (Math.Abs(ang - 90.0) < AngleTolerance || Math.Abs(ang - 270.0) < AngleTolerance)
            {
                return 90.0;
            }
            return ang;
        }

        private List<DBObjectCollection> GroupByXCoordinate(DBObjectCollection yDirLines)
        {
            var results = new List<DBObjectCollection>();
            var xCoords = yDirLines.OfType<Line>().Select(o => GetUcsPoint(o.StartPoint).X).Distinct().OrderBy(o => o).ToList();
            var groups = yDirLines.OfType<Line>().GroupBy(o => GetCoordGroupKey(xCoords, GetUcsPoint(o.StartPoint).X));
            foreach (var group in groups)
            {
                results.Add(group.ToCollection());
            }
            return results;
        }

        private List<DBObjectCollection> GroupByYCoordinate(DBObjectCollection xDirLines)
        {
            var results = new List<DBObjectCollection>();
            var yCoords = xDirLines.OfType<Line>().Select(o => GetUcsPoint(o.StartPoint).Y).Distinct().OrderBy(o=>o).ToList();
            var groups = xDirLines.OfType<Line>().GroupBy(o => GetCoordGroupKey(yCoords, GetUcsPoint(o.StartPoint).Y));
            foreach(var group in groups)
            {
                results.Add(group.ToCollection());
            }
            return results;
        }

        private double GetCoordGroupKey(List<double> values,double coord)
        {
            for(int i=0;i<values.Count;i++)
            {
                if(Math.Abs(coord-values[i])<= Colliear_Gap_Distance)
                {
                    return values[i];
                }
            }
            return coord;
        }

        private Line ToLine(DBObjectCollection colliearLines)
        {
            var res = colliearLines.OfType<Line>().ToList().GetCollinearMaxPts();
            return new Line(res.Item1,res.Item2);
        }

        private List<DBObjectCollection> GroupLines(DBObjectCollection lines)
        {
            var results = new List<DBObjectCollection>();
            var sptialIndex = new ThCADCoreNTSSpatialIndex(lines);
            lines.OfType<Line>().ForEach(o =>
            {
                var res = Query(o, sptialIndex);
                if(res.Count==1)
                {
                    results.Add(res);
                }
                else if(res.Count >1)
                {
                   var groupIndex =  FindGroupIndex(results, res);
                    if(groupIndex == -1)
                    {
                        results.Add(res);
                    }
                    else
                    {
                        results[groupIndex] = results[groupIndex].Union(res);
                    }
                }
            });           
            return results;
        }

        private int FindGroupIndex(List<DBObjectCollection> groups, DBObjectCollection newGroup)
        {
            for(int i =0;i< groups.Count;i++)
            {
                if(newGroup.OfType<Line>().Where(o => groups[i].Contains(o)).Any())
                {
                    return i;
                }
            }
            return -1;
        }

        private DBObjectCollection Query(Line line,ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var envelop = ThDrawTool.ToOutline(line.StartPoint, line.EndPoint, Colliear_Gap_Distance);
            return spatialIndex
                .SelectCrossingPolygon(envelop)
                .OfType<Line>()
                .Where(o => ThGeometryTool.IsCollinearEx(o.StartPoint,o.EndPoint,line.StartPoint,line.EndPoint))
                .ToCollection();
        }

        private DBObjectCollection FilterSmallLines(DBObjectCollection lines,double length)
        {
            return lines.OfType<Line>().Where(o => o.Length > length).ToCollection();
        }
        private DBObjectCollection Extend(DBObjectCollection lines,double length)
        {
            var results = new DBObjectCollection();
            lines.OfType<Line>().ForEach(o =>
            {
                results.Add(o.ExtendLine(length));
            });
            return results;
        }
        private void CloseAndFilter(DBObjectCollection objs)
        {
            // 此逻辑是想把所有传入的元素转成Polygon
            // 因为目前是要弧段打散，造出的面瑕疵很大
            // 后期若能完全支持弧，此逻辑也是可以使用的。
            // 把传入的数据全部转成Polygon
            var polygons = ToAcPolygons(objs, BufferDistance);

            // 造面
            Areas = polygons.PolygonsEx();
            Areas = Areas.FilterSmallArea(AreaTolerance);

            // 新造的区域,因为扩大导致面积变小，需要扩大
            Areas = Buffer(Areas, BufferDistance);
            Areas = Areas.FilterSmallArea(AreaTolerance);
        }
        private DBObjectCollection ToAcPolygons(DBObjectCollection objs,double dis)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            objs.OfType<Entity>().ForEach(e =>
            {
                if (e is Polyline poly)
                {
                    if (IsClosed(poly))
                    {
                        poly.Closed = true;
                    }
                    var newPoly = bufferService.Buffer(poly, dis);
                    if (newPoly != null)
                    {
                        results.Add(newPoly);
                    }
                }
                else if(e is MPolygon mPolygon)
                {
                    var newPolygon = bufferService.Buffer(mPolygon, dis) as MPolygon;
                    if (newPolygon != null)
                    {
                        results.Add(newPolygon.Shell());
                        newPolygon.Holes().ForEach(h => results.Add(h));
                    }
                }
                else if (e is Line line)
                {
                    results.Add(line.BufferSquare(dis));
                }
                else
                {
                    //TODO
                }
            });            
            return results;
        }

        private DBObjectCollection Buffer(DBObjectCollection polygons, double length)
        {
            var results = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            polygons.OfType<Entity>().ForEach(e =>
            {
                try
                {
                    var bufferEntity = bufferService.Buffer(e, length);
                    if (bufferEntity != null)
                    {
                        results.Add(bufferEntity);
                    }
                }
                catch
                {    
                    //
                }
            });
            return results;
        }

        private bool IsClosed(Polyline poly,double tolerance =5.0)
        {
            return poly.Closed || poly.StartPoint.DistanceTo(poly.EndPoint) <= tolerance;
        }

        public Entity Query(Point3d point)
        {
            var outlines =  ContainsPoint(Areas, point);
            if(outlines.Count==0)
            {
                return null;
            }
            else
            {
                return outlines.Cast<Entity>().OrderByDescending(e => e.EntityArea()).First();
            }
        }

        private DBObjectCollection ContainsPoint(DBObjectCollection polygons,Point3d point)
        {
            var result = new DBObjectCollection();
            foreach (DBObject obj in polygons)
            {
                if (obj is Polyline polyline && polyline.Contains(point))
                {
                    result.Add(polyline);
                }
                else if (obj is MPolygon polygon && polygon.Contains(point))
                {
                    result.Add(polygon);
                }
            }
            return result;
        }
    }
}
