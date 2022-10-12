using System;
using System.IO;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Command;
using TianHua.FanSelection;
using TianHua.Hvac.UI.CAD;

namespace TianHua.Hvac.UI.Command
{
    public class ThHvacDrawFanParameterCmd : ThMEPBaseCommand, IDisposable
    {
        public ThHvacDrawFanParameterCmd()
        {
            CommandName = "THFJDW";
            ActionName = "风机工况线绘制";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //轴流单速
                string axialPath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Parameters);
                //轴流双速
                string axialDoublePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.AXIAL_Parameters_Double);
                //离心前倾双速
                string htfcFrontDoublePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters_Double);
                //离心前倾单速
                string htfcFrontSinglePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters);
                //离心后倾单速
                string htfcBackSinglePath = Path.Combine(ThCADCommon.SupportPath(), ThFanSelectionCommon.HTFC_Parameters_Single);

                var axialModel = new DrawAxialModelsInCAD(axialPath);
                var axialDoubleModel = new DrawAxialModelsInCAD(axialDoublePath);
                var htfcFrontDouble = new DrawHtfcModelsInCAD(htfcFrontDoublePath);
                var htfcFrontSingle = new DrawHtfcModelsInCAD(htfcFrontSinglePath);
                var htfcBackSingle = new DrawHtfcModelsInCAD(htfcBackSinglePath);

                axialModel.DrawInCAD();
                axialDoubleModel.DrawInCAD();
                htfcFrontDouble.DrawInCAD();
                htfcFrontSingle.DrawInCAD();
                htfcBackSingle.DrawInCAD();
            }
        }
    }
}
