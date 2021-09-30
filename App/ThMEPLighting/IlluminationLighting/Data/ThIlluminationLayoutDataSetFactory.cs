using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;


using ThMEPLighting.IlluminationLighting.Common;

namespace ThMEPLighting.IlluminationLighting.Data
{
    public class ThIlluminationLayoutDataSetFactory : ThMEPDataSetFactory
    {
        public bool ReferBeam { get; set; } = true;
        private List<ThGeometry> Geos { get; set; }
        public ThIlluminationLayoutDataSetFactory()
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
                        //IsWithHole=false,
                        UseDb3Engine=true,
                        Transformer = Transformer,
                    },
                    new ThHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        Transformer = Transformer,
                    },
                    new ThFaBeamExtractor()
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

            ThIlluminationUtils.MoveToXYPlane(Geos);
        }

        private ThFaPlaceCoverageExtractor BuildPlaceCoverage(List<ThExtractorBase> extractors, bool referBeam)
        {
            var roomExtract = extractors.Where(x => x is ThFaRoomExtractor).FirstOrDefault() as ThFaRoomExtractor;
            var wallExtract = extractors.Where(x => x is ThFaShearWallExtractor).FirstOrDefault() as ThFaShearWallExtractor;
            var columnExtract = extractors.Where(x => x is ThFaColumnExtractor).FirstOrDefault() as ThFaColumnExtractor;
            var beamExtract = extractors.Where(x => x is ThFaBeamExtractor).FirstOrDefault() as ThFaBeamExtractor;
            var holeExtract = extractors.Where(x => x is ThHoleExtractor).FirstOrDefault() as ThHoleExtractor;

            var placeConverageExtract = new ThFaPlaceCoverageExtractor()
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
