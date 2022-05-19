using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Internal;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class BlockTools
    {
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
        public static Point3d AddToBlockTableRecord(this Database db, string blockName, List<Entity> ents)
        {
            var center = ents.GetCenter();//取中心点作为插入点
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
        public static ObjectId InsertBlockReference(this ObjectId spaceId, string layer, string blockName, Point3d position, Scale3d scale, double rotateAngle,int colorIndex)
        {
            ObjectId blockRefId;//存储要插入的块参照的Id
            Database db = spaceId.Database;//获取数据库对象
            //以读的方式打开块表
            BlockTable bt = (BlockTable)db.BlockTableId.GetObject(OpenMode.ForRead);
            //如果没有blockName表示的块，则程序返回
            if (!bt.Has(blockName)) return ObjectId.Null;
            //以写的方式打开空间（模型空间或图纸空间）
            BlockTableRecord space = (BlockTableRecord)spaceId.GetObject(OpenMode.ForWrite);
            //创建一个块参照并设置插入点
            BlockReference br = new BlockReference(position, bt[blockName]);
            br.ScaleFactors = scale;//设置块参照的缩放比例
            br.Layer = layer;//设置块参照的层名
            br.Rotation = rotateAngle;//设置块参照的旋转角度
            br.ColorIndex = colorIndex;
            ObjectId btrId = bt[blockName];//获取块表记录的Id
            //打开块表记录
            BlockTableRecord record = (BlockTableRecord)btrId.GetObject(OpenMode.ForRead);
            //添加可缩放性支持
            if (record.Annotative == AnnotativeStates.True)
            {
                ObjectContextCollection contextCollection = db.ObjectContextManager.GetContextCollection("ACDB_ANNOTATIONSCALES");
                ObjectContexts.AddContext(br, contextCollection.GetContext("1:1"));
            }
            blockRefId = space.AppendEntity(br);//在空间中加入创建的块参照
            db.TransactionManager.AddNewlyCreatedDBObject(br, true);//通知事务处理加入创建的块参照
            space.DowngradeOpen();//为了安全，将块表状态改为读
            return blockRefId;//返回添加的块参照的Id
        }
        public static void ShowBlock(this List<Entity> entities, string blockTag, string LayerName,double InsertX = double.NaN,double InsertY = double.NaN)
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(LayerName))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, LayerName, 0);
                var blockName = acad.Database.GetBlockName(blockTag);
                Point3d InsertPoint;
                    // 创建块，并且插入到原位
                InsertPoint = acad.Database.AddToBlockTableRecord(blockName, entities);

                if(!double.IsNaN(InsertX) && !double.IsNaN(InsertY))
                {
                    InsertPoint = new Point3d(InsertX, InsertY, 0);
                }
                acad.ModelSpace.ObjectId.InsertBlockReference(LayerName, blockName, InsertPoint, new Scale3d(1), 0,0);
            }
        }
    }
}
