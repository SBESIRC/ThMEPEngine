using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThCADExtension;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamConnectRecogitionEngine : IDisposable
    {
        public List<ThBeamLink> PrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> HalfPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> OverhangingPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> SecondaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThSingleBeamLink> SingleBeamLinks { get; set; } = new List<ThSingleBeamLink>();
        public ThSpatialIndexManager SpatialIndexManager { get; set; } = new ThSpatialIndexManager();

        public ThColumnRecognitionEngine ColumnEngine { get; private set; }
        public ThBeamRecognitionEngine BeamEngine { get; private set; }
        public ThShearWallRecognitionEngine ShearWallEngine { get; private set; }
        private ThBeamLinkExtension BeamLinkExtension = new ThBeamLinkExtension();

        public ThBeamConnectRecogitionEngine()
        {
            BeamLinkExtension.ConnectionEngine = this;
        }
        public void Dispose()
        {
            SpatialIndexManager.Dispose();
        }
        public static ThBeamConnectRecogitionEngine ExecuteRecognize(Database database, Point3dCollection polygon)
        {
            ThBeamConnectRecogitionEngine beamConnectEngine = new ThBeamConnectRecogitionEngine();
            beamConnectEngine.Recognize(database, polygon);
            return beamConnectEngine;
        }
        public void Recognize(Database database, Point3dCollection polygon)
        {
            // 启动柱识别引擎
            ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(database, polygon);

            // 启动墙识别引擎
            ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(database, polygon);

            // 启动梁识别引擎
            BeamEngine = new ThBeamRecognitionEngine();
            BeamEngine.Recognize(database, polygon);

            // 创建空间索引
            CreateWallSpatialIndex();
            CreateBeamSpatialIndex();
            CreateColumnSpatialIndex();

            // 预处理梁段
            {
                // 梁到梁的延伸
                var extendEngine = new ThExtendBeamEngine(this);
                extendEngine.Extend();
                SyncBeamSpatialIndex();

                // 梁的合并
                var mergeEngine = new ThMergeBeamEngine(this);
                mergeEngine.Merge();
                SyncBeamSpatialIndex();
               
                // 按柱，墙分割梁
                ThSplitBeamEngine thSplitBeams = new ThSplitBeamEngine(this);
                thSplitBeams.Split();
                SyncBeamSpatialIndex();

                // 梁到竖向构件的延伸
                ThSnapBeamEngine thSnapBeams = new ThSnapBeamEngine(this);
                thSnapBeams.Snap();
                SyncBeamSpatialIndex();
            }

            //建立单根梁两端连接的物体列表
            CreateSingleBeamLink();

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

            // Pass Seven 把悬挑梁末端连接的未定义梁去除
            RemoveUndefinedFromOverhanging();

            // Pass Eight
            FindRestSecondaryBeamLink();

            // Pass Nine 对BeamLink中的Beams属性进行梁合并
            MergeBeamLinks();
        }
        private void CreateColumnSpatialIndex()
        {
            SpatialIndexManager.CreateColumnSpaticalIndex(ColumnEngine.Geometries);
            ColumnEngine.UpdateWithSpatialIndex(SpatialIndexManager.ColumnSpatialIndex);
        }
        private void CreateWallSpatialIndex()
        {
            SpatialIndexManager.CreateWallSpaticalIndex(ShearWallEngine.Geometries);
            ShearWallEngine.UpdateWithSpatialIndex(SpatialIndexManager.WallSpatialIndex);
        }
        private void CreateBeamSpatialIndex()
        {
            SpatialIndexManager.CreateBeamSpaticalIndex(BeamEngine.Geometries);
            BeamEngine.UpdateWithSpatialIndex(SpatialIndexManager.BeamSpatialIndex);
        }
        public void SyncBeamSpatialIndex()
        {
            BeamEngine.UpdateSpatialIndex(SpatialIndexManager.BeamSpatialIndex);
            BeamEngine.UpdateWithSpatialIndex(SpatialIndexManager.BeamSpatialIndex);
        }

        private void SimilarityMeasure()
        {
            var thSimilarityMeasureEngine = new ThSimilarityMeasureEngine(this);
            thSimilarityMeasureEngine.SimilarityMeasure();
        }
        private void CreateSingleBeamLink()
        {
            BeamEngine.Elements.ForEach(o =>
            {
                if (o is ThIfcBeam thIfcBeam)
                {                    
                    SingleBeamLinks.Add(CreateSingleBeamLink(thIfcBeam));
                }
            });
        }
        public ThSingleBeamLink CreateSingleBeamLink(ThIfcBeam thIfcBeam)
        {
            ThSingleBeamLink thSingleBeamLink = new ThSingleBeamLink();
            thSingleBeamLink.Beam = thIfcBeam;
            thSingleBeamLink.StartVerComponents = BeamLinkExtension.QueryPortLinkElements(
                thIfcBeam, thIfcBeam.StartPoint, ThMEPEngineCoreCommon.BeamComponentConnectionTolerance);
            thSingleBeamLink.EndVerComponents = BeamLinkExtension.QueryPortLinkElements(
                thIfcBeam, thIfcBeam.EndPoint, ThMEPEngineCoreCommon.BeamComponentConnectionTolerance);

            thSingleBeamLink.StartBeams = BeamLinkExtension.QueryPortLinkBeams(
                thIfcBeam, thIfcBeam.StartPoint,
                ThMEPEngineCoreCommon.BeamExtensionRatio,
                ThMEPEngineCoreCommon.BeamIntervalMaximumTolerance);
            thSingleBeamLink.UpdateStartLink(BeamLinkExtension);
            thSingleBeamLink.EndBeams = BeamLinkExtension.QueryPortLinkBeams(
                thIfcBeam, thIfcBeam.EndPoint,
                ThMEPEngineCoreCommon.BeamExtensionRatio,
                ThMEPEngineCoreCommon.BeamIntervalMaximumTolerance);
            thSingleBeamLink.UpdateEndLink(BeamLinkExtension);
            return thSingleBeamLink;
        }
        public ThSingleBeamLink QuerySingleBeamLink(ThIfcBeam thIfcBeam)
        {
            return SingleBeamLinks.Where(o => o.Beam.Uuid == thIfcBeam.Uuid).First();
        }
        public void RemoveSingleBeamLink(ThIfcBeam thIfcBeam)
        {
            SingleBeamLinks = SingleBeamLinks.Where(o => o.Beam.Uuid != thIfcBeam.Uuid).ToList();
        }
        public void AddSingleBeamLink(ThIfcBeam thIfcBeam)
        {            
            SingleBeamLinks.Add(CreateSingleBeamLink(thIfcBeam));
        }
        private void FindSingleBeamLinkTwoVerComponent()
        {
            foreach (ThIfcElement beamElement in BeamEngine.Elements)
            {
                ThBeamLinkExtension thBeamLinkExtension = new ThBeamLinkExtension()
                {
                    ConnectionEngine = this,
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
            List<ThIfcBuildingElement> unPrimaryBeams = FilterNotPrimaryBeams(BeamEngine.Elements).ToList();
            ThVerticalComponentBeamLinkExtension multiBeamLink = new ThVerticalComponentBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks)
            {
                ConnectionEngine = this,
            };
            multiBeamLink.CreatePrimaryBeamLink();
        }
        private void FindHalfPrimaryBeamLink()
        {
            //半主梁：一端为竖向构件，另一端为主梁            
            ThBeamBreakEngine thBeamBreakEngine = new ThBeamBreakEngine(PrimaryBeamLinks, this);
            thBeamBreakEngine.Break();
            List<ThIfcBuildingElement> unPrimaryBeams = FilterNotPrimaryBeams(BeamEngine.Elements).ToList();
            ThHalfPrimaryBeamLinkExtension halfPrimaryBeamLink = new ThHalfPrimaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks)
            {
                ConnectionEngine = this,
            };
            halfPrimaryBeamLink.CreateHalfPrimaryBeamLink();
            HalfPrimaryBeamLinks.AddRange(halfPrimaryBeamLink.HalfPrimaryBeamLinks);
        }
        private void FindOverhangingPrimaryBeamLink()
        {
            //悬挑主梁：一端为竖向构件，另一端无主梁或竖向构件,且无延续构件
            ThBeamBreakEngine thBeamBreakEngine = new ThBeamBreakEngine(HalfPrimaryBeamLinks, this);
            thBeamBreakEngine.Break();
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.Elements).ToList();
            ThOverhangingPrimaryBeamLinkExtension thOverhangingPrimaryBeamLinkExtension =
                new ThOverhangingPrimaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks)
                {
                    ConnectionEngine = this,
                };
            thOverhangingPrimaryBeamLinkExtension.CreateOverhangingPrimaryBeamLink();
            OverhangingPrimaryBeamLinks.AddRange(thOverhangingPrimaryBeamLinkExtension.OverhangingPrimaryBeamLinks);
        }
        private void FindSecondaryBeamLink()
        {
            //次梁：两端搭在主梁、半主梁、悬挑柱梁上的梁
            ThBeamBreakEngine thBeamBreakEngine = new ThBeamBreakEngine(OverhangingPrimaryBeamLinks, this);
            thBeamBreakEngine.Break();            
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.Elements).ToList();
            ThSecondaryBeamLinkExtension thSecondaryBeamLinkExtension =
                new ThSecondaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks, OverhangingPrimaryBeamLinks)
                {
                    ConnectionEngine = this,
                };
            thSecondaryBeamLinkExtension.CreateSecondaryBeamLink();
            SecondaryBeamLinks.AddRange(thSecondaryBeamLinkExtension.SecondaryBeamLinks);
        }
        private void FindSubSecondaryBeamLink()
        {
            //次次梁：两端搭在主梁、半主梁、悬挑柱梁或次梁上的梁
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.Elements).ToList();
            ThSubSecondaryBeamLinkExtension thSubSecondaryBeamLinkExtension =
                new ThSubSecondaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks, OverhangingPrimaryBeamLinks, SecondaryBeamLinks)
                {
                    ConnectionEngine = this,
                };
            thSubSecondaryBeamLinkExtension.CreateSubSecondaryBeamLink();
        }
        private void FindRestSecondaryBeamLink()
        {
            //次次梁：两端搭在主梁、半主梁、悬挑柱梁或次梁上的梁
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.Elements).ToList();
            ThSubSecondaryBeamLinkExtension thSubSecondaryBeamLinkExtension =
                new ThSubSecondaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks, OverhangingPrimaryBeamLinks, SecondaryBeamLinks)
                {
                    ConnectionEngine = this,
                };
            thSubSecondaryBeamLinkExtension.FindRestBeamLink();
        }
        private void RemoveUndefinedFromOverhanging()
        {
            OverhangingPrimaryBeamLinks.ForEach(m =>
            {
                m.Beams = m.Beams.Where(n => n.ComponentType != BeamComponentType.Undefined).ToList();
                var uuids = new List<string>();
                m.Beams = m.Beams.Where(n =>
                {
                    if (uuids.IndexOf(n.Uuid) < 0)
                    {
                        uuids.Add(n.Uuid);
                        return true;
                    }
                    return false;
                }).ToList();
            });
        }
        private void MergeBeamLinks()
        {
            PrimaryBeamLinks.ForEach(o => ThBeamMerger.Merge(o));
            HalfPrimaryBeamLinks.ForEach(o => ThBeamMerger.Merge(o));
            OverhangingPrimaryBeamLinks.ForEach(o => ThBeamMerger.Merge(o));
            SecondaryBeamLinks.ForEach(o => ThBeamMerger.Merge(o));
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
