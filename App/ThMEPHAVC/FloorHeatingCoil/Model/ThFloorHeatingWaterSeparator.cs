using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;

using ThMEPHVAC.FloorHeatingCoil.Service;

namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThFloorHeatingWaterSeparator
    {
        public BlockReference Blk { get; private set; }
        public Polyline OBB { get; private set; }
        public List<Point3d> StartPts { get; private set; }

        private double StartPtDist = 50;

        public ThFloorHeatingWaterSeparator(BlockReference blk)
        {
            Blk = blk;
            OBB = ThGeomUtil.GetVisibleOBB(blk);
            SetStartPts();

        }
        private void SetStartPts()
        {
            StartPts = new List<Point3d>();

            var i = GetStartPtCount();
            var dir = new Vector3d(1, 0, 0);
            dir = dir.TransformBy(Blk.BlockTransform).GetNormal();

            for (int j = 0; j < i; j++)
            {
                var pt = Blk.Position + j * StartPtDist * dir;
                StartPts.Add(pt);
            }
        }

        private int GetStartPtCount()
        {
            //之后天正改成读附加信息
            var i = 0;
            var objs = new DBObjectCollection();
            Blk.ExplodeWithVisible(objs);
            var text = objs.OfType<DBText>().FirstOrDefault();
            if (text != null)
            {
                i = ThGeomUtil.GetNumberInText(text);
            }
            return i*2;
        }
    }
}
