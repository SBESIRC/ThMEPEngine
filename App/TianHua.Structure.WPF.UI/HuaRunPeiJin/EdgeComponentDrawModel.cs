using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Structure.WPF.UI.HuaRunPeiJin
{
    internal class EdgeComponentDrawModel : NotifyPropertyChangedBase
    {
        private string wallColumnLayer = "";
        /// <summary>
        /// 墙柱图层
        /// </summary>

        public string WallColumnLayer
        {
            get => wallColumnLayer; 
            set
            {
                wallColumnLayer = value;
                RaisePropertyChanged("WallColumnLayer");
            }
        }
        private string textLayer = "";
        /// <summary>
        /// 文字图层
        /// </summary>
        public string TextLayer
        {
            get => textLayer; 
            set
            {
                textLayer = value;
                RaisePropertyChanged("TextLayer");
            }
        }
        private string wallLayer = "";
        /// <summary>
        /// 墙层
        /// </summary>
        public string WallLayer
        {
            get => wallLayer;
            set
            {
                wallLayer = value;
                RaisePropertyChanged("WallLayer");
            }
        }
        private string sortWay = "";
        /// <summary>
        /// 排序方式
        /// </summary>
        public string SortWay
        {
            get => sortWay; 
            set
            {
                sortWay = value;
                RaisePropertyChanged("SortWay");
            }
        }
        private string leaderType = "";
        /// <summary>
        /// 引线形式
        /// </summary>
        public string LeaderType
        {
            get => leaderType;            
            set
            {
                leaderType = value;
                RaisePropertyChanged("LeaderType");
            }
        }
        private string markPosition = "";
        /// <summary>
        /// 标准位置
        /// </summary>
        public string MarkPosition
        {
            get => markPosition;
            set
            {
                markPosition = value;
                RaisePropertyChanged("MarkPosition");
            }
        }
        private int size;
        /// <summary>
        /// 归并系数->尺寸
        /// </summary>
        public int Size
        {
            get => size; 
            set 
            { 
                size = value;
                RaisePropertyChanged("Size");
            }
        }
        private bool isConsiderWall;
        /// <summary>
        /// 归并系数->考虑墙体
        /// </summary>
        public bool IsConsiderWall
        {
            get => isConsiderWall; 
            set 
            { 
                isConsiderWall = value;
                RaisePropertyChanged("IsConsiderWall");
            }
        }
        private double reinforceRatio;
        /// <summary>
        /// 配筋率
        /// </summary>
        public double ReinforceRatio
        {
            get => reinforceRatio; 
            set 
            { 
                reinforceRatio = value;
                RaisePropertyChanged("ReinforceRatio");
            }
        }
        private double stirrupRatio;
        /// <summary>
        /// 配箍率
        /// </summary>
        public double StirrupRatio
        {
            get => stirrupRatio; 
            set 
            { 
                stirrupRatio = value;
                RaisePropertyChanged("StirrupRatio");
            }
        }
    }
}
