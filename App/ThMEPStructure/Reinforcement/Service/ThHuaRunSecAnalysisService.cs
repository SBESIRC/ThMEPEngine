using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.Reinforcement.Service
{
    internal abstract class ThHuaRunSecAnalysisService
    {
        public string Spec { get; set; }
        public abstract void Analysis(Polyline polyline);
    }
    internal class ThHuaRunRectSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public override void Analysis(Polyline rectangle)
        {
            var specService = new ThRectangleSpecAnalysisService();
            specService.Analysis(rectangle);
            Spec = specService.L + "x" + specService.W;
        }
    }
    internal class ThHuaRunLTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public override void Analysis(Polyline rectangle)
        {

        }
    }
    internal class ThHuaRunTTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public override void Analysis(Polyline rectangle)
        {

        }
    }
}
