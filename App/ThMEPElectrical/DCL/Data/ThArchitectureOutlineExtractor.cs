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
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor.Model;
using NFox.Cad;
using ThCADCore.NTS;

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
        /// <summary>
        /// 将第二层轮廓线复制进第一层。暂时不处理洞口
        /// </summary>
        /// <param name="storeyInfos"></param>
        public void MoveSecondToFirst(List<ThEStoreyInfo> storeyInfos, Dictionary<string, string> map)
        {
            //假定包含真实的1楼和2楼对应的值必定为1F和2F
            if (OuterArchOutlineIdDic.Count == 0)
                return;
            bool flag1, flag2;
            flag1 = flag2 = false;
            foreach(var item in map)
            {
                if (item.Value == "1F" && item.Key.Contains("1F"))
                    flag1 = true;
                if (item.Value == "2F" && item.Key.Contains("2F"))
                    flag2 = true;
            }
            if (!(flag1 && flag2))
                return;
            //if(!(storeyInfos.Where(o => IsContains(o.StoreyNumber, "1F")).Any() &&
            //    storeyInfos.Where(o => IsContains(o.StoreyNumber, "2F")).Any()))
            //{
            //    return; 
            //}
            var firstStorey = storeyInfos.Where(o => IsContains(o.StoreyNumber, "1F")).First();
            var secondStorey = storeyInfos.Where(o => IsContains(o.StoreyNumber, "2F")).First();
            var firtbasepoint = firstStorey.BasePoint.Split(',');
            var secondbasepoint = secondStorey.BasePoint.Split(',');
            if (firtbasepoint.Length != 2 || secondbasepoint.Length != 2)
                return;
            //Boundary为楼层框线，并不是建筑轮廓线。建立两者的联系
            var boundaryOuterArchlineDic = GetStoryOuterLineDic(storeyInfos);
            Matrix3d transform = new Matrix3d(new double[]{1, 0, 0, double.Parse(firtbasepoint[0]) - double.Parse(secondbasepoint[0]),
                                                           0, 1, 0, double.Parse(firtbasepoint[1]) - double.Parse(secondbasepoint[1]),
                                                           0, 0, 1, 0,
                                                           0, 0, 0, 1});
            //移除所有的除洞口外的一楼，因为一楼的不会闭合
            boundaryOuterArchlineDic[firstStorey.Boundary].ForEach(o=>
            {
                if(OuterArchOutlineIdDic.ContainsKey(o))
                    OuterArchOutlineIdDic.Remove(o);
            });
            boundaryOuterArchlineDic[secondStorey.Boundary].ForEach(o =>
            {
                //拿到第二层的形状，并进行平移变换
                var firsrstoryarchline = boundaryOuterArchlineDic[secondStorey.Boundary];
                firsrstoryarchline.ForEach(e =>
                {
                    var poly = (Polyline)(e.Clone());
                    poly.TransformBy(transform);
                    OuterArchOutlineIdDic.Add(poly, Guid.NewGuid().ToString());
                });
            });
        }
        private Dictionary<Entity,List<Entity>> GetStoryOuterLineDic(List<ThEStoreyInfo> storeyInfos)
        {
            Dictionary<Entity, List<Entity>> res = new Dictionary<Entity, List<Entity>>();
            ThCADCoreNTSSpatialIndex outerArchlineIndex = new ThCADCoreNTSSpatialIndex(OuterArchOutlineIdDic.Keys.ToCollection());
            storeyInfos.ForEach(o =>
            {
                var outerArchlineInBoundary = outerArchlineIndex.SelectWindowPolygon(o.Boundary).Cast<Entity>().ToList();
                res.Add(o.Boundary, outerArchlineInBoundary);
            });
            return res;
        }
        private bool IsContains(string storeyNumber, string floorNo)
        {
            var splitChars = storeyNumber.Split(',');
            return splitChars.Where(o => o.ToUpper().Equals(floorNo.ToUpper())).Any();
        }
    }
}
