using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;

using ThMEPEngineCore.Algorithm;
using ThMEPHVAC.Service;
using ThMEPHVAC.FloorHeatingCoil.Service;

namespace ThMEPHVAC.FloorHeatingCoil.Model
{
    public class ThFloorHeatingWaterSeparator
    {
        public BlockReference Blk { get; private set; }
        public Polyline OBB { get; private set; }
        public List<Point3d> StartPts { get; private set; }
        public Vector3d DirLine { get; private set; }
        public Vector3d DirStartPt { get; private set; }

        private double StartPtDist = 50;

        public ThFloorHeatingWaterSeparator(BlockReference blk)
        {
            Blk = blk;
            OBB = GetOBB(blk);
            SetDir();
            SetStartPts();

        }

        private void SetDir()
        {
            var dir = new Vector3d(0, -1, 0);
            DirLine = dir.TransformBy(Blk.BlockTransform).GetNormal();
        }
        private void SetStartPts()
        {
            StartPts = new List<Point3d>();

            var i = GetStartPtCount();
            var dir = new Vector3d(1, 0, 0);
            DirStartPt = dir.TransformBy(Blk.BlockTransform).GetNormal();

            for (int j = 0; j < i; j++)
            {
                var pt = Blk.Position + j * StartPtDist * DirStartPt;
                StartPts.Add(pt);
            }
        }

        private int GetStartPtCount()
        {
            //var value = Blk.ObjectId.GetDynBlockValue(ThFloorHeatingCommon.BlkSettingAttrName_WaterSeparator);
            //var length = Convert.ToInt32(value);
            //var count = length / 50;
            //count = count - 1;

            var value = Blk.ObjectId.GetDynBlockValue(ThFloorHeatingCommon.BlkSettingAttrName_WaterSeparator);
            var i = ThFloorHeatingCoilUtilServices.GetNumberFromString(value);
            var count = Convert.ToInt32(i) * 2;
            return count;
        }

        public void Transform(ThMEPOriginTransformer transformer)
        {
            transformer.Transform(Blk);
            transformer.Transform(OBB);
            StartPts = StartPts.Select(x => transformer.Transform(x)).ToList();
        }

        private static Polyline GetOBB(BlockReference blk)
        {
            var objs = new DBObjectCollection();
            blk.ExplodeWithVisible(objs);
            var obb = objs.OfType<Polyline>().First();
            return obb;
        }

        //private void SetStartPts2()
        //{
        //    StartPts = new List<Point3d>();

        //    var i = GetStartPtCount2();
        //    var dir = new Vector3d(1, 0, 0);
        //    DirStartPt = dir.TransformBy(Blk.BlockTransform).GetNormal();

        //    for (int j = 0; j < i; j++)
        //    {
        //        var pt = Blk.Position + j * StartPtDist * DirStartPt;
        //        StartPts.Add(pt);
        //    }
        //}

        //private int GetStartPtCount2()
        //{
        //    //之后天正改成读附加信息
        //    var i = 0;
        //    var objs = new DBObjectCollection();
        //    Blk.ExplodeWithVisible(objs);
        //    var text = objs.OfType<DBText>().FirstOrDefault();
        //    if (text != null)
        //    {
        //        i = ThGeomUtil.GetNumberInText(text);
        //    }
        //    return i * 2;
        //}
    }

    public class ThFloorHeatingBathRadiator
    {
        public BlockReference Blk { get; private set; }
        public Polyline OBB { get; private set; }
        public List<Point3d> StartPts { get; private set; }

        public ThFloorHeatingBathRadiator(BlockReference blk)
        {
            Blk = blk;
            OBB = ThGeomUtil.GetVisibleOBB(blk);
            //SetDir();
            SetStartPts();

        }

        private void SetStartPts()
        {
            StartPts = new List<Point3d>();

            var dir = new Vector3d(1, 0, 0);
            var ptDirX = dir.TransformBy(Blk.BlockTransform).GetNormal();
            var diry = new Vector3d(0, 1, 0);
            var ptDirY = diry.TransformBy(Blk.BlockTransform).GetNormal();

            var basePt = Blk.Position;

            var x1s = Blk.ObjectId.GetDynBlockValue(ThFloorHeatingCommon.BlkSettingAttrName_Radiator_x1);
            var x1 = Convert.ToInt32(x1s);
            var y1s = Blk.ObjectId.GetDynBlockValue(ThFloorHeatingCommon.BlkSettingAttrName_Radiator_y1);
            var y1 = Convert.ToInt32(y1s);

            var x2s = Blk.ObjectId.GetDynBlockValue(ThFloorHeatingCommon.BlkSettingAttrName_Radiator_x2);
            var x2 = Convert.ToInt32(x2s);
            var y2s = Blk.ObjectId.GetDynBlockValue(ThFloorHeatingCommon.BlkSettingAttrName_Radiator_y2);
            var y2 = Convert.ToInt32(y2s);

            var p1 = basePt + x1 * ptDirX + y1 * ptDirY;
            var p2 = basePt + x2 * ptDirX + y2 * ptDirY;

            StartPts.Add(p1);
            StartPts.Add(p2);
        }

        public void Transform(ThMEPOriginTransformer transformer)
        {
            transformer.Transform(Blk);
            transformer.Transform(OBB);
            StartPts = StartPts.Select(x => transformer.Transform(x)).ToList();
        }


    }
}
