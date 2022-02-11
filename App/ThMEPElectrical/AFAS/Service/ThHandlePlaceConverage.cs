using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Data;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Model;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Interface;
using ThMEPElectrical.AFAS.Utils;

namespace ThMEPElectrical.AFAS.Service
{
    internal class ThHandlePlaceConverage
    {
        public static ThAFASPlaceCoverageExtractor BuildPlaceCoverage(List<ThExtractorBase> extractors, ThMEPEngineCore.Algorithm.ThMEPOriginTransformer transformer, ThBeamDataParameter beamDataParameter)
        {
            var roomExtract = extractors.Where(x => x is ThAFASRoomExtractor).FirstOrDefault() as ThAFASRoomExtractor;
            var wallExtract = extractors.Where(x => x is ThAFASShearWallExtractor).FirstOrDefault() as ThAFASShearWallExtractor;
            var archiWallExtract = extractors.Where(x => x is ThAFASArchitectureWallExtractor).FirstOrDefault() as ThAFASArchitectureWallExtractor;
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
                Transformer = transformer,
                ReferBeam = beamDataParameter.ReferBeam,
                WallThickness = beamDataParameter.WallThickness,
                BufferDist = beamDataParameter.BufferDist,
            };
            placeConverageExtract.Walls.AddRange(archiWallExtract.Walls.Select(w => ThIfcWall.Create(w)).ToList());

            placeConverageExtract.Extract(null, new Point3dCollection());

            return placeConverageExtract;
        }
    }
}
