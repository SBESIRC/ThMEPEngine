using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageADPrivate.Data;
using ThMEPWSS.DrainageADPrivate.Service;
using ThMEPWSS.DrainageADPrivate.Model;

namespace ThMEPWSS.DrainageADPrivate.Engine
{
    internal class ThDrainageADEngine
    {
        public static void DrainageTransADEngine(ThDrainageADPDataPass dataPass)
        {
            //树
            var treeEngine = new ThDraingeTreeEngine(dataPass);
            treeEngine.BuildDraingeTree();

            //--管径计算--
            var dimEngine = new ThCalculateDimEngine(dataPass, treeEngine);
            dimEngine.CalculateDim();



            //------

            //--转换--
            //ThDrainageADEngine.TransformTopToAD(dataPass);

            //

            //
        }


        internal static void TransformTopToAD(ThDrainageADPDataPass dataPass)
        {
            var coolPipeTopView = dataPass.CoolPipeTopView;
            var vierticalPipe = dataPass.VerticalPipe;

            var transService = new TransformTopToADService();
            var transLine = coolPipeTopView.Select(x => transService.TransformLine(x)).ToList();
            var transVLine = vierticalPipe.Select(x => transService.TransformLine(x)).ToList();

            DrawUtils.ShowGeometry(transLine, "l0TranslineCool");
            DrawUtils.ShowGeometry(transVLine, "l0TVierticalPipe");
        }


    }
}
