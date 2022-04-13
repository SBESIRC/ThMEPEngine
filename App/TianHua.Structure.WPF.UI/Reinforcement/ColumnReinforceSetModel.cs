using ThControlLibraryWPF.ControlUtils;
using ThMEPStructure.Reinforcement.Model;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class ThColumnReinforceSetModel : NotifyPropertyChangedBase
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
        private bool isBiggerThanTwo = true;
        /// <summary>
        /// λ是否大于2
        /// </summary>
        public bool IsBiggerThanTwo
        {
            get
            {
                return isBiggerThanTwo;
            }
            set
            {
                isBiggerThanTwo = value;
                RaisePropertyChanged("IsBiggerThanTwo");
            }
        }
        public ThColumnReinforceSetModel()
        {
            Init();
        }
        private void Init()
        {
            C = ThColumnReinforceConfig.Instance.C;
            Frame = ThColumnReinforceConfig.Instance.Frame;            
            DrawScale = ThColumnReinforceConfig.Instance.DrawScale;
            Elevation = ThColumnReinforceConfig.Instance.Elevation;
            IsBiggerThanTwo = ThColumnReinforceConfig.Instance.IsBiggerThanTwo;
            TableRowHeight = ThColumnReinforceConfig.Instance.TableRowHeight;
            AntiSeismicGrade = ThColumnReinforceConfig.Instance.AntiSeismicGrade;
            StirrupLineWeight = ThColumnReinforceConfig.Instance.StirrupLineWeight;
            ConcreteStrengthGrade = ThColumnReinforceConfig.Instance.ConcreteStrengthGrade;            
            PointReinforceLineWeight = ThColumnReinforceConfig.Instance.PointReinforceLineWeight;
        }
        public void Reset()
        {
            ThColumnReinforceConfig.Instance.Reset();
            Init();
        }
        public void Set()
        {
            ThColumnReinforceConfig.Instance.C = C;
            ThColumnReinforceConfig.Instance.Frame = Frame;
            ThColumnReinforceConfig.Instance.IsBiggerThanTwo = IsBiggerThanTwo;
            ThColumnReinforceConfig.Instance.DrawScale = DrawScale;
            ThColumnReinforceConfig.Instance.Elevation = Elevation;
            ThColumnReinforceConfig.Instance.TableRowHeight = TableRowHeight;
            ThColumnReinforceConfig.Instance.AntiSeismicGrade = AntiSeismicGrade;
            ThColumnReinforceConfig.Instance.StirrupLineWeight = StirrupLineWeight;
            ThColumnReinforceConfig.Instance.ConcreteStrengthGrade = ConcreteStrengthGrade;
            ThColumnReinforceConfig.Instance.PointReinforceLineWeight = PointReinforceLineWeight;
        }
    }
}
