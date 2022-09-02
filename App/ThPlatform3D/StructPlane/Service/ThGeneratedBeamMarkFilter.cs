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

namespace ThPlatform3D.StructPlane.Service
{
    /// <summary>
    /// 过滤在一定范围内已经生成的梁标注
    /// </summary>
    internal class ThGeneratedBeamMarkFilter
    {
        private Dictionary<Polyline, Curve> _beamPolygonCenters;
        private Dictionary<DBText, Point3d> _beamMarkOriginPos;
        private ThCADCoreNTSSpatialIndex _blkSpatialIndex;
        private readonly double _beamEdgeDistance = 150.0;
        private double _parallelAngleTolerance = 1.0;

        public List<DBObjectCollection> Results { get; private set; }
        public DBObjectCollection KeepBeamMarkBlks { get; private set; }

        public ThGeneratedBeamMarkFilter(
            Dictionary<DBText, Point3d> beamMarkOriginPos,
            Dictionary<Polyline,Curve> beamPolygonCenters,
            DBObjectCollection generatedBeamMarkBlks)
        {
            _beamMarkOriginPos = beamMarkOriginPos;
            _beamPolygonCenters = beamPolygonCenters;
            _blkSpatialIndex = new ThCADCoreNTSSpatialIndex(generatedBeamMarkBlks);           
        }
        public void Filter(List<DBObjectCollection> beamMarkGroups)
        {
            Results = new List<DBObjectCollection>();
            KeepBeamMarkBlks = new DBObjectCollection();
            Results =  beamMarkGroups.Where(group =>
            {
                string content = group.GetMultiTextString();
                var rotation = group.OfType<DBText>().First().Rotation;
                var blks = new DBObjectCollection();
                group.OfType<DBText>().ForEach(text =>
                {
                    var originPos = GetOriginPos(text); // 获取文字原始的位置                    
                    if (originPos.HasValue)
                    {
                        var polygons = GetBeamPolygons(originPos.Value);
                        polygons.OfType<Polyline>().ForEach(p =>
                        {
                            var center = _beamPolygonCenters[p];
                            if(center is Line line)
                            {
                                var perpendVec = line.LineDirection().GetPerpendicularVector();
                                var closePt = line.GetClosestPointTo(originPos.Value, true);
                                var dir = closePt.GetVectorTo(text.Position);
                                if(dir.DotProduct(perpendVec)>0)
                                {
                                    dir = perpendVec;
                                }
                                else
                                {
                                    dir = perpendVec.Negate();
                                }
                                var beamWidth = GetBeamWidth(p, center);
                                var beamArea = SingleBuffer(line, beamWidth / 2.0 + _beamEdgeDistance+text.Height, dir);
                                blks.AddRange(QueryExistedBlks(beamArea));
                            }
                            else if(center is Arc arc)
                            {
                                throw new NotImplementedException();
                            }                            
                        });
                    }
                });
                var existedBlks = blks.OfType<BlockReference>()
                .Where(o => o.Name == content && o.Rotation.IsRadianParallel(rotation, _parallelAngleTolerance))
                .ToCollection();
                KeepBeamMarkBlks.AddRange(existedBlks);
                return existedBlks.Count == 0;
            }).ToList();
        }

        private DBObjectCollection QueryExistedBlks(Polyline area)
        {
            return _blkSpatialIndex.SelectCrossingPolygon(area);
        }

        private double GetBeamWidth(Polyline polygon ,Curve center)
        {
            if(center is Line line)
            {
                var segments = GetLineSegments(polygon);
                var lineDir = line.StartPoint.GetVectorTo(line.EndPoint);
                segments = segments.Where(o => ThGeometryTool.IsParallelToEx(lineDir, o.Item1.GetVectorTo(o.Item2))).ToList();
                if(segments.Count==2)
                {
                    var pt = segments[0].Item1.GetProjectPtOnLine(segments[1].Item1, segments[1].Item2);
                    return pt.DistanceTo(segments[0].Item1);
                }
                else
                {
                    return 0.0;
                }
            }
            else if (center is Arc arc)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private List<Tuple<Point3d,Point3d>> GetLineSegments(Polyline polygon)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            for (int i=0;i< polygon.NumberOfVertices;i++)
            {
                var segType = polygon.GetSegmentType(i);
                if(segType == SegmentType.Line)
                {
                    var lineSeg = polygon.GetLineSegmentAt(i);
                    results.Add(Tuple.Create(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            return results;
        }

        private Polyline SingleBuffer(Line line,double length,Vector3d dir)
        {
            var pt1 = line.StartPoint + dir.GetNormal().MultiplyBy(length);
            var pt2 = line.EndPoint + dir.GetNormal().MultiplyBy(length);
            var pts = new Point3dCollection() { pt1,pt2,line.EndPoint,line.StartPoint};
            return pts.CreatePolyline();
        }

        private Polyline SingleBuffer(Arc arc, double length, Vector3d dir)
        {
            throw new NotImplementedException();
        }

        private Point3d? GetOriginPos(DBText text)
        {
            if(_beamMarkOriginPos.ContainsKey(text))
            {
                return _beamMarkOriginPos[text];
            }
            else
            {
                return null;
            }
        }
        private DBObjectCollection GetBeamPolygons(Point3d pt)
        {
            return _beamPolygonCenters
                .Where(o => o.Key.Contains(pt))
                .Select(o=>o.Key)
                .ToCollection();
        }
    }
}
