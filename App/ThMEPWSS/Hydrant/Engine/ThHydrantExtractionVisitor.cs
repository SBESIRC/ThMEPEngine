using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Engine
{
    public class ThHydrantExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public List<string> BlkNames { get; set; }
        public Func<string, List<string>, bool> JudgeBlkNameExisted {get;set;}
        /// <summary>
        /// 获取块中心的小方块
        /// </summary>
        public bool BuildCenterSquare { get; set; }
        public ThHydrantExtractionVisitor()
        {
            BuildCenterSquare = true;
            JudgeBlkNameExisted = IsExisted;
            BlkNames = new List<string>() { "室内消火栓平面" };            
        }
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if(dbObj is BlockReference br)
            {
                HandleBlockReference(elements,br, matrix);
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
            //ToDo
            if (entity is BlockReference br)
            {
                var blkName = br.GetEffectiveName();
                return JudgeBlkNameExisted(blkName, BlkNames);
            }
            return false;
        }


        private bool IsExisted(string blkName,List<string> blkNames)
        {
            return blkNames.Where(o => blkName.ToUpper().Contains(o.ToUpper())).Any();
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else if(ent is Polyline polyline)
            {
                return xclip.Contains(polyline);
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
