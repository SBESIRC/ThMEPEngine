using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamTypeRecogitionEngine:IDisposable
    {
        public List<ThBeamLink> PrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> HalfPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> OverhangingPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> SecondaryBeamLinks { get; set; } = new List<ThBeamLink>();

        public ThColumnRecognitionEngine thColumnRecognitionEngine;
        public ThBeamRecognitionEngine thBeamRecognitionEngine;
        public ThShearWallRecognitionEngine thShearWallRecognitionEngine;
        public ThBeamTypeRecogitionEngine()
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
        }
        private void CreateSpatialIndex()
        {
            ThSpatialIndexManager.Instance.CreateColumnSpaticalIndex(thColumnRecognitionEngine.Collect());
            var columnGeometries = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectAll();
            thColumnRecognitionEngine.UpdateValidElements(columnGeometries);
            ThSpatialIndexManager.Instance.CreateBeamSpaticalIndex(thBeamRecognitionEngine.Collect());
            var beamGeometries = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectAll();
            thBeamRecognitionEngine.UpdateValidElements(beamGeometries);
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
            List<ThIfcElement> unPrimaryBeams = FilterNotPrimaryBeams(thBeamRecognitionEngine.ValidElements).ToList();
            ThVerticalComponentBeamLinkExtension multiBeamLink = new ThVerticalComponentBeamLinkExtension()
            {
                ColumnEngine = thColumnRecognitionEngine,
                BeamEngine = thBeamRecognitionEngine,
                ShearWallEngine = thShearWallRecognitionEngine
            };
            multiBeamLink.CreateMultipleBeamLink(unPrimaryBeams);
            //using (AcadDatabase acadDatabase = AcadDatabase.Active())
            //{
            //    foreach (var primary in multiBeamLink.BeamLinks)
            //    {
            //        primary.Beams.ForEach(o=> acadDatabase.ModelSpace.Add(o.Outline));
            //    }
            //}
            PrimaryBeamLinks.AddRange(multiBeamLink.BeamLinks);
        }
        private IEnumerable<ThIfcElement> FilterNotPrimaryBeams(List<ThIfcElement> totalBeams)
        {
           return totalBeams.Where(o =>
            {
                if (o is ThIfcBeam thIfcBeam)
                {
                    if (thIfcBeam.ComponentType != BeamComponentType.PrimaryBeam)
                    {
                        return true;
                    }
                }
                return false;
            });
        }
    }
}
