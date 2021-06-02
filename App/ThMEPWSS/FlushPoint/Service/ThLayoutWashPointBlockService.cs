using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPWSS.FlushPoint.Service
{
    public class ThLayoutWashPointBlockService
    {
        private WashPointLayoutData LayoutData { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThLayoutWashPointBlockService(WashPointLayoutData layOutData)
        {
            LayoutData = layOutData;        
        }
        public Dictionary<Point3d, BlockReference> Layout()
        {
            if(LayoutData==null || !LayoutData.IsValid)
            {
                return new Dictionary<Point3d, BlockReference>();
            }
            BuildSpatialIndex();
            var ptDic = new Dictionary<Point3d, Vector3d>();
            LayoutData.WashPoints.ForEach(o =>
            {
                var vec = CalculateDirection(o);
                ptDic.Add(o, vec);
            });
            return Print(ptDic);
        }
        private Dictionary<Point3d, BlockReference> Print(Dictionary<Point3d, Vector3d> ptDic)
        {
            ImportBlock(); //导入块
            CreateLayer();
            return InsertBlock(ptDic); //根据点位，插块
        }
        private void ImportBlock()
        {
            using (var currentDb = AcadDatabase.Active())
            using (var blockDb = AcadDatabase.Open(ThCADCommon.WashPointLayoutDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(LayoutData.WashPointBlkName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(LayoutData.WashPointLayerName), false);
            }
        }
        private void CreateLayer()
        {
            LayoutData.Db.CreateAILayer(LayoutData.WashPointLayerName, (short)ColorIndex.BYLAYER);
        }
        private void BuildSpatialIndex()
        {
            var objs = new DBObjectCollection();
            LayoutData.Walls.ForEach(o => objs.Add(o));
            LayoutData.Columns.ForEach(o => objs.Add(o));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        private Vector3d CalculateDirection(Point3d pt)
        {
            var square = ThDrawTool.CreateSquare(pt, LayoutData.PtRange);
            var objs = SpatialIndex.SelectCrossingPolygon(square);
            if(objs.Count==0)
            {
                return Vector3d.XAxis;
            }
            else
            {
                var obj = GetClosestObj(objs, pt);                
                var edge = GetPropertyEdge(obj, pt);
                var leftVec = edge.LineDirection().GetPerpendicularVector().GetNormal();
                var rightVec = leftVec.Negate();
                var pt1 = pt + leftVec.MultiplyBy(LayoutData.PtRange + 1);
                return IsContains(obj, pt1) ? rightVec : leftVec;
            }
        }
        private DBObject GetClosestObj(DBObjectCollection objs, Point3d pt)
        {
            var disDic = new Dictionary<DBObject, double>();
            objs.Cast<DBObject>().ForEach(o =>
            {
                if(o is Polyline polyline)
                {
                    var closePt = polyline.GetClosestPointTo(pt, false);
                    disDic.Add(o, pt.DistanceTo(closePt));
                }
                else if(o is MPolygon mPolygon)
                {
                    var dis = mPolygon.Loops().Select(e =>
                    {
                        var closePt = e.GetClosestPointTo(pt, false);
                        return pt.DistanceTo(closePt);
                    }).OrderBy(d=>d).First();
                    disDic.Add(o, dis);
                }
            });
            return disDic.OrderBy(o => o.Value).First().Key;
        }
        private List<Line> GetLines(DBObject dbObj)
        {
            if (dbObj is Polyline polyline)
            {
                return GetLines(polyline);
            }
            else if (dbObj is MPolygon mPolygon)
            {
                return GetLines(mPolygon);
            }
            else
            {
                return new List<Line>();
            }
        }
        private List<Line> GetLines(Polyline poly)
        {
            return poly.ExplodeLines();
        }
        private List<Line> GetLines(MPolygon polygon)
        {
            return polygon.Loops().SelectMany(o=> GetLines(o)).ToList();
        }
        private Line GetPropertyEdge(DBObject obj,Point3d pt)
        {
            var lines = new List<Line>();
            var edges = GetLines(obj);
            //优先找点在线段内的线
            lines = edges
                .Select(o => o.ExtendLine(-1.0))
                .Where(o => ThGeometryTool.IsPointOnLine(o.StartPoint, o.EndPoint, pt, 1.0))
                .ToList();

            //找点在端点的
            if (lines.Count==0)
            {
                lines = edges
                    .Where(o => o.StartPoint.DistanceTo(pt) <= 1.0 || o.EndPoint.DistanceTo(pt) <= 1.0)
                    .ToList();
            }

            //找端点距离边最近的
            if (lines.Count == 0)
            {
                var disDic = new Dictionary<Line, double>();
                edges.ForEach(o=>
                {
                    var closePt = o.GetClosestPointTo(pt, false);
                    disDic.Add(o, closePt.DistanceTo(pt));
                });
                var dis = disDic.OrderBy(o => o.Value).First().Value;
                lines = disDic.Where(o => o.Value == dis).Select(o => o.Key).ToList();
            }
            //后续根据规则对线过滤,找出合适的边
            return lines.First(); 
        }
        private bool IsContains(DBObject obj, Point3d pt)
        {
            if (obj is Polyline polyline)
            {
                var polygon = polyline.ToNTSPolygon();
                return polyline.IsContains(pt) && !polygon.OnBoundary(pt);
            }
            else if (obj is MPolygon mPolygon)
            {
                var polygon = mPolygon.ToNTSPolygon();
                return mPolygon.IsContains(pt) && !polygon.OnBoundary(pt);
            }
            else
            {
                return false;
            }
        }
        private Dictionary<Point3d,BlockReference> InsertBlock(Dictionary<Point3d,Vector3d> ptDic)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var results = new Dictionary<Point3d, BlockReference>();
                ptDic.ForEach(o =>
                {
                    var rad = Vector3d.XAxis.GetAngleTo(o.Value, Vector3d.ZAxis);
                    var objId = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                        LayoutData.WashPointLayerName, LayoutData.WashPointBlkName,
                        o.Key, new Scale3d(1.0, 1.0, 1.0), rad);
                    results.Add(o.Key, acadDb.Element<BlockReference>(objId));
                });
                return results;
            }
        }
    }
    public class WashPointLayoutData
    {
        public List<Entity> Walls { get; set; }
        public List<Entity> Columns { get; set; }
        public string WashPointBlkName { get; set; }
        public string WashPointLayerName { get; set; }
        public List<Point3d> WashPoints { get; set; }
        public double PtRange { get; set; }
        public Database Db { get; set; }
        public WashPointLayoutData()
        {
            PtRange = 5.0;
            WashPointBlkName = "给水角阀平面";
            WashPointLayerName = "W-WSUP-EQPM";
            Walls = new List<Entity>();
            Columns = new List<Entity>();
            WashPoints = new List<Point3d>();
        }
        public bool IsValid
        {
            get
            {
                return Check();
            }
        }
        private bool Check()
        {
            if (WashPoints.Count == 0)
            {
                return false;
            }
            if (string.IsNullOrEmpty(WashPointBlkName))
            {
                return false;
            }
            if(string.IsNullOrEmpty(WashPointLayerName))
            {
                return false;
            }
            if (Db == null)
            {
                return false;
            }
            return true;
        }
    }
}
