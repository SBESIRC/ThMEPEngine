using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using AcHelper;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Diagnostics;

using ThMEPTCH.TCHDrawServices;
using ThMEPTCH.Model;
using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.ApplicationServices;
using Dreambuild.AutoCAD;
using DotNetARX;

namespace ThMEPWSS.SprinklerDim.Service
{
    public static class ThSprinklerDimInsertService
    {
        public static void ToCADDim(List<ThSprinklerDimension> dims, double scale)
        {
            var cadDim = DimModelToCADDim(dims, scale);
            InsertDim(cadDim);
        }

        private static List<RotatedDimension> DimModelToCADDim(List<ThSprinklerDimension> dims, double scale)
        {
            var cadDim = new List<RotatedDimension>();
            foreach (var dim in dims)
            {
                var dimBasePt = dim.DimPts.First() + dim.Dirrection * dim.Distance;
                double rotation = Vector3d.XAxis.GetAngleTo((dim.DimPts.Last() - dim.DimPts.First()).GetNormal(), Vector3d.ZAxis);

                for (int i = 0; i < dim.DimPts.Count - 1; i++)
                {
                    var p1 = dim.DimPts[i];
                    var p2 = dim.DimPts[i + 1];

                    var newDim = new RotatedDimension();
                    newDim.XLine1Point = p1;
                    newDim.XLine2Point = p2;
                    newDim.DimLinePoint = dimBasePt;
                    newDim.Rotation = rotation;
                    //newDim.Dimtxt = 350;
                    //if (scale==150)
                    //{
                    //    newDim.Dimtxt = 450;
                    //}
                    //newDim.Dimscale = 1;
                    newDim.Dimscale = scale;
                    cadDim.Add(newDim);
                }
            }

            return cadDim;
        }

        private static void InsertDim(List<RotatedDimension> dims)
        {
            if (dims == null || dims.Count() == 0)
            {
                return;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(ThSprinklerDimCommon.Style_DimCAD, acadDatabase.Database);

                for (int i = 0; i < dims.Count(); i++)
                {
                    var dim = dims[i];

                    dim.Layer = ThSprinklerDimCommon.Layer_Dim;
                    dim.Color = Color.FromColorIndex(ColorMethod.ByLayer, (short)ColorIndex.BYLAYER);
                    dim.DimensionStyle = id;
                    acadDatabase.ModelSpace.Add(dim);

                }
            }
        }


        public static void ToTCHDim(List<ThSprinklerDimension> dims, double scale, string tchDBPath)
        {
            var dbDimsModel = DimToDBDimModel(dims, scale);

            var tchDrawSprinklerDimService = new TCHDrawSprinklerDimService(tchDBPath);
            tchDrawSprinklerDimService.Init(dbDimsModel);
            tchDrawSprinklerDimService.DrawExecute(false, true);

        }

        private static List<ThTCHSprinklerDim> DimToDBDimModel(List<ThSprinklerDimension> dims, double scale)
        {
            var tchDimModeList = new List<ThTCHSprinklerDim>();

            foreach (var item in dims)
            {
                if (item.DimPts.Count() == 0)
                {
                    continue;
                }

                var lineDir = (item.DimPts.Last() - item.DimPts.First()).GetNormal();
                var rotation = Vector3d.XAxis.GetAngleTo(lineDir, Vector3d.ZAxis);
                var distDir = Math.Round(Math.Sin(lineDir.GetAngleTo(item.Dirrection, Vector3d.ZAxis))); //左 1 右-1
                double dist = distDir * item.Distance;

                var wct2ucs = Vector3d.XAxis.TransformBy(Active.Editor.CurrentUserCoordinateSystem).GetNormal();
                var anglew2c = Vector3d.XAxis.GetAngleTo(wct2ucs, Vector3d.ZAxis);
                var layoutRotation = anglew2c * 180 / Math.PI;
                var ptDist = new List<double>();
                for (int i = 0; i < item.DimPts.Count - 1; i++)
                {
                    ptDist.Add(item.DimPts[i].DistanceTo(item.DimPts[i + 1]));
                }


                var tchDim = new ThTCHSprinklerDim();
                tchDim.FirstPoint = item.DimPts.First();
                tchDim.Rotation = rotation;
                tchDim.Scale = scale;
                tchDim.Dist2DimLine = dist / scale;
                tchDim.LayoutRotation = layoutRotation;
                tchDim.SegmentValues.AddRange(ptDist);
                tchDim.System = "喷淋";

                tchDimModeList.Add(tchDim);
            }

            return tchDimModeList;
        }

