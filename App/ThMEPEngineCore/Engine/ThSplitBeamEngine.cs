using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;

namespace ThMEPEngineCore.Service
{
    public class ThSplitBeamEngine
    {
        private List<ThIfcBuildingElement> ColumnElements { get; set; }
        private List<ThIfcBuildingElement> ShearWallElements { get; set; }
        public List<ThIfcBuildingElement> BeamElements { get; set; }
        public ThSplitBeamEngine(List<ThIfcBuildingElement> columnElements, 
            List<ThIfcBuildingElement> shearWallElements, 
            List<ThIfcBuildingElement> beamElements)
        {
            ColumnElements = columnElements;
            ShearWallElements = shearWallElements;
            BeamElements = beamElements;
        }
        public virtual void Split()
        {
            //处理梁穿过竖向构件(柱，剪力墙)
            SplitBeamPassWalls();

            //处理梁穿过梁
            SplitBeamPassBeams();

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
            List<string> uuids = new List<string>();
            List<ThIfcBeam> divideBeams = new List<ThIfcBeam>();
            for(int i=0;i< BeamElements.Count;i++)
            {
                DBObjectCollection wallComponents = ThSpatialIndexManager.Instance.WallSpatialIndex.
                  SelectCrossingPolygon(BeamElements[i].Outline as Polyline);
                if (BeamElements[i] is ThIfcLineBeam thIfcLineBeam)
                {
                    ThSplitLinearBeamSevice thDivideLineBeam = new ThSplitLinearBeamSevice(thIfcLineBeam, wallComponents);
                    thDivideLineBeam.Split();
                    if(thDivideLineBeam.SplitBeams.Count>1)
                    {
                        uuids.Add(BeamElements[i].Uuid); //记录要移除的梁UUID
                        divideBeams.AddRange(thDivideLineBeam.SplitBeams);
                    }
                }
                else if(BeamElements[i] is ThIfcArcBeam thIfcArcBeam)
                {
                    //TODO
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            BeamElements.Where(o => uuids.IndexOf(o.Uuid) >= 0).ToList().ForEach(o => o.Outline.Dispose());
            BeamElements = BeamElements.Where(o => uuids.IndexOf(o.Uuid) < 0).ToList();
            BeamElements.AddRange(divideBeams);
        }
        private void SplitBeamColumnComponents()
        {
            List<string> uuids = new List<string>();
            List<ThIfcBeam> divideBeams = new List<ThIfcBeam>();
            for (int i = 0; i < BeamElements.Count; i++)
            {
                DBObjectCollection columnComponents = ThSpatialIndexManager.Instance.ColumnSpatialIndex.
                   SelectCrossingPolygon(BeamElements[i].Outline as Polyline);
                if (BeamElements[i] is ThIfcLineBeam thIfcLineBeam)
                {
                    ThSplitLinearBeamSevice thDivideLineBeam = new ThSplitLinearBeamSevice(thIfcLineBeam, columnComponents);
                    thDivideLineBeam.Split();
                    if (thDivideLineBeam.SplitBeams.Count > 1)
                    {
                        uuids.Add(BeamElements[i].Uuid); //记录要移除的梁UUID
                        divideBeams.AddRange(thDivideLineBeam.SplitBeams);
                    }
                }
                else if (BeamElements[i] is ThIfcArcBeam thIfcArcBeam)
                {
                    //TODO
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            BeamElements.Where(o => uuids.IndexOf(o.Uuid) >= 0).ToList().ForEach(o => o.Outline.Dispose());
            BeamElements = BeamElements.Where(o => uuids.IndexOf(o.Uuid) < 0).ToList();
            BeamElements.AddRange(divideBeams);
        }
        private void SplitBeamPassBeams()
        {
            var unintersectBeams = FilterBeamUnIntersectOtherBeams();
            List<ThIfcBuildingElement> restBeams = BeamElements.Where(m=> !unintersectBeams.Where(n=>m.Uuid==n.Uuid).Any()).ToList();
            List<ThIfcBuildingElement> inValidBeams = new List<ThIfcBuildingElement>();
            while (restBeams.Count>0)
            {
                var firstBeam = restBeams[0];
                restBeams.RemoveAt(0);
                DBObjectCollection beamOutlines = new DBObjectCollection();
                restBeams.ForEach(o=>beamOutlines.Add(o.Outline));
                var beamSpatialIndex = ThSpatialIndexService.CreateBeamSpatialIndex(beamOutlines);
                DBObjectCollection passComponents = beamSpatialIndex.SelectCrossingPolygon(firstBeam.Outline as Polyline);
                passComponents.Remove(firstBeam.Outline);
                if(passComponents.Count==0)
                {
                    unintersectBeams.Add(firstBeam);
                    continue;
                }
                if (firstBeam is ThIfcLineBeam thIfcLineBeam)
                {
                    ThSplitLinearBeamSevice thDivideLineBeam = new ThSplitLinearBeamSevice(thIfcLineBeam, passComponents);
                    thDivideLineBeam.Split();
                    if (thDivideLineBeam.SplitBeams.Count > 1)
                    {
                        inValidBeams.Add(firstBeam);  //分段成功,将原始梁段存进 “inValidBeams”中，便于回收
                        restBeams.AddRange(thDivideLineBeam.SplitBeams); //将分段的梁段存到 “restBeams”中，继续与其它梁段判断
                    }
                    else
                    {
                        unintersectBeams.Add(thIfcLineBeam); //没有分段，存入 “unintersectBeams”中
                    }
                }
                else if (firstBeam is ThIfcArcBeam thIfcArcBeam)
                {
                    //TODO
                }
                else
                {
                    throw new NotSupportedException();
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
     }
}
