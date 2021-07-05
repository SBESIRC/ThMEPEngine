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
        public BlockReferenceDataEnginVisitor(Dictionary<string, int> bNames,bool isLayerName) 
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
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference blkref)
            {
                HandleBlockReference(elements, blkref, matrix);
            }
            else if (dbObj.GetType().ToString().ToUpper().Contains("IMPENTITY")) 
            {
                //自定义实体，直接炸开继续循环获取
                try 
                {
                    var explodes = ThMEPTCHService.ExplodeTCHElement(dbObj);
                    if (null != explodes && explodes.Count > 0)
                        foreach (Entity entity in explodes)
                            DoExtract(elements, entity, matrix);
                }
                catch (Exception ex) 
                { }
                
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
                        else
                        {
                            isAdd = name.Equals(checkName);
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
                    Geometry = copy
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
    }
}
