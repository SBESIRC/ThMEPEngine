using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPEngineCore;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;

namespace ThMEPArchitecture.PartitionLayout
{
    public class LayoutOutput
    {
        public LayoutOutput(List<InfoCar> cars, List<Polyline> columns, List<Line> lanes)
        {
            Cars = cars;
            Columns = columns;
            Lanes = lanes;
        }
        public static string CarLayerName;
        public static string ColumnLayerName;
        public static string LaneLayerName;
        public static string PCarLayerName = "C-平行式";
        public static string BACKVCarLayerName = "C-标准车位-背靠背";
        public static string VCarLayerName = "C-标准车位";
        public static string PCARBLKNAME = "AI-平行式2460";
        public static string VCARBLKNAME = "AI-垂直式车位5324";
        public static string VCARBLKNAMEDOUBLEBACK = "AI-背靠背垂直式车位5124";
        public static bool ExistedDashedLineType=false;
        public List<InfoCar> Cars;
        public List<Polyline> Columns;
        public List<Line> Lanes;
        public int ColumnDisplayColorIndex = -1;
        public static int LaneDisplayColorIndex = -1;
        public static void InitializeLayer()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                if (!adb.Layers.Contains(CarLayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, CarLayerName, 0);
                if (!adb.Layers.Contains(ColumnLayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, ColumnLayerName, 0);
                if(adb.Linetypes.Contains("DASHED"))ExistedDashedLineType= true;
            }
        }
        private static List<Entity> DrawParallelCar()
        {
            int color_dark_yellow = 47;
            int colorgray = 8;
            double CT = 1400;
            var width = Math.Min(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth); 
            var widthDa = 700;
            var widthDb = 500;
            var widthDc = 750;
            var widthDd = 600;
            var thickness = 100;
            var length = Math.Max(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth); ;
            var doorlength = 1200;
            List<Entity> ents = new List<Entity>();
            var ori = Point3d.Origin;

            //
            var x = Vector3d.XAxis;
            var y = Vector3d.YAxis;
            var pt = ori;
            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(x * length / 2));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(y * width));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-x * length));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-y * width));
            pts.Add(pt);
            var pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.ColorIndex = color_dark_yellow;
            pl.Layer = PCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(x * thickness));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(y * (width - widthDd)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-x * thickness));
            pts.Add(pt);
            pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.TransformBy(Matrix3d.Displacement(x * (length / 2 - widthDb - widthDc - thickness)));
            pl.ColorIndex = color_dark_yellow;
            pl.Layer = PCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(x * (length / 2 - widthDb)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(y * (width - widthDd)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-x * (length - widthDb - widthDa)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-y * (width - widthDd)));
            pts.Add(pt);
            pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.ColorIndex = colorgray;
            pl.Layer = PCarLayerName;
            if (ExistedDashedLineType)
                pl.Linetype = "DASHED";
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pt = pt.TransformBy(Matrix3d.Displacement(-x * (length / 2 - widthDa - CT - 100)));
            var vec = Vector3d.XAxis;
            vec = vec.RotateBy(Math.PI / 6, Vector3d.ZAxis);
            var door = GeoUtilities.CreateLineFromStartPtAndVector(pt, vec, doorlength);
            door.ColorIndex = colorgray;
            if (ExistedDashedLineType)
                door.Linetype = "DASHED";
            var dr = door.Clone() as Line;
            door.TransformBy(Matrix3d.Displacement(y * (width - widthDd)));
            door.Layer = PCarLayerName;
            ents.Add(door);
            dr.TransformBy(Matrix3d.Mirroring(new Line3d(ori, new Point3d(1, 0, 0))));
            dr.Layer = PCarLayerName;
            if (ExistedDashedLineType)
                dr.Linetype = "DASHED";
            ents.Add(dr);

            return ents;
        }
        private static List<Entity> DrawVertBackBackCar()
        {
            int color_cyan = 4;
            int colorgray = 8;
            double CT = 1400;
            var width = Math.Min(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
            var widthD = 300;
            var thickness = 100;
            var length = Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth)- MParkingPartitionPro.DifferenceFromBackBcek;
            var doorlength = 1200;
            List<Entity> ents = new List<Entity>();
            var ori = Point3d.Origin;

            //
            var pt = ori;
            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * width / 2));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * length));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(1, 0, 0) * width));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(0, 1, 0) * length));
            pts.Add(pt);
            var pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.ColorIndex = color_cyan;
            pl.Layer = BACKVCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * (width / 2 - widthD)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * (length - widthD)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(1, 0, 0) * (width - widthD * 2)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(0, 1, 0) * (length - widthD)));
            pts.Add(pt);
            pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.ColorIndex = colorgray;
            if (ExistedDashedLineType)
                pl.Linetype = "DASHED";
            pl.Layer = BACKVCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * (width / 2 - widthD)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * thickness));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(1, 0, 0) * (width - widthD * 2)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(0, 1, 0) * thickness));
            pts.Add(pt);
            pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * (length - 1150)));
            pl.ColorIndex = color_cyan;
            pl.Layer = BACKVCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * (width / 2 - widthD)));
            pt = pt.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * (CT + 100)));
            pts.Add(pt);
            var vec = Vector3d.XAxis;
            vec = vec.RotateBy(Math.PI / 3, Vector3d.ZAxis);
            var door = GeoUtilities.CreateLineFromStartPtAndVector(pt, vec, doorlength);
            door.ColorIndex = colorgray;
            if (ExistedDashedLineType)
                door.Linetype = "DASHED";
            door.Layer = BACKVCarLayerName;
            ents.Add(door);
            var dr = door.Clone() as Line;
            dr.TransformBy(Matrix3d.Mirroring(new Line3d(ori, new Point3d(0, 1, 0))));
            dr.Layer = BACKVCarLayerName;
            if (ExistedDashedLineType)
                dr.Linetype = "DASHED";
            ents.Add(dr);
            return ents;
        }
        private static List<Entity> DrawVertCar()
        {
            int color_darkgreen = 74;
            int colorgray = 8;
            double CT = 1400;
            var width = Math.Min(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
            var widthD = 300;
            var thickness = 100;
            var length = Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth);
            var doorlength = 1200;
            List<Entity> ents = new List<Entity>();
            var ori = Point3d.Origin;

            //
            var pt = ori;
            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * width / 2));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * length));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(1, 0, 0) * width));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(0, 1, 0) * length));
            pts.Add(pt);
            var pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.ColorIndex = color_darkgreen;
            pl.Layer = VCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * (width / 2 - widthD)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * (length - widthD)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(1, 0, 0) * (width - widthD * 2)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(0, 1, 0) * (length - widthD)));
            pts.Add(pt);
            pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.ColorIndex = colorgray;
            if (ExistedDashedLineType)
                pl.Linetype = "DASHED";
            pl.Layer = VCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * (width / 2 - widthD)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * thickness));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(1, 0, 0) * (width - widthD * 2)));
            pts.Add(pt);
            pt = pt.TransformBy(Matrix3d.Displacement(-new Vector3d(0, 1, 0) * thickness));
            pts.Add(pt);
            pl = GeoUtilities.CreatePolyFromPoints(pts.ToArray());
            pl.TransformBy(Matrix3d.Displacement(new Vector3d(0, 1, 0) * (length - 1150)));
            pl.ColorIndex = color_darkgreen;
            pl.Layer = VCarLayerName;
            ents.Add(pl);
            //
            pt = ori;
            pts.Clear();
            pt = pt.TransformBy(Matrix3d.Displacement(new Vector3d(1, 0, 0) * (width / 2 - widthD)));
            pt = pt.TransformBy(Matrix3d.Displacement(Vector3d.YAxis * (CT + 100)));
            pts.Add(pt);
            var vec = Vector3d.XAxis;
            vec = vec.RotateBy(Math.PI / 3, Vector3d.ZAxis);
            var door = GeoUtilities.CreateLineFromStartPtAndVector(pt, vec, doorlength);
            door.ColorIndex = colorgray;
            if (ExistedDashedLineType)
                door.Linetype = "DASHED";
            door.Layer = VCarLayerName;
            ents.Add(door);
            var dr = door.Clone() as Line;
            dr.Layer = VCarLayerName;
            dr.TransformBy(Matrix3d.Mirroring(new Line3d(ori, new Point3d(0, 1, 0))));
            if (ExistedDashedLineType)
                dr.Linetype = "DASHED";
            ents.Add(dr);
            return ents;
        }
        public static BlockReference _VCar = null;
        public static BlockReference VCar
        {
            get
            {
                if (true)
                {
                    var blkname = VCARBLKNAME;
                    using (AcadDatabase adb = AcadDatabase.Active())
                    {
                        if (!adb.Layers.Contains(VCarLayerName))
                            ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, VCarLayerName, 0);
                        BlockTable bt = (BlockTable)adb.Database.BlockTableId.GetObject(OpenMode.ForRead);
                        try
                        {
                            BlockTableRecord record = new BlockTableRecord();
                            record.Name = blkname;
                            var ents = DrawVertCar();
                            ents.ForEach(e => record.AppendEntity(e));
                            bt.UpgradeOpen();
                            bt.Add(record);
                            adb.Database.TransactionManager.AddNewlyCreatedDBObject(record, true);
                            bt.DowngradeOpen();
                        }
                        catch { }
                        BlockTableRecord space = (BlockTableRecord)adb.Database.CurrentSpaceId.GetObject(OpenMode.ForWrite);
                        BlockReference br = new BlockReference(Point3d.Origin, bt[blkname]);
                        br.ScaleFactors = new Scale3d(1);
                        br.Rotation = 0;
                        br.Layer = CarLayerName;
                        //space.AppendEntity(br);
                        //adb.Database.TransactionManager.AddNewlyCreatedDBObject(br, true);
                        space.DowngradeOpen();
                        _VCar = br;
                    }
                }
                return _VCar;
            }
        }
        public static BlockReference _VBackCar = null;
        public static BlockReference VBackCar
        {
            get
            {
                if (true)
                {
                    var blkname = VCARBLKNAMEDOUBLEBACK;
                    using (AcadDatabase adb = AcadDatabase.Active())
                    {
                        if (!adb.Layers.Contains(BACKVCarLayerName))
                            ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, BACKVCarLayerName, 0);
                        BlockTable bt = (BlockTable)adb.Database.BlockTableId.GetObject(OpenMode.ForRead);
                        try
                        {
                            BlockTableRecord record = new BlockTableRecord();
                            record.Name = blkname;
                            var ents = DrawVertBackBackCar();
                            ents.ForEach(e => record.AppendEntity(e));
                            bt.UpgradeOpen();
                            bt.Add(record);
                            adb.Database.TransactionManager.AddNewlyCreatedDBObject(record, true);
                            bt.DowngradeOpen();
                        }
                        catch { }
                        BlockTableRecord space = (BlockTableRecord)adb.Database.CurrentSpaceId.GetObject(OpenMode.ForWrite);
                        BlockReference br = new BlockReference(Point3d.Origin, bt[blkname]);
                        br.ScaleFactors = new Scale3d(1);
                        br.Rotation = 0;
                        br.Layer = CarLayerName;
                        //space.AppendEntity(br);
                        //adb.Database.TransactionManager.AddNewlyCreatedDBObject(br, true);
                        space.DowngradeOpen();
                        _VBackCar = br;
                    }
                }
                return _VBackCar;
            }
        }
        public static BlockReference _PCar = null;
        public static BlockReference PCar
        {
            get
            {
                if (true)
                {
                    var blkname = PCARBLKNAME;
                    using (AcadDatabase adb = AcadDatabase.Active())
                    {
                        if (!adb.Layers.Contains(PCarLayerName))
                            ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, PCarLayerName, 0);
                        BlockTable bt = (BlockTable)adb.Database.BlockTableId.GetObject(OpenMode.ForRead);
                        try
                        {
                            BlockTableRecord record = new BlockTableRecord();
                            record.Name = blkname;
                            var ents = DrawParallelCar();
                            ents.ForEach(e => record.AppendEntity(e));
                            bt.UpgradeOpen();
                            bt.Add(record);
                            adb.Database.TransactionManager.AddNewlyCreatedDBObject(record, true);
                            bt.DowngradeOpen();
                        }
                        catch { }
                        BlockTableRecord space = (BlockTableRecord)adb.Database.CurrentSpaceId.GetObject(OpenMode.ForWrite);
                        BlockReference br = new BlockReference(Point3d.Origin, bt[blkname]);
                        br.ScaleFactors = new Scale3d(1);
                        br.Rotation = 0;
                        br.Layer = CarLayerName;
                        //space.AppendEntity(br);
                        //adb.Database.TransactionManager.AddNewlyCreatedDBObject(br, true);
                        space.DowngradeOpen();
                        _VCar = br;
                    }
                }
                return _VCar;
            }
        }
        public void DisplayColumns()
        {
            Columns.Select(e =>
            {
                e.Layer = ColumnLayerName;
                if (ColumnDisplayColorIndex < 0)
                    e.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(15, 240, 206);
                else e.ColorIndex = ColumnDisplayColorIndex;
                DisplayParkingStall.Add(e);
                return e;
            }).AddToCurrentSpace();
        }
        public void DisplayCars()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                foreach (var car in Cars.Where(e => e.CarLayoutMode == 0))
                {
                    var angle = 0.0;
                    var vec = car.Vector;
                    if (Math.Abs(vec.X) < 0.0001) vec = new Vector3d(0, vec.Y, 0);
                    if (Math.Abs(vec.Y) < 0.0001) vec = new Vector3d(vec.X, 0, 0);
                    vec = vec.GetNormal();
                    if (vec.Equals(Vector3d.YAxis)) angle = 0;
                    else if (vec.Equals(-Vector3d.YAxis)) angle = Math.PI;
                    else if (vec.Equals(Vector3d.XAxis)) angle = -Math.PI / 2;
                    else if (vec.Equals(-Vector3d.XAxis)) angle = Math.PI / 2;
                    else
                    {
                        angle = Vector3d.YAxis.GetAngleTo(vec);
                        if (vec.X > 0) 
                            angle = - angle;
                    }
                    var brId = adb.CurrentSpace.ObjectId.InsertBlockReference(CarLayerName, VCARBLKNAME, car.Point, new Scale3d(1), angle);
                    var br = adb.Element<BlockReference>(brId);
                    DisplayParkingStall.Add(br);
                }
                foreach (var car in Cars.Where(e => e.CarLayoutMode == 1))
                {
                    var angle = 0.0;
                    var vec = car.Vector;
                    if (Math.Abs(vec.X) < 0.0001) vec = new Vector3d(0, vec.Y, 0);
                    if (Math.Abs(vec.Y) < 0.0001) vec = new Vector3d(vec.X, 0, 0);
                    vec = vec.GetNormal();
                    if (vec.Equals(Vector3d.YAxis)) angle = 0;
                    else if (vec.Equals(-Vector3d.YAxis)) angle = Math.PI;
                    else if (vec.Equals(Vector3d.XAxis)) angle = -Math.PI / 2;
                    else if (vec.Equals(-Vector3d.XAxis)) angle = Math.PI / 2;
                    else
                    {
                        angle = Vector3d.YAxis.GetAngleTo(vec);
                        if (vec.X > 0) 
                            angle = - angle;
                    }
                    var brId = adb.CurrentSpace.ObjectId.InsertBlockReference(CarLayerName, PCARBLKNAME, car.Point, new Scale3d(1), angle);
                    var br = adb.Element<BlockReference>(brId);
                    DisplayParkingStall.Add(br);
                }
                foreach (var car in Cars.Where(e => e.CarLayoutMode == 2))
                {
                    var angle = 0.0;
                    var vec = car.Vector;
                    if (Math.Abs(vec.X) < 0.0001) vec = new Vector3d(0, vec.Y, 0);
                    if (Math.Abs(vec.Y) < 0.0001) vec = new Vector3d(vec.X, 0, 0);
                    vec = vec.GetNormal();
                    if (vec.Equals(Vector3d.YAxis)) angle = 0;
                    else if (vec.Equals(-Vector3d.YAxis)) angle = Math.PI;
                    else if (vec.Equals(Vector3d.XAxis)) angle = -Math.PI / 2;
                    else if (vec.Equals(-Vector3d.XAxis)) angle = Math.PI / 2;
                    else
                    {
                        angle = Vector3d.YAxis.GetAngleTo(vec);
                        if (vec.X > 0) 
                            angle = - angle;
                    }
                    var brId = adb.CurrentSpace.ObjectId.InsertBlockReference(CarLayerName, VCARBLKNAMEDOUBLEBACK, car.Point, new Scale3d(1), angle);
                    var br = adb.Element<BlockReference>(brId);
                    DisplayParkingStall.Add(br);
                }
            }
        }
        public void DisplayLanes()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                if (!adb.Layers.Contains(LaneLayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, LaneLayerName, (short)LaneDisplayColorIndex);
                Lanes.Select(e =>
                {
                    e.Layer = LaneLayerName;
                    DisplayParkingStall.Add(e);
                    return e;
                }).AddToCurrentSpace();
            }
        }
        public static void RefreshBlocks()
        {
            var parallelwidth = Math.Min(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth).ToString().Substring(0, 2);
            var parallellength = Math.Max(VMStock.ParallelSpotLength, VMStock.ParallelSpotWidth).ToString().Substring(0, 2);
            var vertlength = Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth).ToString().Substring(0, 2);
            var vertwidth = Math.Min(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth).ToString().Substring(0, 2);
            var vertlength_backback = (Math.Max(VMStock.VerticalSpotLength, VMStock.VerticalSpotWidth) - 200).ToString().Substring(0, 2);
            PCARBLKNAME = "AI-平行式" + parallelwidth + parallellength;
            VCARBLKNAME = "AI-垂直式车位" + vertlength + vertwidth;
            VCARBLKNAMEDOUBLEBACK = "AI-背靠背垂直式车位" + vertlength_backback + vertwidth;
        }
    }
}