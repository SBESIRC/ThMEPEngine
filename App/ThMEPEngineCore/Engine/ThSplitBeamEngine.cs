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
using ThMEPEngineCore.Service;
using ThMEPEngineCore.BeamInfo.Business;

namespace ThMEPEngineCore.Engine
{
    public class ThSplitBeamEngine:IDisposable
    {
        private ThColumnRecognitionEngine ColumnEngine { get; set; }
        private ThShearWallRecognitionEngine ShearWallEngine { get; set; }
        private ThBeamRecognitionEngine BeamEngine { get; set; }
        private ThSpatialIndexManager SpatialIndexManager { get; set; }

        public List<ThIfcBuildingElement> BeamElements { get; set; }
        protected Dictionary<ThIfcBuildingElement, List<ThSegment>> ShearWallSegDic;
        protected Dictionary<ThIfcBuildingElement, List<ThSegment>> ColumnSegDic;

        public ThSplitBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThColumnRecognitionEngine thColumnRecognitionEngine,
            ThShearWallRecognitionEngine thShearWallRecognitionEngine, 
            ThSpatialIndexManager thSpatialIndexManager)
        {
            ColumnEngine = thColumnRecognitionEngine;
            ShearWallEngine = thShearWallRecognitionEngine;
            BeamEngine = thBeamRecognitionEngine;
            SpatialIndexManager = thSpatialIndexManager;
            BeamElements = BeamEngine.Elements;
            ShearWallSegDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
            ColumnSegDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
        }
        public void Split()
        {
            //创建剪力墙Segment
            BuildShearwallSegment();

            //处理梁穿过剪力墙竖向构件
            SplitBeamPassWalls();

            //处理梁穿过柱竖向构件
            SplitBeamPassColumns();

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
        private void FilterSmallBeams(double tolerance=10.0)
        {
            var removeBeams = BeamElements.Where(o => o is ThIfcLineBeam thIfcLinearBeam && thIfcLinearBeam.Length <= tolerance).ToList();
            removeBeams.ForEach(o=>o.Outline.Dispose());
            BeamElements = BeamElements.Where(m => !removeBeams.Where(n => m.Uuid == n.Uuid).Any()).ToList();
        }
        private void SplitBeamPassWalls()
        {
            var beamWallComponentDic = BeamCrossWallSegments();
            List<string> uuids = new List<string>();     
            List<ThIfcBeam> divideBeams = new List<ThIfcBeam>();
            foreach (var item in beamWallComponentDic)
            {
                ThBeamSplitter thSplitBeam=null;
                if (item.Key is ThIfcLineBeam thIfcLineBeam)
                {                    
                    thSplitBeam = new ThLinealBeamSplitter(thIfcLineBeam, item.Value);                    
                }
                else if(item.Key is ThIfcArcBeam thIfcArcBeam)
                {
                    thSplitBeam = new ThCurveBeamSplitter(thIfcArcBeam, item.Value);
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
        private Dictionary<ThIfcBuildingElement, List<ThSegment>> BeamCrossWallSegments()
        {
            //搜索与梁相交的墙段
            var beamWallComponentDic = new Dictionary<ThIfcBuildingElement, List<ThSegment>>();
            BeamElements.ForEach(o =>
            {
                var beam = o as ThIfcBeam;
                Polyline outline = beam.Extend(0.0, -ThMEPEngineCoreCommon.BeamBufferDistance);
                DBObjectCollection wallComponents = SpatialIndexManager.WallSpatialIndex.SelectCrossingPolygon(outline);
                if (wallComponents.Count > 0)
                {
                    List<ThSegment> passSegments = new List<ThSegment>();
                    var intersectShearWalls = ShearWallEngine.FilterByOutline(wallComponents).ToList();
                    intersectShearWalls.ForEach(m =>
                    {
                        foreach (var item in ShearWallSegDic)
                        {
                            if (item.Key.Uuid == m.Uuid)
                            {
                                passSegments.AddRange(item.Value);
                            }
                        }
                    });
                    if (passSegments.Count > 0)
                    {
                        beamWallComponentDic.Add(o, passSegments);
                    }
                }
            });
            return beamWallComponentDic;
        }
        private void SplitBeamPassColumns()
        {
            List<string> uuids = new List<string>();
            List<ThIfcBeam> divideBeams = new List<ThIfcBeam>();
            BeamElements.ForEach(o =>
            {      
                var crossSegments = GetBeamCrossColumns(o as ThIfcBeam);
                if (crossSegments.Count > 0)
                {
                    ThBeamSplitter thSplitBeam = null;
                    if (o is ThIfcLineBeam thIfcLineBeam)
                    {                        
                        thSplitBeam = new ThLinealBeamSplitter(thIfcLineBeam, crossSegments);
                    }
                    else if (o is ThIfcArcBeam thIfcArcBeam)
                    {
                        thSplitBeam = new ThCurveBeamSplitter(thIfcArcBeam, crossSegments);
                    }
                    thSplitBeam.Split();
                    if (thSplitBeam.SplitBeams.Count > 1)
                    {
                        uuids.Add(o.Uuid); //记录要移除的梁UUID
                        divideBeams.AddRange(thSplitBeam.SplitBeams);
                    }
                }
            });
            foreach(var item in BeamElements)
            {
                var beam = item as ThIfcBeam;                
            }
            BeamElements.Where(o => uuids.IndexOf(o.Uuid) >= 0).ToList().ForEach(o => o.Outline.Dispose());
            BeamElements = BeamElements.Where(o => uuids.IndexOf(o.Uuid) < 0).ToList();
            BeamElements.AddRange(divideBeams);
        }
        private List<ThSegment> GetBeamCrossColumns(ThIfcBeam thIfcBeam)
        {            
            List<ThSegment> crossSegments = new List<ThSegment>();
            Polyline outline = thIfcBeam.Extend(0.0, -ThMEPEngineCoreCommon.BeamBufferDistance);
            DBObjectCollection columnComponents = SpatialIndexManager.ColumnSpatialIndex.SelectCrossingPolygon(outline);
            if (columnComponents.Count > 0)
            {
                var intersectColumns = ColumnEngine.FilterByOutline(columnComponents).ToList();
                intersectColumns.ForEach(o =>
                {
                    crossSegments.Add(new ThLinearSegment { Outline = o.Outline as Polyline });
                });
            }
            return crossSegments;
        }
        private void SplitBeamPassBeams()
        {
            BeamEngine.Elements = BeamElements;
            SpatialIndexManager.CreateBeamSpaticalIndex(BeamEngine.Geometries);
            var unintersectBeams = FilterBeamUnIntersectOtherBeams();
            List<ThIfcBuildingElement> restBeams = BeamElements.Where(m=> !unintersectBeams.Where(n=>m.Uuid==n.Uuid).Any()).ToList();
            List<ThIfcBuildingElement> inValidBeams = new List<ThIfcBuildingElement>();
            foreach(ThIfcBeam beam in restBeams)
            {
                Polyline outline = beam.Extend(0.0, ThMEPEngineCoreCommon.BeamIntersectExtentionTolerance);
                DBObjectCollection passComponents = SpatialIndexManager.BeamSpatialIndex.SelectCrossingPolygon(outline);
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
                ThBeamSplitter thSplitBeam = null;
                if (beam is ThIfcLineBeam thIfcLineBeam)
                {
                    thSplitBeam = new ThLinealBeamSplitter(thIfcLineBeam, passSegments);
                }
                else if (beam is ThIfcArcBeam thIfcArcBeam)
                {
                    thSplitBeam = new ThCurveBeamSplitter(thIfcArcBeam, passSegments);
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
                if (BeamElements[i] is ThIfcBeam thIfcbeam)
                {
                    Polyline extendOutLine = thIfcbeam.Extend(0, 0.01 * thIfcbeam.ActualWidth);
                    DBObjectCollection passComponents = beamSpatialIndex.SelectCrossingPolygon(extendOutLine);
                    extendOutLine.Dispose();
                    passComponents.Remove(BeamElements[i].Outline);
                    if (passComponents.Count == 0)
                    {
                        unIntersectBeams.Add(BeamElements[i]);
                    }
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
