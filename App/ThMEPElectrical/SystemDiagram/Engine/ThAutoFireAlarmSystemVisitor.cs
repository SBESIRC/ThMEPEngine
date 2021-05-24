using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public  class ThAutoFireAlarmSystemVisitor : ThDistributionElementExtractionVisitor
    {
        public Dictionary<string, string> BlockNameDic { get; set; }
        public ThAutoFireAlarmSystemVisitor()
        {
            BlockNameDic = new Dictionary<string, string>();
            Build();
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
            //ToDO
            if (entity is BlockReference br)
            {
                var blockName = br.Name;
                if (BlockNameDic.Keys.Contains(blockName))
                {
                    return true;
                }
            }
            return false;
        }

        private List<BlockReference> ExplodeBlockReference(BlockReference br)
        {
            List<BlockReference> rValue = new List<BlockReference>();
            var blockName = br.GetEffectiveName();
            if (BlockNameDic.Keys.Contains(blockName))
            {
                rValue.Add(br);
            }
            else
            {
                var objs = new DBObjectCollection();
                br.Explode(objs);//炸开一层
                foreach (Entity ent in objs)
                {
                    if  (IsDistributionElement(ent))
                    {
                        rValue.Add(ent as BlockReference);
                    }
                }
            }
            return rValue;
        }

        private void HandleBlockReference(List<ThRawIfcDistributionElementData> elements, BlockReference blkref, Matrix3d matrix)
        {
            ExplodeBlockReference(blkref).ForEach(br =>
            {
                var info = new ElementInfo()
                {
                    Layer = br.Layer,
                    Name = br.Name
                };
                elements.Add(new ThRawIfcDistributionElementData()
                {
                    Data = info,
                    Geometry = br.GetTransformedCopy(matrix),
                });
            });
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
        private void Build()
        {
            foreach (var blockInfo in ThBlockConfigModel.BlockConfig)
            {
                BlockNameDic.Add(blockInfo.BlockName, blockInfo.BlockNameRemark);
            }
        }
    }
    public class ElementInfo
    {
        public string Layer { get; set; }
        public string Name { get; set; }
    }
}
