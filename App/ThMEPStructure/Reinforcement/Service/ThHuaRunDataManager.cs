using System.Linq;
using System.Collections.Generic;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThHuaRunDataManager
    {
        private static readonly ThHuaRunDataManager instance = new ThHuaRunDataManager() { };
        static ThHuaRunDataManager() { }
        internal ThHuaRunDataManager() 
        {
            RectSpecInfos = GetRectSpecInfos();
            LTypeSpecInfos = GetLTypeSpecInfos();
            TTypeSpecInfos = GetTTypeSpecInfos();
        }
        public static ThHuaRunDataManager Instance { get { return instance; } }
        public List<ThHuaRunRectComponentSpecInfo> RectSpecInfos { get; private set; }
        public List<ThHuaRunLTypeComponentSpecInfo> LTypeSpecInfos { get; private set; }
        public List<ThHuaRunTTypeComponentSpecInfo> TTypeSpecInfos { get; private set; }
        public List<ThHuaRunRectComponentSpecInfo> QueryRect(string antiSeismicGrade,string code,int hc,int bw)
        {
            var upperCode = code.ToUpper();
            return RectSpecInfos.Where(
                o => o.AntiSeismicGrade == antiSeismicGrade &&
                o.Code == upperCode &&
                o.Hc == hc && 
                o.Bw == bw)
                .ToList();            
        }
        public List<ThHuaRunLTypeComponentSpecInfo> QueryLType(string antiSeismicGrade, string code, int hc1, int bw, int hc2, int bf)
        {
            var upperCode = code.ToUpper();
            return LTypeSpecInfos.Where(
                o => o.AntiSeismicGrade == antiSeismicGrade &&
                o.Code == upperCode &&
                o.Hc1 == hc1 &&
                o.Bw == bw &&
                 o.Hc2 == hc2 &&
                o.Bf == bf)
                .ToList();
        }
        public List<ThHuaRunTTypeComponentSpecInfo> QueryTType(string antiSeismicGrade, string code, int hc1, int bw, int hc2, int bf)
        {
            var upperCode = code.ToUpper();
            return TTypeSpecInfos.Where(
                o => o.AntiSeismicGrade == antiSeismicGrade &&
                o.Code == upperCode &&
                o.Hc1 == hc1 &&
                o.Bw == bw &&
                 o.Hc2 == hc2 &&
                o.Bf == bf)
                .ToList();
        }
        private List<ThHuaRunRectComponentSpecInfo> GetRectSpecInfos()
        {
            var results = new List<ThHuaRunRectComponentSpecInfo>();
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "一级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "一级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "一级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "一级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "一级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "一级", 400, 400));

            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "二级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "二级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "二级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "二级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "二级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "二级", 400, 400));

            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "三级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "三级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "三级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "三级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "三级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("YBZ", "三级", 400, 400));

            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "一级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "一级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "一级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "一级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "一级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "一级", 400, 400));

            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "二级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "二级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "二级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "二级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "二级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "二级", 400, 400));

            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "三级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "三级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "三级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "三级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "三级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "三级", 400, 400));

            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "四级", 400, 180));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "四级", 400, 200));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "四级", 400, 250));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "四级", 400, 300));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "四级", 400, 350));
            results.Add(new ThHuaRunRectComponentSpecInfo("GBZ", "四级", 400, 400));
            return results;
        }
        private List<ThHuaRunLTypeComponentSpecInfo> GetLTypeSpecInfos()
        {
            var results = new List<ThHuaRunLTypeComponentSpecInfo>();
            #region ---------- YBZ一级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 350, 350, 350, 350));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 350, 350, 350, 450));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 350, 350, 350, 550));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 400, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "一级", 400, 400, 400, 500));
            #endregion
            #region ---------- YBZ二级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 350, 350, 350, 350));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 350, 350, 350, 450));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 350, 350, 350, 550));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 400, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "二级", 400, 400, 400, 500));
            #endregion
            #region ---------- YBZ三级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 350, 350, 350, 350));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 350, 350, 350, 450));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 350, 350, 350, 550));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 400, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("YBZ", "三级", 400, 400, 400, 500));
            #endregion
            #region ---------- GBZ一级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 350, 350, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 350, 350, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 350, 350, 500));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 400, 400, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "一级", 300, 400, 400, 500));
            #endregion
            #region ---------- GBZ二级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 350, 350, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 350, 350, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 350, 350, 500));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 400, 400, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "二级", 300, 400, 400, 500));
            #endregion
            #region ---------- GBZ三级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 350, 350, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 350, 350, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 350, 350, 500));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 400, 400, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "三级", 300, 400, 400, 500));
            #endregion
            #region ---------- GBZ四级 ----------
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 500));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 600));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 350, 350, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 350, 350, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 350, 350, 500));

            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 400, 400, 300));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 400, 400, 400));
            results.Add(new ThHuaRunLTypeComponentSpecInfo("GBZ", "四级", 300, 400, 400, 500));
            #endregion
            return results; 
        }
        private List<ThHuaRunTTypeComponentSpecInfo> GetTTypeSpecInfos()
        {
            var results = new List<ThHuaRunTTypeComponentSpecInfo>();
            #region ---------- YBZ一级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 350, 350, 350, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 350, 350, 350, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 350, 350, 350, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 400, 400, 400, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 400, 400, 400, 1400));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "一级", 400, 400, 400, 1600));
            #endregion
            #region ---------- YBZ二级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 350, 350, 350, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 350, 350, 350, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 350, 350, 350, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 400, 400, 400, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 400, 400, 400, 1400));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "二级", 400, 400, 400, 1600));
            #endregion
            #region ---------- YBZ三级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 350, 350, 350, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 350, 350, 350, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 350, 350, 350, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 400, 400, 400, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 400, 400, 400, 1400));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("YBZ", "三级", 400, 400, 400, 1600));
            #endregion
            #region ---------- GBZ一级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 350, 350, 950));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 350, 350, 1150));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 400, 400, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "一级", 300, 400, 400, 1200));
            #endregion
            #region ---------- GBZ二级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 350, 350, 950));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 350, 350, 1150));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 400, 400, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "二级", 300, 400, 400, 1200));
            #endregion
            #region ---------- GBZ三级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 350, 350, 950));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 350, 350, 1150));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 400, 400, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "三级", 300, 400, 400, 1200));
            #endregion
            #region ---------- GBZ四级 ----------
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 780));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 980));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 1180));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 180, 1380));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 800));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 1200));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 200, 200, 1400));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 850));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 1050));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 1250));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 250, 250, 1450));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 900));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 1100));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 1300));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 300, 300, 1500));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 350, 350, 950));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 350, 350, 1150));

            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 400, 400, 1000));
            results.Add(new ThHuaRunTTypeComponentSpecInfo("GBZ", "四级", 300, 400, 400, 1200));
            #endregion
            return results;
        }
    } 
    internal abstract class ThHuaRunComponentSpecInfo
    {
        /// <summary>
        /// 约束边缘构件（YBZ）、构造边缘构件(GBZ)
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; }   
        public ThHuaRunComponentSpecInfo(string code,string antiSeismicGrade)
        {
            this.Code = code;
            this.AntiSeismicGrade = antiSeismicGrade;
        }
    }
    internal class ThHuaRunRectComponentSpecInfo: ThHuaRunComponentSpecInfo
    {
        public int Hc { get; set; }
        public int Bw { get; set; }
        public ThHuaRunRectComponentSpecInfo(string code, string antiSeismicGrade,int hc,int bw):base(code, antiSeismicGrade)
        {
            this.Hc = hc;
            this.Bw = bw;
        }
    }
    internal class ThHuaRunLTypeComponentSpecInfo : ThHuaRunComponentSpecInfo
    {
        public int Hc1 { get; set; }
        public int Bw { get; set; }
        public int Bf { get; set; }
        public int Hc2 { get; set; }
        public ThHuaRunLTypeComponentSpecInfo(string code, string antiSeismicGrade, int hc1, int bw, int bf, int hc2) 
            : base(code, antiSeismicGrade)
        {
            this.Hc1 = hc1;
            this.Bw = bw;
            this.Hc2 = hc2;
            this.Bf = bf;
        }
    }
    internal class ThHuaRunTTypeComponentSpecInfo : ThHuaRunComponentSpecInfo
    {
        public int Hc1 { get; set; }
        public int Bw { get; set; }
        public int Bf { get; set; }
        public int Hc2 { get; set; }
        public ThHuaRunTTypeComponentSpecInfo(string code, string antiSeismicGrade, int hc1, int bw, int bf, int hc2)
            : base(code, antiSeismicGrade)
        {
            this.Hc1 = hc1;
            this.Bw = bw;
            this.Hc2 = hc2;
            this.Bf = bf;
        }
    }
}
