using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.FireAlarm.Service;

namespace FireAlarm.Data
{
    public class ThFireAlarmSimpleDataSetFactory : ThMEPDataSetFactory
    {
        private List<ThGeometry> Geos { get; set; }
        public ThFireAlarmSimpleDataSetFactory()
        {
            Geos = new List<ThGeometry>();
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            // ArchitectureWall、Shearwall、Column、Room、Hole
            UpdateTransformer(collection);
            var vm = Extract(database); // visitor manager,提取的是原始数据
            vm.MoveToOrigin(Transformer); // 移动到原点

            var extractors = new List<ThExtractorBase>()
                {
                    new ThFaArchitectureWallExtractor()
                    {
                        ElementLayer = "AI-墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ArchWallVisitor.Results,
                    },
                    new ThFaShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ShearWallVisitor.Results,
                        NonDb3ExtractResults = vm.ShearWallVisitor.Results,
                    },
                    new ThFaColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ColumnVisitor.Results,
                        NonDb3ExtractResults = vm.ColumnVisitor.Results,
                    },
                    new ThFaRoomExtractor()
                    {
                        IsWithHole=false,
                        UseDb3Engine=true,
                        Transformer = Transformer,
                    },
                    new ThHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        Transformer = Transformer,
                    },
                    new ThFaPlaceCoverageExtractor()
                    {
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
        private void Print(Database database , List<ThExtractorBase> extractors)
        {
            short colorIndex = 1;
            extractors.ForEach(o =>
            {
                o.ColorIndex = colorIndex++;
                if (o is IPrint printer)
                {
                    printer.Print(database);
                }
            });
        }
    }
}
