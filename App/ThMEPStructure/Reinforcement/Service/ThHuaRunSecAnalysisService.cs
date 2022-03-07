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
        public bool IsStanard { get; private set; } // 是否为标准
        public bool Type { get; set; } // A型 或 B型
        protected string Code { get; set; } //YBZ,GBZ
        protected string AntiSeismicGrade { get; set; }
        protected DBObjectCollection Walls { get; set; }
        public ThHuaRunSecAnalysisService(DBObjectCollection walls,string code,string antiSeismicGrade)
        {
            Code = code;
            Walls = walls;
            AntiSeismicGrade = antiSeismicGrade;
        }
        public string Spec { get; set; }
        public abstract void Analysis(Polyline polyline);
    }
    internal class ThHuaRunRectSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public ThHuaRunRectSecAnalysisService(DBObjectCollection walls, string code, string antiSeismicGrade):
            base(walls, code, antiSeismicGrade)
        {
        }
        public override void Analysis(Polyline rectangle)
        {
            var specService = new ThRectangleSpecAnalysisService();
            specService.Analysis(rectangle);
            Spec = specService.L + "x" + specService.W;
        }
    }
    internal class ThHuaRunLTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public ThHuaRunLTypeSecAnalysisService(DBObjectCollection walls, string code, string antiSeismicGrade) :
            base(walls, code, antiSeismicGrade)
        {
        }
        public override void Analysis(Polyline lType)
        {
            var specService = new ThLTypeSpecAnalysisService();
            specService.Analysis(lType);
        }
    }
    internal class ThHuaRunTTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public ThHuaRunTTypeSecAnalysisService(DBObjectCollection walls, string code, string antiSeismicGrade):
            base(walls, code, antiSeismicGrade)
        {
        }
        public override void Analysis(Polyline tType)
        {
            var specService = new ThTTypeSpecAnalysisService();
            specService.Analysis(tType);
        }
    }
}
