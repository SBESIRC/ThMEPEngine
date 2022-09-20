using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADExtension;
using ThCADCore.NTS;

using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    internal class ThObstacleLineVisitor : ThDistributionElementExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (CheckLayerValid(dbObj) && IsDistributionElement(dbObj))
            {
                elements.AddRange(Handle(dbObj, matrix));
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(Entity entity, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            var clone = entity.Clone() as Entity;
            clone.TransformBy(matrix);
           
            results.Add(new ThRawIfcDistributionElementData()
            {
                Geometry = clone,
            });


            return results;
        }

        public override bool IsDistributionElement(Entity e)
        {
            var bReturn = false;
            if (e is Polyline || e is Line)
            {
                bReturn = true;
            }
            return bReturn;
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
        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            if (blockTableRecord.IsFromExternalReference )
            {

            }
            // 忽略动态块
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
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
        
        

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

    }
    internal class ThObstacleLineRecognitionEngine
    {
    }
}
