using ThMEPEngineCore.Model;

namespace TianHua.Platform3D.UI.Model
{
    public class ThEditStoreyInfo : ThIfcStoreyInfo
    {
        private string _paperName = "";
        /// <summary>
        /// 图纸名称
        /// </summary>
        public string PaperName
        {
            get => _paperName;
            set
            {
                _paperName = value;
            }
        }

        private bool _followStoreyPreview;
        /// <summary>
        /// 按楼层预览
        /// </summary>
        public bool FollowStoreyPreview
        {
            get => _followStoreyPreview;
            set
            {
                _followStoreyPreview = value;
            }
        }

        private double _jiacengHeight;
        /// <summary>
        /// 夹层高度
        /// </summary>
        public double JiacengHeight
        {
            get => _jiacengHeight;
            set
            {
                _jiacengHeight = value;
            }
        }

        private double _elecDropElevation;
        /// <summary>
        /// 机电降标高
        /// </summary>
        public double ElecDropElevation
        {
            get => _elecDropElevation;
            set
            {
                _elecDropElevation = value;
            }
        }
    }
}
