using System;
using Linq2Acad;
using System.Linq;
using GeometryExtensions;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using ThMEPElectrical.BlockConvert;

namespace TianHua.Electrical.UI.CAD
{
    public static class ElectricalUIDbExtension
    {
        public static BlockDataModel CreateBlockDataModel(this Database database, ThBlockConvertBlock convert)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var block = acadDatabase.Blocks.ElementOrDefault(convert.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string);
                if (block != null)
                {
                    var model = new BlockDataModel()
                    {
                        ID = block.Name,
                        Name = block.Name,
                        Icon = block.PreviewIcon,
                    };
                    if (convert.Attributes.ContainsKey(ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY))
                    {
                        model.Visibility = convert.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY] as string;
                    }
                    return model;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
