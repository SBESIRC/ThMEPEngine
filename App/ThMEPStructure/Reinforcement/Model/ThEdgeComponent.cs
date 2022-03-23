using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
namespace ThMEPStructure.Reinforcement.Model
{
    public abstract class ThEdgeComponent
    {
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public float C { get; set; }
        /// <summary>
        /// 箍筋规格
        /// </summary>
        public string Stirrup { get; set; }
        /// <summary>
        /// 纵筋规格
        /// </summary>
        public string Reinforce { get; set; }
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade { get; set; }
        /// <summary>
        /// 类型
        /// 取值："A" or "B"
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 是否为计算型
        /// </summary>
        public bool IsCalculation { get; set; }
        /// <summary>
        /// 连接墙的位置说明
        /// </summary>
        public string LinkWallPos { get; set; }
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade { get; set; }
        /// <summary>
        /// 点筋宽度
        /// </summary>
        public double PointReinforceLineWeight { get; set; }
        /// <summary>
        /// 箍筋线宽
        /// </summary>
        public double StirrupLineWeight { get; set; }
        /// <summary>
        /// 迭代增大后的纵筋规格
        /// </summary>
        public string EnhancedReinforce { get; set; }
        /// <summary>
        /// 迭代步数
        /// </summary>
        public int X { get; set; }

        public abstract void InitAndCalTableSize(string elevation, double tblRowHeight, double scale,out double firstRowH,out double firstRowW);
        public abstract DBObjectCollection Draw(double firstRowH, double firstRowW, Point3d point);
    }
}
