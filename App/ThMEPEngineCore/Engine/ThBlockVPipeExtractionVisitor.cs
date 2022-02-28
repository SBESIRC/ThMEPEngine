using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Engine
{
    public class ThBlockVPipeExtractionVisitor : ThFlowSegmentExtractionVisitor
    {

        private List<string> BlkNameList { get; set; } = new List<string> { "带定位立管", "带定位立管150" };
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {

            if (CheckLayerValid(dbObj) && IsVirticalPipeBlk(dbObj))
            {
                var geom = HandleBlkPipe(dbObj as BlockReference);
                geom.TransformBy(matrix);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }

        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            if (CheckLayerValid(dbObj) && IsVirticalPipeBlk(dbObj))
            {
                var geom = HandleBlkPipe(dbObj as BlockReference);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }
        }

        public override bool IsFlowSegment(Entity e)
        {
            return IsVirticalPipeBlk(e);
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

        private bool IsVirticalPipeBlk(Entity e)
        {
            var bReturn = false;
            if (e is BlockReference blk)
            {
                if (BlkNameList.Contains(blk.GetEffectiveName()))
                {
                    bReturn = true;
                }
            }
            return bReturn;
        }

        private DBPoint HandleBlkPipe(BlockReference blk)
        {
            var ptObj = new DBPoint(blk.Position);
            return ptObj;
        }


        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }
    }
}
