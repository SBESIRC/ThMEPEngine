using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using Dreambuild.AutoCAD;

using ThCADExtension;

using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    class ThInsertOutputService
    {
        public static void LoadBlockLayerToDocument(Database database, List<string> blockNames, List<string> layerNames)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            {
                //解锁0图层，后面块有用0图层的
                DbHelper.EnsureLayerOn("0");
            }
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                foreach (var item in blockNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var block = blockDb.Blocks.ElementOrDefault(item);
                    if (null == block)
                        continue;
                    currentDb.Blocks.Import(block, true);
                }
                foreach (var item in layerNames)
                {
                    if (string.IsNullOrEmpty(item))
                        continue;
                    var layer = blockDb.Layers.ElementOrDefault(item);
                    if (null == layer)
                        continue;
                    currentDb.Layers.Import(layer, true);
                }
            }
        }

        public static void InsertBlk(List<ThDrainageBlkOutput> outputList)
        {
            if (outputList == null || outputList.Count() == 0)
            {
                return;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                for (int i = 0; i < outputList.Count(); i++)
                {
                    var output = outputList[i];
                    var pt = output.Position;
                    double rotateAngle = Vector3d.XAxis.GetAngleTo(output.Dir, Vector3d.ZAxis);
                    double scale = output.Scale;
                    var layer = output.Layer;

                    var id = acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                           layer,
                           output.Name,
                           pt,
                           new Scale3d(scale),
                           rotateAngle,
                          new Dictionary<string, string>()
                       );
                    foreach (var dynamic in output.Visibility)
                    {
                        id.SetDynBlockValue(dynamic.Key, dynamic.Value);
                    }
                }
            }
        }

        public static void InsertLine(List<Line> outputLine, string layer)
        {
            if (outputLine == null || outputLine.Count() == 0)
            {
                return;
            }
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                for (int i = 0; i < outputLine.Count(); i++)
                {
                    var linkLine = outputLine[i];
                    linkLine.Layer = layer;
                    linkLine.Color = Color.FromColorIndex(ColorMethod.ByLayer, (short)ColorIndex.BYLAYER);
                    acadDatabase.ModelSpace.Add(linkLine);
                }
            }
        }
    }
}
