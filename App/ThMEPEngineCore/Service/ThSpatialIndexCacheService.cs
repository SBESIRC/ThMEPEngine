using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Service
{
    public class ThSpatialIndexCacheService
    {       
        private static readonly ThSpatialIndexCacheService instance = new ThSpatialIndexCacheService() { };
        static ThSpatialIndexCacheService() { }
        internal ThSpatialIndexCacheService() 
        {
            Factories = new List<SpatialIndexFactory>();
            Transformer = new ThMEPOriginTransformer(Point3d.Origin);
        }
        public ThMEPOriginTransformer Transformer { get; set; }
        public static ThSpatialIndexCacheService Instance { get { return instance; } }
        public List<SpatialIndexFactory> Factories { get; set; }
        public void Build(Database database, Point3dCollection polygon)
        {
            foreach(var factory in Factories)
            {
                factory.Transformer = Transformer;
                factory.Create(database, polygon);
            }
        }
        public void Build(Dictionary<BuiltInCategory,DBObjectCollection> dict)
        {
            foreach(var item in dict)
            {
                SpatialIndexFactory spatialIndex = null;
                switch(item.Key)
                {
                    case BuiltInCategory.ArchitectureOutline:
                        spatialIndex = new ArchitectureWallSpatialIndexFactory();
                        break;
                    case BuiltInCategory.ShearWall:
                        spatialIndex = new ShearWallSpatialIndexFactory();
                        break;
                    case BuiltInCategory.Column:
                        spatialIndex = new ColumnSpatialIndexFactory();
                        break;
                    case BuiltInCategory.Window:
                        spatialIndex = new WindowSpatialIndexFactory();
                        break;
                    case BuiltInCategory.CurtainWall:
                        spatialIndex = new CurtainWallSpatialIndexFactory();
                        break;
                }
                if(spatialIndex!=null)
                {
                    spatialIndex.Create(item.Value);
                    Factories.Add(spatialIndex);
                }
            }
        }

        public void Add(List<BuiltInCategory> builtInCategories)
        {
            builtInCategories.ForEach(o => Add(o));
        }
        public void Add(BuiltInCategory builtInCategory)
        {
            if(Factories.Where(o => o.BuiltCategory == builtInCategory).Count()==0)
            {
                switch(builtInCategory)
                {
                    case BuiltInCategory.ArchitectureWall:
                        Factories.Add(new ArchitectureWallSpatialIndexFactory());
                        break;
                    case BuiltInCategory.Column:
                        Factories.Add(new ColumnSpatialIndexFactory());
                        break;
                    case BuiltInCategory.CurtainWall:
                        Factories.Add(new CurtainWallSpatialIndexFactory());
                        break;
                    case BuiltInCategory.ShearWall:
                        Factories.Add(new ShearWallSpatialIndexFactory());
                        break;
                    case BuiltInCategory.Window:
                        Factories.Add(new WindowSpatialIndexFactory());
                        break;
                    default:
                        break;
                }                
            }
        }
        public void Remove(List<BuiltInCategory> builtInCategories)
        {
            builtInCategories.ForEach(o => Remove(o));
        }
        public void Remove(BuiltInCategory builtInCategory)
        {
            Factories = Factories.Where(o => o.BuiltCategory != builtInCategory).ToList();
        }
        public List<Entity> Find(BuiltInCategory builtInCategory, Polyline envelope)
        {
            var results = Factories.Where(o => o.BuiltCategory == builtInCategory);
            if (results.Count() == 1)
            {
                return results.First().Query(envelope);
            }
            else
            {
                return new List<Entity>();
            }
        }
    }
    #region ---------------创建索引工厂----------------
    public abstract class SpatialIndexFactory
    {
        public BuiltInCategory BuiltCategory { get; protected set; } = BuiltInCategory.UnNone;
        protected ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThMEPOriginTransformer Transformer { get; set; }
        public abstract void Create(Database database, Point3dCollection polygon);
        public abstract void Create(DBObjectCollection elements);
        public virtual List<Entity> Query(Polyline envelope)
        {
            if (SpatialIndex != null)
            {
                return SpatialIndex
                .SelectCrossingPolygon(envelope)
                .Cast<Entity>().ToList();
            }
            else
            {
                return new List<Entity>();
            }
        }
        protected Point3dCollection Transform(Point3dCollection pts)
        {
            var newPts = new Point3dCollection();
            pts.Cast<Point3d>().ForEach(p =>
            {
                var pt = new Point3d(p.X, p.Y, p.Z);
                Transformer.Transform(ref pt);
                newPts.Add(pt);
            });
            return newPts;
        }
    }
    public class ColumnSpatialIndexFactory : SpatialIndexFactory
    {
        public ColumnSpatialIndexFactory()
        {
            BuiltCategory = BuiltInCategory.Column;
        }
        public override void Create(Database database, Point3dCollection polygon)
        {
            var columnBuilder = new ThColumnBuilderEngine();
            var columns = columnBuilder.Build(database, polygon);
            columns.ForEach(o => Transformer.Transform(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(
                columns.Select(o=>o.Outline)
                .ToCollection()
                .FilterSmallArea(1.0));
        }
        public override void Create(DBObjectCollection elements)
        {
            Transformer.Transform(elements);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(elements);
        }
    }
    public class ArchitectureWallSpatialIndexFactory : SpatialIndexFactory
    {
        public ArchitectureWallSpatialIndexFactory()
        {
            BuiltCategory = BuiltInCategory.ArchitectureWall;
        }
        public override void Create(Database database, Point3dCollection polygon)
        {
            //提取了DB3中的墙，并移动到原点
            var engine = new ThDB3ArchWallExtractionEngine();
            engine.Extract(database); //提取跟NTS算法没有关系
            engine.Results.ForEach(o => Transformer.Transform(o.Geometry));      
            var wallEngine = new ThDB3ArchWallRecognitionEngine();
            wallEngine.Recognize(engine.Results, Transform(polygon));
            var db3Walls = wallEngine.Elements.Select(o => o.Outline).ToCollection();
            SpatialIndex = new ThCADCoreNTSSpatialIndex(db3Walls.FilterSmallArea(1.0));
        }
        public override void Create(DBObjectCollection elements)
        {
            Transformer.Transform(elements);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(elements);
        }
    }
    public class ShearWallSpatialIndexFactory : SpatialIndexFactory
    {
        public ShearWallSpatialIndexFactory()
        {
            BuiltCategory = BuiltInCategory.ShearWall;
        }
        public override void Create(Database database, Point3dCollection polygon)
        {
            var newPts = Transform(polygon);
            var walls = new DBObjectCollection();            
            var db3ShearWallExtractionEngine = new ThDB3ShearWallExtractionEngine();
            db3ShearWallExtractionEngine.Extract(database); //提取跟NTS算法没有关系
            db3ShearWallExtractionEngine.Results.ForEach(o => Transformer.Transform(o.Geometry));
            var db3ShearWallEngine = new ThDB3ShearWallRecognitionEngine();
            db3ShearWallEngine.Recognize(db3ShearWallExtractionEngine.Results, newPts);
            db3ShearWallEngine.Elements.ForEach(o => walls.Add(o.Outline));

            var shearWallExtractionEngine = new ThShearWallExtractionEngine();
            shearWallExtractionEngine.Extract(database); //提取跟NTS算法没有关系
            shearWallExtractionEngine.Results.ForEach(o => Transformer.Transform(o.Geometry));
            var shearWallEngine = new ThShearWallRecognitionEngine();
            shearWallEngine.Recognize(shearWallExtractionEngine.Results, newPts);
            shearWallEngine.Elements.ForEach(o => walls.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(walls.FilterSmallArea(1.0));
        }
        public override void Create(DBObjectCollection elements)
        {
            Transformer.Transform(elements);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(elements);
        }
    }
    public class CurtainWallSpatialIndexFactory : SpatialIndexFactory
    {
        public CurtainWallSpatialIndexFactory()
        {
            BuiltCategory = BuiltInCategory.CurtainWall;
        }
        public override void Create(Database database, Point3dCollection polygon)
        {
            var newPts = Transform(polygon);
            var extractionEngine = new ThDB3CurtainWallExtractionEngine();
            extractionEngine.Extract(database);
            extractionEngine.Results.ForEach(o => Transformer.Transform(o.Geometry));
            var recognizeEngine = new ThDB3CurtainWallRecognitionEngine();
            recognizeEngine.Recognize(extractionEngine.Results, newPts);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(
                recognizeEngine.Elements
                .Select(o=>o.Outline)
                .ToCollection().FilterSmallArea(1.0));
        }
        public override void Create(DBObjectCollection elements)
        {
            Transformer.Transform(elements);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(elements);
        }
    }
    public class WindowSpatialIndexFactory : SpatialIndexFactory
    {
        public WindowSpatialIndexFactory()
        {
            BuiltCategory = BuiltInCategory.Window;
        }
        public override void Create(Database database, Point3dCollection polygon)
        {
            var newPts = Transform(polygon);
            var extractionEngine = new ThDB3WindowExtractionEngine();
            extractionEngine.Extract(database);
            extractionEngine.Results.ForEach(o => Transformer.Transform(o.Geometry));

            var recognizeEngine = new ThDB3WindowRecognitionEngine();
            recognizeEngine.Recognize(extractionEngine.Results, newPts);     
            SpatialIndex = new ThCADCoreNTSSpatialIndex(
                recognizeEngine.Elements
                .Select(o => o.Outline)
                .ToCollection().FilterSmallArea(1.0));
        }
        public override void Create(DBObjectCollection elements)
        {
            Transformer.Transform(elements);
            SpatialIndex = new ThCADCoreNTSSpatialIndex(elements);
        }
    }
    #endregion
}
