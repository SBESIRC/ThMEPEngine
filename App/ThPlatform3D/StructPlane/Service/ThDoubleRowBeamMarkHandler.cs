using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThDoubleRowBeamMarkHandler
    {
        public List<DBObjectCollection> Handle(DBObjectCollection beamTexts)
        {
            try
            {           
                var groups = Group(beamTexts);
                return groups
                    .Where(g => g.Count > 1)
                    .Where(g => IsDoubleRowBeamMark(g)) // 按标高不同来判断是不是双梁
                    .Select(g => Sort(g))
                    .ToList();
            }
            catch
            {
            }
            return new List<DBObjectCollection>();
        }

        private bool IsDoubleRowBeamMark(DBObjectCollection beamTexts)
        {
            // 业务叫双梁标注，但实际Case会有三梁或，四梁
            var elevations = beamTexts
                   .OfType<DBText>()
                   .Select(o => GetBeamElevation(o.TextString))
                   .ToList();
            bool isSameElevaton = true;
            for (int i = 1; i < elevations.Count; i++)
            {
                if (Math.Abs(elevations[i] - elevations[0]) > 1.0)
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
                .OrderByDescending(o => GetBeamElevation(o.TextString))
                .ToCollection();
        }

        private double GetBeamElevation(string beamSpec)
        {
            var elevation = beamSpec.GetElevation();
            return elevation.HasValue ? elevation.Value : 0.0;
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
        private Dictionary<DBText, Point3d> _textCenterDict;
        public List<DBObjectCollection> Groups { get; private set; }
        public ThFullOverlapBeamMarkGrouper(DBObjectCollection beamMarks)
        {
            Groups = new List<DBObjectCollection>();
            _textCenterDict = GetTextCenter(beamMarks);     
        }
        public void Group()
        {
            // 按文字中心靠近
            foreach(var item in _textCenterDict)
            {
                if (IsGrouped(item.Key))
                {
                    continue;
                }                
                var objs = GetCloseObjs(item.Value, ClosestDistanceTolerance);                
                objs = objs.OfType<DBText>()
                    .Where(o=> !IsGrouped(o))
                    .Where(o => item.Key.Rotation.IsRadianParallel(o.Rotation, TextParallelTolerance))
                    .ToCollection();
                Groups.Add(objs);
            }
        }

        private DBObjectCollection GetCloseObjs(Point3d textCenter,double radius)
        {
            return _textCenterDict
                .Where(o => o.Value.DistanceTo(textCenter) <= radius)
                .Select(o => o.Key)
                .ToCollection();
        }

        private bool IsGrouped(DBText text)
        {
            return Groups.Where(o => o.Contains(text)).Any();
        }

        private Dictionary<DBText,Point3d> GetTextCenter(DBObjectCollection texts)
        {
            var results = new Dictionary<DBText,Point3d>();
            texts
                .OfType<DBText>()
                .ForEach(e => results.Add(e, e.AlignmentPoint));
            return results;
        }
    }
}
