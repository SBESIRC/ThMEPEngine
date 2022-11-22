using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using Dreambuild.AutoCAD;
using ICSharpCode.SharpZipLib.Zip;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPWSS.BlockNameConfig
{
    public static class Block2Pic
    {
        public static string GenerateBlockPic(Point3dCollection selectArea, out Dictionary<string, List<double>> blockSizeDic)
        {
            blockSizeDic = new Dictionary<string, List<double>>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            string iconPath;
            using (var docLock = Active.Document.LockDocument())
            using (var acadDb = AcadDatabase.Active())
            {
                var path = Path.GetDirectoryName(doc.Name);
                var name = Path.GetFileName(doc.Name);
                iconPath = path + "\\" + name + " icons";
                blockSizeDic = ExtractThumbnails(acadDb, selectArea,iconPath);
            }
            new FastZip().CreateZip(iconPath + ".zip", iconPath, true, "");
            return iconPath + ".zip";
        }

        private static Dictionary<string, List<double>> ExtractThumbnails(AcadDatabase acadDb, Point3dCollection selectArea, string iconPath)
        {
            var blockSizeDic = new Dictionary<string, List<double>>();
            if (Directory.Exists(iconPath)) Directory.Delete(iconPath, true);
            var blocks = ThMEPWSS.UndergroundFireHydrantSystem.Extract.BlockExtractService.ExtractBlocks(acadDb.Database);
            
            var spatialIndex = new ThCADCoreNTSSpatialIndex(blocks);
            var blocksInRect = spatialIndex.SelectCrossingPolygon(selectArea);
            var blockList = new Dictionary<string,BlockTableRecord>();
            int numIcons = 0;
            foreach (var obj in blocksInRect)
            {
                var btr = acadDb.Element<BlockTableRecord>((obj as BlockReference).BlockTableRecord);//acadDb.Element<BlockTableRecord>((obj as BlockReference).Id);
                var blkName = btr.Name.Split('|').Last().Split('$').Last();
                var upperName = blkName.ToUpper();
                if (upperName == "*MODEL_SPACE" || upperName == "*PAPER_SPACE" || upperName.Contains("LAYOUT"))
                    continue;
    
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
                if (blockSizeDic.ContainsKey(blkName)) continue;
                blockList.Add(blkName, btr);
                blockSizeDic.Add(blkName, new List<double>() { length, width });
            }
            Directory.CreateDirectory(iconPath);
            foreach (var item in blockList)
            {
                try
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
                catch(Exception ex)
                {
                    ;
                }
            }
            
            Active.Editor.WriteMessage("图块个数： " + numIcons.ToString() + "\n");
           
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
