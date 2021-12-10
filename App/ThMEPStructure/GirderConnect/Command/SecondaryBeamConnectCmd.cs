using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Command;
using ThMEPStructure.GirderConnect.SecondaryBeamConnect.Service;

namespace ThMEPStructure.GirderConnect.Command
{
    public class SecondaryBeamConnectCmd : ThMEPBaseCommand, IDisposable
    {
        public SecondaryBeamConnectCmd()
        {
            ActionName = "生成次梁";
            CommandName = "THSCCL";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using(AcadDatabase acad=AcadDatabase.Active())
            {
                var ordinaryBeam = acad.ModelSpace
                .OfType<Line>()
                //.Where(o => o.Layer == "xk-ceshi" && o.ColorIndex !=90)
                .Where(o => o.Layer == "TH_AI_BEAM")
                .ToList();

                var OutLineBeam = acad.ModelSpace
                .OfType<Line>()
                //.Where(o => o.Layer == "xk-ceshi" && o.ColorIndex !=90)
                .Where(o => o.Layer == "TH_AI_WALLBOUND")
                .ToList();
                ordinaryBeam = ordinaryBeam.Union(OutLineBeam).ToList();

                var brinkBeam = acad.ModelSpace
                .OfType<Line>()
                //.Where(o => o.Layer == "xk-ceshi" && o.ColorIndex == 90)
                .Where(o => o.Layer == "TH_AI_HOUSEBOUND")
                .ToList();

                ConnectSecondaryBeamService.ConnectSecondaryBeam(ordinaryBeam, brinkBeam);
            }
        }
    }
}
