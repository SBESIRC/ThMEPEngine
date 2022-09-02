using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThMEPTCH.Services;

namespace TianHua.Platform3D.UI.ViewModels
{
    public class InsertStoreyVM : INotifyPropertyChanged
    {
        public ObservableCollection<string> StoreyTypes { get; set; }
        /// <summary>
        /// 用户选择的插入位置集合
        /// </summary>
        public ObservableCollection<ThEditStoreyInfo> InsertStoreys { get; set; }
        private const string BelowStoreyMark = "地下层";
        private const string UpperStoreyMark = "地上楼层";
        private const string RoofStoreyMark = "屋顶层";
        // 与EditStoreyVM保持一致
        private const string BelowStoreyPrefix = "B";
        private const string RoofStoreyPrefix = "R";

        private Dictionary<string, List<ThEditStoreyInfo>> _storeyDict; // 记录地下、地上、屋顶对应楼层名
        private Dictionary<string, string> _storeyTypeDict; // 记录地下、地上、屋顶对应楼层名的前缀

        public InsertStoreyVM(List<ThEditStoreyInfo> belowStoreys,List<ThEditStoreyInfo> upperStoreys,List<ThEditStoreyInfo> roofStores,
            ThEditStoreyInfo insertStorey)
        {
            _insertStorey = insertStorey; // 用户选择的楼层名
            _storeyDict = new Dictionary<string, List<ThEditStoreyInfo>>();
            StoreyTypes = new ObservableCollection<string>();
            InsertStoreys = new ObservableCollection<ThEditStoreyInfo>();

            // build _storeyTypeDict
            _storeyTypeDict = new Dictionary<string, string>();
            _storeyTypeDict.Add(BelowStoreyMark, BelowStoreyPrefix);
            _storeyTypeDict.Add(UpperStoreyMark, ""); // 地上层楼层名没有前缀
            _storeyTypeDict.Add(RoofStoreyMark, RoofStoreyPrefix);

            // build _storeyDict
            _storeyDict.Add(BelowStoreyMark, belowStoreys);
            _storeyDict.Add(UpperStoreyMark, upperStoreys);
            _storeyDict.Add(RoofStoreyMark, roofStores);
            
            // 初始化 StoreyTypes
            StoreyTypes = new ObservableCollection<string>(_storeyTypeDict.Keys.ToList());
            if (_insertStorey.StoreyName.StartsWith(BelowStoreyPrefix))
            {
                _storeyType = BelowStoreyMark;
                InsertStoreys = new ObservableCollection<ThEditStoreyInfo>(GetInsertStoreys(BelowStoreyMark));
            }
            else if (_insertStorey.StoreyName.StartsWith("R"))
            {
                _storeyType = RoofStoreyMark;
                InsertStoreys = new ObservableCollection<ThEditStoreyInfo>(GetInsertStoreys(RoofStoreyMark));
            }
            else
            {
                _storeyType = UpperStoreyMark;
                InsertStoreys = new ObservableCollection<ThEditStoreyInfo>(GetInsertStoreys(UpperStoreyMark));
            }
        }

        public string Prefix
        {
            get
            {
                return _storeyTypeDict.ContainsKey(_storeyType) ? _storeyTypeDict[_storeyType] : "";
            }
        }

        private string _storeyType = "";
        public string StoreyType
        {
            get => _storeyType;
            set
            {
                _storeyType = value;
                RaisePropertyChanged("StoreyType");
            }
        }

        private ThEditStoreyInfo _insertStorey;
        /// <summary>
        /// 插入基准位置(此层不动)
        /// </summary>
        public ThEditStoreyInfo InsertStorey
        {
            get => _insertStorey;
            set
            {
                _insertStorey = value;
                RaisePropertyChanged("InsertStorey");
            }
        }

