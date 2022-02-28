using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Engine
{
    public class ThCircleVPipeExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public List<double> Radius { get; set; } = new List<double>();

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            if (CheckLayerValid(dbObj) && IsCircleVPipe(dbObj))
            {
                var geom = new DBPoint((dbObj as Circle).Center);

                elements.Add(new ThRawIfcFlowSegmentData()
                {
                    Data = dbObj,
                    Geometry = geom
                });
            }
        }

        public override bool IsFlowSegment(Entity e)
        {
            return IsCircleVPipe(e);
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

        private bool IsCircleVPipe(Entity e)
        {
            var bReturn = false;
            if (e is Circle c)
            {
                if (Radius.Where(tol => Math.Abs(tol - c.Radius) <= 1).Count() > 0)
                {
                    bReturn = true;
                }
            }
            return bReturn;
        }

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {

        }
    }
}

