using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.BeamInfo.Business;

namespace ThMEPEngineCore.Service
{
    public class ThSplitBeamEngine:IDisposable
    {
        private ThColumnRecognitionEngine ColumnEngine { get; set; }
        private ThShearWallRecognitionEngine ShearWallEngine { get; set; }
        private ThBeamRecognitionEngine BeamEngine;
        public List<ThIfcBuildingElement> BeamElements { get; set; }
        protected Dictionary<ThIfcBuildingElement, List<ThSegment>> ShearWallSegDic;
        protected Dictionary<ThIfcBuildingElement, List<ThSegment>> ColumnSegDic;

        public ThSplitBeamEngine(ThColumnRecognitionEngine thColumnRecognitionEngine,
            ThShearWallRecognitionEngine thShearWallRecognitionEngine, 
            ThBeamRecognitionEngine thBeamRecognitionEngine)
        {
            ColumnEngine = thColumnRecognitionEngine;
            ShearWallEngine = thShearWallRecognitionEngine;
            BeamEngine = thBeamRecognitionEngine;
            BeamElements = BeamEngine.Elements;
            ShearWallSegDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
            ColumnSegDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
        }
        public virtual void Split()
        {
            //创建剪力墙Segment
            BuildShearwallSegment();

            //创建柱子Segment
            BuildColumnSegment();

            //处理梁穿过竖向构件(柱，剪力墙)
            SplitBeamPassWalls();

            //处理梁穿过梁
            SplitBeamPassBeams();

            //将梁长度过小的排除
            FilterSmallBeams();
        }
        private void BuildShearwallSegment()
        {
            ShearWallEngine.ValidElements.ForEach(o =>
            {
                ThSegmentService thSegmentService = new ThSegmentService(o.Outline as Polyline);
                thSegmentService.SegmentAll(new CalBeamStruService());
                ShearWallSegDic.Add(o, thSegmentService.Segments);
            });
        }
        private void BuildColumnSegment()
        {
            ColumnEngine.ValidElements.ForEach(o =>
            {
                ThSegmentService thSegmentService = new ThSegmentService(o.Outline as Polyline);
                thSegmentService.SegmentAll(new CalBeamStruService());
                ColumnSegDic.Add(o, thSegmentService.Segments);
            });
        }
        private void FilterSmallBeams(double tolerance=10.0)
        {
            var removeBeams = BeamElements.Where(o => o is ThIfcLineBeam thIfcLinearBeam && thIfcLinearBeam.Length <= tolerance).ToList();
            removeBeams.ForEach(o=>o.Outline.Dispose());
            BeamElements = BeamElements.Where(m => !removeBeams.Where(n => m.Uuid == n.Uuid).Any()).ToList();
        }
        private void SplitBeamPassWalls()
        {
            List<string> uuids = new List<string>();
            //搜索与梁相交的墙
            Dictionary<ThIfcBuildingElement, List<ThSegment>> beamWallComponentDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
            BeamElements.ForEach(o =>
            {
                Polyline outline = ThSplitBeamService.CreateExtendOutline(o);
                DBObjectCollection wallComponents = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectCrossingPolygon(outline);
                if(wallComponents.Count>0)
                {
                    List<ThSegment> passSegments = new List<ThSegment>();
                    var intersectShearWalls = ShearWallEngine.FilterByOutline(wallComponents).ToList();
                    intersectShearWalls.ForEach(m =>
                    {
                        foreach(var item in ShearWallSegDic)
                        {
                            if(item.Key.Uuid==m.Uuid)
                            {
                                passSegments.AddRange(item.Value);
                            }
                        }                       
                    });
                    if(passSegments.Count>0)
                    {
                        beamWallComponentDic.Add(o, passSegments);
                    }                    
                }
            });
            List<ThIfcBeam> divideBeams = new List<ThIfcBeam>();
            foreach (var item in beamWallComponentDic)
            {
                ThSplitBeamService thSplitBeam=null;
                if (item.Key is ThIfcLineBeam thIfcLineBeam)
                {
                    thSplitBeam = new ThSplitLinearBeamService(thIfcLineBeam, item.Value);                    
                }
                else if(item.Key is ThIfcArcBeam thIfcArcBeam)
                {
                    thSplitBeam = new ThSplitArcBeamService(thIfcArcBeam, item.Value);
                }
                thSplitBeam.Split();
                if (thSplitBeam.SplitBeams.Count > 1)
                {
                    uuids.Add(item.Key.Uuid); //记录要移除的梁UUID
                    divideBeams.AddRange(thSplitBeam.SplitBeams);
                }
            }
            BeamElements.Where(o => uuids.IndexOf(o.Uuid) >= 0).ToList().ForEach(o => o.Outline.Dispose());
            BeamElements = BeamElements.Where(o => uuids.IndexOf(o.Uuid) < 0).ToList();
            BeamElements.AddRange(divideBeams);
        }
        private void SplitBeamColumnComponents()
        {
            List<string> uuids = new List<string>();
            //搜索与梁相交的柱
            Dictionary<ThIfcBuildingElement, List<ThSegment>> beamColumnComponentDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
            BeamElements.ForEach(o =>
            {
                Polyline outline = ThSplitBeamService.CreateExtendOutline(o);
                DBObjectCollection columnComponents = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectCrossingPolygon(outline);
                if (columnComponents.Count > 0)
                {
                    List<ThSegment> passSegments = new List<ThSegment>();
                    var intersectColumns = ColumnEngine.FilterByOutline(columnComponents).ToList();
                    intersectColumns.ForEach(m =>
                    {
                        foreach (var item in ColumnSegDic)
                        {
                            if (item.Key.Uuid == m.Uuid)
                            {
                                passSegments.AddRange(item.Value);
                            }
                        }
                    });
                    beamColumnComponentDic.Add(o, passSegments);
                }
            });
            List<ThIfcBeam> divideBeams = new List<ThIfcBeam>();
            foreach (var item in beamColumnComponentDic)
            {
                ThSplitBeamService thSplitBeam = null;
                if (item.Key is ThIfcLineBeam thIfcLineBeam)
                {
                    thSplitBeam = new ThSplitLinearBeamService(thIfcLineBeam, item.Value);
                }
                else if (item.Key is ThIfcArcBeam thIfcArcBeam)
                {
                    thSplitBeam = new ThSplitArcBeamService(thIfcArcBeam, item.Value);
                }
                thSplitBeam.Split();
                if (thSplitBeam.SplitBeams.Count > 1)
                {
                    uuids.Add(item.Key.Uuid); //记录要移除的梁UUID
                    divideBeams.AddRange(thSplitBeam.SplitBeams);
                }
            }
            BeamElements.Where(o => uuids.IndexOf(o.Uuid) >= 0).ToList().ForEach(o => o.Outline.Dispose());
            BeamElements = BeamElements.Where(o => uuids.IndexOf(o.Uuid) < 0).ToList();
            BeamElements.AddRange(divideBeams);
        }
        private void SplitBeamPassBeams()
        {
            BeamEngine.Elements = BeamElements;
            ThSpatialIndexManager.Instance.CreateBeamSpaticalIndex(BeamEngine.Collect());
            var unintersectBeams = FilterBeamUnIntersectOtherBeams();
            List<ThIfcBuildingElement> restBeams = BeamElements.Where(m=> !unintersectBeams.Where(n=>m.Uuid==n.Uuid).Any()).ToList();
            List<ThIfcBuildingElement> inValidBeams = new List<ThIfcBuildingElement>();
            foreach(var beam in restBeams)
            {
                Polyline outline = ThSplitBeamService.CreateExtendOutline(beam);
                DBObjectCollection passComponents = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectCrossingPolygon(outline);
                if (passComponents.Count == 0)
                {
                    unintersectBeams.Add(beam);
                    continue;
                }
                List<ThSegment> passSegments = new List<ThSegment>();
                var intersectBeams = BeamEngine.FilterByOutline(passComponents).ToList();
                intersectBeams = intersectBeams.Where(o => o.Uuid != beam.Uuid).ToList();
                intersectBeams.ForEach(o =>
                {
                    if (o is ThIfcBeam thIfcBeam)
                    {
                        passSegments.Add(CreateSegment(thIfcBeam));
                    }
                });
                ThSplitBeamService thSplitBeam = null;
                if (beam is ThIfcLineBeam thIfcLineBeam)
                {
                    thSplitBeam = new ThSplitLinearBeamService(thIfcLineBeam, passSegments);
                }
                else if (beam is ThIfcArcBeam thIfcArcBeam)
                {
                    thSplitBeam = new ThSplitArcBeamService(thIfcArcBeam, passSegments);
                }
                thSplitBeam.Split();
                if (thSplitBeam.SplitBeams.Count > 1)
                {
                    inValidBeams.Add(beam);  //分段成功,将原始梁段存进 “inValidBeams”中，便于回收
                    unintersectBeams.AddRange(thSplitBeam.SplitBeams);
                }
                else
                {
                    unintersectBeams.Add(beam);
                }
            }            
            inValidBeams.ForEach(o => o.Outline.Dispose());
            BeamElements.Clear();
            BeamElements = unintersectBeams;
        }
        private List<ThIfcBuildingElement> FilterBeamUnIntersectOtherBeams()
        {
            // 过滤梁段不与其它梁相交的情况
            List<ThIfcBuildingElement> unIntersectBeams = new List<ThIfcBuildingElement>();
            DBObjectCollection dbObjs = new DBObjectCollection();
            BeamElements.ForEach(o => dbObjs.Add(o.Outline));
            var beamSpatialIndex = ThSpatialIndexService.CreateBeamSpatialIndex(dbObjs);
            for (int i = 0; i < BeamElements.Count; i++)
            {
                DBObjectCollection passComponents = beamSpatialIndex.
                   SelectCrossingPolygon(BeamElements[i].Outline as Polyline);
                passComponents.Remove(BeamElements[i].Outline);
                if (passComponents.Count==0)
                {
                    unIntersectBeams.Add(BeamElements[i]);
                }
            }
            return unIntersectBeams;
        }
        private ThSegment CreateSegment(ThIfcBeam thIfcBeam)
        {
            if(thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                return CreateSegment(thIfcLineBeam);
            }
            else if(thIfcBeam is ThIfcArcBeam thIfcArcBeam)
            {
                return CreateSegment(thIfcArcBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private ThLinearSegment CreateSegment(ThIfcLineBeam thIfclineBeam)
        {
            ThLinearSegment thLinearSegment = new ThLinearSegment();
            thLinearSegment.StartPoint = thIfclineBeam.StartPoint;
            thLinearSegment.EndPoint = thIfclineBeam.EndPoint;
            thLinearSegment.Outline = thIfclineBeam.Outline as Polyline;
            thLinearSegment.Width = thIfclineBeam.ActualWidth;
            return thLinearSegment;
        }
        private ThArcSegment CreateSegment(ThIfcArcBeam thIfcArcBeam)
        {
            ThArcSegment thArcSegment = new ThArcSegment();
            thArcSegment.StartPoint = thIfcArcBeam.StartPoint;
            thArcSegment.EndPoint = thIfcArcBeam.EndPoint;
            thArcSegment.Outline = thIfcArcBeam.Outline as Polyline;
            thArcSegment.Width = thIfcArcBeam.ActualWidth;
            thArcSegment.StartTangent = thIfcArcBeam.StartTangent;
            thArcSegment.EndTangent = thIfcArcBeam.EndTangent;
            thArcSegment.Radius = thIfcArcBeam.Radius;
            thArcSegment.Normal = thIfcArcBeam.Normal;
            return thArcSegment;
        }
        public void Dispose()
        {
            foreach(var segItem in ShearWallSegDic)
            {
                segItem.Value.ForEach(o => o.Outline.Dispose());
            }
            foreach (var segItem in ColumnSegDic)
            {
                segItem.Value.ForEach(o => o.Outline.Dispose());
            }
        }
    }
}
