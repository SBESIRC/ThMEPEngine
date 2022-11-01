using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Windows.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ICSharpCode.SharpZipLib.Zip;
using System.Drawing;
using System;
using System.Windows.Documents;
using System.Collections.Generic;
using Autodesk.AutoCAD.MacroRecorder;
using Autodesk.AutoCAD.ApplicationServices;
using Linq2Acad;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.BlockNameConfig
{
    public static class Block2Pic
    {
        public static string GenerateBlockPic(out Dictionary<string, List<double>> blockSizeDic)
        {
            blockSizeDic = new Dictionary<string, List<double>>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            string iconPath;
            using (var acadDb = AcadDatabase.Active())
            {
                var path = Path.GetDirectoryName(doc.Name);
                var name = Path.GetFileName(doc.Name);
                iconPath = path + "\\" + name + " icons";
                blockSizeDic = ExtractThumbnails(acadDb,iconPath);
            }
            new FastZip().CreateZip(iconPath + ".zip", iconPath, true, "");
            return iconPath + ".zip";
        }

        private static Dictionary<string, List<double>> ExtractThumbnails(AcadDatabase acadDb, string iconPath)
        {
            var blockSizeDic = new Dictionary<string, List<double>>();
            var blockTableDic = new Dictionary<string,BlockTableRecord>();
            var blackNames = new List<string>();
            blackNames.Add("重力流雨水斗");
            int numIcons = 0;
            var blockTable = acadDb.Element<BlockTable>(acadDb.Database.BlockTableId);
            if (Directory.Exists(iconPath)) Directory.Delete(iconPath, true);
            foreach (var btrId in blockTable)
            {
                var btr = acadDb.Element<BlockTableRecord>(btrId);
                var blkName = btr.Name.Split('|').Last().Split('$').Last();
                var upperName = blkName.ToUpper();
                if (upperName == "*MODEL_SPACE" || upperName == "*PAPER_SPACE" || upperName.Contains("LAYOUT"))
                    continue;
                
                if (upperName.Contains("AI"))  continue;
                if (blkName.IsBlackName(blackNames)) continue;
                if (blockSizeDic.ContainsKey(blkName)) continue;
                var ids = btr.GetObjectIds();
                var idsCount = ids.Count();
                if (idsCount == 0) continue;
                
                bool isBlockReference = false;
                foreach (var id in ids)
                {
                    var entity = acadDb.Element<Entity>(id);
                    if (entity is not DBText)
                    {
                        isBlockReference = true;
                        break;
                    }
                }
                if (!isBlockReference) continue;
                var extents = new Extents3d();
                try
                {
                    extents = btr.GeometricExtents();
                }
                catch
                {
                    continue;
                }
                double length = extents.MaxPoint.X - extents.MinPoint.X;
                double width = extents.MaxPoint.Y - extents.MinPoint.Y;
                if (length > 1000 || width > 1000 || length < 50 || width < 50)
                {
                    continue;
                }
                if (btr.IsLayout || btr.IsAnonymous) continue;
                blockSizeDic.Add(blkName, new List<double>() { length, width });
                
                Directory.CreateDirectory(iconPath);
                blockTableDic.Add(blkName,btr);
                
            }
            foreach(var item in blockTableDic)
            {
                var btr = item.Value;
                var imgsrc = CMLContentSearchPreviews.GetBlockTRThumbnail(btr);
                var bmp = ImageSourceToGDI(imgsrc as System.Windows.Media.Imaging.BitmapSource);
                var reverseBmp = GrayReverse(new Bitmap(bmp));
                var fname = iconPath + "\\" + item.Key + ".jpg";
                if (File.Exists(fname))
                    File.Delete(fname);
                reverseBmp.Save(fname);
                numIcons++;
            }
            Active.Editor.WriteMessage("图块个数： "+numIcons.ToString()+"\n");
            return blockSizeDic;
        }

     

        public static bool IsBlackName(this string name,List<string> blackNames)
        {
            foreach (var blackName in blackNames)
            {
                if (name.Contains(blackName))
                {
                    return true;
                }
            }
            return false;
        }


        private static Bitmap GrayReverse(Bitmap bmp)
        {
            int value;
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color color = bmp.GetPixel(i, j);
                    if (color.R < 125) value = 255;
                    else value = 0;
                    var newColor = Color.FromArgb(value, value, value);
                    bmp.SetPixel(i, j, newColor);
                }
            }
            return bmp;
        }

        private static DBObjectCollection GetBTRObjIds(Transaction tr, BlockTableRecord btr)
        {
            var objs = new DBObjectCollection();
            foreach (ObjectId id in btr)
            {
                var entity = tr.GetObject(id, OpenMode.ForRead);
                if (entity is BlockReference br)
                {
                    var subBtr = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                    var subObjs = GetBTRObjIds(tr, subBtr);
                    subObjs.OfType<DBObject>().ForEach(o => objs.Add(o));
                }
                else
                {
                    objs.Add(entity);
                }
            }
            return objs;
        }

        private static System.Drawing.Image ImageSourceToGDI(System.Windows.Media.Imaging.BitmapSource src)
        {
            var ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(src));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }
    }
}
