using ThControlLibraryWPF.ControlUtils;
using ThMEPStructure.Reinforcement.Model;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class ThWallColumnReinforceSetModel : NotifyPropertyChangedBase
    {
        private string concreteStrengthGrade = "";
        /// <summary>
        /// 砼强度等级
        /// </summary>
        public string ConcreteStrengthGrade
        {
            get
            {
                return concreteStrengthGrade;
            }
            set
            {
                concreteStrengthGrade = value;
                RaisePropertyChanged("ConcreteStrengthGrade");
            }
        }
        private string antiSeismicGrade = "";
        /// <summary>
        /// 抗震等级
        /// </summary>
        public string AntiSeismicGrade
        {
            get
            {
                return antiSeismicGrade;
            }

            set
            {
                antiSeismicGrade = value;
                RaisePropertyChanged("AntiSeismicGrade");
            }
        }
        private double c;
        /// <summary>
        /// 保护层厚度
        /// </summary>
        public double C
        {
            get
            {
                return c;
            }
            set
            {
                c = value;
                RaisePropertyChanged("C");
            }
        }

        private string frame = "";
        /// <summary>
        /// 自适应柱表
        /// 取值为：A0、A1、A2...
        /// </summary>
        public string Frame
        {
            get
            {
                return frame;
            }
            set
            {
                frame = value;
                RaisePropertyChanged("Frame");
            }
        }

        private double tableRowHeight;
        /// <summary>
        /// 字符行高
        /// </summary>
        public double TableRowHeight
        {
            get
            {
                return tableRowHeight;
            }
            set
            {
                tableRowHeight = value;
                RaisePropertyChanged("TableRowHeight");
            }
        }
        private string elevation = "";
        /// <summary>
        /// 墙柱标高
        /// </summary>
        public string Elevation
        {
            get
            {
                return elevation;
            }
            set
            {
                elevation = value;
                RaisePropertyChanged("Elevation");
            }
        }
        private string drawScale = "";
        /// <summary>
        /// 绘图比例
        /// </summary>
        public string DrawScale
        {
            get
            {
                return drawScale;
            }
            set
            { 
                drawScale = value;
                RaisePropertyChanged("DrawScale");
            }
        }
        private double pointReinforceLineWeight;
        /// <summary>
        /// 点筋线宽
        /// </summary>
        public double PointReinforceLineWeight
        {
            get
            {
                return pointReinforceLineWeight;
            }
            set
            {
                pointReinforceLineWeight = value;
                RaisePropertyChanged("PointReinforceLineWeight");
            }
        }
        private double stirrupLineWeight;
        /// <summary>
        /// 箍线线宽
        /// </summary>
        public double StirrupLineWeight 
        { 
            get 
            {
                return stirrupLineWeight;
            } 
            set 
            {
                stirrupLineWeight= value;
                RaisePropertyChanged("StirrupLineWeight");
            } 
        }
        private string wallLocation = "";
        /// <summary>
        /// 构造筋区域(底部加强筋，其它部位)
        /// </summary>
        public string WallLocation
        {
            get
            {
                return wallLocation;
            }
            set
            {
                wallLocation = value;
                RaisePropertyChanged("WallLocation");
            }
        }
        public ThWallColumnReinforceSetModel()
        {
            Init();
        }
        private void Init()
        {
            C = ThWallColumnReinforceConfig.Instance.C;
            Frame = ThWallColumnReinforceConfig.Instance.Frame;
            WallLocation = ThWallColumnReinforceConfig.Instance.WallLocation;
            DrawScale = ThWallColumnReinforceConfig.Instance.DrawScale;
            Elevation = ThWallColumnReinforceConfig.Instance.Elevation;
            TableRowHeight = ThWallColumnReinforceConfig.Instance.TableRowHeight;
            AntiSeismicGrade = ThWallColumnReinforceConfig.Instance.AntiSeismicGrade;
            StirrupLineWeight = ThWallColumnReinforceConfig.Instance.StirrupLineWeight;
            ConcreteStrengthGrade = ThWallColumnReinforceConfig.Instance.ConcreteStrengthGrade;            
            PointReinforceLineWeight = ThWallColumnReinforceConfig.Instance.PointReinforceLineWeight;
        }
        public void Reset()
        {
            ThWallColumnReinforceConfig.Instance.Reset();
            Init();
        }
        public void Set()
        {
            ThWallColumnReinforceConfig.Instance.C = C;
            ThWallColumnReinforceConfig.Instance.Frame = Frame;
            ThWallColumnReinforceConfig.Instance.WallLocation = WallLocation;
            ThWallColumnReinforceConfig.Instance.DrawScale = DrawScale;
            ThWallColumnReinforceConfig.Instance.Elevation = Elevation;
            ThWallColumnReinforceConfig.Instance.TableRowHeight = TableRowHeight;
            ThWallColumnReinforceConfig.Instance.AntiSeismicGrade = AntiSeismicGrade;
            ThWallColumnReinforceConfig.Instance.StirrupLineWeight = StirrupLineWeight;
            ThWallColumnReinforceConfig.Instance.ConcreteStrengthGrade = ConcreteStrengthGrade;
            ThWallColumnReinforceConfig.Instance.PointReinforceLineWeight = PointReinforceLineWeight;
        }
    }
}
