using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using ThMEPTCH.CAD;

namespace ThMEPIFC
{
    public class ThMEPIFCInternalCmds
    {
        [CommandMethod("TIANHUACAD", "THExtractTCH", CommandFlags.Modal)]
        public void THExtractTCH()
        {

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var engine = new ThTCHBuildingElementExtractionEngine();
                engine.Extract(acdb.Database);
                engine.ExtractFromMS(acdb.Database);
                engine.Results.ForEach(o =>
                {
                    acdb.ModelSpace.Add(o.Geometry);
                });
            }
        }
    }
}
