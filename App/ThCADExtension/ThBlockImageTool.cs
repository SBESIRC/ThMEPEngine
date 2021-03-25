using System;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Internal;
using Autodesk.AutoCAD.DatabaseServices;
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace ThCADExtension
{

#if ACAD_ABOVE_2014
    // https://drive-cad-with-code.blogspot.com/2020/12/obtaining-blocks-image.html
    public class ThBlockImageTool
    {
        public static IntPtr GetBlockImagePointer(
            string blkName,
            Database db,
            int imgWidth,
            int imgHeight,
            AcColor backColor)
        {
            var blockId = GetBlockTableRecordId(blkName, db);
            if (!blockId.IsNull)
            {
                var imgPtr = Utils.GetBlockImage(blockId, imgWidth, imgHeight, backColor);
                return imgPtr;
            }
            else
            {
                throw new ArgumentException(
                    $"Cannot find block definition in current database: \"{blkName}\".");
            }
        }

        public static IntPtr GetBlockImagePointer(
            ObjectId blockDefinitionId,
            int imgWidth,
            int imgHeight,
            AcColor backColor)
        {
            var imgPtr = Utils.GetBlockImage(blockDefinitionId, imgWidth, imgHeight, backColor);
            return imgPtr;
        }

        public static System.Drawing.Image GetBlockImage(
            string blkName,
            Database db,
            int imgWidth,
            int imgHeight,
            AcColor backColor)
        {
            return System.Drawing.Image.FromHbitmap(GetBlockImagePointer(blkName, db, imgWidth, imgHeight, backColor));
        }

        public static System.Drawing.Image GetBlockImage(
            ObjectId blockDefinitionId,
            int imgWidth,
            int imgHeight,
            AcColor backColor)
        {
            return System.Drawing.Image.FromHbitmap(
                GetBlockImagePointer(blockDefinitionId, imgWidth, imgHeight, backColor));
        }

        public static ImageSource GetImageSource(
            string blkName,
            Database db,
            int imgWidth,
            int imgHeight,
            AcColor backColor)
        {
            var imgPtr = GetBlockImagePointer(blkName, db, imgWidth, imgHeight, backColor);
            return ConvertBitmapToImageSource(imgPtr);
        }

        public static ImageSource GetImageSource(
            ObjectId blockDefinitionId,
            int imgWidth,
            int imgHeight,
            AcColor backColor)
        {
            var imgPtr = GetBlockImagePointer(blockDefinitionId, imgWidth, imgHeight, backColor);
            return ConvertBitmapToImageSource(imgPtr);
        }

        #region private methods

        private static ObjectId GetBlockTableRecordId(string blkName, Database db)
        {
            var blkId = ObjectId.Null;

            using (var tran = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tran.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (bt.Has(blkName))
                {
                    blkId = bt[blkName];
                }
                tran.Commit();
            }

            return blkId;
        }

        private static ImageSource ConvertBitmapToImageSource(IntPtr imgHandle)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                imgHandle,
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

        }

        #endregion
    }
#endif
}
