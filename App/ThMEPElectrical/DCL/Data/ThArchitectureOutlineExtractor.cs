using System;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPElectrical.DCL.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.DCL.Data
{
    public class ThArchitectureOutlineExtractor : ThExtractorBase, IPrint, IGroup
    {
        public Dictionary<Entity, string> OuterArchOutlineIdDic { get; set; }
        public Dictionary<Entity, string> InnerArchOutlineIdDic { get; set; }
        public ModelData ModelData { get; private set; }
        public ThArchitectureOutlineExtractor()
        {
            OuterArchOutlineIdDic = new Dictionary<Entity, string>();
            InnerArchOutlineIdDic = new Dictionary<Entity, string>();
            Category = BuiltInCategory.ArchitectureOutline.ToString();
            ElementLayer = "AI-洞";
        }
        public override void Extract(Database database, Point3dCollection pts)
        {
            var buildServie = new ThBuildOuterArchOutline();
            buildServie.Extract(database, pts);
            buildServie.ExtractHoles(database, pts);
            ModelData = buildServie.ModelData;
            buildServie.OuterOutlineList.ForEach(o => OuterArchOutlineIdDic.Add(o, Guid.NewGuid().ToString()));
            buildServie.InnerOutlineList.ForEach(o => OuterArchOutlineIdDic.Add(o, Guid.NewGuid().ToString()));
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            OuterArchOutlineIdDic.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Value);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, "");
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "建筑轮廓");
                if (GroupSwitch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o.Key));
                }
                geometry.Boundary = o.Key;
                geos.Add(geometry);
            });

            InnerArchOutlineIdDic.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, o.Value);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, QueryParentId(o.Key as Polyline));
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "建筑轮廓");
                if (GroupSwitch)
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, BuildString(GroupOwner, o.Key));
                }
                geometry.Boundary = o.Key;
                geos.Add(geometry);
            });

            return geos;
        }

        public void Print(Database database)
        {
            var ents = new List<Entity>();
            ents.AddRange(OuterArchOutlineIdDic.Keys.ToList());
            ents.AddRange(InnerArchOutlineIdDic.Keys.ToList());
            ents.CreateGroup(database, ColorIndex);
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            if (GroupSwitch)
            {
                OuterArchOutlineIdDic.ForEach(o => GroupOwner.Add(o.Key, FindCurveGroupIds(groupId, o.Key)));
                InnerArchOutlineIdDic.ForEach(o => GroupOwner.Add(o.Key, FindCurveGroupIds(groupId, o.Key)));
            }
        }
        private string QueryParentId(Polyline hole)
        {
            foreach (var item in OuterArchOutlineIdDic)
            {
                if (item.Key.IsContains(hole))
                {
                    return item.Value;
                }
            }
            return "";
        }
    }
}
