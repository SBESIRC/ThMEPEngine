using ThControlLibraryWPF.ControlUtils;
using ThMEPStructure.Reinforcement.Model;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class ColumnReinforceDrawModel : NotifyPropertyChangedBase
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
       
        private string dwgSource = "";
        public string DwgSource
        {
            get => dwgSource;
            set
            {
                dwgSource = value;
                RaisePropertyChanged("DwgSource");
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
        public ColumnReinforceDrawModel()
        {     
            size = ThColumnReinforceDrawConfig.Instance.Size;
            sortWay = ThColumnReinforceDrawConfig.Instance.SortWay;
            textLayer = ThColumnReinforceDrawConfig.Instance.TextLayer;            
            dwgSource = ThColumnReinforceDrawConfig.Instance.DwgSource;
            leaderType = ThColumnReinforceDrawConfig.Instance.LeaderType;            
            markPosition = ThColumnReinforceDrawConfig.Instance.MarkPosition;           
            wallColumnLayer = ThColumnReinforceDrawConfig.Instance.WallColumnLayer;
        }
        public void SetConfig()
        {
            ThColumnReinforceDrawConfig.Instance.Size = size;
            ThColumnReinforceDrawConfig.Instance.SortWay = sortWay;
            ThColumnReinforceDrawConfig.Instance.TextLayer = textLayer;
            ThColumnReinforceDrawConfig.Instance.DwgSource = dwgSource;
            ThColumnReinforceDrawConfig.Instance.LeaderType = leaderType;
            ThColumnReinforceDrawConfig.Instance.MarkPosition = markPosition;
            ThColumnReinforceDrawConfig.Instance.WallColumnLayer = wallColumnLayer;
        }
    }
}
