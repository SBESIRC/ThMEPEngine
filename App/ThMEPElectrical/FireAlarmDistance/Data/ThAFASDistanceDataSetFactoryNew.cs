using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.FireAlarmDistance.Data
{
    public class ThAFASDistanceDataSetFactoryNew : ThMEPDataSetFactory
    {
        /////input
        public bool ReferBeam { get; set; } = true;
        public bool NeedConverage { get; set; } = true;
        public List<ThExtractorBase> InputExtractors { get; set; }

        /////output
        private List<ThGeometry> Geos { get; set; }
        public ThAFASDistanceDataSetFactoryNew()
        {
            Geos = new List<ThGeometry>();
            InputExtractors = new List<ThExtractorBase>();
        }


        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
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
            //房间元素后期会改，需要clone
            var roomExtractorClone = ThAFASDataUtils.CloneRoom(roomExtractor);
            var beamExtractor = InputExtractors.Where(o => o is ThAFASBeamExtractor).First() as ThAFASBeamExtractor;
            var doorOpeningExtractor = InputExtractors.Where(o => o is ThAFASDoorOpeningExtractor).First() as ThAFASDoorOpeningExtractor;
            var fireProofExtractor = InputExtractors.Where(o => o is ThAFASFireProofShutterExtractor).First() as ThAFASFireProofShutterExtractor;
            var holeExtractor = InputExtractors.Where(o => o is ThAFASHoleExtractor).First() as ThAFASHoleExtractor;
            var centerLineExtractor = InputExtractors.Where(o => o is ThAFASCenterLineExtractor).First() as ThAFASCenterLineExtractor;
            var railingExtractor = InputExtractors.Where(o => o is ThAFASRailingExtractor).First() as ThAFASRailingExtractor;
            
            var extractors = new List<ThExtractorBase>()
                            {
                                archiWallExtractor,
                                shearWallExtractor,
                                columnExtractor,
                                windowExtractor,
                                roomExtractorClone,
                                beamExtractor,
                                doorOpeningExtractor,
                                fireProofExtractor,
                                holeExtractor,
                                railingExtractor,
                                centerLineExtractor
                            };

            extractors.ForEach(o =>
            {
                if (o is ITransformer iTransformer)
                {
                    iTransformer.Transform();
                }
            });

            /////楼层框线。防火分区//////
            var storeyExtractor = InputExtractors.Where(o => o is ThAFASEStoreyExtractor).First() as ThAFASEStoreyExtractor;
            var fireApartExtractor = InputExtractors.Where(o => o is ThAFASFireCompartmentExtractor).First() as ThAFASFireCompartmentExtractor;
            fireApartExtractor.Transform();
            storeyExtractor.Transform();

            var storeyInfos = storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList();

            //把楼层信息传入到提取器中，对于不在防火分区内的图形要判断在哪个楼层
            extractors.ForEach(o =>
            {
                if (o is ISetStorey iStorey)
                {
                    iStorey.Set(storeyInfos);
                }
            });

            //用防火分区对房间进行分割，保留在防火分区内的房间,distance独有
            var splitRooms = roomExtractorClone.SplitByFrames(fireApartExtractor.FireCompartments);
            roomExtractorClone.UpdateRooms(splitRooms);

            //用防火分区对墙、柱...分组
            extractors.ForEach(o =>
            {
                if (o is IGroup group)
                {
                    group.Group(fireApartExtractor.FireApartIds);
                }
            });

            //找到防火门、防火卷帘邻接的防火分区
            doorOpeningExtractor.SetTags(fireApartExtractor.FireApartIds);
            fireProofExtractor.SetTags(fireApartExtractor.FireApartIds);

            // 把房间传给门提取器            
            doorOpeningExtractor.SetRooms(roomExtractorClone.Rooms);

            //把洞传给门提取器
            doorOpeningExtractor.SetHoles(holeExtractor.HoleDic.Keys.ToList());

            //提取可布区域
            if (NeedConverage == true)
            {
                var placeConverage = ThHandlePlaceConverage.BuildPlaceCoverage(extractors, Transformer, ReferBeam);
                placeConverage.Set(storeyInfos);
                placeConverage.Group(fireApartExtractor.FireApartIds);
                extractors.Add(placeConverage);
            }

            // 造中心线Geo数据
            var centerLineGeoFactory = new ThAFASCenterLineGeoFactory(centerLineExtractor.CenterLines)
            {
                FireApartIds = fireApartExtractor.FireApartIds,
            };
            centerLineGeoFactory.Produce();
            centerLineGeoFactory.Geos.ForEach(o => Transformer.Reset(o.Boundary));
            centerLineExtractor.Set(centerLineGeoFactory.Geos);

            //收集数据
            //最后将楼层框线和防火分区提取器加入，生成Geometries
            extractors.Add(storeyExtractor);
            extractors.Add(fireApartExtractor);
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
    }
}