        private int _count =1;
        /// <summary>
        /// 层数
        /// </summary>
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                RaisePropertyChanged("Count");
            }
        }

        private int _height;
        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                RaisePropertyChanged("Height");
            }
        }

        public string Confirm()
        {
            if (_count<=0)
            {
                return "楼层数请输入正整数！";
            }
            else
            {
                return "";
            }
        }

        public List<ThEditStoreyInfo> BuildFinalStoreys()
        {
            // 执行插入
            var results = new List<ThEditStoreyInfo>();
            switch (StoreyType)
            {
                case BelowStoreyMark:
                    var newBelowStoreys = InsertBelowStoreys();
                    results.AddRange(newBelowStoreys);
                    results.AddRange(GetInsertStoreys(UpperStoreyMark));
                    results.AddRange(GetInsertStoreys(RoofStoreyMark));
                    break;
                case UpperStoreyMark:
                    var newUpperStoreys = InsertUpperStoreys();
                    results.AddRange(GetInsertStoreys(BelowStoreyMark));
                    results.AddRange(newUpperStoreys);
                    results.AddRange(GetInsertStoreys(RoofStoreyMark));
                    break;
                case RoofStoreyMark:
                    var newRoofStoreys = InsertRoofStoreys();
                    results.AddRange(GetInsertStoreys(BelowStoreyMark));
                    results.AddRange(GetInsertStoreys(UpperStoreyMark));
                    results.AddRange(newRoofStoreys);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return results;
        }

        public void UpdateInsertStoreys(string storeyType)
        {
            this._storeyType = storeyType;
            var belowStoreys = GetInsertStoreys(BelowStoreyMark);
            var upperStoreys = GetInsertStoreys(UpperStoreyMark);
            var roofStoreys = GetInsertStoreys(RoofStoreyMark);
            if (_storeyType == BelowStoreyMark) // 楼层类型选择地下楼层
            {
                //if (belowStoreys.Count == 0)
                //{
                //    // 如果地下层数量为空，则选取地上层第一个作为插入位置
                //    if (upperStoreys.Count > 0)
                //    {
                //        belowStoreys.Add(upperStoreys.First());
                //    }
                //    else if (roofStoreys.Count > 0)
                //    {
                //        belowStoreys.Add(roofStoreys.First());
                //    }
                //}
                InsertStoreys = new ObservableCollection<ThEditStoreyInfo>(belowStoreys);
                InsertStorey = UpdateInsertStorey(belowStoreys, _insertStorey, false);           
            }
            else if(_storeyType == UpperStoreyMark)  // 楼层类型选择地上楼层
            {               
                // 当插入地上层，把地下的最后一层显示出来，如"1F"
                //if(upperStoreys.Count==0)
                //{
                //    if(belowStoreys.Count>0)
                //    {
                //        upperStoreys.Insert(0, belowStoreys.Last());
                //    }
                //}                
                InsertStoreys = new ObservableCollection<ThEditStoreyInfo>(upperStoreys);
                InsertStorey =UpdateInsertStorey(upperStoreys, _insertStorey);                
            }
            else if(_storeyType == RoofStoreyMark)
            {
                // 楼层类型选择屋顶层
                // 把其下层的最后一层显示出来              
                InsertStoreys = new ObservableCollection<ThEditStoreyInfo>(roofStoreys);
                InsertStorey = UpdateInsertStorey(roofStoreys, _insertStorey);
            }
        }

        private ThEditStoreyInfo UpdateInsertStorey(List<ThEditStoreyInfo> editInfos,ThEditStoreyInfo insertStorey,bool isFirstDefault =true)
        {
            if (editInfos.Count > 0)
            {
                var res = editInfos.Where(o => insertStorey != null && o.Id == insertStorey.Id);
                if (res.Count() == 1)
                {
                    return editInfos[editInfos.IndexOf(res.First())];
                }
                else
                {
                    return isFirstDefault ? editInfos.First() : editInfos.Last();
                }
            }
            else
            {
                return  null;
            }
        }
       

        private List<ThEditStoreyInfo> InsertBelowStoreys()
        {                    
            var storyes = GetInsertStoreys(_storeyType);// 获取地下层数
            var calculatedStoreys = CalculateInsertStoreys(); // 插入楼层,B1F,B2F...
            calculatedStoreys.Reverse();
            if (calculatedStoreys.Count == 0)
            {
                return storyes;
            }            
            if(storyes.Count==0)
            {                
                return calculatedStoreys;
            }
            else
            {
                var results = new List<ThEditStoreyInfo>();
                // _insertStorey一定有值
                var index = storyes.IndexOf(_insertStorey);
                bool isBelowContinous = index == -1? IsBelowContinuous(storyes, 0, true): IsBelowContinuous(storyes, index, true);
                if(isBelowContinous)
                {
                    if(index ==-1)
                    {
                        // 表示所选项是地上楼层第一层
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            var currentStoreyNo = GetBelowStoreyIndex(storyes[i].StoreyName);
                            currentStoreyNo += calculatedStoreys.Count;
                            storyes[i].StoreyName = BelowStoreyPrefix + currentStoreyNo + "F";
                            results.Add(storyes[i]);
                        }
                        results.AddRange(calculatedStoreys);
                    }
                    else
                    {
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            if (i < index)
                            {
                                var currentStoreyNo = GetBelowStoreyIndex(storyes[i].StoreyName);
                                currentStoreyNo += calculatedStoreys.Count;
                                storyes[i].StoreyName = BelowStoreyPrefix + currentStoreyNo + "F";
                                results.Add(storyes[i]);
                            }
                            else if (i == index)
                            {
                                results.AddRange(calculatedStoreys);
                                results.Add(storyes[i]);
                            }
                            else
                            {
                                results.Add(storyes[i]);
                            }
                        }
                    }
                }
                else
                {
                    if (index == -1)
                    {
                        // 表示所选项是上层楼层第一层
                        results.AddRange(storyes);
                        results.AddRange(calculatedStoreys);
                    }
                    else
                    {
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            if (i == index)
                            {
                                results.AddRange(calculatedStoreys);
                                results.Add(storyes[i]);
                            }
                            else
                            {
                                results.Add(storyes[i]);
                            }
                        }
                    }
                }
                return results;
            }
        }

        private List<ThEditStoreyInfo> InsertUpperStoreys()
        {
            var calculatedStoreys = CalculateInsertStoreys(); // 插入楼层            
            var storyes = GetInsertStoreys(_storeyType); // 获取地下层数
            if (calculatedStoreys.Count == 0)
            {
                return storyes;
            }            
            if (storyes.Count == 0)
            {
                return calculatedStoreys;
            }
            else
            {
                var results = new List<ThEditStoreyInfo>();                
                var index = storyes.IndexOf(_insertStorey); // _insertStorey一定有值
                bool isUpperContinous = index == -1 ? IsUpperContinuous(storyes, 0, false,GetUpperStoreyIndex) :
                    IsUpperContinuous(storyes, index, false, GetUpperStoreyIndex);
                if (isUpperContinous)
                {
                    if (index == -1)
                    {
                        // 表示所选项是地下楼层最上层
                        results.AddRange(calculatedStoreys);
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            var currentStoreyNo = GetUpperStoreyIndex(storyes[i].StoreyName);
                            currentStoreyNo += calculatedStoreys.Count;
                            storyes[i].StoreyName = currentStoreyNo + "F";
                            results.Add(storyes[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            if (i < index)
                            {
                                results.Add(storyes[i]);
                            }
                            else if (i == index)
                            {
                                results.Add(storyes[i]);
                                results.AddRange(calculatedStoreys);                                
                            }
                            else
                            {
                                var currentStoreyNo = GetUpperStoreyIndex(storyes[i].StoreyName);
                                currentStoreyNo += calculatedStoreys.Count;
                                storyes[i].StoreyName = currentStoreyNo + "F";
                                results.Add(storyes[i]);
                            }
                        }
                    }
                }
                else
                {
                    if (index == -1)
                    {
                        // 表示所选项是上层楼层第一层
                        results.AddRange(calculatedStoreys);
                        results.AddRange(storyes);
                    }
                    else
                    {
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            if (i == index)
                            {                                
                                results.Add(storyes[i]);
                                results.AddRange(calculatedStoreys);
                            }
                            else
                            {
                                results.Add(storyes[i]);
                            }
                        }
                    }
                }
                return results;
            }
        }

        private List<ThEditStoreyInfo> InsertRoofStoreys()
        {
            var calculatedStoreys = CalculateInsertStoreys(); // 插入楼层            
            var storyes = GetInsertStoreys(_storeyType); // 获取地下层数
            if (calculatedStoreys.Count == 0)
            {
                return storyes;
            }
            if (storyes.Count == 0)
            {
                return calculatedStoreys;
            }
            else
            {
                var results = new List<ThEditStoreyInfo>();
                var index = storyes.IndexOf(_insertStorey); // _insertStorey一定有值
                bool isUpperContinous = index == -1 ? IsUpperContinuous(storyes, 0, false, GetRoofStoreyIndex) :
                    IsUpperContinuous(storyes, index, false, GetRoofStoreyIndex);
                if (isUpperContinous)
                {
                    if (index == -1)
                    {
                        // 表示所选项是地上楼层最上层
                        results.AddRange(calculatedStoreys);
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            var currentStoreyNo = GetRoofStoreyIndex(storyes[i].StoreyName);
                            currentStoreyNo += calculatedStoreys.Count;
                            storyes[i].StoreyName =RoofStoreyPrefix + currentStoreyNo + "F";
                            results.Add(storyes[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            if (i < index)
                            {
                                results.Add(storyes[i]);
                            }
                            else if (i == index)
                            {
                                results.Add(storyes[i]);
                                results.AddRange(calculatedStoreys);                                
                            }
                            else
                            {
                                var currentStoreyNo = GetRoofStoreyIndex(storyes[i].StoreyName);
                                currentStoreyNo += calculatedStoreys.Count;
                                storyes[i].StoreyName = RoofStoreyPrefix + currentStoreyNo + "F";
                                results.Add(storyes[i]);
                            }
                        }
                    }
                }
                else
                {
                    if (index == -1)
                    {
                        // 表示所选项是地上楼层最上层
                        results.AddRange(calculatedStoreys);
                        results.AddRange(storyes);
                    }
                    else
                    {
                        for (int i = 0; i < storyes.Count; i++)
                        {
                            if (i == index)
                            {
                                results.Add(storyes[i]);
                                results.AddRange(calculatedStoreys);
                            }
                            else
                            {
                                results.Add(storyes[i]);
                            }
                        }
                    }
                }
                return results;
            }
        }


        private bool IsBelowContinuous(List<ThEditStoreyInfo> queues, int index,bool isPrev)
        {
            //B4F,B3F,B2F,B1F
            //<------- -------->
            //   prev    next
            if(isPrev)
            {
                for (int i = index; i > 0; i--)
                {
                    var currentIndex = GetBelowStoreyIndex(queues[i].StoreyName);
                    var prevIndex = GetBelowStoreyIndex(queues[i - 1].StoreyName);
                    if (prevIndex - currentIndex != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                for (int i = index; i < queues.Count - 1; i++)
                {
                    var currentIndex = GetBelowStoreyIndex(queues[i].StoreyName);
                    var nextIndex = GetBelowStoreyIndex(queues[i + 1].StoreyName);
                    if (currentIndex - nextIndex != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        private bool IsUpperContinuous(List<ThEditStoreyInfo> queues, int index, bool isPrev, Func<string, int> GetStoreyIndex)
        {
            //1F,2F,3F,4F
            //<------- -------->
            //   prev    next
            if (isPrev)
            {
                // 看之前的是否连续
                for (int i = index; i > 0; i--)
                {
                    var currentIndex = GetStoreyIndex(queues[i].StoreyName);
                    var prevIndex = GetStoreyIndex(queues[i - 1].StoreyName);
                    if (currentIndex - prevIndex != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                // 看之后的是否连续
                for (int i = index; i < queues.Count - 1; i++)
                {
                    var currentIndex = GetStoreyIndex(queues[i].StoreyName);
                    var nextIndex = GetStoreyIndex(queues[i + 1].StoreyName);
                    if (nextIndex - currentIndex != 1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        private List<ThEditStoreyInfo> CalculateInsertStoreys()
        {
            // 计算插入的层数，和插入顺序
            var results = new List<ThEditStoreyInfo>();
            // 如果选的是BF，则往上插，当前层动
            int startIndex = -1;
            var prefixStr = "";
            if (_storeyType == BelowStoreyMark)
            {
                prefixStr = BelowStoreyPrefix;
                if (_insertStorey == null)
                {
                    startIndex = 0;
                }
                else
                {
                    if (_insertStorey.StoreyName.StartsWith(BelowStoreyPrefix))
                    {
                        startIndex = GetBelowStoreyIndex(_insertStorey.StoreyName);
                    }
                    else
                    {
                        startIndex = 0;
                    }
                }
            }
            else if (_storeyType == RoofStoreyMark)
            {
                prefixStr = RoofStoreyPrefix;
                if (_insertStorey == null)
                {
                    startIndex = 0;
                }
                else
                {
                    if (_insertStorey.StoreyName.StartsWith(RoofStoreyPrefix))
                    {
                        startIndex = GetRoofStoreyIndex(_insertStorey.StoreyName);
                    }
                    else
                    {
                        startIndex = 0;
                    }
                }
            }
            else if (_storeyType == UpperStoreyMark)
            {
                if (_insertStorey == null)
                {
                    startIndex = 0;
                }
                else
                {
                    if (_insertStorey.StoreyName.StartsWith(BelowStoreyPrefix) ||
                        _insertStorey.StoreyName.StartsWith(RoofStoreyPrefix))
                    {
                        startIndex = 0;
                    }
                    else
                    {
                        startIndex = GetUpperStoreyIndex(_insertStorey.StoreyName);
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            if (startIndex != -1)
            {                
                for (int i = 1; i <= _count; i++)
                {
                    results.Add(new ThEditStoreyInfo() { StoreyName = prefixStr + (startIndex + i) + "F", Height = Height.ToString()});
                }
            }
            return results;
        }

        private int GetBelowStoreyIndex(string storeyName)
        {
            var numStr = storeyName.Substring(1, storeyName.Length - 2);
            return int.Parse(numStr);
        }
        private int GetRoofStoreyIndex(string storeyName)
        {
            var numStr = storeyName.Substring(1, storeyName.Length - 2);
            return int.Parse(numStr);
        }
        private int GetUpperStoreyIndex(string storeyName)
        {
            var numStr = storeyName.Substring(0, storeyName.Length - 1);
            return int.Parse(numStr);
        }
        private List<ThEditStoreyInfo> GetInsertStoreys(string storeyType)
        {
            if (_storeyDict.ContainsKey(storeyType))
            {
                return _storeyDict[storeyType];
            }
            else
            {
                return new List<ThEditStoreyInfo>();
            }
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
