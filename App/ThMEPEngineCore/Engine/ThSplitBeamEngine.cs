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

        public List<ThIfcBeam> BeamElements { get; set; }

        public ThSplitBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamConnectRecogitionEngine = thBeamConnectRecogitionEngine;           
            BeamElements = BeamConnectRecogitionEngine.BeamEngine.Elements.Cast<ThIfcBeam>().ToList();
        }
        public void Split()
        {
            //处理梁穿过剪力墙竖向构件
            SplitBeamPassWalls();

            //处理梁穿过柱竖向构件
            SplitBeamPassColumns();

            //将梁长度过小的排除
            FilterSmallBeams();
        }
        private void FilterSmallBeams(double tolerance=10.0)
        {
            var removeBeams = BeamElements.Where(o => o is ThIfcLineBeam thIfcLinearBeam && thIfcLinearBeam.Length <= tolerance).ToList();
            removeBeams.ForEach(o=>o.Outline.Dispose());
            BeamElements = BeamElements.Where(m => !removeBeams.Where(n => m.Uuid == n.Uuid).Any()).ToList();
        }
        private void SplitBeamPassWalls()
        {
            BeamElements.ForEach(o =>
                {
                    List<Polyline> passWalls = BeamCrossWallOutlines(o);
                    ThBeamSplitter thSplitBeam = null;
                    if (o is ThIfcLineBeam thIfcLineBeam)
                    {
                        thSplitBeam = new ThLinealBeamSplitter(thIfcLineBeam);
                    }
                    else if (o is ThIfcArcBeam thIfcArcBeam)
                    {
                        thSplitBeam = new ThCurveBeamSplitter(thIfcArcBeam);
                    }
                    thSplitBeam.Split(passWalls);
                    if (thSplitBeam.SplitBeams.Count > 0)
                    {     
                        BeamConnectRecogitionEngine.BeamEngine.Remove(o.Uuid);
                        BeamConnectRecogitionEngine.BeamEngine.Elements.AddRange(thSplitBeam.SplitBeams);
                        BeamConnectRecogitionEngine.SyncBeamSpatialIndex();
                    }
                });
        }        
        private void SplitBeamPassColumns()
        {
            BeamElements.ForEach(o =>
            {      
                var passColumns = BeamCrossColumnOutlines(o as ThIfcBeam);
                ThBeamSplitter thSplitBeam = null;
                if (o is ThIfcLineBeam thIfcLineBeam)
                {
                    thSplitBeam = new ThLinealBeamSplitter(thIfcLineBeam);
                }
                else if (o is ThIfcArcBeam thIfcArcBeam)
                {
                    thSplitBeam = new ThCurveBeamSplitter(thIfcArcBeam);
                }
                thSplitBeam.Split(passColumns);
                if (thSplitBeam.SplitBeams.Count > 1)
                {
                    BeamConnectRecogitionEngine.BeamEngine.Remove(o.Uuid);
                    BeamConnectRecogitionEngine.BeamEngine.Elements.AddRange(thSplitBeam.SplitBeams);
                    BeamConnectRecogitionEngine.SyncBeamSpatialIndex();
                }
            });
        }
        private List<Polyline> BeamCrossWallOutlines(ThIfcBeam thIfcBeam)
        {
            List<Polyline> passWalls = new List<Polyline>();
            //搜索与梁相交的墙段
            Polyline outline = thIfcBeam.Extend(0.0, -ThMEPEngineCoreCommon.BeamBufferDistance);
            DBObjectCollection wallComponents = BeamConnectRecogitionEngine.SpatialIndexManager.WallSpatialIndex.SelectCrossingPolygon(outline);
            if (wallComponents.Count > 0)
            {
                var intersectShearWalls = BeamConnectRecogitionEngine.ShearWallEngine.FilterByOutline(wallComponents).ToList();
                intersectShearWalls.ForEach(m => passWalls.Add(m.Outline as Polyline));
            }
            return passWalls;
        }
        private List<Polyline> BeamCrossColumnOutlines(ThIfcBeam thIfcBeam)
        {            
            List<Polyline> passColumns = new List<Polyline>();
            Polyline outline = thIfcBeam.Extend(0.0, -ThMEPEngineCoreCommon.BeamBufferDistance);
            DBObjectCollection columnComponents = BeamConnectRecogitionEngine.SpatialIndexManager.ColumnSpatialIndex.SelectCrossingPolygon(outline);
            if (columnComponents.Count > 0)
            {
                var intersectColumns = BeamConnectRecogitionEngine.ColumnEngine.FilterByOutline(columnComponents).ToList();
                intersectColumns.ForEach(o =>
                {
                    passColumns.Add(o.Outline as Polyline);
                });
            }
            return passColumns;
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
        }
    }
}
