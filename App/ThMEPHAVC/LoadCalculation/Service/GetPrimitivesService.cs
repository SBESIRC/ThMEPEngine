using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;
using ThMEPHVAC.LoadCalculation.Model;

namespace ThMEPHVAC.LoadCalculation.Service
{
    public class GetPrimitivesService
    {
        public ThMEPOriginTransformer originTransformer;
        public GetPrimitivesService(ThMEPOriginTransformer originTransformer)
        {
            this.originTransformer = originTransformer;
        }

        public List<BlockReference> GetRoomFunctionBlocks()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var roomFunctionBlks = acdb.ModelSpace
                .OfType<BlockReference>()
                .Where(o =>
                {
                    var blockName = o.GetEffectiveName();
                    return blockName== LoadCalculationParameterFromConfig.RoomFunctionBlockName | blockName == LoadCalculationParameterFromConfig.RoomFunctionBlockName_New;
                })
                .ToList();
                roomFunctionBlks.ForEach(o =>
                {
                    o.UpgradeOpen();
                    originTransformer.Transform(o);
                    o.DowngradeOpen();
                });
                return roomFunctionBlks;
            }
        }

        public List<Table> GetLoadCalculationTables()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var tables = acdb.ModelSpace
                .OfType<Table>()
                .Where(o => o.Layer == LoadCalculationParameterFromConfig.LoadCalculationTableLayer && o.TableStyleName == LoadCalculationParameterFromConfig.LoadCalculationTableName)
                .ToList();
                tables.ForEach(o =>
                {
                    o.UpgradeOpen();
                    originTransformer.Transform(o);
                    o.DowngradeOpen();
                });
                return tables;
            }
        }

        public List<Curve> GetLoadCalculationCurves()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var curves = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == LoadCalculationParameterFromConfig.LoadCalculationTableLayer)
                .Select(o => o.Clone() as Curve)
                .ToList();
                curves.ForEach(o =>
                {
                    originTransformer.Transform(o);
                });
                return curves;
            }
        }
    }
}
