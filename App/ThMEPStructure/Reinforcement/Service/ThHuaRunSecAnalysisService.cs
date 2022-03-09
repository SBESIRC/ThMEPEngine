using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.Reinforcement.Service
{
    internal abstract class ThHuaRunSecAnalysisService
    {
        #region ----------output-----------
        public bool IsStanard { get; protected set; } // 是否为标准
        public StandardType Type { get; protected set; } // A型 或 B型
        #endregion
        protected string Code { get; set; } //YBZ,GBZ
        protected string AntiSeismicGrade { get; set; }
        protected DBObjectCollection Walls { get; set; }
        protected double BufferLength = 1.0;        
        private ThCADCoreNTSSpatialIndex WallSpatialIndex { get; set; }
        public ThHuaRunSecAnalysisService(DBObjectCollection walls,string code,string antiSeismicGrade)
        {
            Code = code;
            Walls = walls;
            Type = StandardType.None;
            IsStanard = false;
            AntiSeismicGrade = antiSeismicGrade;
            WallSpatialIndex = new ThCADCoreNTSSpatialIndex(Walls);
        }        
        public abstract void Analysis(Polyline polyline);
        protected DBObjectCollection Query(Polyline outline)
        {
            return WallSpatialIndex.SelectCrossingPolygon(outline);
        }
    }
    internal class ThHuaRunRectSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public int Hc { get; private set; }
        public int Bw { get; private set; }
        public string Spec => Hc + "x" + Bw;
        public ThHuaRunRectSecAnalysisService(DBObjectCollection walls, string code, string antiSeismicGrade):
            base(walls, code, antiSeismicGrade)
        {
        }
        public override void Analysis(Polyline rectangle)
        {
            var specService = new ThRectangleSpecAnalysisService();
            specService.Analysis(rectangle);

            // 查询
            var specInfos = ThHuaRunDataManager.Instance.QueryRect(
                this.AntiSeismicGrade, this.Code, specService.L, specService.W);
            if(specInfos.Count>0)
            {
                IsStanard = true;
            }
            // 查找是否有连接的墙
            var objs = rectangle.Buffer(BufferLength);
            var linkWalls = new DBObjectCollection();
            if(objs.OfType<Polyline>().Count()>0)
            {
                linkWalls = Query(objs.OfType<Polyline>().OrderByDescending(p => p.Area).First());
            }
            else
            {
                linkWalls = Query(rectangle);
            }
            if(linkWalls.Count>0)
            {
                Type = StandardType.A;
            }
            Hc = specService.L;
            Bw= specService.W;
        }
    }
    internal class ThHuaRunLTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public int Hc1 { get; private set; }
        public int Bw { get; private set; }
        public int Hc2 { get; private set; }
        public int Bf { get; private set; }
        public string Spec => Hc1 + "x" + Bw + "," + Hc2 + "x" + Bf;
        public ThHuaRunLTypeSecAnalysisService(DBObjectCollection walls, string code, string antiSeismicGrade) :
            base(walls, code, antiSeismicGrade)
        {
        }
        public override void Analysis(Polyline lType)
        {
            var specService = new ThLTypeSpecAnalysisService();
            specService.Analysis(lType);

            // 查询 A 端
            var specInfos = ThHuaRunDataManager.Instance.QueryLType(
               this.AntiSeismicGrade, this.Code, specService.B, specService.D, specService.C, specService.A);
            if (specInfos.Count > 0)
            {
                IsStanard = true;
                var outline = specService.EdgeA.CreateRectangle(1.0);
                var linkWalls = Query(outline);
                if (linkWalls.Count > 0)
                {
                    Type = StandardType.A;
                }
                else
                {
                    Type = StandardType.B;
                }
                outline.Dispose();
                Hc1 = specService.B;
                Bw = specService.D;
                Hc2= specService.C;
                Bf = specService.A;
            }
            else
            {
                // 查询 D 端
                specInfos = ThHuaRunDataManager.Instance.QueryLType(
               this.AntiSeismicGrade, this.Code, specService.C, specService.A, specService.B, specService.D);
                if (specInfos.Count > 0)
                {
                    IsStanard = true;
                    var outline = specService.EdgeD.CreateRectangle(1.0);
                    var linkWalls = Query(outline);
                    if (linkWalls.Count > 0)
                    {
                        Type = StandardType.A;
                    }
                    else
                    {
                        Type = StandardType.B;
                    }
                    outline.Dispose();
                    Hc1 = specService.C;
                    Bw = specService.A;
                    Hc2 = specService.B;
                    Bf = specService.D;
                }
            }
        }
    }

    internal class ThHuaRunTTypeSecAnalysisService : ThHuaRunSecAnalysisService
    {
        public int Hc1 { get; private set; }
        public int Bw { get; private set; }
        public int Hc2 { get; private set; }
        public int Bf { get; private set; }
        public string Spec => Hc1 + "x" + Bw + "," + Hc2 + "x" + Bf;
        public ThHuaRunTTypeSecAnalysisService(DBObjectCollection walls, string code, string antiSeismicGrade):
            base(walls, code, antiSeismicGrade)
        {
        }
        public override void Analysis(Polyline tType)
        {
            var specService = new ThTTypeSpecAnalysisService();
            specService.Analysis(tType);

            var specInfos = ThHuaRunDataManager.Instance.QueryTType(
               this.AntiSeismicGrade, this.Code, specService.D, specService.B, specService.A, specService.E);
            if (specInfos.Count > 0)
            {
                IsStanard = true;
                var outline = specService.EdgeA.CreateRectangle(1.0);
                var linkWalls = Query(outline);
                if (linkWalls.Count > 0)
                {
                    Type = StandardType.A;
                }
                else
                {
                    Type = StandardType.B;
                }
                outline.Dispose();
                Hc1 = specService.D;
                Bw = specService.B;
                Hc2 = specService.A;
                Bf = specService.E;
            }
        }
    }
    internal enum StandardType
    {
        A,
        B,
        None
    }
}
