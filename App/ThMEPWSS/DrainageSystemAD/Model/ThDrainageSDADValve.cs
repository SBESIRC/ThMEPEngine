using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDADValve
    {
        public BlockReference blk { get; private set; }
        public Polyline boundary { get; private set; }

        public Vector3d dir { get; private set; }

        public string type { get; private set; }

        public string visibility { get; private set; }

        public Line centerLine { get; private set; }

        public double scale { get; private set; }

        public ThDrainageSDADValve(Entity outline, string blkName)
        {
            var blkOri = outline as BlockReference;
            blk = blkOri.Clone() as BlockReference;
            type = blkName;
            setInfo(blkOri);
        }

        private void setInfo(BlockReference blkOri)
        {
            var objId = blkOri.ObjectId;
            var thBlk = new ThBlockReferenceData(objId);
            visibility = thBlk.CurrentVisibilityStateValue();
            boundary = blkOri.ToOBB(blkOri.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
            dir = (boundary.GetPoint3dAt(2) - boundary.GetPoint3dAt(1)).GetNormal();
            centerLine = getCenterLine();
            scale = Math.Abs(blkOri.ScaleFactors.X);
        }

        /// <summary>
        /// not support for angle defined in visivility of the blk
        /// 不支持动态块属性里的旋转
        /// </summary>
        /// <returns></returns>
        private Line getCenterLine()
        {
            Line centerLine = new Line();
            switch (type)
            {
                case "$VALVE$00000333":
                case "截止阀":
                case "水表1":
                case "给水角阀平面":
                case "给水管径50":
                    centerLine = getCenterLineFromBoundary();
                    break;

                case "进户水表":
                    centerLine = getCenterLineFromBoundary(type);
                    break;

                case "室内水表详图":
                    centerLine = getCenterLineFromBoundary(visibility);
                    break;

                default:

                    break;

            }

            return centerLine;
        }

        private Line getCenterLineFromBoundary()
        {
            var pt0 = boundary.GetPoint3dAt(0);
            var pt1 = boundary.GetPoint3dAt(1);
            var pt2 = boundary.GetPoint3dAt(2);

            var startPt = new Point3d((pt0.X + pt1.X) / 2, (pt0.Y + pt1.Y) / 2, 0);
            var endPt = startPt + dir * ((pt2 - pt1).Length);

            var line = new Line(startPt, endPt);

            return line;
        }

        private Line getCenterLineFromBoundary(string type)
        {
            var scale = Math.Abs(blk.ScaleFactors.X);
            var startPt = blk.Position;
            var endPt = startPt + dir * (ThDrainageADCommon.blk_WM_SV[type]) * scale;

            var line = new Line(startPt, endPt);

            return line;
        }

        public void TransformBy(Matrix3d matrix)
        {
            blk.TransformBy(matrix);
            boundary.TransformBy(matrix);
            dir = (boundary.GetPoint3dAt(2) - boundary.GetPoint3dAt(1)).GetNormal();
            centerLine.TransformBy(matrix);
        }
    }
}
