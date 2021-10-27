using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm;

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
                .Where(o => o.GetEffectiveName() == "AI-暖通-房间功能")
                .ToList();
                return roomFunctionBlks;
            }
        }
    }
}
