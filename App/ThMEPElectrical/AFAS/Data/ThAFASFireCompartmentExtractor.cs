using NFox.Cad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.AFAS.Data
{
    class ThAFASFireCompartmentExtractor : ThExtractorBase, IGroup, ITransformer
    {
        public DBObjectCollection FireCompartments { get; private set; }

        public List<ThStoreyInfo> StoreyInfos { get; set; }

        public Dictionary<Entity, string> FireApartIds { get; private set; }

        public ThAFASFireCompartmentExtractor()
        {
            FireCompartments = new DBObjectCollection();
            Category = BuiltInCategory.FireApart.ToString();
            StoreyInfos = new List<ThStoreyInfo>();
            FireApartIds = new Dictionary<Entity, string>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            FireCompartments.OfType<Entity>().ForEach(o =>
            {
                var geometry = new ThGeometry();
                if (FireApartIds.ContainsKey(o))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, FireApartIds[o]);
                }
                else
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, "");
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, BuildString(GroupOwner, o));
                geos.Add(geometry);
            });

            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            var builder = new ThFireCompartmentBuilder()
            {
                LayerFilter = ElementLayer.Split(',').ToList(),
            };
            var compartments = builder.BuildFromMS(database, pts);
            FireCompartments = compartments.Select(o => o.Boundary).ToCollection();

            // 移动到原点附近
            Transformer.Transform(FireCompartments);

            // 业务需求：
            //  如果楼层框线没有防火分区，就认为楼层框线是一个防火分区
            var spatialIndex = new ThCADCoreNTSSpatialIndex(FireCompartments);
            StoreyInfos.ForEach(o =>
            {
                if (spatialIndex.SelectWindowPolygon(o.Boundary).Count == 0)
                {
                    var bufferService = new ThNTSBufferService();
                    var fireApartOutline = bufferService.Buffer(o.Boundary, -1.0) as Polyline;
                    FireCompartments.Add(fireApartOutline);
                }
            });
        }

        public void Group(Dictionary<Entity, string> groupId)
        {
            //FireCompartments.OfType<Entity>().ForEach(o => GroupOwner.Add(o, FindCurveGroupIds(groupId, o)));
            foreach (var item in FireCompartments)
            {
                if (item is Entity o)
                {
                    if (GroupOwner.ContainsKey(o) == false)
                    {
                        GroupOwner.Add(o, FindCurveGroupIds(groupId, o));
                    }
                    else
                    {
                        GroupOwner[o] = FindCurveGroupIds(groupId, o);
                    }
                }
            }
        }

        public void BuildFireAPartIds()
        {
            //只有分组才能获取
            StoreyInfos.ForEach(o =>
            {
                var fireAparts = GroupOwner.Where(g => g.Value.Contains(o.Id)).Select(g => g.Key).ToList();
                string startCode = "";
                switch (o.StoreyType)
                {
                    case "大屋面":
                        startCode = "JF";
                        break;
                    case "小屋面":
                        startCode = "RF";
                        break;
                    default:
                        startCode = o.StoreyNumber.Split(',')[0];
                        break;
                }
                int startIndex = 1;
                fireAparts.ForEach(f =>
                {
                    string number = startIndex++.ToString().PadLeft(2, '0');
                    FireApartIds.Add(f, startCode + number);
                });
            });
        }

        public void Print(Database database)
        {
            FireCompartments.OfType<Entity>().ToList().CreateGroup(database, ColorIndex);
        }

        public void Transform()
        {
            Transformer.Transform(FireCompartments);
        }

        public void Reset()
        {
            Transformer.Reset(FireCompartments);
        }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
    }
}
