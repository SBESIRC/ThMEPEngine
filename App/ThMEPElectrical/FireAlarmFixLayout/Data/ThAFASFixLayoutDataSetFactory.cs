using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Interface;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Utils;
using ThMEPEngineCore.Extension;

namespace ThMEPElectrical.FireAlarmFixLayout.Data
{
    public class ThAFASFixLayoutDataSetFactory : ThMEPDataSetFactory
    {
        /////input
        public List<ThExtractorBase> InputExtractors { get; set; }

        /////output
        private List<ThGeometry> Geos { get; set; }

        public ThAFASFixLayoutDataSetFactory()
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
            //archiWall有改动,浅拷贝 
            var archiWallExtractor = InputExtractors.Where(o => o is ThAFASArchitectureWallExtractor).First() as ThAFASArchitectureWallExtractor;
            var archiWallExtractorClone = CloneArchiWallExtractor(archiWallExtractor);
            /////
            var shearWallExtractor = InputExtractors.Where(o => o is ThAFASShearWallExtractor).First() as ThAFASShearWallExtractor;
            var columnExtractor = InputExtractors.Where(o => o is ThAFASColumnExtractor).First() as ThAFASColumnExtractor;
            var windowExtractor = InputExtractors.Where(o => o is ThAFASWindowExtractor).First() as ThAFASWindowExtractor;
            //房间元素后期会改，拷贝到boundary
            var roomExtractor = InputExtractors.Where(o => o is ThAFASRoomExtractor).First() as ThAFASRoomExtractor;
            var roomExtractorClone = ThAFASDataUtils.CloneRoom(roomExtractor);
            /////
            var beamExtractor = InputExtractors.Where(o => o is ThAFASBeamExtractor).First() as ThAFASBeamExtractor;
            var doorOpeningExtractor = InputExtractors.Where(o => o is ThAFASDoorOpeningExtractor).First() as ThAFASDoorOpeningExtractor;
            var railingExtractor = InputExtractors.Where(o => o is ThAFASRailingExtractor).First() as ThAFASRailingExtractor;
            var fireProofExtractor = InputExtractors.Where(o => o is ThAFASFireProofShutterExtractor).First() as ThAFASFireProofShutterExtractor;
            var holeExtractor = InputExtractors.Where(o => o is ThAFASHoleExtractor).First() as ThAFASHoleExtractor;


            var extractors = new List<ThExtractorBase>()
                            {
                                archiWallExtractorClone,
                                shearWallExtractor,
                                columnExtractor,
                                windowExtractor,
                                roomExtractorClone,
                                beamExtractor,
                                doorOpeningExtractor,
                                railingExtractor,
                                fireProofExtractor,
                                holeExtractor,
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

            //将房间外扩的区域得到的差集作为墙传入到建筑墙中
            var selfBuildWalls = BuildWalls(extractors);
            archiWallExtractorClone.Walls.AddRange(selfBuildWalls);

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

        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }

        private static ThAFASArchitectureWallExtractor CloneArchiWallExtractor(ThAFASArchitectureWallExtractor origWallExtractor)
        {
            //房间元素后期会改，需要clone
            var wallExtractorClone = new ThAFASArchitectureWallExtractor();
            wallExtractorClone.Walls.AddRange(origWallExtractor.Walls);
            wallExtractorClone.Transformer = origWallExtractor.Transformer;
            return wallExtractorClone;
        }

        private List<Entity> BuildWalls(List<ThExtractorBase> extractors)
        {
            var roomExtractor = extractors.Where(o => o is ThAFASRoomExtractor).First() as ThAFASRoomExtractor;
            var handleBufferService = new ThHandleRoomBufferService(roomExtractor.GetEntities());
            extractors.ForEach(o =>
            {
                if (o is ThAFASArchitectureWallExtractor ||
                o is ThAFASShearWallExtractor ||
                o is ThAFASColumnExtractor ||
                o is ThAFASWindowExtractor ||
                o is ThAFASDoorOpeningExtractor ||
                o is ThAFASBeamExtractor ||
                o is ThAFASRailingExtractor ||
                o is ThAFASFireProofShutterExtractor)
                {
                    handleBufferService.Add(o.GetEntities());
                }
            });
            handleBufferService.Handle();
            return handleBufferService.Walls;
        }
    }
}
