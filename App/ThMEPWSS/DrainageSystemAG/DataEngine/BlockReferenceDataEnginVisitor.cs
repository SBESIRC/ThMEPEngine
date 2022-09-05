using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.DrainageSystemAG.DataEngineVisitor
{
    public class BlockReferenceDataEnginVisitor : ThDistributionElementExtractionVisitor
    {
        protected Dictionary<string, int> blockNames;
        protected bool isLayerName;
        protected bool isModelSpace;
        public BlockReferenceDataEnginVisitor(Dictionary<string, int> bNames,bool isLayerName,bool modelSpace) 
        {
            blockNames = new Dictionary<string, int>();
            this.isLayerName = isLayerName;
            if (null != bNames && bNames.Count > 0)
            {
                foreach (var name in bNames)
                {
                    if (string.IsNullOrEmpty(name.Key))
                        continue;
                    this.blockNames.Add(name.Key, name.Value);
                }
            }
            this.isModelSpace = modelSpace;
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
                bool isAdd = false;
                var name = ThMEPXRefService.OriginalFromXref(blockObj.GetEffectiveName());
                if (this.isLayerName)
                    name = ThMEPXRefService.OriginalFromXref(blockObj.Layer); 
                foreach (var keyValue in this.blockNames)
                {
                    if (isAdd)
                        break;
                    if (string.IsNullOrEmpty(keyValue.Key))
                        continue;
                    string[] allNames = keyValue.Key.Split(',');
                    isAdd = true;
                    for (int i = 0; i < allNames.Length; i++) 
                    {
                        if (!isAdd)
                            break;
                        string checkName = allNames[i];
                        if (keyValue.Value == 1)
                        {
                            //包含
                            isAdd = name.Contains(checkName);
                        }
                        else if (keyValue.Value == 2)
                        {
                            isAdd = name.Equals(checkName);
                        }
                        else if (keyValue.Value == 3)
                        {
                            isAdd = name.StartsWith(checkName);
                        } 
                        else if (keyValue.Value ==4) 
                        {
                            isAdd = name.EndsWith(checkName);
                        }
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
                var copy = blkref.GetTransformedCopy(matrix);
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Geometry = copy,
                });
            }
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                //TODO: 获取块的OBB
                try
                {
                    //块有些是空的,获取GeometricExtents会报错
                    if (br.Bounds == null)
                        return false;
                    return xclip.Contains(br.GeometricExtents.ToRectangle());
                }
                catch 
                {
                    return false;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (!isModelSpace)
                return base.IsBuildElementBlock(blockTableRecord);
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
                return false;
            if (isModelSpace && blockTableRecord.IsFromExternalReference)
                return false;
            
            return true;
        }
    }
}
