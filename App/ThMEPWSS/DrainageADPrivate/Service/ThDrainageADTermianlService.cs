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
        internal static Polyline GetVisibleOBB(BlockReference blk)
        {
            var objs = new DBObjectCollection();
            blk.ExplodeWithVisible(objs);
            var curves = objs.OfType<Entity>()
                .Where(e => e is Curve).ToCollection();
            curves = Tesslate(curves);
            curves = curves.OfType<Curve>().Where(o => o != null && o.GetLength() > 1e-6).ToCollection();
            var obb = curves.GetMinimumRectangle();
            return obb;
        }

        internal static ThDrainageADCommon.TerminalType GetTerminalType(string name, Dictionary<string, List<string>> BlockNameDict)
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

        private static DBObjectCollection Tesslate(DBObjectCollection curves,
   double arcLength = 50.0, double chordHeight = 50.0)
        {
            var results = new DBObjectCollection();
            curves.OfType<Curve>().ToList().ForEach(o =>
            {
                if (o is Line)
                {
                    results.Add(o);
                }
                else if (o is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(arcLength));
                }
                else if (o is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(arcLength));
                }
                else if (o is Polyline polyline)
                {
                    results.Add(polyline.TessellatePolylineWithArc(arcLength));
                }
                else if (o is Ellipse ellipse)
                {
                    results.Add(ellipse.Tessellate(chordHeight));
                }
                else if (o is Spline spline)
                {
                    results.Add(spline.Tessellate(chordHeight));
                }
            });
            return results;
        }

    }
}
