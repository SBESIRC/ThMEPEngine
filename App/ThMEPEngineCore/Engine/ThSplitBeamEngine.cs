using System;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.BeamInfo.Business;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThSplitBeamEngine:IDisposable
    {
        private ThBeamConnectRecogitionEngine BeamConnectRecogitionEngine { get; set; }

        public List<ThIfcBuildingElement> BeamElements { get; set; }
        protected Dictionary<ThIfcBuildingElement, List<ThSegment>> ShearWallSegDic;
        protected Dictionary<ThIfcBuildingElement, List<ThSegment>> ColumnSegDic;

        public ThSplitBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamConnectRecogitionEngine = thBeamConnectRecogitionEngine;           
            BeamElements = BeamConnectRecogitionEngine.BeamEngine.Elements;
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

            BeamConnectRecogitionEngine.BeamEngine.Elements = this.BeamElements;
        }
        private void BuildShearwallSegment()
        {
            BeamConnectRecogitionEngine.ShearWallEngine.Elements.ForEach(o =>
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
                DBObjectCollection wallComponents = BeamConnectRecogitionEngine.SpatialIndexManager.WallSpatialIndex.SelectCrossingPolygon(outline);
                if (wallComponents.Count > 0)
                {
                    List<ThSegment> passSegments = new List<ThSegment>();
                    var intersectShearWalls = BeamConnectRecogitionEngine.ShearWallEngine.FilterByOutline(wallComponents).ToList();
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
            DBObjectCollection columnComponents = BeamConnectRecogitionEngine.SpatialIndexManager.ColumnSpatialIndex.SelectCrossingPolygon(outline);
            if (columnComponents.Count > 0)
            {
                var intersectColumns = BeamConnectRecogitionEngine.ColumnEngine.FilterByOutline(columnComponents).ToList();
                intersectColumns.ForEach(o =>
                {
                    crossSegments.Add(new ThLinearSegment { Outline = o.Outline as Polyline });
                });
            }
            return crossSegments;
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
