using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThObstructSpatialIndexService:IDisposable
    {
        public ThCADCoreNTSSpatialIndex ColumnSpatialIndex { get; private set; }
        public ThCADCoreNTSSpatialIndex ArchitectureWallSpatialIndex { get; private set; }
        public ThCADCoreNTSSpatialIndex ShearWallSpatialIndex { get; private set; }
        public ThObstructSpatialIndexService()
        {
        }
        public void Dispose()
        {            
        }
        public void Build(Database database, Point3dCollection polygon)
        {
            using (var archWallEngine = new ThArchitectureWallRecognitionEngine())
            using (var columnEngine = new ThColumnRecognitionEngine())
            using (var shearWallEngine = new ThShearWallRecognitionEngine())
            {
                // 识别建筑墙
                archWallEngine.Recognize(database, polygon);
                // 识别结构柱
                columnEngine.Recognize(database, polygon);
                // 识别剪力墙
                shearWallEngine.Recognize(database, polygon);

                ColumnSpatialIndex = BuildSpatialIndex(columnEngine.Elements);
                ShearWallSpatialIndex = BuildSpatialIndex(shearWallEngine.Elements);
                ArchitectureWallSpatialIndex = BuildSpatialIndex(archWallEngine.Elements);
            }
        }
        private ThCADCoreNTSSpatialIndex BuildSpatialIndex(List<ThIfcBuildingElement> elements)
        {
            var objs = new DBObjectCollection();
            elements.ForEach(o => objs.Add(o.Outline));
            return new ThCADCoreNTSSpatialIndex(objs);
        }
        public List<Polyline> FindColumns(Polyline envelope)
        {
            return ColumnSpatialIndex.SelectCrossingPolygon(envelope).Cast<Polyline>().ToList();
        }
        public List<Polyline> FindShearWalls(Polyline envelope)
        {
            return ShearWallSpatialIndex.SelectCrossingPolygon(envelope).Cast<Polyline>().ToList();
        }
        public List<Polyline> FindArchitectureWalls(Polyline envelope)
        {
            return ArchitectureWallSpatialIndex.SelectCrossingPolygon(envelope).Cast<Polyline>().ToList();
        }
        public List<Polyline> FindAll(Polyline envelope)
        {
            var results = new List<Polyline>();
            results.AddRange(FindColumns(envelope));
            results.AddRange(FindShearWalls(envelope));
            results.AddRange(FindArchitectureWalls(envelope));
            return results;
        }
    }
}
