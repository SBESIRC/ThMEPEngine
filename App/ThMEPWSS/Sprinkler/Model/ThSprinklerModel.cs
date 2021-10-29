using System.Linq;
using System.ComponentModel;
using ThMEPWSS.Sprinkler.Analysis;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ThMEPWSS.Sprinkler.Model
{
    public class ThSprinklerModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> DangerGrades { get; set; }
        public Dictionary<string, List<string>> BlockNameDict { get; set; }
        public ThSprinklerModel()
        {
            checkItem1 = true;
            checkItem2 = true;
            checkItem3 = true;
            checkItem4 = true;
            checkItem6 = true;
            checkItem7 = true;
            checkItem8 = true;
            checkItem9 = true;
            checkItem10 = true;
            checkItem12 = true;
            areaDensity = 2400;
            aboveBeamHeight = 900;
            checkSprinklerType = SprinklerType.Up;
            sprinklerRange = SprinklerRange.Standard;
            var items = DangerGradeDataManager.Datas.Select(o => o.DangerGrade).Distinct();
            DangerGrades = new ObservableCollection<string>(items);
            dangerGrade = items.ToList()[2];
            BlockNameDict = new Dictionary<string, List<string>>();
        }

        private SprinklerType checkSprinklerType;
        /// <summary>
        /// 校核喷头类型
        /// </summary>
        public SprinklerType CheckSprinklerType
        {
            get
            {
                return checkSprinklerType;
            }
            set
            {
                checkSprinklerType = value;
                RaisePropertyChanged("CheckSprinklerType");
            }
        }

        private string dangerGrade = "";
        /// <summary>
        /// 保护强度->双股
        /// </summary>
        public string DangerGrade
        {
            get
            {
                return dangerGrade;
            }
            set
            {
                dangerGrade = value;
                RaisePropertyChanged("DangerGrade");
            }
        }

        private SprinklerRange sprinklerRange;
        public SprinklerRange SprinklerRange
        {
            get
            {
                return sprinklerRange;
            }
            set
            {
                sprinklerRange = value;
                RaisePropertyChanged("SprinklerRange");
            }
        }

        #region ---------- 校核项目 ----------
        private bool checkItem1;
        /// <summary>
        /// 盲区检测
        /// </summary>
        public bool CheckItem1
        {
            get
            {
                return checkItem1;
            }
            set
            {
                checkItem1 = value;
                RaisePropertyChanged("CheckItem1");
            }
        }

        private bool checkItem2;
        /// <summary>
        /// 喷头距边过大
        /// </summary>
        public bool CheckItem2
        {
            get
            {
                return checkItem2;
            }
            set
            {
                checkItem2 = value;
                RaisePropertyChanged("CheckItem2");
            }
        }

        private bool checkItem3;
        /// <summary>
        /// 房间是否布置喷头
        /// </summary>
        public bool CheckItem3
        {
            get
            {
                return checkItem3;
            }
            set
            {
                checkItem3 = value;
                RaisePropertyChanged("CheckItem3");
            }
        }

        private bool checkItem4;
        /// <summary>
        /// 车位上方喷头
        /// </summary>
        public bool CheckItem4
        {
            get
            {
                return checkItem4;
            }
            set
            {
                checkItem4 = value;
                RaisePropertyChanged("CheckItem4");
            }
        }

        private bool checkItem5;
        /// <summary>
        /// 机械车位侧喷
        /// </summary>
        public bool CheckItem5
        {
            get
            {
                return checkItem5;
            }
            set
            {
                checkItem5 = value;
                RaisePropertyChanged("CheckItem5");
            }
        }

        private bool checkItem6;
        /// <summary>
        /// 喷头间距是否过小
        /// </summary>
        public bool CheckItem6
        {
            get
            {
                return checkItem6;
            }
            set
            {
                checkItem6 = value;
                RaisePropertyChanged("CheckItem6");
            }
        }

        private bool checkItem7;
        /// <summary>
        /// 喷头间距是否过大
        /// </summary>
        public bool CheckItem7
        {
            get
            {
                return checkItem7;
            }
            set
            {
                checkItem7 = value;
                RaisePropertyChanged("CheckItem7");
            }
        }

        private bool checkItem8;
        /// <summary>
        /// 喷头距梁是否过小
        /// </summary>
        public bool CheckItem8
        {
            get
            {
                return checkItem8;
            }
            set
            {
                checkItem8 = value;
                RaisePropertyChanged("CheckItem8");
            }
        }

        private bool checkItem9;
        /// <summary>
        /// 高于 xxx mm的梁
        /// </summary>
        public bool CheckItem9
        {
            get
            {
                return checkItem9;
            }
            set
            {
                checkItem9 = value;
                RaisePropertyChanged("CheckItem9");
            }
        }

        private int aboveBeamHeight;
        /// <summary>
        /// 高于多少的的梁的具体数值
        /// </summary>
        public int AboveBeamHeight
        {
            get
            {
                return aboveBeamHeight;
            }
            set
            {
                aboveBeamHeight = value;
                RaisePropertyChanged("AboveBeamHeight");
            }
        }

        private bool checkItem10;
        /// <summary>
        /// 喷头是否连管
        /// </summary>
        public bool CheckItem10
        {
            get
            {
                return checkItem10;
            }
            set
            {
                checkItem10 = value;
                RaisePropertyChanged("CheckItem10");
            }
        }

        private bool checkItem11;
        /// <summary>
        /// 宽度大于1200的风管
        /// </summary>
        public bool CheckItem11
        {
            get
            {
                return checkItem11;
            }
            set
            {
                checkItem11 = value;
                RaisePropertyChanged("CheckItem11");
            }
        }

        private bool checkItem12;
        /// <summary>
        /// 区域喷头过密
        /// </summary>
        public bool CheckItem12
        {
            get
            {
                return checkItem12;
            }
            set
            {
                checkItem12 = value;
                RaisePropertyChanged("CheckItem12");
            }
        }

        private int areaDensity;
        /// <summary>
        /// 喷头间距阈值
        /// </summary>
        public int AreaDensity
        {
            get
            {
                return areaDensity;
            }
            set
            {
                areaDensity = value;
                RaisePropertyChanged("AreaDensity");
            }
        }
        #endregion
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        } 
    }

    /// <summary>
    /// 检查对象
    /// </summary>
    public enum SprinklerType
    {
        /// <summary>
        /// 上喷
        /// </summary>
        Up = 0,
        /// <summary>
        /// 下喷
        /// </summary>
        Down = 1,
        /// <summary>
        /// 侧喷
        /// </summary>
        Side =2 ,
    }
    /// <summary>
    /// 最大保护距离
    /// </summary>
    public enum SprinklerRange
    {
        /// <summary>
        /// 标准覆盖
        /// </summary>
        Standard = 0,
        /// <summary>
        /// 扩大覆盖
        /// </summary>
        Enlarge = 1,
    }
}
