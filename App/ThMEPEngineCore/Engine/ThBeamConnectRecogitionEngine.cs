using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamConnectRecogitionEngine:IDisposable
    {
        public List<ThBeamLink> PrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> HalfPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> OverhangingPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> SecondaryBeamLinks { get; set; } = new List<ThBeamLink>();

        public ThColumnRecognitionEngine thColumnRecognitionEngine;
        public ThBeamRecognitionEngine thBeamRecognitionEngine;
        public ThShearWallRecognitionEngine thShearWallRecognitionEngine;
        public ThBeamConnectRecogitionEngine()
        {
        }
        public void Dispose()
        {
            //TODO
        }
        public void Recognize(Database database)
        {
            // 启动柱识别引擎
            thColumnRecognitionEngine = new ThColumnRecognitionEngine();
            thColumnRecognitionEngine.Recognize(database);

            // 启动梁识别引擎
            thBeamRecognitionEngine = new ThBeamRecognitionEngine();
            thBeamRecognitionEngine.Recognize(database);

            // 启动墙识别引擎
            thShearWallRecognitionEngine = new ThShearWallRecognitionEngine();
            thShearWallRecognitionEngine.Recognize(database);

            // 创建空间索引
            CreateSpatialIndex();

            // Pass One 通过单根梁过滤
            FindSingleBeamLinkTwoVerComponent();

            // Pass Two 在剩余梁中找出两个柱子或墙之间有多根梁的梁段
            FindMultiBeamLinkInTwoVerComponent();

            // Pass Three 在剩余梁中找出连接竖向构件的半主梁
            FindHalfPrimaryBeamLink();

            // Pass Four 在剩余梁中找出单端连接竖向构件的悬梁
            FindOverhangingPrimaryBeamLink();
        }
        private void CreateSpatialIndex()
        {
            ThSpatialIndexManager.Instance.CreateColumnSpaticalIndex(thColumnRecognitionEngine.Collect());
            var columnGeometries = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectAll();
            thColumnRecognitionEngine.UpdateValidElements(columnGeometries);
            ThSpatialIndexManager.Instance.CreateBeamSpaticalIndex(thBeamRecognitionEngine.Collect());
            var beamGeometries = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectAll();
            thBeamRecognitionEngine.UpdateValidElements(beamGeometries);
            thBeamRecognitionEngine.SimilarityBeamRemove();
            ThSpatialIndexManager.Instance.CreateWallSpaticalIndex(thShearWallRecognitionEngine.Collect());
            var wallGeometries = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectAll();
            thShearWallRecognitionEngine.UpdateValidElements(wallGeometries);
        }
        private void FindSingleBeamLinkTwoVerComponent()
        {
            foreach (ThIfcElement beamElement in thBeamRecognitionEngine.ValidElements)
            {
                ThBeamLinkExtension thBeamLinkExtension = new ThBeamLinkExtension()
                {
                    ColumnEngine = thColumnRecognitionEngine,
                    ShearWallEngine = thShearWallRecognitionEngine,
                };
                ThBeamLink thBeamLink = thBeamLinkExtension.CreateSinglePrimaryBeamLink(beamElement as ThIfcBeam);
                if (thBeamLink.Beams.Count > 0)
                {
                    PrimaryBeamLinks.Add(thBeamLink);
                }
            }
        }
        private void FindMultiBeamLinkInTwoVerComponent()
        {
            //主梁：两端均为竖向构件
            List<ThIfcBuildingElement> unPrimaryBeams = FilterNotPrimaryBeams(thBeamRecognitionEngine.ValidElements).ToList();
            ThVerticalComponentBeamLinkExtension multiBeamLink = new ThVerticalComponentBeamLinkExtension(unPrimaryBeams)
            {
                ColumnEngine = thColumnRecognitionEngine,
                BeamEngine = thBeamRecognitionEngine,
                ShearWallEngine = thShearWallRecognitionEngine
            };
            multiBeamLink.CreatePrimaryBeamLink();
            PrimaryBeamLinks.AddRange(multiBeamLink.BeamLinks);
        }
        private void FindHalfPrimaryBeamLink()
        {
            //半主梁：一端为竖向构件，另一端为主梁
            List<ThIfcBuildingElement> unPrimaryBeams = FilterNotPrimaryBeams(thBeamRecognitionEngine.ValidElements).ToList();
            ThHalfPrimaryBeamLinkExtension halfPrimaryBeamLink = new ThHalfPrimaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks)
            {
                ColumnEngine = thColumnRecognitionEngine,
                BeamEngine = thBeamRecognitionEngine,
                ShearWallEngine = thShearWallRecognitionEngine
            };
            halfPrimaryBeamLink.CreateHalfPrimaryBeamLink();
            HalfPrimaryBeamLinks.AddRange(halfPrimaryBeamLink.HalfPrimaryBeamLinks);
        }
        private void FindOverhangingPrimaryBeamLink()
        {
            //悬挑主梁：一端为竖向构件，另一端无主梁或竖向构件,且无延续构件
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(thBeamRecognitionEngine.ValidElements).ToList();
            ThOverhangingPrimaryBeamLinkExtension thOverhangingPrimaryBeamLinkExtension =
                new ThOverhangingPrimaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks)
                {
                    ColumnEngine = thColumnRecognitionEngine,
                    BeamEngine = thBeamRecognitionEngine,
                    ShearWallEngine = thShearWallRecognitionEngine
                };
            thOverhangingPrimaryBeamLinkExtension.CreateOverhangingPrimaryBeamLink();
            OverhangingPrimaryBeamLinks.AddRange(thOverhangingPrimaryBeamLinkExtension.OverhangingPrimaryBeamLinks);
        }
        private IEnumerable<ThIfcBuildingElement> FilterNotPrimaryBeams(List<ThIfcBuildingElement> totalBeams)
        {
            return totalBeams.Where(o => o is ThIfcBeam thIfcBeam && 
            thIfcBeam.ComponentType != BeamComponentType.PrimaryBeam);
        }
        private IEnumerable<ThIfcBuildingElement> FilterUndefinedBeams(List<ThIfcBuildingElement> totalBeams)
        {
            return totalBeams.Where(o => o is ThIfcBeam thIfcBeam &&
            thIfcBeam.ComponentType == BeamComponentType.Undefined);
        }
    }
}
