using System;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Interface;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.BuildRoom.Service;
using ThMEPEngineCore.BuildRoom.Interface;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Engine
{
    public class ThRoomOutlineBuilderEngine
    {
        //public double BufferLength { get; set; }
        //public double SimilarityTolerance { get; set; }


        //public List<Entity> Outlines { get; private set; }
        //public ThRoomOutlineBuilderEngine()
        //{
        //    BufferLength = 3.0;
        //    SimilarityTolerance = 0.99;
        //}
        //public void Build(IRoomBuildData roomData)
        //{
        //    /*
        //     * 对房间的边界物体进行外偏移，用于修补因坐标误差导致不搭接的情况
        //     * 炸掉偏移后的物体，（返回MPolygon对应的Loops，用mPolygonCurves变量保存）
        //     * 用 Polygonize 方法构件边界。
        //     * 对创建的边界进行内偏移（如果边界是mPolygonCurves变量中的洞，则进行外偏移）
        //     * BuildArea 创建面域
        //     * 对偏移后的边界,进行过滤。(被原先的边界包含，或其内部有原来的物体，丢弃。)
        //     * 对于获取的轮廓再正向偏移
        //     */
        //    if (BufferLength < 0)
        //    {
        //        BufferLength = 5.0;
        //    }
        //    // Handle
        //    var boundaryEnts = BufferHandle(roomData); // 把出入的物体按BufferLength正向偏移
        //    var mPolygonCurves = new List<Tuple<MPolygon, List<Polyline>>>(); // 记录MPolygon的内外圈
        //    var explodeEnts = ExplodeHandle(boundaryEnts, out mPolygonCurves);
        //    var holes = GetHoles(mPolygonCurves);

        //    // Polygonize
        //    var iPolygonize = new ThNTSPolygonizeService();
        //    var polygons = iPolygonize.Polygonize(explodeEnts.ToCollection());

        //    //  Filter
        //    var positivePolygons = Filter(holes, polygons); // polygons中存在于Holes中的实体
        //    var negativePolygons = polygons.Cast<Entity>().Where(o => !positivePolygons.Contains(o)).ToList();

        //    //  Buffer
        //    double offsetDistance = BufferLength * 2.0;
        //    positivePolygons = Buffer(positivePolygons, offsetDistance); // 对于洞的边界按offsetDistance正向偏移
        //    negativePolygons = Buffer(negativePolygons, offsetDistance * -1.0); // 对于其它边界按offsetDistance负向偏移

        //    // BuildArea
        //    IBuildArea buildArea = new ThNTSBuildAreaService();
        //    var areaObjs = buildArea.BuildArea(positivePolygons.Union(negativePolygons).ToCollection());

        //    //var count = areaObjs.Cast<Entity>().Where(o => o is MPolygon).Count();

        //    // Filter
        //    var iFilter = new ThFilterService()
        //    {
        //        InnerBufferLength = offsetDistance, //表示传入的框内缩的长度
        //    };
        //    iFilter.Filter(boundaryEnts, areaObjs.Cast<Entity>().ToList());

        //    // Buffer
        //    Outlines = Buffer(iFilter.Results, offsetDistance - BufferLength);
        //}

        //private List<Entity> BufferHandle(IRoomBuildData roomData)
        //{
        //    var boundaryEnts = new List<Entity>();
        //    // 收集边界实体
        //    // 延伸墙(确保元素搭接)
        //    var walls = Buffer(roomData.Walls, BufferLength);
        //    var windows = Buffer(roomData.Windows, BufferLength);
        //    var doors = Buffer(roomData.Doors, BufferLength);
        //    var lineFoots = Buffer(roomData.Cornices, BufferLength);
        //    //var railings = Buffer(roomData.Railings, BufferLength);
        //    boundaryEnts.AddRange(roomData.Columns);
        //    boundaryEnts.AddRange(walls);
        //    boundaryEnts.AddRange(doors);
        //    boundaryEnts.AddRange(windows);
        //    boundaryEnts.AddRange(lineFoots);
        //    //boundaryEnts.AddRange(railings);
        //    return boundaryEnts;
        //}

        //private List<Entity> ExplodeHandle(List<Entity> ents, out List<Tuple<MPolygon, List<Polyline>>> mPolygonCurves)
        //{
        //    var results = new List<Entity>();
        //    mPolygonCurves = new List<Tuple<MPolygon, List<Polyline>>>();
        //    var objs = new DBObjectCollection();
        //    foreach (Entity ent in ents)
        //    {
        //        if (ent is Polyline || ent is Line)
        //        {
        //            objs.Add(ent);
        //        }
        //        else if (ent is MPolygon mPolygon)
        //        {
        //            var subObjs = new DBObjectCollection();
        //            mPolygon.Explode(subObjs);
        //            subObjs.Cast<Polyline>().ForEach(o => objs.Add(o));
        //            mPolygonCurves.Add(Tuple.Create(mPolygon, subObjs.Cast<Polyline>().ToList()));
        //        }
        //        else
        //        {
        //            throw new NotSupportedException();
        //        }
        //    }
        //    IExplode iExplode = new ThExplodeService();
        //    var lineObjs = iExplode.Explode(objs);

        //    return lineObjs.Cast<Entity>().ToList();
        //}

        //private List<Entity> Buffer(List<Entity> ents, double bufferLength)
        //{
        //    var results = new List<Entity>();
        //    var iBuffer = new ThNTSBufferService();
        //    ents.ForEach(o =>
        //    {
        //        var bufferObj = iBuffer.Buffer(o, bufferLength);
        //        if (bufferObj != null)
        //        {
        //            results.Add(bufferObj);
        //        }
        //    });
        //    return results;
        //}

        //private List<Entity> Filter(List<Polyline> holes, DBObjectCollection objs)
        //{
        //    var results = new List<Entity>();
        //    ISimilarityMeasure similarityMeasure = new ThNTSSimilarityMeasureService();
        //    var holeCoincedes = new Dictionary<Polyline, List<Tuple<Polyline, double>>>();
        //    foreach (Polyline hole in holes)
        //    {
        //        var coincides = new List<Tuple<Polyline, double>>();
        //        foreach (Entity ent in objs)
        //        {
        //            if (!(ent is Polyline))
        //            {
        //                continue;
        //            }
        //            var polyline = ent as Polyline;
        //            var measure = similarityMeasure.SimilarityMeasure(hole, polyline);
        //            if (measure >= SimilarityTolerance)
        //            {
        //                coincides.Add(Tuple.Create(polyline, measure));
        //            }
        //        }
        //        coincides = coincides.OrderByDescending(o => o.Item2).ToList();
        //        holeCoincedes.Add(hole, coincides);
        //    }
        //    foreach (var item in holeCoincedes)
        //    {
        //        if (item.Value.Count > 0)
        //        {
        //            results.Add(item.Value.First().Item1);
        //        }
        //    }
        //    return results;
        //}
        //private List<Polyline> GetHoles(List<Tuple<MPolygon, List<Polyline>>> mPolygonCurves)
        //{
        //    var holes = new List<Polyline>();
        //    mPolygonCurves.ForEach(o =>
        //    {
        //        if (o.Item2.Count > 1)
        //        {
        //            for (int i = 1; i < o.Item2.Count; i++)
        //            {
        //                holes.Add(o.Item2[i]);
        //            }
        //        }
        //    });
        //    return holes;
        //}

        //public void Dispose()
        //{
        //}

        //public List<Entity> FindRooms(Point3d pt)
        //{
        //    return Outlines.Where(o => o.IsContains(pt)).ToList();
        //}
        private const double BufferDistance = 10.0;
        private const double AreaTolerance = 20.0;
        public int Count { get { return _data.Count; }}
        public double SimilarityTolerance { get; set; }
        private DBObjectCollection _data;
        //创建数据
        public ThRoomOutlineBuilderEngine(DBObjectCollection dbobjs)
        {
            SimilarityTolerance = 0.99;
            _data = dbobjs;
        }

        //此时拿到的数据均作了初步处理，但可能存在缝隙
        public void CloseAndFilter()
        {
            _data = _data.BufferPolygons(BufferDistance).BufferPolygons(-BufferDistance);
            _data = _data.FilterSmallArea(AreaTolerance);//这里不知道还需不需要过滤小面积
            //进行Polygonizer以便Build使用
            //NOTICES: MTS和CAD的互相转换
            //TODO: Update Convert
            DBObjectCollection temp = new DBObjectCollection();
            _data.Polygonize().ForEach(o =>
            {
                foreach (DBObject obj in o.ToDbCollection())
                {
                    temp.Add(obj);
                }
            });
            _data = temp;
            _data = _data.FilterSmallArea(AreaTolerance);
        }
        
        private DBObjectCollection Build(Point3d point)
        {
            Polyline MinPolyline = new Polyline();
            double area = double.MaxValue;
            foreach(DBObject obj in _data)
            {
                if(obj is Polyline polyline && polyline.Contains(point) && polyline.Area<area)
                {
                    MinPolyline = polyline;
                    area = polyline.Area;
                }
            }
            DBObjectCollection result = new DBObjectCollection();
            result.Add(MinPolyline);
            foreach(DBObject obj in _data)
            {
               if(obj is Polyline polyline && MinPolyline.Contains(polyline))
                {
                    result.Add(polyline);
                }
            }
            result = result.BuildArea();
            return result;
        }
        public DBObjectCollection Build(List<Point3d> points)
        {
            DBObjectCollection result = new DBObjectCollection();
            DBObjectCollection temp;
            for (int i=0;i<points.Count;i++)
            {
                temp = Build(points[i]);
                foreach (DBObject obj in temp)
                {
                    result.Add(obj);
                }
            }
            return result;
        }
    }
}
