using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Extension;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Data;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.AFAS.Interface;


namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASDataSetFactory : ThMEPDataSetFactory
    {
        public bool NeedConverage { get; set; } = true;
        public bool NeedDetective { get; set; } = false;
        public bool ReferBeam { get; set; } = true;
        public double WallThick { get; set; } = 100;
        public List<string> BlkNameList { get; set; } = new List<string>();
        private List<ThGeometry> Geos { get; set; }

        public ThAFASDataSetFactory()
        {
            Geos = new List<ThGeometry>();
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            // ArchitectureWall、Shearwall、Column、Room、Hole
            UpdateTransformer(collection);
            var vm = Extract(database); // visitor manager,提取的是原始数据
            vm.MoveToOrigin(Transformer); // 移动到原点

            //先提取楼层框线
            var storeyExtractor = new ThAFASEStoreyExtractor()
            {
                // ElementLayer = "AI-楼层框定E",
                ElementLayer = "AD-FLOOR-AREA",
                Transformer = Transformer,
            };
            storeyExtractor.Extract(database, collection);
            storeyExtractor.Transform(); //移到原点

            //再提取防火分区，接着用楼层框线对防火分区分组
            var storeyInfos = storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList();
            var fireApartExtractor = new ThAFASFireCompartmentExtractor()
            {
                ElementLayer = "AI-防火分区,AD-AREA-DIVD",
                StoreyInfos = storeyInfos, //用于创建防火分区
                Transformer = Transformer, //把变换器传给防火分区
            };
            fireApartExtractor.Extract(database, collection);
            fireApartExtractor.Group(storeyExtractor.StoreyIds); //判断防火分区属于哪个楼层框线
            fireApartExtractor.BuildFireAPartIds(); //创建防火分区编号


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
                    new ThAFASWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3WindowVisitor.Results,
                    },
                    new ThAFASRoomExtractor()
                    {
                        UseDb3Engine=true,
                        Transformer = Transformer,
                    },          
                    new ThAFASBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3BeamVisitor.Results,
                    },
                    new ThAFASDoorOpeningExtractor()
                    {
                        ElementLayer = "AI-门",
                        Transformer = Transformer,
                        VisitorManager = vm,
                    },
                    new ThAFASRailingExtractor()
                    {
                        ElementLayer = "AI-栏杆",
                        Transformer = Transformer,
                        Db3ExtractResults = vm.DB3RailingVisitor.Results,
                    },
                    new ThAFASFireProofShutterExtractor()
                    {
                        ElementLayer = "AI-防火卷帘",
                        Transformer = Transformer,
                    },
                    new ThAFASHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        Transformer = Transformer,
                    },
                      new ThFireAlarmBlkExtractor()
                    {
                        Transformer = Transformer ,
                        BlkNameList = this.BlkNameList, //add needed all blk name string 
                    },

            };
            extractors.ForEach(o => o.Extract(database, collection));

            //把楼层信息传入到提取器中，对于不在防火分区内的图形要判断在哪个楼层
            extractors.ForEach(o =>
            {
                if (o is ISetStorey iStorey)
                {
                    iStorey.Set(storeyInfos);
                }
            });

            //将房间外扩的区域得到的差集作为墙传入到建筑墙中(?fix 独有)
            var selfBuildWalls = BuildWalls(extractors);
            var architectureWallExtractor = extractors.Where(
                o => o is ThAFASArchitectureWallExtractor).First()
                as ThAFASArchitectureWallExtractor;
            architectureWallExtractor.Walls.AddRange(selfBuildWalls);

            // 用防火分区对房间进行分割，保留在防火分区内的房间 （dist 独有
            var roomExtractor = extractors.Where(o => o is ThAFASRoomExtractor).First() as ThAFASRoomExtractor;
            var splitRooms = roomExtractor.SplitByFrames(fireApartExtractor.FireCompartments);
            roomExtractor.UpdateRooms(splitRooms);

            //用防火分区对墙、柱...分组,不能放到前面，fix有添加墙体
            extractors.ForEach(o =>
            {
                if (o is IGroup group)
                {
                    group.Group(fireApartExtractor.FireApartIds);
                }
            });

            //找到防火门、防火卷帘邻接的防火分区
            var faDoorExtractor = extractors.Where(o => o is ThAFASDoorOpeningExtractor).First() as ThAFASDoorOpeningExtractor;
            faDoorExtractor.SetTags(fireApartExtractor.FireApartIds);
            var fireProofShutter = extractors.Where(o => o is ThAFASFireProofShutterExtractor).First() as ThAFASFireProofShutterExtractor;
            fireProofShutter.SetTags(fireApartExtractor.FireApartIds);

            // 把房间传给门提取器            
            faDoorExtractor.SetRooms(roomExtractor.Rooms);

            //把洞传给门提取器
            var holeExtractor = extractors.Where(o => o is ThAFASHoleExtractor).First() as ThAFASHoleExtractor;
            faDoorExtractor.SetHoles(holeExtractor.HoleDic.Keys.ToList());

            //提取可布区域
            if (NeedConverage == true)
            {
                var placeConverage = ThHandlePlaceConverage.BuildPlaceCoverage(extractors, Transformer, ReferBeam);
                placeConverage.Set(storeyInfos);
                placeConverage.Group(fireApartExtractor.FireApartIds);
                extractors.Add(placeConverage);
            }

            //提取探测区域
            if (NeedDetective == true)
            {
                var detectiveConverage = BuildDetectionRegion(extractors, WallThick);
                extractors.Add(detectiveConverage);
            }

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

        private ThAFASDetectionRegionExtractor BuildDetectionRegion(List<ThExtractorBase> extractors, double wallThick)
        {
            var roomExtract = extractors.Where(x => x is ThAFASRoomExtractor).FirstOrDefault() as ThAFASRoomExtractor;
            var wallExtract = extractors.Where(x => x is ThAFASShearWallExtractor).FirstOrDefault() as ThAFASShearWallExtractor;
            var columnExtract = extractors.Where(x => x is ThAFASColumnExtractor).FirstOrDefault() as ThAFASColumnExtractor;
            var beamExtract = extractors.Where(x => x is ThAFASBeamExtractor).FirstOrDefault() as ThAFASBeamExtractor;
            var holeExtract = extractors.Where(x => x is ThAFASHoleExtractor).FirstOrDefault() as ThAFASHoleExtractor;

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

            detectionRegionExtract.Extract(null, new Point3dCollection());

            return detectionRegionExtract;
        }


        private ThBuildingElementVisitorManager Extract(Database database)
        {
            var visitors = new ThBuildingElementVisitorManager(database);
            var extractor = new ThBuildingElementExtractorEx();
            extractor.Accept(visitors.DB3ArchWallVisitor);
            extractor.Accept(visitors.DB3ShearWallVisitor);
            extractor.Accept(visitors.DB3ColumnVisitor);
            extractor.Accept(visitors.DB3WindowVisitor);
            extractor.Accept(visitors.DB3BeamVisitor);
            extractor.Accept(visitors.DB3RailingVisitor);
            extractor.Accept(visitors.ColumnVisitor);
            extractor.Accept(visitors.ShearWallVisitor);
            extractor.Accept(visitors.DB3CurtainWallVisitor);
            extractor.Accept(visitors.DB3DoorMarkVisitor);
            extractor.Accept(visitors.DB3DoorStoneVisitor);
            extractor.Extract(database);
            return visitors;
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
