using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThDoubleRowBeamMarkHandler
    {
        public List<DBObjectCollection> Handle(DBObjectCollection beamTexts)
        {
            var groups = Group(beamTexts);
            return groups
                .Where(g => g.Count > 1)
                .Where(g => IsDoubleRowBeamMark(g)) // 按标高不同来判断是不是双梁
                .Select(g => Sort(g))
                .ToList();
        }

        private bool IsDoubleRowBeamMark(DBObjectCollection beamTexts)
        {
            // 业务叫双梁标注，但实际Case会有三梁或，四梁
            var elevations = beamTexts.OfType<DBText>().Select(o => o.TextString.GetElevation().Value).ToList();
            bool isSameElevaton = true;
            for(int i=1;i<elevations.Count;i++)
            {
                if(Math.Abs(elevations[i]- elevations[0])>1.0)
                {
                    isSameElevaton = false;
                    break;
                }
            }
            return !isSameElevaton;
        }

        private DBObjectCollection Sort(DBObjectCollection beamTexts)
        {
            return beamTexts
                .OfType<DBText>()
                .OrderByDescending(o => o.TextString.GetElevation().Value)
                .ToCollection();
        }

        private List<DBObjectCollection> Group(DBObjectCollection texts)
        {
            var grouper = new ThFullOverlapBeamMarkGrouper(texts);
            grouper.Group();
            return grouper.Groups;
        }
    }
    internal class ThFullOverlapBeamMarkGrouper
    {
        private double TextParallelTolerance = 1.0; // 文字平行容差
        private double ClosestDistanceTolerance = 50.0; // 文字中心到文字中心的距离范围
        private Dictionary<DBText, Point3d> TextCenterDict { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public List<DBObjectCollection> Groups { get; private set; }
        public ThFullOverlapBeamMarkGrouper(DBObjectCollection beamMarks)
        {
            Groups = new List<DBObjectCollection>();
            TextCenterDict = GetTextCenter(beamMarks)
                .Where(o => o.Value.HasValue)
                .ToDictionary(item=>item.Key,item=>item.Value.Value);            
            SpatialIndex = new ThCADCoreNTSSpatialIndex(TextCenterDict.Keys.ToCollection());
        }
        public void Group()
        {
            // 按文字中心靠近
            foreach(var item in TextCenterDict)
            {
                if(IsGrouped(item.Key))
                {
                    continue;
                }
                var envelope = CreateEnvelope(item.Value, ClosestDistanceTolerance, ClosestDistanceTolerance);
                var objs = Query(SpatialIndex, envelope);
                objs = objs.OfType<DBText>()
                    .Where(o => IsParallel(item.Key.Rotation, o.Rotation, TextParallelTolerance))
                    .ToCollection();
                Groups.Add(objs);
            }
        }

        private bool IsGrouped(DBText text)
        {
            return Groups.Where(o => o.Contains(text)).Any();
        }

        private DBObjectCollection Query(ThCADCoreNTSSpatialIndex spatialIndex, Polyline outline)
        {
            return spatialIndex.SelectCrossingPolygon(outline);
        }

        private Polyline CreateEnvelope(Point3d center,double length,double width)
        {
            return center.CreateRectangle(length,width);
        }

        private Point3d? TextCenter(DBText dbtext)
        {
            Point3d? result = null;
            var obb = dbtext.TextOBB();
            if (obb.Closed && obb.NumberOfVertices >= 4)
            {
                result = obb.GetPoint3dAt(0).GetMidPt(obb.GetPoint3dAt(2));
            }
            obb.Dispose();
            return result;
        }
        private Dictionary<DBText,Point3d?> GetTextCenter(DBObjectCollection texts)
        {
            var results = new Dictionary<DBText,Point3d?>();
            texts.OfType<DBText>().ForEach(e =>
            {
                var center = TextCenter(e);
                results.Add(e, center);
            });
            return results;
        }
        private bool IsParallel(double firstRad,double secondRad,double tolerance)
        {
            var firstAng = firstRad.RadToAng();
            var secondAng = secondRad.RadToAng();
            return firstAng.IsAngleParallel(secondAng, tolerance);
        }
    }
}
