using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Print
{
    public static class PrintPipes
    {
        public static void Print(List<RouteModel> routes)
        {
            var routeGroup = routes.GroupBy(x => x.verticalPipeType).ToList();
            string layer = ThWSSCommon.DraiLayerName;
            foreach (var group in routeGroup)
            {
                switch (group.Key)
                {
                    case VerticalPipeType.SewagePipe:
                    case VerticalPipeType.ConfluencePipe:
                        layer = ThWSSCommon.DraiSewageLayerName;
                        break;
                    case VerticalPipeType.WasteWaterPipe:
                        layer = ThWSSCommon.DraiWasteLayerName;
                        break;
                    case VerticalPipeType.CondensatePipe:
                    case VerticalPipeType.rainPipe:
                        layer = ThWSSCommon.DraiLayerName;
                        break;
                    default:
                        break;
                }
                InsertBlockService.InsertConnectPipe(group.Select(x=>x.route).ToList(), layer, null);
            }
        }
    }
}
