using System.Linq;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using NetTopologySuite.Operation.Buffer;
using JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class Roomdata
    {
        private const double ColumnEnlargeDistance = 50.0;
        private const double SlabBufferDistance = 20.0;
        private DBObjectCollection _architectureWall; //仅支持Polyline
        private DBObjectCollection _shearWall; //仅支持Polyline
        private DBObjectCollection _column; //仅支持Polyline
        private DBObjectCollection _door; //仅支持Polyline
        private DBObjectCollection _window; //仅支持Polyline
        private DBObjectCollection _slab;  //仅支持Polyline
        private DBObjectCollection _cornice; //仅支持Polyline
        private DBObjectCollection _roomSplitline;

        public ThMEPOriginTransformer Transformer { get; set; }

        public Roomdata(Database database, Point3dCollection polygon)
        {
            var architectureWallEngine = new ThDB3ArchWallRecognitionEngine();
            architectureWallEngine.Recognize(database, polygon);
            _architectureWall = architectureWallEngine.Elements.Select(o => o.Outline).ToList().ToCollection();

            var shearWallEngine = new ThShearwallBuilderEngine();
            shearWallEngine.Build(database, polygon);
            _shearWall = shearWallEngine.Elements.Select(o => o.Outline).ToList().ToCollection();

            var doorengine = new ThDB3DoorRecognitionEngine();
            doorengine.Recognize(database, polygon);
            _door = doorengine.Elements.Select(o => o.Outline).ToList().ToCollection();

            var windowengine = new ThDB3WindowRecognitionEngine();
            windowengine.Recognize(database, polygon);
            _window = windowengine.Elements.Select(o => o.Outline).ToList().ToCollection();

            var slabengine = new ThDB3SlabRecognitionEngine();
            slabengine.Recognize(database, polygon);
            _slab = slabengine.Elements.Select(o => o.Outline).ToList().ToCollection();

            var corniceengine = new ThDB3CorniceRecognitionEngine();
            corniceengine.Recognize(database, polygon);
            _cornice = corniceengine.Elements.Select(o => o.Outline).ToList().ToCollection();

            var columnBuilder = new ThColumnBuilderEngine();
            columnBuilder.Build(database, polygon);
            _column = columnBuilder.Elements.Select(o => o.Outline).ToCollection();

            var extractPolyService = new ThExtractPolylineService()
            {
                ElementLayer = ThMEPEngineCoreLayerUtils.ROOMSPLITLINE,
            };
            extractPolyService.Extract(database, polygon);
            _roomSplitline = extractPolyService.Polys.ToCollection();

            BuildTransformer();
        }

        private void BuildTransformer()
        {
            var objs = MergeData();
            Transformer = new ThMEPOriginTransformer(objs);
        }

        public void Preprocess()
        {
            Deburring();
            FilterIsolatedColumns(ColumnEnlargeDistance);
        }

        private void FilterIsolatedColumns(double enlargeTolerance)
        {
            var data =  MergeData();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(data);
            var collector = new DBObjectCollection();
            var bufferService = new ThNTSBufferService();
            _column.OfType<Entity>().ForEach(e =>
            {
                var newEnt = bufferService.Buffer(e, enlargeTolerance) as Entity;
                var objs = spatialIndex.SelectCrossingPolygon(newEnt);
                objs.Remove(e);
                if (objs.Count == 0)
                {
                    collector.Add(e);
                }
            });
            collector.OfType<Entity>().ForEach(e => _column.Remove(e));
        }

        /// <summary>
        /// 拿到数据后根据需求去毛皮
        /// </summary>
        private void Deburring()
        {
            _architectureWall = _architectureWall.FilterSmallArea(1.0);
            _shearWall = _shearWall.FilterSmallArea(1.0);
            _door = _door.FilterSmallArea(1.0);
            _window = _window.FilterSmallArea(1.0);
            _column = _column.FilterSmallArea(1.0);

            //楼板去毛皮
            _slab = BufferCollectionContainsLines(_slab, -SlabBufferDistance);
            _slab = BufferCollectionContainsLines(_slab, SlabBufferDistance);
        }

        /// <summary>
        /// 为包含碎线的DBObjectCollection进行buffer，并保留碎线
        /// </summary>
        /// <param name="polys"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private DBObjectCollection BufferCollectionContainsLines(DBObjectCollection polys,double length, bool lineflag=false)
        {
            DBObjectCollection res = new DBObjectCollection();
            polys.Cast<Entity>().ForEach(o => 
            {
                if (o is Polyline poly && ThAuxiliaryUtils.DoubleEquals(poly.Area, 0.0))
                {
                    if (length < 0)
                        res.Add(poly);//TODO: 碎线可能需要延伸一些长度
                    else if (lineflag)
                    {
                        var bufferRes = poly.ToNTSLineString().Buffer(
                            length, new BufferParameters() { JoinStyle = JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square }).ToDbCollection();
                        bufferRes.Cast<Entity>().ForEach(e => res.Add(e));
                    }
                    else
                        res.Add(poly);
                }
                else if(o is Polyline polygon && polygon.Area>1.0)
                    polygon.ToNTSPolygon().Buffer(length, new BufferParameters() { JoinStyle = JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square })
                    .ToDbCollection().Cast<Entity>()
                    .ForEach(e => res.Add(e));
            });
            return res;
        }


        /// <summary>
        /// 对每个Polyline进行buffer，而不进行多余的操作
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private DBObjectCollection Buffer(DBObjectCollection polygons,double length)
        {
            var results = new DBObjectCollection();
            polygons = polygons.FilterSmallArea(1.0);
            polygons.Cast<Entity>().ForEach(e =>
            {
                e.ToNTSPolygonalGeometry().Buffer(length, new BufferParameters() { JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre, EndCapStyle = EndCapStyle.Square })
                .ToDbCollection().Cast<Entity>()
                .ForEach(o => results.Add(o));
            });
            results = results.FilterSmallArea(1.0);
            return results;
        }

        /// <summary>
        /// 将所有数据汇总打包
        /// </summary>
        /// <returns></returns>
        public DBObjectCollection MergeData()
        {
            var result = new DBObjectCollection();
            _architectureWall.Cast<DBObject>().ForEach(o => result.Add(o));
            _shearWall.Cast<DBObject>().ForEach(o => result.Add(o));
            _column.Cast<DBObject>().ForEach(o => result.Add(o));
            _door.Cast<DBObject>().ForEach(o => result.Add(o));
            _window.Cast<DBObject>().ForEach(o => result.Add(o));
            _slab.Cast<DBObject>().ForEach(o => result.Add(o));
            _cornice.Cast<DBObject>().ForEach(o => result.Add(o));
            _roomSplitline.Cast<DBObject>().ForEach(o => result.Add(o));
            return result;
        }
        public bool ContatinPoint3d(Point3d p)
        {
            bool isInArchWall = IsInComponents(_architectureWall, p);
            bool isInShearWall = IsInComponents(_shearWall,p);
            bool isInColumn = IsInComponents(_column, p);
            bool isInDoor = IsInComponents(_door, p);
            bool isInWindow = IsInComponents(_window, p);
            return isInArchWall || isInShearWall || isInColumn || isInDoor || isInWindow;
        }

        private bool IsInComponents(DBObjectCollection polygons,Point3d pt)
        {
            bool isIn = false;
            foreach(DBObject obj in polygons)
            {
                if (obj is Polyline polyline)
                {
                    if (polyline.Area > 1e-6)
                    {
                        isIn = polyline.IsContains(pt);
                    }
                }
                else if(obj is MPolygon mPolygon)
                {
                    isIn = mPolygon.IsContains(pt);
                }
                if (isIn)
                {
                    break;
                }
            }
            return isIn;
        }
        public void Transform()
        {
            var objs = MergeData();
            Transformer.Transform(objs);
        }
        public void Reset()
        {
            var objs = MergeData();
            Transformer.Reset(objs);
        }
    }
}

