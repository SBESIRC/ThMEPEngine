using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPWSS.JsonExtensionsNs;
using AcHelper;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using NFox.Cad;
using ThCADExtension;
using System.IO;
using System.Windows.Forms;

namespace ThMEPWSS.UndergroundSpraySystem.Service
{
    class BlocksImport
    {
        public static bool ImportElementsFromStdDwg()
        {

            var file = ThCADCommon.WSSDwgPath();
            if (!File.Exists(file))
            {
                MessageBox.Show($"\"{file}\"不存在");
                return false;
            }
            using (var @lock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase adb = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(file, DwgOpenMode.ReadOnly, false))
            {
                var fs = new Dictionary<Action, string>();
                var blocks = blockDb.Blocks.Select(x => x.Name).ToList();
                foreach (var blk in blocks)
                {
                    fs.Add(() => adb.Blocks.Import(blockDb.Blocks.ElementOrDefault(blk)), blk);
                }
                blockDb.DimStyles.ForEach(x => adb.DimStyles.Import(x));
                foreach (var txtStyle in blockDb.TextStyles)
                {
                    adb.TextStyles.Import(txtStyle);
                }
                var layers = blockDb.Layers.Select(x => x.Name).ToList();
                foreach (var layer in layers)
                {
                    fs.Add(() => adb.Layers.Import(blockDb.Layers.ElementOrDefault(layer)), layer);
                }
                foreach (var kv in fs)
                {
                    try
                    {
                        kv.Key();
                    }
                    catch (System.Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
            }
            return true;
        }
    }
}
