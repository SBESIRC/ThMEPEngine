﻿using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPWSS.Hydrant.Engine
{
    public class ThFireExtinguisherExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public List<string> BlkNames { get; set; }
        /// <summary>
        /// 获取块中心的小方块
        /// </summary>
        public bool BuildCenterSquare { get; set; }
        public ThFireExtinguisherExtractionVisitor()
        {
            BuildCenterSquare = true;
            BlkNames = new List<string>();
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                HandleBlockReference(elements, br, matrix);
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
        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            if (BuildCenterSquare)
            {
                var rec = blkref.GeometricExtents.ToRectangle();
                rec.TransformBy(matrix);
                var center = rec.GetPoint3dAt(0).GetMidPt(rec.GetPoint3dAt(2));
                ThDrawTool.CreateSquare(center, 0.5);
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = blkref.GetEffectiveName(),
                    Geometry = rec
                });
            }
            else
            {
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = blkref.GetEffectiveName(),
                    Geometry = blkref.GetTransformedCopy(matrix),
                });
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            if (entity is BlockReference br && br.Visible)
            {
                var blkName = br.GetEffectiveName();
                if(CheckBlkNameIsExisted(blkName))
                {
                    return true;
                }
                else if(BlkNames.Contains("手提式灭火器"))
                {
                    //当提取手提式灭火器时，对于消火栓块，且可见性中包括“组合柜”,也当做手提式灭火器
                    return CheckBlockReferenceVisibility(br);
                }
                else
                {
                    return false;
                }
            }
            return false;
        }


        private bool CheckBlkNameIsExisted(string blkName)
        {
            return BlkNames.Where(o => blkName.ToUpper().Contains(o.ToUpper())).Any(); 
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else if (ent is Polyline polyline)
            {
                return xclip.Contains(polyline);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public bool CheckBlockReferenceVisibility(BlockReference br)
        {
            if (br.GetEffectiveName().Contains("消火栓"))
            {
                var blockReferenceData = new ThBlockReferenceData(br.ObjectId);
                var blockVisiblityDic = blockReferenceData.DynablockVisibilityStates();
                foreach (var item in blockVisiblityDic)
                {
                    if (item.Key.Contains("组合柜"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 不支持外部参照、附着
            if (blockTableRecord.IsFromExternalReference ||
                blockTableRecord.IsFromOverlayReference)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }
    }
}
