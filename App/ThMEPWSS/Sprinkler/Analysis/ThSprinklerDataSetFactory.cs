using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Sprinkler.Data;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;


namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDataSetFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; }
        public ThSprinklerDataSetFactory()
        {
            Geos = new List<ThGeometry>();
        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            UpdateTransformer(collection);
            var manger = Extract(database); // visitor manager,提取的是原始数据
            manger.MoveToOrigin(Transformer); // 移动到原点
            
            var extractors = new List<ThExtractorBase>()
            {
                new ThSprinklerArchitectureWallExtractor()
                {
                    ElementLayer = "AI-墙",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ArchWallVisitor.Results,
                 },
                new ThSprinklerShearWallExtractor()
                {
                    ElementLayer = "AI-剪力墙",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ShearWallVisitor.Results,
                    NonDb3ExtractResults = manger.ShearWallVisitor.Results,
                },
                new ThSprinklerColumnExtractor()
                {
                    ElementLayer = "AI-柱",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3ColumnVisitor.Results,
                    NonDb3ExtractResults = manger.ColumnVisitor.Results,
                },
                new ThSprinklerHoleExtractor()
                {
                    ElementLayer = "AI-洞",
                    Transformer = Transformer,
                },
                new ThSprinklerDoorOpeningExtractor()
                {
                    ElementLayer = "AI-门",
                    Transformer = Transformer,
                    VisitorManager = manger,
                },
                //new ThSprinklerFireproofshutterExtractor()
                //{
                //    ElementLayer = "AI-防火卷帘",
                //    Transformer = Transformer,
                //},
                new ThSprinklerBeamExtractor()
                {
                    ElementLayer = "AI-梁",
                    Transformer = Transformer,
                    Db3ExtractResults = manger.DB3BeamVisitor.Results,
                },
                new ThSprinklerWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        Transformer = Transformer,
                        Db3ExtractResults = manger.DB3WindowVisitor.Results,
                    },
                new ThSprinklerRoomExtractor()
                {
                    IsWithHole=false,
                    UseDb3Engine=true,
                    Transformer = Transformer,
                },
            };
            extractors.ForEach(o => o.Extract(database, collection));
            //收集数据
            extractors.ForEach(o => Geos.AddRange(o.BuildGeometries()));
            // 移回原位
            extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Reset();
                }
            });
        }

        private ThBuildingElementVisitorManager Extract(Database database)
        {
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.DB3ArchWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);
            extractor.Accept(visitors.DB3BeamVisitor);
            extractor.Accept(visitors.DB3DoorMarkVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Extract(database);
            return visitors;
        }

        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }
    }
}
