using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.Engine
{
    public class ThWallColumnsEngine
    {
        ThColumnExtractionEngine columnEngine = new ThColumnExtractionEngine();
        ThShearWallExtractionEngine shearWallEngine = new ThShearWallExtractionEngine();
        ThDB3ArchWallExtractionEngine archWallEngine = new ThDB3ArchWallExtractionEngine();
        ThMEPOriginTransformer _originTransformer;
        /// <summary>
        /// 获取墙、柱信息
        /// </summary>
        public ThWallColumnsEngine(ThMEPOriginTransformer originTransformer)
        {
            _originTransformer = originTransformer;
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                columnEngine.Extract(acdb.Database);
                shearWallEngine.Extract(acdb.Database);
                archWallEngine.Extract(acdb.Database);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Polyline> GetColumns(Polyline polyline)
        {
            ////获取柱
            var dateEngine = new ThColumnRecognitionEngine();
            var copyPl = (Polyline)polyline.Clone();
            if (null != _originTransformer)
                _originTransformer.Reset(copyPl);
            dateEngine.Recognize(columnEngine.Results, copyPl.Vertices());
            var columns = new List<Polyline>();
            columns = dateEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x =>
            {
                if (x is Polyline pline)
                {
                    var copy = (Polyline)x.Clone();
                    if (null != _originTransformer)
                        _originTransformer.Transform(copy);
                    objs.Add(copy);
                }
            });
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
            return columns;
        }
        public List<Polyline> AllWalls()
        {
            var dateEngine = new ThShearWallRecognitionEngine();
            dateEngine.Recognize(shearWallEngine.Results, new Autodesk.AutoCAD.Geometry.Point3dCollection());
            var walls = new List<Polyline>();
            walls = dateEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            walls.ForEach(x =>
            {
                if (x is Polyline pline) 
                {
                    var copy = (Polyline)x.Clone();
                    if (null != _originTransformer)
                        _originTransformer.Transform(copy);
                    objs.Add(copy);
                }
            });
            //获取建筑墙
            var archDataEngine = new ThDB3ArchWallRecognitionEngine();
            archDataEngine.Recognize(archWallEngine.Results, new Autodesk.AutoCAD.Geometry.Point3dCollection());
            foreach (var o in archDataEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    walls.Add(wall);
                }
            }
            return walls;
        }
        public List<Polyline> GetArchShearWalls(Polyline polyline)
        {
            var dataEngine = new ThShearWallRecognitionEngine();
            var copyPl = (Polyline)polyline.Clone();
            if (null != _originTransformer)
                _originTransformer.Reset(copyPl);
            dataEngine.Recognize(shearWallEngine.Results, copyPl.Vertices());
            var walls = new List<Polyline>();
            walls = dataEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            walls.ForEach(x =>
            {
                if (x is Polyline pline)
                {
                    var copy = (Polyline)x.Clone();
                    if (null != _originTransformer)
                        _originTransformer.Transform(copy);
                    objs.Add(copy);
                }
            });
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取建筑墙
            var archDataEngine = new ThDB3ArchWallRecognitionEngine();
            archDataEngine.Recognize(archWallEngine.Results, copyPl.Vertices());
            foreach (var o in archDataEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    var copy = (Polyline)o.Outline.Clone();
                    if (null != _originTransformer)
                        _originTransformer.Transform(copy);
                    walls.Add(copy);
                }
            }
            return walls;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        public void GetStructureInfo(Polyline polyline, out List<Polyline> columns, out List<Polyline> walls)
        {
            walls = GetArchShearWalls(polyline);
            columns = GetColumns(polyline);
        }
    }
}
