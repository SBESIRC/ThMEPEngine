using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.Engine
{
    class ThWallColumnsEngine
    {
        ThColumnExtractionEngine columnEngine = new ThColumnExtractionEngine();
        ThShearWallExtractionEngine shearWallEngine = new ThShearWallExtractionEngine();
        ThDB3ArchWallExtractionEngine archWallEngine = new ThDB3ArchWallExtractionEngine();
        /// <summary>
        /// 获取墙、柱信息
        /// </summary>
        public ThWallColumnsEngine() 
        {
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
            dateEngine.Recognize(columnEngine.Results, polyline.Vertices());
            var columns = new List<Polyline>();
            columns = dateEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            columns.ForEach(x => objs.Add(x));
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
            walls.ForEach(x => objs.Add(x));
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
            //ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            //walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
            return walls;
        }
        public List<Polyline> GetShearWalls(Polyline polyline)
        {
            var dateEngine = new ThShearWallRecognitionEngine();
            dateEngine.Recognize(shearWallEngine.Results, polyline.Vertices());
            var walls = new List<Polyline>();
            walls = dateEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
            return walls;
        }
        public List<Polyline> GetArchWalls(Polyline polyline) 
        {
            var dataEngine = new ThDB3ArchWallRecognitionEngine();
            dataEngine.Recognize(archWallEngine.Results, polyline.Vertices());
            var walls = new List<Polyline>();
            //获取建筑墙
            foreach (var o in dataEngine.Elements)
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
            dataEngine.Recognize(shearWallEngine.Results, polyline.Vertices());
            var walls = new List<Polyline>();
            walls = dataEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            var objs = new DBObjectCollection();
            walls.ForEach(x => objs.Add(x));
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            //获取建筑墙
            var archDataEngine = new ThDB3ArchWallRecognitionEngine();
            archDataEngine.Recognize(archWallEngine.Results, polyline.Vertices());
            foreach (var o in archDataEngine.Elements)
            {
                if (o.Outline is Polyline wall)
                {
                    walls.Add(wall);
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
