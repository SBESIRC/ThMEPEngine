using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.ElectricalLoadCalculation
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
                .Where(o => o.GetEffectiveName() == ElectricalLoadCalculationConfig.RoomFunctionBlockName)
                .ToList();
                roomFunctionBlks.ForEach(o =>
                {
                    //o.UpgradeOpen();
                    var newBr = acdb.Element<Entity>(o.ObjectId,true);
                    originTransformer.Transform(newBr);
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
                .Where(o => o.Layer == ElectricalLoadCalculationConfig.LoadCalculationTableLayer && o.TableStyleName == ElectricalLoadCalculationConfig.LoadCalculationTableName)
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
                .Where(o => o.Layer == ElectricalLoadCalculationConfig.LoadCalculationTableLayer)
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
