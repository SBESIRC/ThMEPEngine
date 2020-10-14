using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamBreakEngine
    {
        private List<ThBeamLink> PrimaryBeamLinks { get; set; }
        private List<ThIfcBeam> PrimaryBeams { get; set; }

        private ThBeamConnectRecogitionEngine BeamConnectRecognitionEngine { get; set; }
        private ThCADCoreNTSSpatialIndex BeamSpatialIndex { get; set; }

        public ThBeamBreakEngine(
            List<ThBeamLink> primaryBeamLinks,
            ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            PrimaryBeamLinks = primaryBeamLinks;
            BeamConnectRecognitionEngine = thBeamConnectRecogitionEngine;
            BeamSpatialIndex = BeamConnectRecognitionEngine.SpatialIndexManager.BeamSpatialIndex;
            Init();
        }
        private void Init()
        {
            PrimaryBeams = new List<ThIfcBeam>();
            PrimaryBeamLinks.ForEach(m => m.Beams.ForEach(n => PrimaryBeams.Add(n)));
        }
        public void Break()
        {
            PrimaryBeams.ForEach(o => Break(o));
        }
        private void Break(ThIfcBeam primaryBeam)
        {
            if(primaryBeam is ThIfcLineBeam thIfcLineBeam)
            {
                Break(thIfcLineBeam);
            }
            else if(primaryBeam is ThIfcArcBeam thIfcArcBeam)
            {
                Break(thIfcArcBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void Break(ThIfcLineBeam primaryBeam)
        {
            Polyline outline = primaryBeam.Outline as Polyline;
            var RestBeams= BeamConnectRecognitionEngine.BeamEngine.Elements.Cast<ThIfcBeam>().ToList().Where(o =>
            {
                return o.ComponentType == BeamComponentType.Undefined ? true : false;
            }).ToList();
            DBObjectCollection passComponents = BeamSpatialIndex.SelectCrossingPolygon(outline);
            var passBeams = BeamConnectRecognitionEngine.BeamEngine.FilterByOutline(passComponents);
            passBeams=passBeams.Where(m => RestBeams.Where(n => n.Uuid == m.Uuid).Any());
            passBeams=passBeams.Where(o=> JudgeBeamIsFullIntersect(primaryBeam, o as ThIfcBeam));
            passBeams.ForEach(o => SplitCrossedBeam(primaryBeam, o as ThIfcBeam));
            if(primaryBeam.ComponentType==BeamComponentType.OverhangingPrimaryBeam)
            {
                HandleOverhangingPrimaryBeam(primaryBeam);
            }
        }
        private void HandleOverhangingPrimaryBeam(ThIfcLineBeam primaryBeam)
        {
            double startExtendDis = 0.0;
            double endExtendDis = 0.0;
            var findRes = PrimaryBeamLinks.Where(m => m.Beams.Where(n => n.Uuid == primaryBeam.Uuid).Any());
            if (findRes.Count() > 0)
            {
                ThBeamLink thBeamLink = findRes.First();
                if (thBeamLink.StartHasVerticalComponent)
                {
                    endExtendDis= ThMEPEngineCoreCommon.BeamIntersectExtentionTolerance;
                }
                else
                {
                    startExtendDis= ThMEPEngineCoreCommon.BeamIntersectExtentionTolerance;
                }
            }
            Polyline extendOutline = primaryBeam.ExtendBoth(startExtendDis, endExtendDis);
            var RestBeams = BeamConnectRecognitionEngine.BeamEngine.Elements.Cast<ThIfcBeam>().ToList().Where(o =>
            {
                return o.ComponentType == BeamComponentType.Undefined ? true : false;
            }).ToList();
            DBObjectCollection passComponents = BeamSpatialIndex.SelectCrossingPolygon(extendOutline);
            var passBeams = BeamConnectRecognitionEngine.BeamEngine.FilterByOutline(passComponents);
            passBeams = passBeams.Where(m => RestBeams.Where(n => n.Uuid == m.Uuid).Any());
            passBeams.ForEach(o => SplitTTypeBeam(primaryBeam, o as ThIfcBeam));
        }
        private void HandleOverhangingPrimaryBeam(ThIfcArcBeam primaryBeam)
        {
            throw new NotSupportedException();
        }
        private void Break(ThIfcArcBeam thIfcArcBeam)
        {
            throw new NotSupportedException();
        }
        private void SplitCrossedBeam(ThIfcBeam primaryBeam, ThIfcBeam undefinedBeam)
        {
            if(primaryBeam is ThIfcLineBeam && undefinedBeam is ThIfcLineBeam)
            {
                SplitCrossedBeam(primaryBeam as ThIfcLineBeam, undefinedBeam as ThIfcLineBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void SplitCrossedBeam(ThIfcLineBeam primaryBeam, ThIfcLineBeam undefinedBeam)
        {
            List<ThSegment> passSegments = new List<ThSegment>();
            passSegments.Add(CreateSegment(primaryBeam));
            ThBeamSplitter thSplitBeam = new ThLinealBeamSplitter(undefinedBeam, passSegments);
            thSplitBeam.Split();
            if(thSplitBeam.SplitBeams.Count>1)
            {
                var splitBeams=thSplitBeam.SplitBeams.Where(o =>
                {
                    ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(passSegments[0].Outline, o.Outline as Polyline);
                    return !thCADCoreNTSRelate.IsWithIn;
                }).ToList();                
                BeamConnectRecognitionEngine.BeamEngine.Remove(undefinedBeam.Uuid);
                BeamConnectRecognitionEngine.BeamEngine.Elements.AddRange(splitBeams);
                BeamConnectRecognitionEngine.SyncBeamSpatialIndex();
                splitBeams.ForEach(o => BeamConnectRecognitionEngine.AddSingleBeamLink(o));
                BeamConnectRecognitionEngine.RemoveSingleBeamLink(undefinedBeam);
            }
        }
        private void SplitTTypeBeam(ThIfcBeam primaryBeam, ThIfcBeam undefinedBeam)
        {
            if (primaryBeam is ThIfcLineBeam && undefinedBeam is ThIfcLineBeam)
            {
                SplitTTypeBeam(primaryBeam as ThIfcLineBeam, undefinedBeam as ThIfcLineBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void SplitTTypeBeam(ThIfcLineBeam primaryBeam, ThIfcLineBeam undefinedBeam)
        {
            List<ThSegment> passSegments = new List<ThSegment>();
            passSegments.Add(CreateSegment(primaryBeam));
            ThBeamSplitter thSplitBeam = new ThLinealBeamSplitter(undefinedBeam, passSegments);
            thSplitBeam.SplitTType();
            if (thSplitBeam.SplitBeams.Count > 1)
            {
                var splitBeams = thSplitBeam.SplitBeams.Where(o =>
                {
                    ThCADCoreNTSRelate thCADCoreNTSRelate = new ThCADCoreNTSRelate(passSegments[0].Outline, o.Outline as Polyline);
                    return !thCADCoreNTSRelate.IsWithIn;
                }).ToList();
                BeamConnectRecognitionEngine.BeamEngine.Remove(undefinedBeam.Uuid);
                BeamConnectRecognitionEngine.BeamEngine.Elements.AddRange(splitBeams);
                BeamConnectRecognitionEngine.SyncBeamSpatialIndex();
                splitBeams.ForEach(o => BeamConnectRecognitionEngine.AddSingleBeamLink(o));
                BeamConnectRecognitionEngine.RemoveSingleBeamLink(undefinedBeam);
            }
        }
        private bool JudgeBeamIsFullIntersect(ThIfcBeam primaryBeam,ThIfcBeam undefinedBeam)
        {
            if(primaryBeam is ThIfcLineBeam && 
                undefinedBeam is ThIfcLineBeam)
            {
                return JudgeBeamIsFullIntersect(primaryBeam as ThIfcLineBeam, undefinedBeam as ThIfcLineBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private bool JudgeBeamIsFullIntersect(ThIfcLineBeam primaryBeam, ThIfcLineBeam undefinedBeam)
        {
            var intersectPts = ThGeometryTool.IntersectWithEx(primaryBeam.Outline, undefinedBeam.Outline);
            var undefinedOutlinePts=(undefinedBeam.Outline as Polyline).Vertices();
            if(intersectPts.Count==4)
            {
                bool isClose=intersectPts.Cast<Point3d>().Where(m =>
                {
                    return undefinedOutlinePts.Cast<Point3d>().Where(n => m.DistanceTo(n) <= 1.0).Any();
                }).Any();
                return isClose ? false : true;
            }
            return false;
        }
        private ThSegment CreateSegment(ThIfcBeam thIfcBeam)
        {
            if (thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                return CreateSegment(thIfcLineBeam);
            }
            else if (thIfcBeam is ThIfcArcBeam thIfcArcBeam)
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
    }
}
