using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemAG.DataEngineVisitor
{
    public class EquipmentDataEnginVisitor: ThDistributionElementExtractionVisitor
    {
        protected Dictionary<string, int> blockNames;
        public EquipmentDataEnginVisitor(Dictionary<string, int> bNames) 
        {
            blockNames = new Dictionary<string, int>();
            if (null != bNames && bNames.Count > 0)
            {
                foreach (var name in bNames)
                {
                    if (string.IsNullOrEmpty(name.Key))
                        continue;
                    this.blockNames.Add(name.Key, name.Value);
                }
            }
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
        }
        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (blockNames == null || blockNames.Count < 1)
                return false;
            if (entity is BlockReference blockObj)
            {
                var name = ThMEPXRefService.OriginalFromXref(blockObj.GetEffectiveName());
                bool isAdd = false;
                foreach (var keyValue in this.blockNames)
                {
                    if (isAdd)
                        break;
                    if (string.IsNullOrEmpty(keyValue.Key))
                        continue;
                    if (keyValue.Value == 1)
                    {
                        //包含
                        isAdd = name.Contains(keyValue.Key);
                    }
                    else
                    {
                        isAdd = name.Equals(keyValue.Key);
                    }
                }
                return isAdd;
            }
            return false;
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (CheckLayerValid(blkref) && IsDistributionElement(blkref))
            {
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Geometry = blkref.GetTransformedCopy(matrix),
                });
            }
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }
    }
}
