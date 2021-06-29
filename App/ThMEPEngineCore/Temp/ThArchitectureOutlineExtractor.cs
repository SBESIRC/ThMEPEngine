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
        public List<ThArchitectureOutlineInfo> ArchOutlineInfos { get; set; }
        /// <summary>
        /// 建筑轮廓图层
        /// </summary>
        public string ArchOutlineLayer { get; set; }
        public ThArchitectureOutlineExtractor()
        {
            TesslateLength = 200.0;
            ArchOutlineLayer = "建筑轮廓";
            ArchOutlineInfos = new List<ThArchitectureOutlineInfo>();      
            Category = BuiltInCategory.ArchitectureOutline.ToString();
        }
        public void Extract(Database database, Point3dCollection pts)
        {
            var service = new ThExtractPolylineService()
            {
                ElementLayer = ArchOutlineLayer,
            };
            service.Extract(database, pts);
            ArchOutlineInfos = service.Polys.Select(o => new ThArchitectureOutlineInfo(o)).ToList();
            ArchOutlineInfos.ForEach(o =>
            {
                o.Outline = ThTesslateService.Tesslate(o.Outline, TesslateLength) as Polyline;
            });
        }
        
        public List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            ArchOutlineInfos.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(IdPropertyName, o.ID);
                geometry.Properties.Add(ParentIdPropertyName, o.ParentID);
                geometry.Properties.Add(CategoryPropertyName, Category);
                geometry.Properties.Add(NamePropertyName, "建筑轮廓");
                if (GroupSwitch)
                {
                    geometry.Properties.Add(GroupIdPropertyName, BuildString(GroupOwner, o.Outline));
                }                
                geometry.Boundary = o.Outline;
                geos.Add(geometry);
            });
            return geos;
        }
        public void Print(Database database)
        {
            ArchOutlineInfos.Select(o=>o.Outline).Cast<Entity>().ToList().CreateGroup(database, ColorIndex);            
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if(GroupSwitch)
            {
                ArchOutlineInfos.ForEach(o => GroupOwner.Add(o.Outline, FindCurveGroupIds(groupId, o.Outline)));
            }
        }
    }
}
