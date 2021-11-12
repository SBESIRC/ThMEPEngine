using System.Linq;

using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Interface;

using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.AFAS.Utils;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Data
{
    public class ThFaAreaLayoutDataSetFactory : ThMEPDataSetFactory
    {
        public bool ReferBeam { get; set; } = true;

        private List<ThGeometry> Geos { get; set; }

        public ThFaAreaLayoutDataSetFactory()
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
                    new ThAFASArchitectureWallExtractor()
                    {
                        ElementLayer = "AI-墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ArchWallVisitor.Results,
                    },
                    new ThAFASShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ShearWallVisitor.Results,
                        NonDb3ExtractResults = vm.ShearWallVisitor.Results,
                    },
                    new ThAFASColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3ColumnVisitor.Results,
                        NonDb3ExtractResults = vm.ColumnVisitor.Results,
                    },
                    new ThAFASRoomExtractor()
                    {
                        UseDb3Engine=true,
                        Transformer = Transformer,
                    },
                    new ThAFASHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        Transformer = Transformer,
                    },
                    new ThAFASBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3BeamVisitor.Results,
                    },

            };
            extractors.ForEach(o => o.Extract(database, collection));

            //提取可布区域
            var palceConverage = BuildPlaceCoverage(extractors, ReferBeam);
            extractors.Add(palceConverage);

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

            Geos.MoveToXYPlane();
        }

        private ThAFASPlaceCoverageExtractor BuildPlaceCoverage(List<ThExtractorBase> extractors, bool referBeam)
        {
            var roomExtract = extractors.Where(x => x is ThAFASRoomExtractor).FirstOrDefault() as ThAFASRoomExtractor;
            var wallExtract = extractors.Where(x => x is ThAFASShearWallExtractor).FirstOrDefault() as ThAFASShearWallExtractor;
            var columnExtract = extractors.Where(x => x is ThAFASColumnExtractor).FirstOrDefault() as ThAFASColumnExtractor;
            var beamExtract = extractors.Where(x => x is ThAFASBeamExtractor).FirstOrDefault() as ThAFASBeamExtractor;
            var holeExtract = extractors.Where(x => x is ThAFASHoleExtractor).FirstOrDefault() as ThAFASHoleExtractor;

            var placeConverageExtract = new ThAFASPlaceCoverageExtractor()
            {
                Rooms = roomExtract.Rooms,
                Walls = wallExtract.Walls.Select(w => ThIfcWall.Create(w)).ToList(),
                Columns = columnExtract.Columns.Select(x => ThIfcColumn.Create(x)).ToList(),
                Beams = beamExtract.Beams,
                Holes = holeExtract.HoleDic.Select(x => x.Key).ToList(),
                ReferBeam = referBeam,
                Transformer = Transformer,
            };

            placeConverageExtract.Extract(null, new Point3dCollection());

            return placeConverageExtract;
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
            extractor.Accept(visitors.DB3BeamVisitor);
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
