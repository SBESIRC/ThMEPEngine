using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;

using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    class ThFireAlarmBlkExtractor : ThExtractorBase, ITransformer, IGroup, ISetStorey
    {
        public Dictionary<BlockReference, Polyline> Equipment { get; private set; } //key:origin blkreference, value: blk postition dbpoint
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }
        public List<string> BlkNameList { get; set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        private Dictionary<BlockReference, Point3d> BlkCenterDict = new Dictionary<BlockReference, Point3d>();

        public ThFireAlarmBlkExtractor()
        {
            Category = BuiltInCategory.Equipment.ToString();
            Equipment = new Dictionary<BlockReference, Polyline>();
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Equipment.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, o.Key.GetEffectiveName());
                geometry.Properties.Add(ThExtractorPropertyNameManager.HandlerPropertyName, o.Key.Handle.ToString());

                var parentId = BuildString(GroupOwner, o.Key);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Value);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);


                geometry.Boundary = o.Value;
                geos.Add(geometry);
            });
            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            foreach (var blkName in BlkNameList)
            {
                var extractService = new ThExtractBlockReferenceService()
                {
                    BlockName = blkName,
                };
                extractService.Extract(database, pts);

                extractService.Blocks.ForEach(x =>
                {
                    var obb = x.ToOBB(x.BlockTransform);
                    if (obb != null && obb.Area > 1.0)
                    {
                        var bufferObb = obb.GetOffsetClosePolyline(15);
                        if (bufferObb != null)
                        {
                            Equipment.Add(x, bufferObb);
                        }
                    }
                });
            }
            //不加就是原位置。加就是靠近远点。
            Transform();
        }

        public void Transform()
        {
            Transformer.Transform(Equipment.Values.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(Equipment.Values.ToCollection());
        }

        public ThStoreyInfo Query(Entity entity)
        {
            //ToDo
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public void Group(Dictionary<Entity, string> groupId)
        {

            //Equipment.ForEach(o =>
            //{
            //    GroupOwner.Add(o.Key , FindCurveGroupIds(groupId, o.Value));
            //});

            foreach (var o in Equipment)
            {
                if (GroupOwner.ContainsKey(o.Key) == false)
                {
                    GroupOwner.Add(o.Key, FindCurveGroupIds(groupId, o.Value));
                }
                else
                {
                    GroupOwner[o.Key] = FindCurveGroupIds(groupId, o.Value);
                }
            }
        }
    }
}