        public static void InsertUnTagPt(List<Point3d> unTagPt, bool isX)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layer = ThSprinklerDimCommon.Layer_UnTagX;
                if (isX == false)
                {
                    layer = ThSprinklerDimCommon.Layer_UnTagY;
                }

                foreach (var pt in unTagPt)
                {
                    var printItem = new Circle(pt, new Vector3d(0, 0, 1), 500);

                    printItem.Layer = layer;
                    acadDatabase.ModelSpace.Add(printItem);
                }
            }
        }

        /// <summary>
        /// debug mode快速看结果
        /// </summary>
        /// <param name="dims"></param>
        /// <param name="printTag"></param>
        public static void ToDebugDim(List<ThSprinklerDimension> dims, string printTag)
        {
            foreach (var dim in dims)
            {
                var p1 = dim.DimPts.First();
                var p2 = dim.DimPts.Last();

                p1 = p1 + dim.Dirrection * dim.Distance;
                p2 = p2 + dim.Dirrection * dim.Distance;

                var line = new Line(p1, p2);
                DrawUtils.ShowGeometry(line, string.Format("l6finalDim{0}", printTag), 2, 30);

                foreach (var pt in dim.DimPts)
                {
                    var ptprint = pt + dim.Dirrection * dim.Distance;
                    DrawUtils.ShowGeometry(ptprint, string.Format("l6finalDim{0}", printTag), 2, r: 30);
                }
            }
        }

        public static void LoadBlockLayerToDocument(Database database, List<string> blockNames, List<string> layerNames, List<string> DimSytle)
        {
            //插入模版图块时调用了WblockCloneObjects方法。需要之后做QueueForGraphicsFlush更新transaction。并且最后commit此transaction
            //参考
            //https://adndevblog.typepad.com/autocad/2015/01/using-wblockcloneobjects-copied-modelspace-entities-disappear-in-the-current-drawing.html

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                LoadBlockLayerToDocumentWithoutTrans(database, blockNames, layerNames, DimSytle);
                transaction.TransactionManager.QueueForGraphicsFlush();
                transaction.Commit();
            }
        }

        private static void LoadBlockLayerToDocumentWithoutTrans(Database database, List<string> blockNames, List<string> layerNames, List<string> DimSytle)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                //解锁0图层，后面块有用0图层的
                DbHelper.EnsureLayerOn("0");
                DbHelper.EnsureLayerOn("DEFPOINTS");
            }
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, true);
                }

                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, true);

                    LayerTools.UnLockLayer(database, item);
                    LayerTools.UnFrozenLayer(database, item);
                    LayerTools.UnOffLayer(database, item);
                }

                foreach (var item in DimSytle)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;

                    var itemTemplate = blockDb.DimStyles.ElementOrDefault(item);
                    if (item != null)
                    {
                        currentDb.DimStyles.Import(itemTemplate, true);
                    }
                }
            }
        }

        public static void SetCurrentLayer(string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                LayerTools.SetCurrentLayer(acadDatabase.Database, layer);
                acadDatabase.Database.Cecolor = Color.FromColorIndex(ColorMethod.ByColor, (short)ColorIndex.BYLAYER);
            }

        }
    }
}
