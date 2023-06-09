﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;

using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Model;

namespace ThMEPElectrical.FireAlarmArea.Data
{
    public class ThAFASAreaDataSetFactory : ThMEPDataSetFactory
    {
        /////input
        public bool NeedDetective { get; set; } = false;
        public ThBeamDataParameter BeamDataParameter { get; set; }

        public List<ThExtractorBase> InputExtractors { get; set; }
        /////output
        private List<ThGeometry> Geos { get; set; }

        public ThAFASAreaDataSetFactory()
        {
            Geos = new List<ThGeometry>();
            InputExtractors = new List<ThExtractorBase>();
        }
        public void SetTransformer(ThMEPOriginTransformer Transformer)
        {
            this.Transformer = Transformer;
        }
        protected override void GetElements(Database database, Point3dCollection collection)
        {
            //////其他建筑元素
            var archiWallExtractor = InputExtractors.Where(o => o is ThAFASArchitectureWallExtractor).First() as ThAFASArchitectureWallExtractor;
            var shearWallExtractor = InputExtractors.Where(o => o is ThAFASShearWallExtractor).First() as ThAFASShearWallExtractor;
            var columnExtractor = InputExtractors.Where(o => o is ThAFASColumnExtractor).First() as ThAFASColumnExtractor;
            var windowExtractor = InputExtractors.Where(o => o is ThAFASWindowExtractor).First() as ThAFASWindowExtractor;
            var roomExtractor = InputExtractors.Where(o => o is ThAFASRoomExtractor).First() as ThAFASRoomExtractor;
            var beamExtractor = InputExtractors.Where(o => o is ThAFASBeamExtractor).First() as ThAFASBeamExtractor;
            var doorOpeningExtractor = InputExtractors.Where(o => o is ThAFASDoorOpeningExtractor).First() as ThAFASDoorOpeningExtractor;
            var holeExtractor = InputExtractors.Where(o => o is ThAFASHoleExtractor).First() as ThAFASHoleExtractor;

            var extractors = new List<ThExtractorBase>()
                            {
                                archiWallExtractor,
                                shearWallExtractor,
                                columnExtractor,
                                windowExtractor,
                                roomExtractor,
                                beamExtractor,
                                doorOpeningExtractor,
                                holeExtractor,
                            };

            extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Transform();
                }
            });

            //提取可布区域
            var placeConverage = ThHandlePlaceConverage.BuildPlaceCoverage(extractors, Transformer, BeamDataParameter);
            extractors.Add(placeConverage);

            //提取探测区域
            if (NeedDetective == true)
            {
                var detectiveConverage = BuildDetectionRegion(extractors, BeamDataParameter.WallThickness);
                extractors.Add(detectiveConverage);
            }

            // 把房间传给门提取器            
            doorOpeningExtractor.SetRooms(roomExtractor.Rooms);

            //把洞传给门提取器
            doorOpeningExtractor.SetHoles(holeExtractor.HoleDic.Keys.ToList());


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

            Geos.ProjectOntoXYPlane();
        }

        private ThAFASDetectionRegionExtractor BuildDetectionRegion(List<ThExtractorBase> extractors, double wallThick)
        {
            var roomExtract = extractors.Where(x => x is ThAFASRoomExtractor).FirstOrDefault() as ThAFASRoomExtractor;
            var wallExtract = extractors.Where(x => x is ThAFASShearWallExtractor).FirstOrDefault() as ThAFASShearWallExtractor;
            var columnExtract = extractors.Where(x => x is ThAFASColumnExtractor).FirstOrDefault() as ThAFASColumnExtractor;
            var beamExtract = extractors.Where(x => x is ThAFASBeamExtractor).FirstOrDefault() as ThAFASBeamExtractor;
            var holeExtract = extractors.Where(x => x is ThAFASHoleExtractor).FirstOrDefault() as ThAFASHoleExtractor;
            var archiWallExtract = extractors.Where(x => x is ThAFASArchitectureWallExtractor).FirstOrDefault() as ThAFASArchitectureWallExtractor;

            var detectionRegionExtract = new ThAFASDetectionRegionExtractor()
            {
                Rooms = roomExtract.Rooms,
                Walls = wallExtract.Walls.Select(w => ThIfcWall.Create(w)).ToList(),
                Columns = columnExtract.Columns.Select(x => ThIfcColumn.Create(x)).ToList(),
                Beams = beamExtract.Beams,
                Holes = holeExtract.HoleDic.Select(x => x.Key).ToList(),
                WallThickness = wallThick,
                Transformer = Transformer,
            };

            detectionRegionExtract.Walls.AddRange(archiWallExtract.Walls.Select(w => ThIfcWall.Create(w)).ToList());

            detectionRegionExtract.Extract(null, new Point3dCollection());
            detectionRegionExtract.Fix();

            return detectionRegionExtract;
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
