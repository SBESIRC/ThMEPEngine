using ThMEPLighting.Common;

namespace ThMEPLighting.ServiceModels
{
    /// <summary>
    /// 疏散指示灯的参数信息，这里用在界面和命令之间传递到命令之间
    /// </summary>
    public class ThEmgLightService
    {
        ThEmgLightService() 
        {
            //初始化默认值
            this.MaxLightSpace = 10000;
            this.HostLightMoveOffSet = 800;
            this.MaxDeleteDistance = 10000;
            this.MaxDeleteAngle = 30;
            this.IsHostingLight = false;
            this.BlockScale = (double)ThEnumBlockScale.DrawingScale1_100;
        }
        public static ThEmgLightService Instance = new ThEmgLightService();
        /// <summary>
        /// 灯块的绘制比例
        /// </summary>
        public double BlockScale { get; set; }
        public bool IsHostingLight { get; set; }
        /// <summary>
        /// 灯之间最大间距
        /// </summary>
        public double MaxLightSpace { get; set; }
        /// <summary>
        /// 吊灯偏移排布线的偏移值
        /// </summary>
        public double HostLightMoveOffSet { get; set; }
        /// <summary>
        /// 壁装时根据吊装删除壁装的灯具最大间距
        /// </summary>
        public double MaxDeleteDistance { get; set; }
        /// <summary>
        /// 壁装时根据吊装删除壁装与吊装的最大角度
        /// </summary>
        public double MaxDeleteAngle { get; set; }
    }
}
