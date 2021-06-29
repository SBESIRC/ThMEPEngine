using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThSpatialIndexCacheService
    {       
        private static readonly ThSpatialIndexCacheService instance = new ThSpatialIndexCacheService() { };
        static ThSpatialIndexCacheService() { }
        internal ThSpatialIndexCacheService() 
        {
            Factories = new List<SpatialIndexFactory>();
        }
        public static ThSpatialIndexCacheService Instance { get { return instance; } }
        public List<SpatialIndexFactory> Factories { get; set; }
        public void Build(Database database, Point3dCollection polygon)
        {
            foreach(var factory in Factories)
            {
                factory.Create(database, polygon);
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
        public abstract void Create(Database database, Point3dCollection polygon);
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
    }
    public class ColumnSpatialIndexFactory : SpatialIndexFactory
    {
        public ColumnSpatialIndexFactory()
        {
            BuiltCategory = BuiltInCategory.Column;
        }
        public override void Create(Database database, Point3dCollection polygon)
        {
            using (var engine = new ThColumnRecognitionEngine())
            using (var db3Engine = new ThDB3ColumnRecognitionEngine())
            {
                // 识别结构柱
                engine.Recognize(database, polygon);
                db3Engine.Recognize(database, polygon);
                var dbObjs = new DBObjectCollection();
                engine.Elements.ForEach(o=>dbObjs.Add(o.Outline));
                db3Engine.Elements.ForEach(o => dbObjs.Add(o.Outline));
                SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
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
            using (var engine = new ThDB3ArchWallRecognitionEngine())
            {
                // 识别结构柱
                engine.Recognize(database, polygon);
                var dbObjs = new DBObjectCollection();
                engine.Elements.ForEach(o => dbObjs.Add(o.Outline));
                SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
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
            using (var engine = new ThShearWallRecognitionEngine())
            using (var db3Engine = new ThDB3ShearWallRecognitionEngine())
            {
                // 识别结构柱
                engine.Recognize(database, polygon);
                db3Engine.Recognize(database, polygon);
                var dbObjs = new DBObjectCollection();
                engine.Elements.ForEach(o => dbObjs.Add(o.Outline));
                db3Engine.Elements.ForEach(o => dbObjs.Add(o.Outline));
                SpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
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
            using (var engine = new ThCurtainWallRecognitionEngine())
            {
                // 识别结构柱
                engine.Recognize(database, polygon);
                if(engine.Elements.Count>0)
                {
                    SpatialIndex = new ThCADCoreNTSSpatialIndex(
                    engine.Elements.Select(o => o.Outline).ToCollection());
                }
                else
                {
                    SpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
                }
            }
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
            using (var engine = new ThDB3WindowRecognitionEngine())
            {
                // 识别结构柱
                engine.Recognize(database, polygon);
                if(engine.Elements.Count>0)
                {
                    SpatialIndex = new ThCADCoreNTSSpatialIndex(
                    engine.Elements.Select(o => o.Outline).ToCollection());
                }
                else
                {
                    SpatialIndex = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
                }
            }
        }
    }
    #endregion
}
