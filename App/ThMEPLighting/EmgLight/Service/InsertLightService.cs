using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using DotNetARX;
using Linq2Acad;

using ThMEPLighting.EmgLight.Common;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPLighting.EmgLight.Service
{
    public static class InsertLightService
    {
        public static void InsertSprayBlock(Dictionary<Polyline, (Point3d, Vector3d)> insertPtInfo, double scale, string blkName)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportBlock(blkName);
                db.Database.ImportLayer(ThMEPLightingCommon.EmgLightLayerName);

                foreach (var ptInfo in insertPtInfo)
                {
                    var size = EmgLightCommon.blk_move_length[blkName];
                    db.Database.InsertModel(ptInfo.Value.Item1 + ptInfo.Value.Item2 * scale * size, ptInfo.Value.Item2, new Dictionary<string, string>() { }, scale, blkName);
                }
            }
        }

        private static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, Dictionary<string, string> attNameValues, double scale, string blkName)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir, Vector3d.ZAxis);

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    ThMEPLightingCommon.EmgLightLayerName,
                    blkName,
                    pt,
                    new Scale3d(scale),
                    rotateAngle,
                    attNameValues);
            }
        }


        public static void InsertCommentLine(List<Polyline> commentLine)
        {
            using (var db = AcadDatabase.Active())
            {
                foreach (var pl in commentLine)
                {
                    pl.Layer = EmgLightCommon.LayerComment;
                    var objId = db.ModelSpace.Add(pl);
#if ACAD_ABOVE_2014
                    Active.Editor.Command("_.REVCLOUD", "_arc", 500, 500, "_Object", objId, "_No");
#else
                    ResultBuffer args = new ResultBuffer(
                       new TypedValue((int)LispDataType.Text, "_.REVCLOUD"),
                       new TypedValue((int)LispDataType.Text, "_ARC"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "_Object"),
                       new TypedValue((int)LispDataType.ObjectId, objId),
                       new TypedValue((int)LispDataType.Text, "_No"));
                    Active.Editor.AcedCmd(args);
#endif
                }
            }
        }
    }
}
