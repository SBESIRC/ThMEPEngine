using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Diagnostics;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Utils;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.Model;
using ThMEPElectrical.AFAS.Model;

namespace ThMEPElectrical.AFAS.Data
{
    /// <summary>
    /// 取数据测试用
    /// </summary>
    public class ThAFASAllSetTestDataFactory : ThMEPDataSetFactory
    {
        //-------input
        public ThBeamDataParameter BeamDataParameter { get; set; }

        public List<ThExtractorBase> InputExtractors { get; set; }

        //-------output
        private List<ThGeometry> Geos { get; set; }
        public ThAFASAllSetTestDataFactory()
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
            //-------其他建筑元素
            var archiWallExtractor = InputExtractors.Where(o => o is ThAFASArchitectureWallExtractor).First() as ThAFASArchitectureWallExtractor;
            var shearWallExtractor = InputExtractors.Where(o => o is ThAFASShearWallExtractor).First() as ThAFASShearWallExtractor;
            var columnExtractor = InputExtractors.Where(o => o is ThAFASColumnExtractor).First() as ThAFASColumnExtractor;
            var windowExtractor = InputExtractors.Where(o => o is ThAFASWindowExtractor).First() as ThAFASWindowExtractor;
            var roomExtractor = InputExtractors.Where(o => o is ThAFASRoomExtractor).First() as ThAFASRoomExtractor;
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

            /////楼层框线。防火分区//////
            var storeyExtractor = InputExtractors.Where(o => o is ThAFASEStoreyExtractor).First() as ThAFASEStoreyExtractor;
            var fireApartExtractor = InputExtractors.Where(o => o is ThAFASFireCompartmentExtractor).First() as ThAFASFireCompartmentExtractor;
            fireApartExtractor.Transform();
            storeyExtractor.Transform();

            //提取可布区域
            var placeConverage = ThHandlePlaceConverage.BuildPlaceCoverage(extractors, Transformer, BeamDataParameter);
            extractors.Add(placeConverage);

            //提取探测区域
            var detectiveConverage = BuildDetectionRegion(extractors, BeamDataParameter.WallThickness);
            extractors.Add(detectiveConverage);

            // 造中心线Geo数据
            var centerLineGeoFactory = new ThAFASCenterLineGeoFactory(centerLineExtractor.CenterLines)
            {
                FireApartIds = fireApartExtractor.FireApartIds,
            };
            centerLineGeoFactory.Produce();
            centerLineGeoFactory.Geos.ForEach(o => Transformer.Reset(o.Boundary));
            centerLineExtractor.Set(centerLineGeoFactory.Geos);

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

        private ThAFASDetectionRegionExtractor BuildDetectionRegion(List<ThExtractorBase> extractors, double wallThickness)
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
                WallThickness = wallThickness,
                Transformer = Transformer,
            };

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

    public class ThAFASDataQueryService
    {
        //input
        public List<ThGeometry> Data { get; private set; }

        //output
        public List<ThGeometry> Storeys { get; private set; }
        public List<ThGeometry> FireAparts { get; private set; }
        public List<ThGeometry> ArchitectureWalls { get; private set; }
        public List<ThGeometry> Shearwalls { get; private set; }
        public List<ThGeometry> Columns { get; private set; }
        public List<ThGeometry> Windows { get; private set; }
        public List<ThGeometry> Rooms { get; private set; }
        public List<ThGeometry> Beams { get; private set; }
        public List<ThGeometry> DoorOpenings { get; private set; }
        public List<ThGeometry> Railings { get; private set; }
        public List<ThGeometry> FireProofs { get; private set; }
        public List<ThGeometry> Holes { get; private set; }
        public List<ThGeometry> CenterLine { get; private set; }
        public List<ThGeometry> PlaceArea { get; private set; }
        public List<ThGeometry> DetectArea { get; private set; }
        public List<ThGeometry> Equipments { get; private set; }


        public ThAFASDataQueryService(List<ThGeometry> geom)
        {
            this.Data = geom;
            PrepareData();
        }

        private void PrepareData()
        {
            Storeys = ThAFASUtils.QueryCategory(Data, BuiltInCategory.StoreyBorder.ToString());
            FireAparts = ThAFASUtils.QueryCategory(Data, BuiltInCategory.FireApart.ToString());
            ArchitectureWalls = ThAFASUtils.QueryCategory(Data, BuiltInCategory.ArchitectureWall.ToString());
            Shearwalls = ThAFASUtils.QueryCategory(Data, BuiltInCategory.ShearWall.ToString());
            Columns = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Column.ToString());
            Windows = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Window.ToString());
            Rooms = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Room.ToString());
            Beams = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Beam.ToString());
            DoorOpenings = ThAFASUtils.QueryCategory(Data, BuiltInCategory.DoorOpening.ToString());
            Railings = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Railing.ToString());
            FireProofs = ThAFASUtils.QueryCategory(Data, BuiltInCategory.FireproofShutter.ToString());
            Holes = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Hole.ToString());
            CenterLine = ThAFASUtils.QueryCategory(Data, BuiltInCategory.CenterLine.ToString());
            PlaceArea = ThAFASUtils.QueryCategory(Data, "PlaceCoverage");
            DetectArea = ThAFASUtils.QueryCategory(Data, "DetectionRegion");
            Equipments = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Equipment.ToString());
        }

        public void Print()
        {
            Storeys.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Storeys", 2));
            FireAparts.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0FireApart", 112));
            ArchitectureWalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0archWall", 1));
            Shearwalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0shearWall", 3));
            Columns.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Column", 1));
            Windows.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Window", 4));
            Rooms.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0room", 30));
            Beams.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0beam", 190));
            DoorOpenings.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DoorOpening", 4));
            FireProofs.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0FireProofs", 4));
            Railings.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Railings", 4));
            Holes.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Hole", 150));
            CenterLine.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Centerline", 230));
            PlaceArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0PlaceCoverage", 6));
            DetectArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DetectArea", 91));
            Equipments.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Equipment", 152));
        }

    }
}
