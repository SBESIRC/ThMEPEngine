using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

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
        public ThMEPOriginTransformer OriginTransformer { get; private set; }
        public ThColumnRecognitionEngine ColumnEngine { get; private set; } 
        public ThBeamBuilderEngine BeamEngine { get; private set; }
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
        public static ThBeamConnectRecogitionEngine ExecutePreprocess(Database database, Point3dCollection polygon)
        {
            var beamConnectEngine = new ThBeamConnectRecogitionEngine();
            beamConnectEngine.CreateEngines(database, polygon);
            beamConnectEngine.Preprocess();
            beamConnectEngine.ResetEngines();
            return beamConnectEngine;
        }
        public static ThBeamConnectRecogitionEngine ExecuteRecognize(Database database, Point3dCollection polygon)
        {
            var beamConnectEngine = new ThBeamConnectRecogitionEngine();
            beamConnectEngine.CreateEngines(database, polygon);
            beamConnectEngine.Recognize();
            beamConnectEngine.ResetEngines();
            return beamConnectEngine;
        }
        private void CreateEngines(Database database, Point3dCollection pts)
        {
            // 移动到近原点位置
            OriginTransformer = new ThMEPOriginTransformer(pts.Envelope().CenterPoint());
            var newPts = pts.OfType<Point3d>().Select(o => OriginTransformer.Transform(o)).ToCollection();

            // 启动柱识别引擎
            var extractor = new ThColumnExtractionEngine();
            extractor.Extract(database);
            extractor.Results.ForEach(x => OriginTransformer.Transform(x.Geometry));
            ColumnEngine = new ThColumnRecognitionEngine();
            ColumnEngine.Recognize(extractor.Results, newPts);

            // 启动墙识别引擎
            var extractor2 = new ThShearWallExtractionEngine();
            extractor2.Extract(database);
            extractor2.Results.ForEach(x => OriginTransformer.Transform(x.Geometry));
            ShearWallEngine = new ThShearWallRecognitionEngine();
            ShearWallEngine.Recognize(extractor2.Results, newPts);

            // 启动梁识别引擎
            BeamEngine = new ThBeamBuilderEngine();
            var results = BeamEngine.Extract(database);
            BeamEngine.Recognize(results, pts);
            BeamEngine.Transform(OriginTransformer.Displacement);

            // 为了支持这样的处理流程：
            //  1. 所有数据一次性获取后移动到近原点位置
            //  2. 对****近原点数据****进行处理
            //  3. 对处理后生成的结果恢复到原点位置
            // 所以返回近原点数据
        }

        public void Recognize(
            ThColumnRecognitionEngine columnEngine, 
            ThShearWallRecognitionEngine shearWallEngine, 
            ThBeamBuilderEngine beamEngine)
        {
            BeamEngine = beamEngine;
            ColumnEngine = columnEngine;
            ShearWallEngine = shearWallEngine;
            Recognize();
        }

        private void Recognize()
        {
            // 预处理
            Preprocess();

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
        }
        private void ResetEngines()
        {
            var matrix3d = OriginTransformer.Displacement.Inverse();
            ColumnEngine.Elements.ForEach(o => o.TransformBy(matrix3d));
            ShearWallEngine.Elements.ForEach(o => o.TransformBy(matrix3d));
            BeamEngine.Elements.ForEach(o => o.TransformBy(matrix3d));
        }
        private void Preprocess()
        {
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

                // 梁到梁的Join
                var joinEngine = new ThJoinBeamEngine(this);
                joinEngine.Join();
                SyncBeamSpatialIndex();

                // 梁的合并
                var mergeEngine = new ThMergeBeamEngine(this);
                mergeEngine.Merge();
                SyncBeamSpatialIndex();

                // 按柱，墙分割梁
                ThSplitBeamEngine thSplitBeams = new ThSplitBeamEngine(this);
                thSplitBeams.Split();

                // 梁到竖向构件的延伸
                ThSnapBeamEngine thSnapBeams = new ThSnapBeamEngine(this);
                thSnapBeams.Snap();
            }
        }
        private void CreateColumnSpatialIndex()
        {
            SpatialIndexManager.CreateColumnSpatialIndex(ColumnEngine.Geometries);
        }
        private void CreateWallSpatialIndex()
        {
            SpatialIndexManager.CreateWallSpatialIndex(ShearWallEngine.Geometries);
        }
        private void CreateBeamSpatialIndex()
        {
            SpatialIndexManager.CreateBeamSpatialIndex(BeamEngine.Geometries);
        }
        public void SyncBeamSpatialIndex()
        {
            BeamEngine.ResetSpatialIndex(SpatialIndexManager.BeamSpatialIndex);
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
        private ThSingleBeamLink CreateSingleBeamLink(ThIfcBeam thIfcBeam)
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
        public void UpdateSingleBeamLink(List<ThIfcBeam> adds, List<ThIfcBeam> removals)
        {
            adds.ForEach(o => SingleBeamLinks.Add(CreateSingleBeamLink(o)));
            var uuids = removals.Select(o => o.Uuid);
            SingleBeamLinks.RemoveAll(o => uuids.Contains(o.Beam.Uuid));
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
            ThSplitBeamEngine thBeamSplitEngine = new ThSplitBeamEngine(this);
            thBeamSplitEngine.Split(BeamComponentType.PrimaryBeam);
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
            ThSplitBeamEngine thBeamSplitEngine = new ThSplitBeamEngine(this);
            thBeamSplitEngine.Split(BeamComponentType.HalfPrimaryBeam);
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
            ThSplitBeamEngine thBeamSplitEngine = new ThSplitBeamEngine(this);
            thBeamSplitEngine.Split(BeamComponentType.OverhangingPrimaryBeam);
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
