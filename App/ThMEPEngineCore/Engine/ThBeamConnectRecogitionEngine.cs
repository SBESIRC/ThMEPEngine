﻿using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamConnectRecogitionEngine:IDisposable
    {
        public List<ThBeamLink> PrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> HalfPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> OverhangingPrimaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThBeamLink> SecondaryBeamLinks { get; set; } = new List<ThBeamLink>();
        public List<ThSingleBeamLink> SingleBeamLinks { get; set; } = new List<ThSingleBeamLink>();

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
        public static ThBeamConnectRecogitionEngine ExecuteRecognize(Database database, Point3dCollection polygon)
        {
            ThBeamConnectRecogitionEngine beamConnectEngine = new ThBeamConnectRecogitionEngine();
            beamConnectEngine.Recognize(database, polygon);
            return beamConnectEngine;
        }
        public void Recognize(Database database, Point3dCollection polygon)
        {
            SingleBeamLinks = new List<ThSingleBeamLink>();
            // 启动柱识别引擎
            thColumnRecognitionEngine = new ThColumnRecognitionEngine();
            thColumnRecognitionEngine.Recognize(database, polygon);

            // 启动墙识别引擎
            thShearWallRecognitionEngine = new ThShearWallRecognitionEngine();
            thShearWallRecognitionEngine.Recognize(database, polygon);

            // 创建空间索引
            CreateColumnSpatialIndex();
            CreateWallSpatialIndex();
            
            // 启动梁识别引擎
            thBeamRecognitionEngine = new ThBeamRecognitionEngine();
            thBeamRecognitionEngine.Recognize(database, polygon);

            //梁分割
            thBeamRecognitionEngine.Split(thColumnRecognitionEngine, thShearWallRecognitionEngine);

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
        private void CreateSingleBeamLink()
        {
            ThBeamLinkExtension thBeamLinkExtension = new ThBeamLinkExtension()
            {
                ColumnEngine = thColumnRecognitionEngine,
                ShearWallEngine = thShearWallRecognitionEngine,
                BeamEngine = thBeamRecognitionEngine
            };
            foreach (var element in thBeamRecognitionEngine.ValidElements)
            {
                ThSingleBeamLink thSingleBeamLink = new ThSingleBeamLink();
                if(element is ThIfcBeam thIfcBeam)
                {                    
                    thSingleBeamLink.Beam = thIfcBeam;
                    thSingleBeamLink.StartVerComponents = thBeamLinkExtension.QueryPortLinkElements(thIfcBeam, thIfcBeam.StartPoint);
                    thSingleBeamLink.EndVerComponents = thBeamLinkExtension.QueryPortLinkElements(thIfcBeam, thIfcBeam.EndPoint);
                    thSingleBeamLink.StartBeams = thBeamLinkExtension.QueryPortLinkBeams(thIfcBeam, thIfcBeam.StartPoint);
                    thSingleBeamLink.StartBeams = ThFilterPortLinkBeams.Filter(thIfcBeam, thIfcBeam.StartPoint, thSingleBeamLink.StartBeams);
                    thSingleBeamLink.EndBeams = thBeamLinkExtension.QueryPortLinkBeams(thIfcBeam, thIfcBeam.EndPoint);
                    thSingleBeamLink.EndBeams = ThFilterPortLinkBeams.Filter(thIfcBeam, thIfcBeam.EndPoint, thSingleBeamLink.EndBeams);                    
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
            foreach (ThIfcElement beamElement in thBeamRecognitionEngine.ValidElements)
            {
                ThBeamLinkExtension thBeamLinkExtension = new ThBeamLinkExtension()
                {
                    ConnectionEngine = this,
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
                ConnectionEngine = this,
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
                ConnectionEngine = this,
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
                    ConnectionEngine = this,
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
                    ConnectionEngine = this,
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
                    ConnectionEngine = this,
                    ColumnEngine = thColumnRecognitionEngine,
                    BeamEngine = thBeamRecognitionEngine,
                    ShearWallEngine = thShearWallRecognitionEngine
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
            PrimaryBeamLinks.ForEach(o => new ThBeamMerger(o).Merge());
            HalfPrimaryBeamLinks.ForEach(o => new ThBeamMerger(o).Merge());
            OverhangingPrimaryBeamLinks.ForEach(o => new ThBeamMerger(o).Merge());
            SecondaryBeamLinks.ForEach(o => new ThBeamMerger(o).Merge());
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
