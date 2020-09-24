using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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

        public ThBeamConnectRecogitionEngine()
        {
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

            // 创建空间索引
            CreateColumnSpatialIndex();
            CreateWallSpatialIndex();

            // 启动梁识别引擎
            BeamEngine = new ThBeamRecognitionEngine();
            BeamEngine.Recognize(database, polygon);

            //梁分割
            BeamEngine.Split(ColumnEngine, ShearWallEngine, SpatialIndexManager);

            //创建梁空间索引
            CreateBeamSpatialIndex();

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

            // Pass Eight 对BeamLink中的Beams属性进行梁合并
            MergeBeamLinks();

            // Pass Nine 梁吸附到相邻物体上
            SnapBeams();
        }
        private void SnapBeams()
        {
            PrimaryBeamLinks.ForEach(o => ThBeamLinkSnapService.Snap(o));
            HalfPrimaryBeamLinks.ForEach(o => ThBeamLinkSnapService.Snap(o));
            OverhangingPrimaryBeamLinks.ForEach(o => ThBeamLinkSnapService.Snap(o));
            SecondaryBeamLinks.ForEach(o => ThBeamLinkSnapService.Snap(o));
        }
        public void Split(Database database, Point3dCollection polygon)
        {
            // 启动柱识别引擎
            ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(database, polygon);

            // 启动墙识别引擎
            ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(database, polygon);

            // 创建空间索引
            CreateColumnSpatialIndex();
            CreateWallSpatialIndex();

            // 启动梁识别引擎
            BeamEngine = new ThBeamRecognitionEngine();
            BeamEngine.Recognize(database, polygon);

            //梁分割
            BeamEngine.Split(ColumnEngine, ShearWallEngine, SpatialIndexManager);
        }
        private void CreateColumnSpatialIndex()
        {
            SpatialIndexManager.CreateColumnSpaticalIndex(ColumnEngine.Collect());
            var columnGeometries = SpatialIndexManager.ColumnSpatialIndex.SelectAll();
            ColumnEngine.UpdateValidElements(columnGeometries);
        }
        private void CreateWallSpatialIndex()
        {
            SpatialIndexManager.CreateWallSpaticalIndex(ShearWallEngine.Collect());
            var wallGeometries = SpatialIndexManager.WallSpatialIndex.SelectAll();
            ShearWallEngine.UpdateValidElements(wallGeometries);
        }
        private void CreateBeamSpatialIndex()
        {
            SpatialIndexManager.CreateBeamSpaticalIndex(BeamEngine.Collect());
            var beamGeometries = SpatialIndexManager.BeamSpatialIndex.SelectAll();
            BeamEngine.UpdateValidElements(beamGeometries);
            BeamEngine.SimilarityBeamRemove(SpatialIndexManager);
            DBObjectCollection dbObjs = new DBObjectCollection();
            BeamEngine.ValidElements.ForEach(o=>dbObjs.Add(o.Outline));
            SpatialIndexManager.CreateBeamSpaticalIndex(dbObjs);
        }
        private void CreateSingleBeamLink()
        {
            ThBeamLinkExtension thBeamLinkExtension = new ThBeamLinkExtension()
            {
                ConnectionEngine = this,
            };
            foreach (var element in BeamEngine.ValidElements)
            {
                ThSingleBeamLink thSingleBeamLink = new ThSingleBeamLink();
                if(element is ThIfcBeam thIfcBeam)
                {
                    thSingleBeamLink.Beam = thIfcBeam;
                    thSingleBeamLink.StartVerComponents = thBeamLinkExtension.QueryPortLinkElements(
                        thIfcBeam, thIfcBeam.StartPoint,ThMEPEngineCoreCommon.BeamComponentConnectionTolerance);
                    thSingleBeamLink.EndVerComponents = thBeamLinkExtension.QueryPortLinkElements(
                        thIfcBeam, thIfcBeam.EndPoint, ThMEPEngineCoreCommon.BeamComponentConnectionTolerance);

                    thSingleBeamLink.StartBeams = thBeamLinkExtension.QueryPortLinkBeams(
                        thIfcBeam, thIfcBeam.StartPoint, ThMEPEngineCoreCommon.BeamIntervalMaximumTolerance);
                    thSingleBeamLink.UpdateStartLink(thBeamLinkExtension);
                    thSingleBeamLink.EndBeams = thBeamLinkExtension.QueryPortLinkBeams(thIfcBeam, thIfcBeam.EndPoint, 
                        ThMEPEngineCoreCommon.BeamIntervalMaximumTolerance);
                    thSingleBeamLink.UpdateEndLink(thBeamLinkExtension);

                    var beamLinkStartBeams = thBeamLinkExtension.QueryPortLinkBeams(
                        thIfcBeam, thIfcBeam.StartPoint, ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
                    var beamLinkEndBeams = thBeamLinkExtension.QueryPortLinkBeams(
                        thIfcBeam, thIfcBeam.EndPoint, ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
                }
                SingleBeamLinks.Add(thSingleBeamLink);
            }            
        }
        public ThSingleBeamLink QuerySingleBeamLink(ThIfcBeam thIfcBeam)
        {
            return SingleBeamLinks.Where(o => o.Beam.Uuid == thIfcBeam.Uuid).First();
        }
        private void FindSingleBeamLinkTwoVerComponent()
        {
            foreach (ThIfcElement beamElement in BeamEngine.ValidElements)
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
            List<ThIfcBuildingElement> unPrimaryBeams = FilterNotPrimaryBeams(BeamEngine.ValidElements).ToList();
            ThVerticalComponentBeamLinkExtension multiBeamLink = new ThVerticalComponentBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks)
            {
                ConnectionEngine = this,
            };
            multiBeamLink.CreatePrimaryBeamLink();            
        }
        private void FindHalfPrimaryBeamLink()
        {           
            //半主梁：一端为竖向构件，另一端为主梁
            List<ThIfcBuildingElement> unPrimaryBeams = FilterNotPrimaryBeams(BeamEngine.ValidElements).ToList();
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
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.ValidElements).ToList();
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
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.ValidElements).ToList();
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
            List<ThIfcBuildingElement> unPrimaryBeams = FilterUndefinedBeams(BeamEngine.ValidElements).ToList();
            ThSubSecondaryBeamLinkExtension thSubSecondaryBeamLinkExtension =
                new ThSubSecondaryBeamLinkExtension(unPrimaryBeams, PrimaryBeamLinks, HalfPrimaryBeamLinks, OverhangingPrimaryBeamLinks, SecondaryBeamLinks)
                {
                    ConnectionEngine = this,
                };
            thSubSecondaryBeamLinkExtension.CreateSubSecondaryBeamLink();
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
