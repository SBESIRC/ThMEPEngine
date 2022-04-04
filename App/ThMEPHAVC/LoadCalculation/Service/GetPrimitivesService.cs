using Linq2Acad;
using System.Linq;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
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
                .Where(o => IsRoomFunctionBlock(o))
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

        private bool IsRoomFunctionBlock(BlockReference reference)
        {
            try
            {
                var blockName = reference.GetEffectiveName();
                return blockName == LoadCalculationParameterFromConfig.RoomFunctionBlockName |
                    blockName == LoadCalculationParameterFromConfig.RoomFunctionBlockName_New;
            }
            catch
            {
                return false;
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
