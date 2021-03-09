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
    public class ThObstacleSpatialIndexService
    {
        private ThCADCoreNTSSpatialIndex ColumnSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ArchitectureWallSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ShearWallSpatialIndex { get; set; }
        private static readonly ThObstacleSpatialIndexService instance = new ThObstacleSpatialIndexService() { };
        static ThObstacleSpatialIndexService() { }
        internal ThObstacleSpatialIndexService() { }        
        public static ThObstacleSpatialIndexService Instance { get { return instance; } }

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
        public List<Entity> FindColumns(Polyline envelope)
        {
            return ColumnSpatialIndex
                .SelectCrossingPolygon(envelope)
                .Cast<Entity>().ToList();
        }
        public List<Entity> FindShearWalls(Polyline envelope)
        {
            return ShearWallSpatialIndex
                .SelectCrossingPolygon(envelope)
                .Cast<Entity>()
                .ToList();
        }
        public List<Entity> FindArchitectureWalls(Polyline envelope)
        {
            return ArchitectureWallSpatialIndex
                .SelectCrossingPolygon(envelope)
                .Cast<Entity>()
                .ToList();
        }        
    }
}
