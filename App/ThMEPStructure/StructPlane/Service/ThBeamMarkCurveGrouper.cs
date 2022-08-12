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
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.StructPlane.Service
{
    // 对梁线和文字分组
    internal class ThBeamMarkCurveGrouper
    {
        private double _pointTolerance = 1.0;
        private Dictionary<DBText, ThGeometry> _beamMarkDict;
        private Dictionary<Curve, ThGeometry> _beamCurveDict;
        private ThCADCoreNTSSpatialIndex _beamCurveSpatialIndex;

        public ThBeamMarkCurveGrouper(List<ThGeometry> beamGeos, List<ThGeometry> beamMarks)
        {
            // 便于查询            
            _beamCurveDict = new Dictionary<Curve, ThGeometry>();
            beamGeos.Where(o => o.Boundary is Curve).ForEach(o => _beamCurveDict.Add(o.Boundary as Curve, o));

            _beamMarkDict = new Dictionary<DBText, ThGeometry>();
            beamMarks.Where(o => o.Boundary is DBText).ForEach(o => _beamMarkDict.Add(o.Boundary as DBText, o));

            _pointTolerance = ThStructurePlaneCommon.PointTolerance;            
            _beamCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(_beamCurveDict.Keys.ToCollection());
        }
        public List<Dictionary<ThGeometry,List<ThGeometry>>> Group()
        {
            // 查找文字两边的梁线
            var beamMarkPairs = GetBeamMarkPairCurves();
            // 按文字具有共边来分组
            var beamMarkGroups = MergeBeamMarks(beamMarkPairs);
            // 返回结果
            // 一个字典表示一组,key表示beamMark,value表示两边的梁线
            var results = new List<Dictionary<ThGeometry, List<ThGeometry>>>();
            beamMarkGroups.ForEach(g =>
            {
                var groupDict = new Dictionary<ThGeometry, List<ThGeometry>>();
                g.OfType<DBText>().ForEach(o =>
                {
                    var markGeo = _beamMarkDict[o];
                    var sides = beamMarkPairs[o].OfType<Curve>().Select(c => _beamCurveDict[c]).ToList();
                    groupDict.Add(markGeo, sides); 
                });
                results.Add(groupDict);
            });            
            return results;
        }

        public Dictionary<Polyline, Curve> CreateBeamPolygons(List<Dictionary<ThGeometry, List<ThGeometry>>> beamMarkPairs)
        {
            var beamPolygons = new Dictionary<Polyline, Curve>();
            beamMarkPairs.ForEach(o =>
            {
                o.ForEach(k =>
                {
                    if (k.Value.Count == 2 && k.Value[0].Boundary is Curve curve1 && k.Value[1].Boundary is Curve curve2)
                    {
                        var polygon = curve1.CreateBeamPolygon(curve2);
                        var center = curve1.CreateBeamCenter(curve2);
                        beamPolygons.Add(polygon, center);
                    }
                });
            });
            return beamPolygons;
        }

        private List<DBObjectCollection> MergeBeamMarks(Dictionary<DBText, DBObjectCollection> beamMarkPairs)
        {
            // 把有相同边的文字分为一组，文字的内容是相同的，方向是相同的，且文字的Position是在一条线上的

            // 标注内容相同，且具有一条共边的就是一组
            var marks = beamMarkPairs.Keys.ToCollection();
            var markSpatialIndex = new ThCADCoreNTSSpatialIndex(marks);
            // 把具有相同标注内容的文字分组
            var textStringGroups = GroupByTextString(marks);

            // 按文字方向和位置分组
            var positionDirectionGroups = new List<DBObjectCollection>();
            textStringGroups.ForEach(group =>
            {
                var newGroupExtents = GetExtents(group);
                var detectLength = GetCornerLength(newGroupExtents);
                var newGroups = group.OfType<DBObject>().ToCollection();
                while (newGroups.Count > 0)
                {
                    var first = newGroups.OfType<DBText>().First();
                    newGroups.Remove(first);
                    var textDirection = Vector3d.XAxis.RotateBy(first.Rotation, first.Normal);
                    var outline = first.Position.CreateRectangle(textDirection, detectLength * 2.0, 2.0);
                    var sameLineMarks = markSpatialIndex.SelectCrossingPolygon(outline);
                    // 索引中获取的元素必须存在于group中
                    Intersection(sameLineMarks, group);
                    // 过滤存在于positionDirectionGroups中的元素
                    sameLineMarks = sameLineMarks
                    .OfType<DBObject>()
                    .Where(o => !positionDirectionGroups.Where(k => Contains(k, o)).Any())
                    .ToCollection();

                    var parallelMarks = sameLineMarks
                    .OfType<DBText>()
                    .Where(o => o.Rotation.IsAngleParallel(first.Rotation))
                    .ToCollection();
                    parallelMarks.OfType<DBObject>().ForEach(o => newGroups.Remove(o));
                    positionDirectionGroups.Add(parallelMarks);
                }
            });

            // 根据共边来分组
            var results = new List<DBObjectCollection>();
            positionDirectionGroups.ForEach(group =>
            {
                while (group.Count > 0)
                {
                    var first = group.OfType<DBText>().First();
                    group.Remove(first);
                    var firstEdges = beamMarkPairs[first];
                    var sameEdges = group.OfType<DBText>().Where(o => HasCommonEdge(firstEdges, beamMarkPairs[o])).ToCollection();
                    sameEdges.OfType<DBObject>().ForEach(o => group.Remove(o));
                    sameEdges.Add(first);
                    results.Add(sameEdges);
                }
            });
            return results;
        }

        private void Intersection(DBObjectCollection firstObjs,DBObjectCollection secondObjs)
        {
            var firstHash = new HashSet<DBObject>(firstObjs.OfType<DBObject>());
            var secondHash = new HashSet<DBObject>(secondObjs.OfType<DBObject>());
            firstHash.IntersectWith(secondHash);
        }

        private bool Contains(DBObjectCollection objs,DBObject obj)
        {
            var hash = new HashSet<DBObject>(objs.OfType<DBObject>());
            return hash.Contains(obj);
        }

        private Extents3d GetExtents(DBObjectCollection objs)
        {
            var extents = new Extents3d();
            foreach (Entity entity in objs.OfType<Entity>())
            {
                if(entity.Bounds.HasValue)
                {
                    extents.AddExtents(entity.GeometricExtents);
                }
            }
            return extents;
        }

        private double GetCornerLength(Extents3d extents)
        {
            var xLen = extents.MaxPoint.X - extents.MinPoint.X;
            var yLen = extents.MaxPoint.Y - extents.MinPoint.Y;
            return Math.Sqrt(xLen * xLen + yLen * yLen);
        }

        private bool HasCommonEdge(DBObjectCollection firstEdges,DBObjectCollection secondEdges)
        {
            return firstEdges.OfType<DBObject>().Where(o => secondEdges.Contains(o)).Any();
        }

        private List<DBObjectCollection> GroupByTextString(DBObjectCollection beamMarks)
        {
            var results = new List<DBObjectCollection>();
            var groups = beamMarks.OfType<DBText>().GroupBy(o => o.TextString);
            foreach(var group in groups)
            {
                results.Add(group.ToCollection());
            }
            return results;
        }

        private Dictionary<DBText,DBObjectCollection> GetBeamMarkPairCurves()
        {
            var results = new Dictionary<DBText, DBObjectCollection>();
            foreach (var item in _beamMarkDict)
            {
                var markMoveDir = GetMarkDirection(item.Value);
                var beamText = item.Key as DBText;
                if (markMoveDir.Length <= 1e-6 || beamText == null)
                {
                    continue;
                }
                // 要求梁文字Position是压在中心线上
                var beamSpec = beamText.TextString.GetBeamSpec();
                if (!beamSpec.IsBeamSpec())
                {
                    continue;
                }
                var specSizes = beamSpec.GetDoubles();
                var beamWidth = specSizes[0];
                if(beamWidth == 0.0)
                {
                    continue;
                }
                // 根据文字中心、文字移动方向和宽度,获取标注两边的线
                var envelop = beamText.Position.CreateRectangle(markMoveDir, beamWidth * 1.1, 1.0);
                var beamCurves = GetMarkAndLinePair(envelop, beamWidth);
                if(beamCurves.Count==2)
                {
                    results.Add(beamText, beamCurves);
                }
                envelop.Dispose();
            }
            return results;
        }

        private DBObjectCollection GetMarkAndLinePair(Polyline envelop,double beamWidth)
        {
            var beamCurves = QueryBeamCurves(envelop);
            if(beamCurves.Count==2)
            {
                if(beamCurves[0] is Line line1 && beamCurves[1] is Line line2)
                {
                    if(IsValid(line1,line2,beamWidth))
                    {
                        return beamCurves;
                    }
                }
                else if(beamCurves[0] is Arc arc1 && beamCurves[1] is Arc arc2)
                {
                    // not support
                }
            }
            else
            {
                //TODO

            }
            return new DBObjectCollection();
        }

        private bool IsValid(Line line1,Line line2,double gap,double gapTolerance=1.0)
        {
            return ThGeometryTool.IsParallelToEx(line1.LineDirection(), line2.LineDirection()) &&
                Math.Abs(line1.ParallelDistanceTo(line2) - gap)<= gapTolerance;
        }

        private DBObjectCollection QueryBeamCurves(Polyline outline)
        {
            return _beamCurveSpatialIndex.SelectCrossingPolygon(outline);
        }

        private Point3d? GetTextCenter(DBText text)
        {
            Point3d? textCenter = null;
            var obb = text.TextOBB();
            if (obb.Area > 1e-6 && obb.NumberOfVertices > 3)
            {
                textCenter = obb.GetPoint3dAt(0).GetMidPt(obb.GetPoint3dAt(2));
            }
            obb.Dispose();
            return textCenter;
        }

        private Vector3d GetMarkDirection(ThGeometry beamMark)
        {
            if (beamMark.Properties.ContainsKey(ThSvgPropertyNameManager.DirPropertyName))
            {
                return beamMark.Properties.GetDirection().ToVector();
            }
            else
            {
                return new Vector3d();
            }
        }
    }
}
