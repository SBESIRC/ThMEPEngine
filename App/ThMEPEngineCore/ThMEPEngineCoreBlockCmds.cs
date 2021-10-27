using AcHelper;
using System.IO;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Drawing.Imaging;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using AcColor = Autodesk.AutoCAD.Colors.Color;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreBlockCmds
    {
        [CommandMethod("TIANHUACAD", "THDUMPBLOCKIMAGE", CommandFlags.Modal)]
        public void THDUMPBLOCKIMAGE()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .ForEach(o =>
                    {
                        var blkname = o.GetEffectiveName();
                        var image = GenerateThumbnail(acadDatabase.Database, blkname);
                        var filename = Path.Combine(path, blkname);
                        filename = Path.ChangeExtension(filename, "jpg");
                        image.Save(filename, ImageFormat.Jpeg);
                    });
            }
        }

        private System.Drawing.Image GenerateThumbnail(Database db, string blkname)
        {
            return ThBlockImageTool.GetBlockImage(blkname, db, 64, 64, AcColor.FromRgb(255, 255, 255));
        }
    }
}
