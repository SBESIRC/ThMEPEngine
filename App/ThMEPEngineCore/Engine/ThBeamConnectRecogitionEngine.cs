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

            // 启动墙识别引擎
            thShearWallRecognitionEngine = new ThShearWallRecognitionEngine();
            thShearWallRecognitionEngine.Recognize(database);

            // 创建空间索引
            CreateColumnSpatialIndex();
            CreateWallSpatialIndex();
            
            // 启动梁识别引擎
            thBeamRecognitionEngine = new ThBeamRecognitionEngine();
            thBeamRecognitionEngine.Recognize(database);

            //梁分割
            thBeamRecognitionEngine.Split(thColumnRecognitionEngine, thShearWallRecognitionEngine);

            //创建梁空间索引
            CreateBeamSpatialIndex();

            // Pass One 通过单根梁过滤
            FindSingleBeamLinkTwoVerComponent();
           
            // Pass Two 在剩余梁中找出两个柱子或墙之间有多根梁的梁段
            FindMultiBeamLinkInTwoVerComponent();

            // Pass Three 在剩余梁中找出连接竖向构件的半主梁
            FindHalfPrimaryBeamLink();

            // Pass Four 在剩余梁中找出单端连接竖向构件的悬梁
            FindOverhangingPrimaryBeamLink();
            
            // Pass Five 在剩余梁中找出两端搭在主梁、半主梁和悬挑柱梁上的次梁
            FindSecondaryBeamLink();

            // Pass Six 在剩余梁中找出两端搭在主梁、半主梁、悬挑柱梁或次梁上的次次梁
            FindSubSecondaryBeamLink();
            
            // Pass Seven 对BeamLink中的Beams属性进行梁合并
            MergeBeamLinks();
        }
        private void CreateColumnSpatialIndex()
        {
            ThSpatialIndexManager.Instance.CreateColumnSpaticalIndex(thColumnRecognitionEngine.Collect());
            var columnGeometries = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectAll();
            thColumnRecognitionEngine.UpdateValidElements(columnGeometries);
        }
        private void CreateWallSpatialIndex()
        {
            ThSpatialIndexManager.Instance.CreateWallSpaticalIndex(thShearWallRecognitionEngine.Collect());
            var wallGeometries = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectAll();
            thShearWallRecognitionEngine.UpdateValidElements(wallGeometries);
        }
        private void CreateBeamSpatialIndex()
        {
            ThSpatialIndexManager.Instance.CreateBeamSpaticalIndex(thBeamRecognitionEngine.Collect());
            var beamGeometries = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectAll();
            thBeamRecognitionEngine.UpdateValidElements(beamGeometries);
            thBeamRecognitionEngine.SimilarityBeamRemove();
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
            ThVerticalComponentBeamLinkExtension multiBeamLink = new ThVerticalComponentBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks)
            {
                ColumnEngine = thColumnRecognitionEngine,
                BeamEngine = thBeamRecognitionEngine,
                ShearWallEngine = thShearWallRecognitionEngine
            };
            multiBeamLink.CreatePrimaryBeamLink();
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
        private void FindSecondaryBeamLink()
        {
            //次梁：两端搭在主梁、半主梁、悬挑柱梁上的梁
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(thBeamRecognitionEngine.ValidElements).ToList();
            ThSecondaryBeamLinkExtension thSecondaryBeamLinkExtension =
                new ThSecondaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks, OverhangingPrimaryBeamLinks)
                {
                    ColumnEngine = thColumnRecognitionEngine,
                    BeamEngine = thBeamRecognitionEngine,
                    ShearWallEngine = thShearWallRecognitionEngine
                };
            thSecondaryBeamLinkExtension.CreateSecondaryBeamLink();
            SecondaryBeamLinks.AddRange(thSecondaryBeamLinkExtension.SecondaryBeamLinks);
        }
        private void FindSubSecondaryBeamLink()
        {
            //次次梁：两端搭在主梁、半主梁、悬挑柱梁或次梁上的梁
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(thBeamRecognitionEngine.ValidElements).ToList();
            ThSubSecondaryBeamLinkExtension thSubSecondaryBeamLinkExtension =
                new ThSubSecondaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks, OverhangingPrimaryBeamLinks, SecondaryBeamLinks)
                {
                    ColumnEngine = thColumnRecognitionEngine,
                    BeamEngine = thBeamRecognitionEngine,
                    ShearWallEngine = thShearWallRecognitionEngine
                };
            thSubSecondaryBeamLinkExtension.CreateSubSecondaryBeamLink();
        }
        private void MergeBeamLinks()
        {
            PrimaryBeamLinks.ForEach(o => new ThBeamLinkMerge(o).Merge());
            HalfPrimaryBeamLinks.ForEach(o => new ThBeamLinkMerge(o).Merge());
            OverhangingPrimaryBeamLinks.ForEach(o => new ThBeamLinkMerge(o).Merge());
            SecondaryBeamLinks.ForEach(o => new ThBeamLinkMerge(o).Merge());
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
