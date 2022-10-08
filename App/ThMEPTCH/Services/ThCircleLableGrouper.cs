using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using NFox.Cad;

namespace ThMEPTCH.Services
{
    internal class ThCircleLableGrouper
    {
        /// <summary>
        /// 用于查询<Line,Polyline>端点处连接的物体
        /// </summary>
        private const double EnvelopLength = 2.0;
        /// <summary>
        /// 用于判断两个点是否相同 
        /// </summary>
        private const double PointEqualTolerance = 1.0;
        /// <summary>
        /// 圆弧打散长度
        /// 建议比EnvelopLength要小，否则线找不到圆
        /// </summary>
        private const double ArcTessellationLength = 1.0;
        private DBObjectCollection _circleLabels;
        private ThCADCoreNTSSpatialIndex _spatialIndex;
        /// <summary>
        /// 记录每一段<Line,Polyline>两端连接的物体
        /// </summary>
        private Dictionary<Curve, List<HashSet<DBObject>>> _linkSegmentPortObjs;

        private List<CircleLableGroup> _groups;
        public List<CircleLableGroup> Groups => _groups;
        public ThCircleLableGrouper(DBObjectCollection circleLabels)
        {
            _groups = new List<CircleLableGroup>();
            _circleLabels = circleLabels.Distinct();
            using (var t= new ThCADCoreNTSArcTessellationLength(ArcTessellationLength))
            {
                _spatialIndex = new ThCADCoreNTSSpatialIndex(_circleLabels);
            }                
            Init();
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                _spatialIndex.SelectAll().OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e.Clone() as Entity);
                    e.SetDatabaseDefaults();
                });
            }
        }
        private void Init()
        {
            _linkSegmentPortObjs = new Dictionary<Curve, List<HashSet<DBObject>>>();
            _circleLabels.OfType<Curve>().ForEach(c =>
            {
                if(c is Line line)
                {
                    var startObjs = ConvertTo(Query(line.StartPoint, EnvelopLength));
                    startObjs.Remove(line);
                    var endObjs = ConvertTo(Query(line.EndPoint, EnvelopLength));
                    endObjs.Remove(line);
                    _linkSegmentPortObjs.Add(c,new List<HashSet<DBObject>> { startObjs, endObjs });
                }
                else if(c is Polyline polyline)
                {
                    var startObjs = ConvertTo(Query(polyline.StartPoint, EnvelopLength));
                    startObjs.Remove(polyline);
                    var endObjs = ConvertTo(Query(polyline.EndPoint, EnvelopLength));
                    endObjs.Remove(polyline);
                    _linkSegmentPortObjs.Add(c, new List<HashSet<DBObject>> { startObjs, endObjs });
                }
            });
        }
        /// <summary>
        /// 分组结果存在于Groups属性中
        /// </summary>
        public void Group()
        {
            /*         Circle(Circle里有个文字)
             *          /        
             *         /
             *         |
             *         | 
             *         |
             */
            var startLinks = FindStartLinks();
            startLinks.OfType<Curve>().ForEach(c =>
            {
                var spLinks = Query(c, true);
                var epLinks = Query(c, false);
                var links = new DBObjectCollection() { c};
                if (spLinks.Count == 0)
                {
                    FindNext(c.EndPoint, links);
                }
                else
                {
                    FindNext(c.StartPoint, links);
                }
            });
        }

        private void FindNext(Point3d portPt,DBObjectCollection links)
        {
            var last = links.OfType<Curve>().Last();
            bool isStart = IsStart(last, portPt);
            var originLinks = Query(last, isStart);
            var portLinks = Filter(originLinks);
            if(portLinks.Count==0)
            {
                var circles = originLinks.OfType<Circle>();
                if (circles.Count() == 1)
                {
                    var circle = circles.First();
                    var texts = GetCircleTexts(circle);
                    if (texts.Count==1)
                    {
                        _groups.Add(new CircleLableGroup(links.OfType<Curve>().ToList(), 
                            circle, texts.OfType<DBText>().First()));
                    }
                }
                return;
            }    
            portLinks
                .OfType<Curve>()
                .ForEach(c =>
              {
                  var newLinks = links.OfType<Curve>().Select(o => o).ToCollection();
                  newLinks.Add(c);
                  var nextPt = portPt.DistanceTo(c.StartPoint) < portPt.DistanceTo(c.EndPoint)? c.EndPoint:c.StartPoint;
                  FindNext(nextPt, newLinks);
              });
        }


        private DBObjectCollection GetCircleTexts(Circle circle)
        {
            var texts = Query(circle.Center, circle.Diameter).OfType<DBText>().ToCollection();
            return texts.OfType<DBText>().Where(o => circle.EntityContains(o.GetCenterPointByOBB())).ToCollection();
        }
         
        private bool IsStart(Curve link,Point3d portPt)
        {
            return link.StartPoint.DistanceTo(portPt) <= PointEqualTolerance;
        }

        private HashSet<DBObject> Query(Curve link,bool isStart)
        {
            if(isStart)
            {
                return _linkSegmentPortObjs[link][0];
            }
            else
            {
                return _linkSegmentPortObjs[link][1];
            }
        }

        private DBObjectCollection FindStartLinks()
        {
            // 一端未连接任何物体，另一端有连接
            return _linkSegmentPortObjs.Where(o => (o.Value[0].Count == 0 && o.Value[1].Count > 0) ||
            (o.Value[0].Count > 0 && o.Value[1].Count == 0)).Select(o => o.Key).ToCollection();
        }

        private bool HasLink(HashSet<DBObject> objs)
        {
            return objs.OfType<Curve>().Where(o => IsLink(o)).Any();
        }

        private HashSet<DBObject> Filter(HashSet<DBObject> portLinks)
        {
            return portLinks
                .OfType<Curve>()
                .Where(c => IsLink(c))
                .ToHashSet<DBObject>();
        }

        private bool IsLink(Curve curve)
        {
            return curve is Line || curve is Polyline;
        }

        private DBObjectCollection Query(Point3d pt,double length)
        {
            var envelope = pt.CreateSquare(length);
            var results = _spatialIndex.SelectCrossingPolygon(envelope);
            envelope.Dispose();
            return results;
        }

        private HashSet<DBObject> ConvertTo(DBObjectCollection objs)
        {
            return objs.OfType<DBObject>().ToHashSet();
        }
    }
    internal class CircleLableGroup
    {
        private List<Curve> _path;
        public List<Curve> Path => _path;
        private Circle _circle;
        public Circle Circle => _circle;
        private DBText _text;
        public DBText Text => _text;
        public CircleLableGroup(List<Curve> path,Circle circle,DBText text)
        {
            _path = path;
            _text = text;
            _circle = circle;
        }
    }
}
