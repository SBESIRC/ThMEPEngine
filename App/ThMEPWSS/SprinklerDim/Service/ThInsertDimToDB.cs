using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using ThCADExtension;

using ThMEPWSS.SprinklerDim.Model;
using Autodesk.AutoCAD.Colors;

namespace ThMEPWSS.SprinklerDim.Service
{
    //internal class ThInsertDimToDBTchService
    //{
    //    private THMEPSQLiteServices sqlHelper;
    //    string curDbPath = "";
    //    public ThInsertDimToDBTchService()
    //    {
    //        curDbPath = Path.GetTempPath() + "TG20.db";
    //        sqlHelper = new THMEPSQLiteServices(curDbPath);

    //    }
    //    public void InsertDimToDB(List<ThSprinklerDimension> dims)
    //    {
    //        foreach (var dimPts in dims)
    //        {
    //            var segStartId = InsertDimPtsToDB(dimPts.DimPts);
    //            InsertDimStartPtToDB(dimPts.DimPts, segStartId, dimPts.Distance, 100);
    //        }
    //    }

    //    private int InsertDimPtsToDB(List<Point3d> dim)
    //    {
    //        var startId = GetMaxSegId("DimSegments") + 1;

    //        for (int i = 0; i < dim.Count - 1; i++)
    //        {
    //            var id = startId + i;
    //            var nextId = id + 1;
    //            var dist = dim[i + 1].DistanceTo(dim[i]);
    //            if (i == dim.Count - 2)
    //            {
    //                nextId = -1;
    //            }
    //            InsertDimSegmentSQL(id, nextId, dist);
    //        }
    //        return startId;
    //    }

    //    /// <summary>
    //    /// 找最大id
    //    /// </summary>
    //    /// <returns></returns>
    //    private int GetMaxSegId(string tableName)
    //    {
    //        int i = -1;
    //        var sql = string.Format(@"select IFNULL(max(id),-1) from {0}", tableName);

    //        var dt = sqlHelper.GetTable(sql);
    //        i = (int)dt.Rows[0][0];
    //        return i;
    //    }

    //    /// <summary>
    //    /// id:本点的id
    //    /// nextID:下一个点的id 如果是-1，则证明这个标注点是最后一个点
    //    /// segment:到下一个点的距离
    //    /// </summary>
    //    /// <param name="id"></param>
    //    /// <param name="nextid"></param>
    //    /// <param name="dist"></param>
    //    private void InsertDimSegmentSQL(int id, int nextid, double dist)
    //    {
    //        var sql = string.Format($"INSERT INTO {0} (id, NextSegmentID, Segment) VALUES (" +
    //                                                $"{1},{2},{3})", "DimSegments",
    //                                                id.ToString(), nextid.ToString(), dist.ToString());


    //        sqlHelper.ExecuteNonQuery(sql);

    //    }


    //    private void InsertDimStartPtToDB(List<Point3d> dim, int SegId, double dist2Line, int scale)
    //    {
    //        var startId = GetMaxSegId("DimPt2Pts") + 1;
    //        var rotationR = Vector3d.XAxis.GetAngleTo(dim.Last() - dim.First(), Vector3d.ZAxis);

    //        InsertDimPt2PtsSQL(dim[0], startId, SegId, rotationR, dist2Line, scale);
    //    }
    //    private void InsertDimPt2PtsSQL(Point3d startPt, int startId, int SegId, double rotation, double dist2Line, int scale)
    //    {
    //        var sql = string.Format(@"INSERT INTO {0} (id, SegmentID, DimStyle, Location, Rotation, Dist2DimLine, Pscale) VALUES " +
    //                                    @"({1},{2},'{3}','{""X"":{4},""Y"":{5},""Z"":0.0}',{6},{7},{8})",
    //                         "DimPt2Pts", startId.ToString(), SegId.ToString(), "_TCH_ARCH",
    //                         startPt.X.ToString(), startPt.Y.ToString(), rotation.ToString(), dist2Line.ToString(), scale.ToString());


    //        sqlHelper.ExecuteNonQuery(sql);


    //    }
    //}

    internal static class ThInsertDimToDBService
    {
        public static List<RotatedDimension> ToCADDim(List<ThSprinklerDimension> dims)
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
                    newDim.Dimtxt = 350;
                    cadDim.Add(newDim);
                }
            }

            return cadDim;
        }

        public static void InsertDim(List<RotatedDimension> dims)
        {
            if (dims == null || dims.Count() == 0)
            {
                return;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThSprinklerDimCommon.Layer_Dim);
                acadDatabase.Database.ImportDimtype(ThSprinklerDimCommon.Style_Dim);

                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(ThSprinklerDimCommon.Style_Dim, acadDatabase.Database);

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

        private static void ImportDimtype(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                var item = blockDb.DimStyles.ElementOrDefault(name);
                if (item != null)
                {
                    currentDb.DimStyles.Import(item, replaceIfDuplicate);
                }
            }
        }

        private static void ImportLayer(this Database database, string layerName, string TemplateName = "", bool replaceIfDuplicate = true)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {

                if (TemplateName != "")
                {
                    var tempLayer = blockDb.Layers.ElementOrDefault(TemplateName);
                    Color color = Color.FromColorIndex(ColorMethod.ByLayer, (short)3);
                    if (tempLayer != null)
                    {
                        color = tempLayer.Color;
                    }
                    var newLayer = CreateLayer(layerName, color, replaceIfDuplicate);
                }
                else
                {
                    currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layerName), replaceIfDuplicate);
                }

                LayerTableRecord layer = currentDb.Layers.Element(layerName, true);
                if (layer != null)
                {
                    layer.UpgradeOpen();
                    layer.IsOff = false;
                    layer.IsFrozen = false;
                    layer.IsLocked = false;
                    layer.DowngradeOpen();
                }
            }
        }
        private static LayerTableRecord CreateLayer(string aimLayer, Color color, bool replaceIfDuplicate)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                layerRecord = db.Layers.ElementOrDefault(aimLayer);
                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color = color;
                }
                layerRecord.UpgradeOpen();
                if (replaceIfDuplicate == true)
                {
                    layerRecord.Color = color;
                }
                layerRecord.IsPlottable = false;
                layerRecord.DowngradeOpen();
            }

            return layerRecord;
        }




    }
}
