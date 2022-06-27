using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using ThCADCore.NTS;
using ThMEPWSS.Pipe.Engine;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterWellPumpLayout.Model;
using ThMEPWSS.WaterWellPumpLayout.Service;
using ThMEPWSS.Command;

namespace ThMEPWSS.WaterWellPumpLayout.Command
{
    public class ThSelectWaterWellCmd : ThMEPBaseCommand, IDisposable
    {
        public List<ThWaterWellModel> WaterWellList { set; get; }//选取到的集水井
        public WaterWellIdentifyConfigInfo IdentifyInfo { set; get; }
        public void Dispose()
        {
            //
        }
        public ThSelectWaterWellCmd(WaterWellIdentifyConfigInfo identifyInfo)
        {
            ActionName = "选取集水井";
            CommandName = "THSJSB";
            IdentifyInfo = identifyInfo;
        }
        public List<ThWaterWellModel> GetWaterWellEntityList(Point3dCollection input)
        {
            List<ThWaterWellModel> waterWellList = new List<ThWaterWellModel>();
            using (var database = AcadDatabase.Active())
            using (var waterwellEngine = new ThWWaterWellRecognitionEngine(IdentifyInfo))
            {
                //获取潜水泵
                ThWaterWellPumpUtils.GetPumpIndex(out var pumpIndex, out var pumpDict);

                waterwellEngine.Recognize(database.Database, input);
                waterwellEngine.RecognizeMS(database.Database, input);

                var objIds = new ObjectIdCollection(); // Print
                foreach (var element in waterwellEngine.Datas)
                {
                    ThWaterWellModel waterWell = ThWaterWellModel.Create(element);
                    waterWell.InitWellData();
                    waterWell.CheckHavePumpIndex(pumpIndex, pumpDict);
                    waterWellList.Add(waterWell);
                }
            }
            return waterWellList;
        }



        public override void SubExecute()
        {
            Common.Utils.FocusMainWindow();
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                var input = Common.Utils.SelectAreas();

                if (input.Count == 0)
                {
                    WaterWellList = new List<ThWaterWellModel>();
                    return;
                }


                //获取集水井
                WaterWellList = GetWaterWellEntityList(input);
            }

        }
    }
}
