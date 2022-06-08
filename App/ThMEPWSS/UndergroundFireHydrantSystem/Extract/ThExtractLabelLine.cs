using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractLabelLine//引线提取
    {
        public double LengthThresh = 200;//线长最小阈值
        public DBObjectCollection LabelLineCollection { get; private set; }
        public DBObjectCollection Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(o => IsHYDTPipeLayer(o.Layer))
                   .Where(o => IsTargetObject(o)).ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var DBObjs = spatialIndex.SelectWindowPolygon(polygon.ToGRect().ToPolygon().ToDbEntity());

                LabelLineCollection = new DBObjectCollection();

                var dbObjColl = new DBObjectCollection();
                DBObjs.Cast<Entity>()
                    .Where(o => o is Entity)
                    .ForEach(o => dbObjColl.Add(o));
                foreach (var dbObj in dbObjColl)
                {
                    if(dbObj is BlockReference blk)
                    {
                        GetLineInBlock(acadDatabase, blk, LabelLineCollection);
                        continue;
                    }
                    ExplodeLabelLine(dbObj as Entity, LabelLineCollection);
                }
                return LabelLineCollection;
            }
        }
        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-RAIN-DIMS" ||
                   layer.ToUpper() == "W-FRPT-HYDT-DIMS" ||
                   layer.ToUpper() == "W-FRPT-HYDT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-EQPM" ||
                   layer.ToUpper() == "W-WSUP-DIMS" ||
                   layer.ToUpper() == "W-FRPT-NOTE" ||
                   layer.ToUpper() == "W-FRPT-HYDT-NOTE" ||
                   layer.ToUpper() == "W-RAIN-NOTE" ||
                   layer.ToUpper() == "W-NOTE" ||
                   layer.ToUpper() == "W-SHET-PROF" ||
                   layer.ToUpper() == "TWT_TEXT";
        }

        private static bool IsTargetObject(Entity ent)
        {
            var type = ent.GetType().Name;
            return type.Equals("BlockReference")
                || type.Equals("ImpEntity")
                || type.Equals("ImpCurve")
                || type.Equals("Line");
        }

        public List<Line> CreateLabelLineList(DBObjectCollection labelLines)
        {
            var LabelPosition = new List<Line>();

            if (LabelLineCollection.Count != 0)
            {
                foreach (var db in LabelLineCollection)
                {
                    var line = db as Line;
                    if(line.Length < LengthThresh)
                    {
                        continue;
                    }
                    var pt1 = new Point3d(line.StartPoint.X, line.StartPoint.Y, 0);
                    var pt2 = new Point3d(line.EndPoint.X, line.EndPoint.Y, 0);
                    LabelPosition.Add(new Line(pt1,pt2));
                }
            }

#if DEBUG

            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                string layerName = "标注线图层";
                try
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(currentDb.Database, layerName, 30);
                }
                catch { }
                foreach (var line in LabelPosition)
                {
                    line.LayerId = DbHelper.GetLayerId(layerName);
                    currentDb.CurrentSpace.Add(line);
                }
            }
#endif
            return LabelPosition;
        }

        private void ExplodeLabelLine(Entity ent, DBObjectCollection dBObjects)
        {
            if (NotNeedDeal(ent))//炸出不需要关注对象就退出
            {
                return;
            }
            if (ent is Line line)// Line 直接添加
            {
                if(line.Length > LengthThresh)
                {
                    dBObjects.Add(line);
                }
                return;
            }
            try
            {
                var dbObjs = new DBObjectCollection();
                ent.Explode(dbObjs);
                foreach (var obj in dbObjs)
                {
                    if (obj is Entity ent1)
                    {
                        ExplodeLabelLine(ent1, dBObjects);
                    }
                }
            }
            catch
            {
            }
        }

        private bool NotNeedExplode(BlockReference bkr)
        {
            var blockName = bkr.GetEffectiveName();
            if (blockName.Contains("灭火器") ||
                blockName.Contains("消火栓") ||
                blockName.Contains("立管"))
            {
                return true;
            }
            return false;
        }

        private bool NotNeedDeal(Entity ent)//
        {
            if (ent == null ||
                ent is AlignedDimension ||//
                ent is Arc ||//弧
                ent is DBText ||//文字
                ent is Circle ||//圆
                ent.IsTCHText() ||//天正单行文字
                ent.IsTCHValve() ||//天正阀
                ent is DBPoint ||//db点
                ent is Hatch ||//填充
                ent is BlockReference)//块
            {
                return true;
            }
            return false;
        }
        private void GetLineInBlock(AcadDatabase acadDatabase, BlockReference bkr, DBObjectCollection LabelLineCollection)
        {
            if(NotNeedExplode(bkr))//不需要炸的块，直接跳过
            {
                return;
            }
            var blockRecordId = bkr.BlockTableRecord;
            var btr = acadDatabase.Blocks.Element(blockRecordId);
            foreach (var entId in btr)
            {
                var obj = acadDatabase.Element<Entity>(entId);
                if (obj is BlockReference)
                {
                    GetLineInBlock(acadDatabase, obj as BlockReference, LabelLineCollection);
                }
                if(obj is Line line)
                {
                    if(line.Length > LengthThresh)
                    {
                        LabelLineCollection.Add(obj as Line);
                    }
                }
            }
        }
    }
}
