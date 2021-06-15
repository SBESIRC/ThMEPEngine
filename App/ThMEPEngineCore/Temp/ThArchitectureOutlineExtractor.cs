using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThArchitectureOutlineExtractor : ThExtractorBase, IExtract, IPrint, IBuildGeometry,IGroup
    {
        public List<Polyline> ArchOutlines { get; set; }
        /// <summary>
        /// 建筑轮廓图层
        /// </summary>
        public string ArchOutlineLayer { get; set; }
        public double TesslateLength { get; set; }
        public ThArchitectureOutlineExtractor()
        {
            TesslateLength = 200.0;
            ArchOutlineLayer = "建筑轮廓";
            ArchOutlines = new List<Polyline>();      
            Category = BuiltInCategory.ArchitectureOutline.ToString();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var service = new ThExtractPolylineService()
            {
                ElementLayer = ArchOutlineLayer,
            };
            service.Extract(database, pts);
            ArchOutlines.AddRange(service.Polys);
        }
        
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ArchOutlines.ForEach(o =>
            {
                var clone = Tesslate(o, TesslateLength);
                var geometry = new ThGeometry();

                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "建筑轮廓");
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o));
                }
                if(DescribePolygonSwitch)
                {
                    geometry.Properties.Add(PolygonInfoPropertyName, 
                        ThPolylineSerializeService.Serialize(o));
                }
                geometry.Boundary = clone;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Print(Database database)
        {
            ArchOutlines.Cast<Entity>().ToList().CreateGroup(database, ColorIndex);            
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                ArchOutlines.ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            }
        }
    }
}
