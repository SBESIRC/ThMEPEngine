using System.Collections.Generic;
using System.ComponentModel;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class InitStoreyVM : INotifyPropertyChanged
    {
        /// <summary>
        /// 计算出的楼层
        /// </summary>
        public List<string> Storeys { get; private set; }
        private int _totalCount = 0; //用于检查总函数是否相同

        public InitStoreyVM()
        {
            Storeys = new List<string>();
        }
        public InitStoreyVM(int underGroundStoreyNumbers,int aboveGroundStoreyNumbers,int roofStoreyNumbers)
        {
            this._underGroundStoreyNumbers = underGroundStoreyNumbers;
            this._aboveGroundStoreyNumbers=aboveGroundStoreyNumbers;
            this._roofStoreyNumbers=roofStoreyNumbers;
            this._totalCount = GetTotalCount();
        }

        private int _underGroundStoreyNumbers;
        
        public int UnderGroundStoreyNumbers
        {
            get => _underGroundStoreyNumbers;
            set
            {
                _underGroundStoreyNumbers = value;
                RaisePropertyChanged("UnderGroundStoreyNumbers");
            }
        }

        private int _aboveGroundStoreyNumbers;
        public int AboveGroundStoreyNumbers
        {
            get => _aboveGroundStoreyNumbers;
            set
            {
                _aboveGroundStoreyNumbers = value;
                RaisePropertyChanged("AboveGroundStoreyNumbers");
            }
        }

        private int _roofStoreyNumbers;
        public int RoofStoreyNumbers
        {
            get => _roofStoreyNumbers;
            set
            {
                _roofStoreyNumbers = value;
                RaisePropertyChanged("RoofStoreyNumbers");
            }
        }

        public bool IsEqualToTotalCount => GetTotalCount() == _totalCount;
        
        public void Init()
        {
            this.Storeys = Calculate();
        }

        private int GetTotalCount()
        {
            return _aboveGroundStoreyNumbers + _underGroundStoreyNumbers + _roofStoreyNumbers;
        }

        private List<string> Calculate()
        {
            var results = new List<string>();
            if(_underGroundStoreyNumbers > 0)
            {
                for(int i= _underGroundStoreyNumbers; i>= 1;i--)
                {
                    results.Add("B" + i.ToString() + "F");
                }
            }
            if(_aboveGroundStoreyNumbers>0)
            {
                for(int i = 1;i<= _aboveGroundStoreyNumbers;i++)
                {
                    results.Add(i.ToString() + "F");
                }
            }
            if(_roofStoreyNumbers > 0 )
            {
                for (int i = 1; i <= _roofStoreyNumbers; i++)
                {
                    results.Add("R" + i.ToString() + "F");
                }
            }
            return results;
        }


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
}
