using System;
using System.IO;
using DotNetARX;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class BlockTool
    {
        /// <summary>
        /// 从动态块的角度去获取块名
        /// </summary>
        /// <param name="bref"></param>
        /// <returns></returns>
        public static string GetRealBlockName(this BlockReference bref)
        {
            //不管原始块是不是动态块，全部都从动态块去拿其名字（仅设置可见性的块，不一定是动态块）

            string blockName;//存储块名
            if (bref == null) return null;//如果块参照不存在，则返回

            //获取动态块所属的动态块表记录
            ObjectId idDyn = bref.DynamicBlockTableRecord;

            using (var trans = idDyn.Database.TransactionManager.StartOpenCloseTransaction())
            {
                //打开动态块表记录
                BlockTableRecord btr = (BlockTableRecord)trans.GetObject(idDyn, OpenMode.ForRead);
                blockName = btr.Name;//获取块名

                trans.Commit();
            }



            return blockName;//返回块名
        }

        /// <summary>
        /// 获取块引用的变换矩阵
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Matrix3d GetBlockTransform(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.BlockTransform;
            else
                return Matrix3d.Identity;
        }

        /// <summary>
        /// 获取块引用的旋转角度
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static double GetBlockRotation(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.Rotation;
            else
                return 0.0;
        }

        public static Point3d GetBlockPosition(this ObjectId id)
        {
            BlockReference bref = id.GetObject(OpenMode.ForRead) as BlockReference;
            if (bref != null)//如果是块参照
                return bref.Position;
            else
                return Point3d.Origin;
        }

        /// <summary>
        /// 从动态块定义的角度去获取普通块的handle
        /// </summary>
        /// <param name="btr"></param>
        /// <returns></returns>
        public static string GetNormalBlockHandle(this BlockTableRecord btr)
        {
            var result = btr.Handle.ToString();
            var xData = btr.XData;
            //如果没有扩展数据，肯定是普通块
            if (xData != null)
            {
                // Get the XData as an array of TypeValues and loop
                // through it
                var tvs = xData.AsArray();
                for (int i = 0; i < tvs.Length; i++)
                {
                    // The first value should be the RegAppName
                    var tv = tvs[i];
                    if (tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName)
                    {
                        // If it's the one we care about...
                        if ((string)tv.Value == "AcDbBlockRepBTag")
                        {
                            // ... then loop through until we find a
                            // handle matching our blocks or otherwise
                            // another RegAppName
                            for (int j = i + 1; j < tvs.Length; j++)
                            {
                                tv = tvs[j];
                                if (tv.TypeCode == (int)DxfCode.ExtendedDataRegAppName)
                                {
                                    // If we have another RegAppName, then
                                    // we'll break out of this for loop and
                                    // let the outer loop have a chance to
                                    // process this section
                                    i = j - 1;
                                    break;
                                }

                                if (tv.TypeCode == (int)DxfCode.ExtendedDataHandle)
                                {
                                    return (string)tv.Value;
                                    //// If we have a matching handle...
                                    //if ((string)tv.Value == blkHand.ToString())
                                    //{
                                    //    // ... then we can add the block's name
                                    //    // to the list and break from both loops
                                    //    // (which we do by setting the outer index
                                    //    // to the end)
                                    //    blkNames.Add(btr2.Name);

                                    //    i = tvs.Length - 1;

                                    //    break;
                                    //}

                                }

                            }

                        }

                    }
                }
            }


            return result;

        }

        /// <summary>
        /// 将动态块的图元按可见性分类
        /// </summary>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public static Dictionary<string, List<ObjectId>> GetDynablockVisibilityStatesGrp(this ObjectId blockId)
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

                var results = new Dictionary<string, List<ObjectId>>();
                using (var tr = db.TransactionManager.StartOpenCloseTransaction())
                {
                    var blockName = blockId.GetObjectByID<BlockReference>(tr).GetRealBlockName();
                    var bt = db.BlockTableId.GetObjectByID<BlockTable>(tr);

                    if (bt.Has(blockName))
                    {
                        var btr = bt[blockName].GetObjectByID<BlockTableRecord>(tr);

                        if (btr != null && btr.ExtensionDictionary != null)
                        {
                            var dico = btr.ExtensionDictionary.GetObjectByID<DBDictionary>(tr);
                            if (dico.Contains("ACAD_ENHANCEDBLOCK"))
                            {
                                ObjectId graphId = dico.GetAt("ACAD_ENHANCEDBLOCK");

                                var parameterIds = graphId.acdbEntGetObjects(360);

                                var id = parameterIds.OfType<ObjectId>().First(parameterId => parameterId.ObjectClass.Name == "AcDbBlockVisibilityParameter");

                                var visibilityParam = id.acdbEntGetTypedVals().AsEnumerable();

                                //从303开始，按303后的元素个数进行拾取，每一份归为一组
                                var grp = visibilityParam.GroupTake(par => par.TypeCode != 303, parms => (int)parms.Skip(1).First().Value + 2);

                                results = grp.ToDictionary(res => res.First().Value.ToString(), res => res.Skip(2).Select(tv => (ObjectId)tv.Value).ToList());

                            }
                        }
                    }

                    tr.Commit();

                }

                return results;

            }


        /// <summary>
        /// 获取可见动态块当前可见性下的边界
        /// </summary>
        /// <param name="block"></param>
        /// <param name="visibilityStatus"></param>
        /// <returns></returns>
        public static Extents3d GetGeometricExtents(this BlockReference block, string visibilityStatus)
        {
            using (var tr = block.ObjectId.Database.TransactionManager.StartOpenCloseTransaction())
            {
                //获取到指定可见性状态下的实体，如果不是可见性块，则全部获取
                var ids = visibilityStatus != null ? block.ObjectId.GetDynablockVisibilityStatesGrp().FirstOrDefault(kp => kp.Key == visibilityStatus).Value : block.BlockTableRecord.GetObjectByID<BlockTableRecord>(tr).Cast<ObjectId>();

                //排除hatch类型，重新计算边界
                var exts = ids.Where(id => id.ObjectClass.Name != "Hatch").Select(id => id.GetObjectByID<Entity>(tr).GeometricExtents);

                //找出最小及最大点，返回边界
                var minx = exts.Min(ext => ext.MinPoint.X);
                var miny = exts.Min(ext => ext.MinPoint.Y);
                var maxx = exts.Max(ext => ext.MaxPoint.X);
                var maxy = exts.Max(ext => ext.MaxPoint.Y);

                //转回世界坐标系
                var result = new Extents3d(new Point3d(minx, miny, 0), new Point3d(maxx, maxy, 0));
                result.TransformBy(block.BlockTransform);

                return result;

            }

        }

        /// <summary>
        /// 判断是否是可见性块
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static bool IsVisibilityBlock(this BlockReference block)
        {
            return block.ObjectId.GetDynBlockValue("可见性1") != null;
        }

        public static System.Drawing.Bitmap CaptureThumbnail(this BlockTableRecord blk)
        {
#if ACAD2012
            // Attempt to generate an icon, where one doesn't exist
            //  https://www.keanw.com/2011/11/generating-preview-images-for-all-blocks-in-an-autocad-drawing-using-net.html
            if (blk.PreviewIcon == null)
            {
                string[] args = { "_.BLOCKICON " + blk.Name + "\n" };
                blk.Database.GetDocument().SendCommand(args);
            }

            return blk.PreviewIcon;
#else
            // https://www.keanw.com/2013/11/generating-larger-preview-images-for-all-blocks-in-an-autocad-drawing-using-net.html
            var imgsrc = CMLContentSearchPreviews.GetBlockTRThumbnail(blk);
            return ImageSourceToGDI(imgsrc as System.Windows.Media.Imaging.BitmapSource) as Bitmap;
#endif
        }

        // Helper function to generate an Image from a BitmapSource
        private static System.Drawing.Image ImageSourceToGDI(System.Windows.Media.Imaging.BitmapSource src)
        {
            var ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(src));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }

        // Convert BitmapImage to Bitmap
        public static Bitmap BitmapImage2Bitmap(this BitmapImage bitmapImage)
        {
            return new Bitmap(bitmapImage.StreamSource);
        }

        // Convert Bitmap to BitmapImage
        public static BitmapImage Bitmap2BitmapImage(this Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }
}
