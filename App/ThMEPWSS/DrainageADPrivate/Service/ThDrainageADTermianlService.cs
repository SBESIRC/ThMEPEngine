using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Extension;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Hvac;
using ThMEPEngineCore.Diagnostics;
using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.ViewModel;

using ThMEPWSS.DrainageADPrivate;

namespace ThMEPWSS.DrainageADPrivate.Service
{
    internal class ThDrainageADTermianlService
    {
       

        public static ThDrainageADCommon.TerminalType GetTerminalType(string name, Dictionary<string, List<string>> BlockNameDict)
        {
            var blkName = name.ToUpper();
            ThDrainageADCommon.TerminalType type = ThDrainageADCommon.TerminalType.Unknow;

            if (blkName == ThDrainageADCommon.BlkName_WaterHeater)
            {
                type = ThDrainageADCommon.TerminalType.WaterHeater;
            }
            else
            {
                var blockName = BlockNameDict.Where(o => o.Value.Where(x => blkName.EndsWith(x.ToUpper())).Any());
                if (blockName.Count() > 0)
                {
                    var typePair = ThDrainageADCommon.TerminalChineseName.Where(x => x.Value == blockName.First().Key);
                    if (typePair.Count() > 0)
                    {
                        type = (ThDrainageADCommon.TerminalType)typePair.First().Key;
                    }
                }
            }
            return type;
        }

       

    }
}
