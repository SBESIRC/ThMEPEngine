using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThArchitectureOutlineExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> SelectVerComponentUseArchOutlines { get; set; }
        public List<Polyline> RealArchOutlines { get; set; }

        public string SelectVerComponentUseArchOutlineLayer { get; set; }
        public string RealArchOutlineLayer { get; set; }
        public ThArchitectureOutlineExtractor()
        {
            Category = "ArchitectureOutline";
            RealArchOutlines = new List<Polyline>();
            SelectVerComponentUseArchOutlines = new List<Polyline>();
            RealArchOutlineLayer = "真实建筑轮廓";
            SelectVerComponentUseArchOutlineLayer = "选取竖向构建用建筑轮廓";
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var service1 = new ThExtractPolylineService()
            {
                ElementLayer = RealArchOutlineLayer
            };
            service1.Extract(database, pts);
            RealArchOutlines = service1.Polys;

            var service2 = new ThExtractPolylineService()
            {
                ElementLayer = SelectVerComponentUseArchOutlineLayer
            };
            service2.Extract(database, pts);
            SelectVerComponentUseArchOutlines = service2.Polys;
        }


        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            geos.AddRange(BuildRealArchoutlineGeos());
            geos.AddRange(BuildSelectVerComponentArchoutlineGeos());
            return geos;
        }

        private List<ThGeometry> BuildRealArchoutlineGeos()
        {
            var geos = new List<ThGeometry>();
            RealArchOutlines.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "真实建筑轮廓");
                if(GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }                
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        private List<ThGeometry> BuildSelectVerComponentArchoutlineGeos()
        {
            var geos = new List<ThGeometry>();
            SelectVerComponentUseArchOutlines.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "选取竖向构建用建筑轮廓");
                if(GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Print(Database database)
        {
            RealArchOutlines.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
            SelectVerComponentUseArchOutlines.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                SelectVerComponentUseArchOutlines.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
                RealArchOutlines.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
