using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.AFASRegion.Model;
using ThMEPEngineCore.Engine;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThMEPEngineCore.Model;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.AFASRegion.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPEngineCore.AFASRegion
{
    public class AFASRegion
    {
        ///火灾报警 1.1 可布置区域
        ///业务需求：以房间框线，柱，梁 分割区域
        ///其中高梁不可分割，中梁可以合并，低梁直接忽略
        ///按照需求合并可布置区域，并返回到下一个功能模块单元

        /// <summary>
        /// 内缩距离
        /// </summary>
        public double BufferDistance { get; set; } = 500;
        
        /// <summary>
        /// 是否考虑梁的影响
        /// </summary>
        public bool ReferBeams { get; set; } = true;

        private List<ThIfcBuildingElement> columns;
        private List<ThIfcBuildingElement> beams;
        private List<ThIfcBuildingElement> walls;
        private List<Polyline> holes;
        public List<ThIfcRoom> Rooms { get; set; }

        /// <summary>
        /// 柱
        /// </summary>
        public List<ThIfcBuildingElement> Columns
        {
            private get
            {
                return columns;
            }
            set
            {
                columns = value;
            }
        }
        /// <summary>
        /// 梁
        /// </summary>
        public List<ThIfcBuildingElement> Beams
        {
            private get
            {
                return beams;
            }
            set
            {
                beams = value;
            }
        }

        /// <summary>
        /// 墙（包括剪力墙和建筑墙）
        /// </summary>
        public List<ThIfcBuildingElement> Walls
        {
            private get
            {
                return walls;
            }
            set
            {
                walls = value;
            }
        }

        /// <summary>
        /// 洞 AI-洞
        /// </summary>
        public List<Polyline> Holes
        {
            private get
            {
                return holes;
            }
            set
            {
                holes = value;
            }
        }

        /// <summary>
        /// 分割房间探测范围
        /// </summary>
        /// <param name="Roomdata">房间框线</param>
        /// <param name="detectorType">探测器类型</param>
        /// <returns></returns>
        public List<Entity> DivideRoomWithDetectionRegion(Polyline storyPL, AFASDetector detectorType = AFASDetector.SmokeDetectorLow)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ArrangeableSpace = new List<Entity>();
                var pts = storyPL.Vertices();
                //提取房间框线
                var roomBuidler = new ThRoomBuilderEngine();
                var Rooms = roomBuidler.BuildFromMS(acadDatabase.Database, pts);
                if (Rooms.Count == 0)
                {
                    return ArrangeableSpace;
                }

                var allStructure = ThBeamConnectRecogitionEngine.ExecutePreprocess(acadDatabase.Database, pts);
       
                //建筑墙
                var archWallEngine = new ThDB3ArchWallRecognitionEngine();
                archWallEngine.Recognize(acadDatabase.Database, pts);

                AFASBeamExtendFactory beamExtendFactory = new AFASBeamExtendFactory(allStructure, archWallEngine);
                beamExtendFactory.detectorType = detectorType;
                beamExtendFactory.ExtendBeamCenterLine();

                //计算每个房间可布置区域
                Rooms.ForEach(room =>
                {
                    ArrangeableSpace.AddRange(beamExtendFactory.DetectionRegions(room.Boundary).Cast<Entity>());
                });
                return ArrangeableSpace;
            }
        }


        /// <summary>
        /// 分割房间可布置区域
        /// </summary>
        /// <param name="Roomdata">房间框线</param>
        /// <param name="retractiondistance">内缩距离</param>
        /// <returns></returns>
        public List<Entity> DivideRoomWithPlacementRegion(Polyline storyPL)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var ArrangeableSpace = new List<Entity>();
                if (Columns.IsNull())
                    Columns = new List<ThIfcBuildingElement>();
                if (Walls.IsNull())
                    Walls = new List<ThIfcBuildingElement>();
                if (Holes.IsNull())
                    Holes = new List<Polyline>();
                if (!ReferBeams || Beams.IsNull())
                    Beams = new List<ThIfcBuildingElement>();
                if (Rooms.IsNull() || Rooms.Count == 0)
                    return ArrangeableSpace;
                AFASRegionService regionService = new AFASRegionService();
                regionService.BufferDistance = BufferDistance;
                regionService.Initialize(Columns, Beams, Walls,Holes);
                //计算每个房间可布置区域
                Rooms.ForEach(room =>
                {
                    ArrangeableSpace.AddRange(regionService.PlacementRegions(room.Boundary).Cast<Entity>());
                });
                return ArrangeableSpace;
            }
        }
    }
}
