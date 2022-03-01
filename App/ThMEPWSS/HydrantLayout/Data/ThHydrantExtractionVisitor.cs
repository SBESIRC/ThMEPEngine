using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPWSS.HydrantLayout.Data
{
    public class ThHydrantExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public List<string> BlkNames { get; set; }
        public ThHydrantExtractionVisitor()
        {
            BlkNames = new List<string>() { "" };
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (CheckLayerValid(dbObj) && IsBlk(dbObj))
            {
                var geom = new DBPoint((dbObj as BlockReference).Position);
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = dbObj as BlockReference,
                    Geometry = geom
                });
            }
        }

        public override bool CheckLayerValid(Entity e)
        {
            var bReturn = false;
            if (LayerFilter.Count > 0)
            {
                bReturn = LayerFilter.Contains(e.Layer);
            }
            else
            {
                bReturn = true;
            }
            return bReturn;
        }

        public override bool IsDistributionElement(Entity e)
        {
            return true;
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }

        //public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        //{
        //    var xclip = blockReference.XClipInfo();
        //    if (xclip.IsValid)
        //    {
        //        xclip.TransformBy(matrix);
        //        elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
        //    }
        //}
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
        private bool IsBlk(Entity e)
        {
            var bReturn = false;
            if (e is BlockReference blk)
            {
                if (BlkNames.Contains(blk.GetEffectiveName()))
                {
                    bReturn = true;
                }
            }
            return bReturn;
        }
    }
}
