using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPEngineCore.Engine
{
    public class ThDrainageVPipeExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        private List<string> BlkNameList { get; set; } = new List<string> { "污废水管+通气管" };
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {

            if (CheckLayerValid(dbObj) && IsVirticalPipeBlk(dbObj))
            {
                var geomList = HandleBlkPipe(dbObj as BlockReference);
                foreach (var geom in geomList)
                {
                    geom.TransformBy(matrix);

                    elements.Add(new ThRawIfcFlowSegmentData()
                    {
                        Data = dbObj,
                        Geometry = geom
                    });
                }
            }
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            if (CheckLayerValid(dbObj) && IsVirticalPipeBlk(dbObj))
            {
                var geomList = HandleBlkPipe(dbObj as BlockReference);
                foreach (var geom in geomList)
                {
                    elements.Add(new ThRawIfcFlowSegmentData()
                    {
                        Data = dbObj,
                        Geometry = geom
                    });
                }
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

        private List<DBPoint> HandleBlkPipe(BlockReference blk)
        {
            var ptList = new List<DBPoint>();
            var objs = new DBObjectCollection();
            blk.ExplodeWithVisible(objs);
            var circles = objs.OfType<Circle>().ToList();
            circles.ForEach(x => ptList.Add(new DBPoint(x.Center)));
            return ptList;
        }

        public override bool IsFlowSegmentBlock(BlockTableRecord blockTableRecord)
        {
            //忽略外参
            if (blockTableRecord.IsFromExternalReference)
            {
                return false;
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

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }
    }
}
