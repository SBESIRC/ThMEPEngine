using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.HuaRunPeiJin.Data.YJK
{
    public class ThYJKDataSetFactory
    {
        public List<string> TextLayers { get; set; }
        public List<string> WallLayers { get; set; }
        public List<string> WallColumnLayers { get; set; }
        public ThYJKDataSetFactory()
        {
            TextLayers = new List<string>();
            WallLayers = new List<string>();
            WallColumnLayers = new List<string>();
        }
        public void GetElements(Database database, Point3dCollection collection)
        {
            var wallColumns = GetWallColumns(database, collection);
            var walls = GetWallColumns(database, collection);
            var leaderMarks = GetLeaderMarks(database, collection);


        }

        private DBObjectCollection GetWallColumns(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractWallColumnService(WallColumnLayers);
            extractService.Extract(database, collection);
            return extractService.Elements;
        }
        private DBObjectCollection GetWalls(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractWallService(WallLayers);
            extractService.Extract(database, collection);
            return extractService.Elements;
        }
        private Tuple<Dictionary<string,DBObjectCollection>, Dictionary<string, DBObjectCollection>>
            GetLeaderMarks(Database database, Point3dCollection collection)
        {
            var extractService = new ThExtractLeaderMarkService(TextLayers);
            extractService.Extract(database, collection);
            return Tuple.Create(extractService.MarkLines, extractService.MarkTexts);
        }
    }
    public class EdgeComponentInfo
    {
        /// <summary>
        /// 编号
        /// eg. GBZ24,GBZ1
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 规格
        /// eg. 一字型: 1650x200,L型：200x800,200,300
        /// </summary>
        public string Spec { get; set; }
        /// <summary>
        /// 形状
        /// eg. 一形，L形，T形
        /// </summary>
        public string Shape { get; set; }
        /// <summary>
        /// 类型
        /// eg 标准，标准C,非标
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 类型代号，用于标识标准-A,标准-B,标准Cal-A,标准Cal-B
        /// 取值为: A 或 B
        /// </summary>
        public string TypeCode { get; set; }
        /// <summary>
        /// 配筋率
        /// </summary>
        public double ReinforceRatio { get; set; }
        /// <summary>
        /// 配箍率
        /// </summary>
        public double StirrupRatio { get; set; }
    }
}
