using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;

using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;

namespace ThMEPWSS
{
    public partial class ThHydrantCmds
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THHydrantData", CommandFlags.Modal)]
        public void THHydrantData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectPts = ThSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThHydrantUtil.GetTransformer(selectPts);

                var BlockNameDict = new Dictionary<string, List<string>>() { { "非机械车位", new List<string>() { "car0" } } };
                var dataFactory = new ThHydrantLayoutDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = BlockNameDict,
                };
                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThHydrantLayoutDataQueryService()
                {
                    THCVerticalPipe = dataFactory.THCVerticalPipe,
                    BlkVerticalPipe = dataFactory.BlkVerticalPipe,
                    CVerticalPipe = dataFactory.CVerticalPipe,
                    Hydrant = dataFactory.Hydrant,
                    InputExtractors = dataFactory.Extractors,
                    Car = dataFactory .Car, 
                };

                dataQuery.ExtractData();
                dataQuery.Print();
                // dataQuery.Clean();


            }
        }
    }
}
