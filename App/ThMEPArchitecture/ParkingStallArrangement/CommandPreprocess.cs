using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThMEPEngineCore.Service;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPEngineCore.LaneLine;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using Dreambuild.AutoCAD;
using Draw = Dreambuild.AutoCAD.Draw;
using NetTopologySuite.Geometries;
using ThMEPArchitecture.ViewModel;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using NFox.Cad;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    class ThParkingStallPreprocessCmd : ThMEPBaseCommand, IDisposable
    {
        //public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "PreProcessLog.txt");

        //public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
        //    .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day).CreateLogger();

        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;
        private string LayerKeyWord;
        public ThParkingStallPreprocessCmd()
        {
            CommandName = "-THZDCWYCL";//天华自动车位预处理
            ActionName = "生成";
            _CommandMode = CommandMode.WithoutUI;
        }

        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    Run(currentDb);
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"\nseconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }

        public void Run(AcadDatabase acadDatabase)
        {
            double tol = 5.0;
            var blocks = InputData.SelectObstacles(acadDatabase, out string layerKeyWord);
            LayerKeyWord = layerKeyWord;
            if (blocks is null || blocks.Count == 0)
            {
                Active.Editor.WriteMessage("未拿到障碍物块");
                return;
            }
            foreach (BlockReference block in blocks) PreprocessOneBlock(acadDatabase, block, tol);
        }
        
        private void PreprocessOneBlock(AcadDatabase acadDatabase,BlockReference block,double tol = 5)
        {
            var blocks = new DBObjectCollection { block };
            var lines = new DBObjectCollection();

            while (blocks.Count != 0)// 炸块获取所有的线
            {
                blocks = ExplodeToLines(blocks, out DBObjectCollection curwalls);
                foreach (Line l in curwalls) lines.Add(l);
            }
            if (lines.Count == 0)
            {
                Active.Editor.WriteMessage("\n块名为" + block.Name + "的块未提取到障碍物");
                return;// 没有拿到线
            }
#if (DEBUG)
            foreach (Line l in lines) l.AddToCurrentSpace();// 测试用，把所有拿到的线打出来
#endif
            var service = new ThLaneLineCleanService();
            lines = service.CleanWithTol(lines, tol);// 线清理，处理重复线等

            foreach (Line l in lines) // 线延长操作
            {
                var newl = l.ExtendLine(tol);
                l.StartPoint = newl.StartPoint;
                l.EndPoint = newl.EndPoint;
            }
            var objs = LinesToPline(lines);// 线转换为多段线
            if(objs.Count == 0)
            {
                Active.Editor.WriteMessage("\n块名为" + block.Name + "的块中的元素不包含闭合区域");
                return;// 没有拿到线
            }
            objs = objs.ToNTSMultiPolygon().Union().ToDbCollection();// union操作,获取合并后的多段线

            var walls = new List<Entity>();
            foreach (Entity obj in objs) 
            { 
                if (obj  is Polyline pline)
                {
                    if(pline.Area > tol) walls.Add(pline);
                }
            } 
            var LayerName = "AI-障碍物";
            if (!acadDatabase.Layers.Contains(LayerName))
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, LayerName, 1);
            walls.ForEach(e => { e.Layer = LayerName; e.ColorIndex = 1; });
            //获取不重复的块名
            var blockName = acadDatabase.Database.GetBlockName(LayerName);
            // 创建块，并且插入到原位
            Point3d InsertPoint =acadDatabase.Database.AddBlockTableRecord(blockName, walls);
            acadDatabase.ModelSpace.ObjectId.InsertBlockReference(LayerName, blockName, InsertPoint, new Scale3d(1), 0);

        }
        private DBObjectCollection ExplodeToLines(DBObjectCollection input_blocks, out DBObjectCollection outwalls)
        {
            // 输入一堆block，输出对每个block炸之后的block以及炸出的线
            outwalls = new DBObjectCollection();
            var output_blocks = new DBObjectCollection();
            foreach (BlockReference block in input_blocks)
            {
                var blocks = _ExplodeToLines(block, out DBObjectCollection walls);
                foreach (BlockReference b in blocks) output_blocks.Add(b);
                foreach (Line l in walls) outwalls.Add(l);
            }
            return output_blocks;
        }
        private DBObjectCollection _ExplodeToLines(BlockReference block , out DBObjectCollection walls)
        {
            var dbObjs = new DBObjectCollection();
            block.Explode(dbObjs);
            var walls_ent = new DBObjectCollection();
            var blocks = new DBObjectCollection();
            foreach (var obj in dbObjs)
            {
                var ent = obj as Entity;
                if (ent is BlockReference b)
                {
                    blocks.Add(b);
                }
                else
                {
                    if (IsObstacle(ent))
                    {
                        walls_ent.Add(ent);
                    }
                }
            }
            walls = walls_ent.ToLines(50);
            return blocks;
        }
        private bool IsObstacle(Entity ent)
        {
            return ent.Layer.ToUpper().Contains(LayerKeyWord);
        }
        // 线转换到多段线，忽略洞
        private static DBObjectCollection LinesToPline(DBObjectCollection lines)
        {
            var geos = lines.Polygonize();
            var objs = new DBObjectCollection();
            foreach (Polygon polygon in geos)
            {
                objs.Add(polygon.Shell.ToDbPolyline());
            }
            geos.Clear();
            geos = null;
            return objs;
        }

    }

    static class PreprocssEx
    {
        public static DBObjectCollection CleanWithTol(this ThLaneLineCleanService service, DBObjectCollection curves,double tol = 5)
        {
            // 合并处理
            var mergedLines = ThLaneLineEngine.Explode(curves);
            mergedLines = ThLaneLineMergeExtension.Merge(mergedLines);
            mergedLines = ThLaneLineEngine.Noding(mergedLines);
            mergedLines = CleanZeroCurves(mergedLines, tol);

            // 延伸处理
            var extendedLines = ThLaneLineExtendEngine.Extend(mergedLines);
            extendedLines = ThLaneLineMergeExtension.Merge(extendedLines);
            extendedLines = ThLaneLineEngine.Noding(extendedLines);
            extendedLines = ThLaneLineEngine.CleanZeroCurves(extendedLines);

            // 合并处理
            return ThLaneLineMergeExtension.Merge(mergedLines, extendedLines);
        }
        private static DBObjectCollection CleanZeroCurves(DBObjectCollection curves,double tol = 5)
        {
            return curves.Cast<Line>().Where(c => c.Length >  tol).ToCollection();
        }
        public static string GetBlockName(this Database db, string blockTag)
        {
            string blockName;
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            int i = 0;
            while (true)
            {
                blockName = blockTag + i.ToString();
                if (!bt.Has(blockName)) break;
                i += 1;
            }
            return blockName;
        }
        // 创建块并插入原位，插入点为中心点
        public static Point3d AddBlockTableRecord(this Database db, string blockName, List<Entity> ents)
        {
            var center = ents.GetCenter();
            //打开块表
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            if (!bt.Has(blockName)) //判断是否存在名为blockName的块
            {
                //创建一个BlockTableRecord类的对象，表示所要创建的块
                BlockTableRecord btr = new BlockTableRecord();
                btr.Name = blockName;//设置块名                
                //将列表中的实体加入到新建的BlockTableRecord对象
                ents.ForEach(ent => btr.AppendEntity(ent));
                btr.Origin = center;
                bt.UpgradeOpen();//切换块表为写的状态
                bt.Add(btr);//在块表中加入blockName块
                db.TransactionManager.AddNewlyCreatedDBObject(btr, true);//通知事务处理
                bt.DowngradeOpen();//为了安全，将块表状态改为读
            }
            return center;//返回块表记录的Id
        }
        public static void ShowAsBlock(this List<Entity> entities, string blockName, string LayerName)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                // 创建块，并且插入到原位
                Point3d InsertPoint = acad.Database.AddBlockTableRecord(blockName, entities);
                acad.ModelSpace.ObjectId.InsertBlockReference(LayerName, blockName, InsertPoint, new Scale3d(1), 0);
            }
        }
    }
}
