using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.Pipe.Model
{
    public class DynamicObj : DynamicObject
    {
        Dictionary<string, object> dict = new Dictionary<string, object>();
        public bool HasValue(string key) => dict.ContainsKey(key);
        public object this[string key]
        {
            get
            {
                dict.TryGetValue(key, out object ret); return ret;
            }
            set
            {
                dict[key] = value;
            }
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            dict.TryGetValue(binder.Name, out result);
            return true;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            dict[binder.Name] = value;
            return true;
        }
    }
    public static class CadCache
    {
        public const string WaterGroupLock = "WaterGroupLock";
        public static readonly HashSet<string> Locks = new HashSet<string>();
        static readonly HashSet<Window> windows = new HashSet<Window>();
        public static void Register(Window w)
        {
            w.Closed += (s, e) => { windows.Remove(w); };
            windows.Add(w);
        }
        public static void ShowAllWindows()
        {
            foreach (var w in windows)
            {
                w.Show();
            }
        }
        public static void HideAllWindows()
        {
            foreach (var w in windows)
            {
                w.Hide();
            }
        }
        public static void CloseAllWindows()
        {
            foreach (var w in windows.ToList())
            {
                w.Close();
            }
        }
        public static string CurrentFile => Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument?.Database?.Filename?.ToUpper();
        public static readonly Dictionary<string, DynamicObj> Cache = new Dictionary<string, DynamicObj>();
        public static event Action<string, string> CacheChanged;
        public static DynamicObj TryGetValue(string file)
        {
            CadCache.Cache.TryGetValue(file, out DynamicObj o);
            return o;
        }
        public static void SetCache(string file, string key, object value)
        {
            Cache.TryGetValue(file, out DynamicObj o);
            if (o is null)
            {
                o = new DynamicObj();
                Cache[file] = o;
            }
            o[key] = value;
            CacheChanged?.Invoke(file, key);
        }

        static readonly Dictionary<string, Point3dCollection> rangeDict = new Dictionary<string, Point3dCollection>();
        public static void UpdateByRange(Point3dCollection range)
        {
            if (range == null) return;
            if (range.Count <= 1) return;
            var file = CurrentFile;
            if (file is null) return;
            rangeDict[file] = range;
        }
        public static Point3dCollection TryGetRange()
        {
            var file = CurrentFile;
            if (file is null) return null;
            rangeDict.TryGetValue(file, out Point3dCollection range);
            return range;
        }
    }
    public class StoreyContext
    {
        public List<StoreyInfo> StoreyInfos;
    }
    public class StoreyInfo
    {
        public ThMEPEngineCore.Model.Common.StoreyType StoreyType;
        public List<int> Numbers;
        public Point2d ContraPoint;
        public GRect Boundary;
    }
    public class SetHighlevelNozzleAndSemiPlatformNozzlesViewModel : NotifyPropertyChangedBase
    {
        public static readonly SetHighlevelNozzleAndSemiPlatformNozzlesViewModel Singleton = new SetHighlevelNozzleAndSemiPlatformNozzlesViewModel();
        static readonly string[] _PipeConnectionTypes = new string[] { "低位", "高位-板上", "高位-板下", };
        public class Item : NotifyPropertyChangedBase
        {
            int _PipeId;
            public int PipeId
            {
                get => _PipeId;
                set
                {
                    if (value != _PipeId)
                    {
                        _PipeId = value;
                        OnPropertyChanged(nameof(PipeId));
                    }
                }
            }
            bool _HasFireHydrant;
            public bool HasFireHydrant
            {
                get => _HasFireHydrant;
                set
                {
                    if (value != _HasFireHydrant)
                    {
                        _HasFireHydrant = value; OnPropertyChanged(nameof(HasFireHydrant));
                    }
                }
            }
            public IEnumerable<string> PipeConnectionTypes => _PipeConnectionTypes;
            string _PipeConnectionType;
            public string PipeConnectionType
            {
                get => _PipeConnectionType;
                set
                {
                    if (value != _PipeConnectionType)
                    {
                        _PipeConnectionType = value;
                        OnPropertyChanged(nameof(PipeConnectionType));
                    }
                }
            }

            bool _IsHalfPlatform;
            public bool IsHalfPlatform
            {
                get => _IsHalfPlatform;
                set
                {
                    if (value != _IsHalfPlatform)
                    {
                        _IsHalfPlatform = value;
                        OnPropertyChanged(nameof(IsHalfPlatform));
                    }
                }
            }

            int _SimpleSprayCount;
            public int SimpleSprayCount
            {
                get => _SimpleSprayCount;
                set
                {
                    if (value != _SimpleSprayCount)
                    {
                        _SimpleSprayCount = value;
                        if (_SimpleSprayCount < 0) _SimpleSprayCount = 0;
                        if (_SimpleSprayCount > 4) _SimpleSprayCount = 4;
                        OnPropertyChanged(nameof(SimpleSprayCount));
                    }
                }
            }
        }

        public enum AdditionalFireHydrantEnum
        {
            YesYes,
            YesNo,
            NoYes,
            NoNo,
        }

        public ObservableCollection<Item> Items { get; set; } = new ObservableCollection<Item>();

        AdditionalFireHydrantEnum _AdditionalFireHydrant;
        public AdditionalFireHydrantEnum AdditionalFireHydrant
        {
            get => _AdditionalFireHydrant;
            set
            {
                if (value != _AdditionalFireHydrant)
                {
                    _AdditionalFireHydrant = value;
                    OnPropertyChanged(nameof(AdditionalFireHydrant));
                }
            }
        }
    }
    public class FloorHeightsViewModel : NotifyPropertyChangedBase
    {
        public static readonly FloorHeightsViewModel Instance = new FloorHeightsViewModel();
        public static string GetValidFloorString(string str)
        {
            var okNums = new HashSet<int>();
            return string.Join(",", str.Split(',').SelectNotNull(x =>
            {
                var m = Regex.Match(x, @"^(\-?\d+)\-(\-?\d+)$");
                if (m.Success)
                {
                    if (int.TryParse(m.Groups[1].Value, out int v1) && int.TryParse(m.Groups[2].Value, out int v2))
                    {
                        var min = Math.Min(v1, v2);
                        var max = Math.Max(v1, v2);
                        for (int i = min; i <= max; i++)
                        {
                            okNums.Add(i);
                        }

                        return x;
                    }
                    else
                    {
                        return null;
                    }
                }

                m = Regex.Match(x, @"^\-?\d+$");
                if (m.Success)
                {
                    if (int.TryParse(x, out int v))
                    {
                        if (!okNums.Contains(v))
                        {
                            okNums.Add(v);
                            return x;
                        }
                    }
                }

                return null;
            }).Distinct());
        }

        int _GeneralFloor = 3500;
        public int GeneralFloor
        {
            get => _GeneralFloor;
            set
            {
                if (value != _GeneralFloor)
                {
                    _GeneralFloor = value;
                    OnPropertyChanged(nameof(GeneralFloor));
                }
            }
        }

        bool _ExistsSpecialFloor;
        public bool ExistsSpecialFloor
        {
            get => _ExistsSpecialFloor;
            set
            {
                if (value != _ExistsSpecialFloor)
                {
                    _ExistsSpecialFloor = value;
                    OnPropertyChanged(nameof(ExistsSpecialFloor));
                }
            }
        }

        string _SpecialFloors;
        public string SpecialFloors
        {
            get => _SpecialFloors;
            set
            {
                if (value != _SpecialFloors)
                {
                    _SpecialFloors = value;
                    OnPropertyChanged(nameof(SpecialFloors));
                }
            }
        }

        public class Item : NotifyPropertyChangedBase
        {
            string _Floor;
            public string Floor
            {
                get => _Floor;
                set
                {
                    if (value != _Floor)
                    {
                        _Floor = value;
                        OnPropertyChanged(nameof(Floor));
                    }
                }
            }

            int _Height;
            public int Height
            {
                get => _Height;
                set
                {
                    if (value > 99999)
                        value = 99999;
                    if (value < 0)
                        value = 0;
                    if (value != _Height)
                    {
                        _Height = value;
                        OnPropertyChanged(nameof(Height));
                    }
                }
            }
        }

        public ObservableCollection<Item> Items { get; set; } = new ObservableCollection<Item>();

        //齐工提议加的一个函数
        public Dictionary<string, string> GetSpecialFloorHeightsDict(int maxFloorNum)
        {
            var dic = new Dictionary<int, double>();
            foreach (var item in Items)
            {
                foreach (var floor in ParseFloorNums(item.Floor))
                {
                    if (floor == 1)
                    {
                        dic[floor] = 0;
                    }
                    else
                    {
                        dic[floor] = item.Height;//(item.Height / 1000.0).ToString("0.00");
                    }
                }
            }
            var floorHeightDic = new Dictionary<string, string>();
            double floorHeight = 0.00;
            for (int floor = 1; floor < maxFloorNum + 2; floor++)
            {
                if (floor == 1)
                {
                    floorHeight = 0;
                }
                else if (dic.ContainsKey(floor))
                {
                    floorHeight += dic[floor];
                }
                else
                {
                    floorHeight += _GeneralFloor;

                }
                floorHeightDic.Add(Convert.ToString(floor), (floor == 1 ? "±" : "") + (floorHeight / 1000.0).ToString("0.00"));
            }

            return floorHeightDic;
        }

        public static List<int> ParseFloorNums(string floorStr)
        {
            if (string.IsNullOrWhiteSpace(floorStr))
                return new List<int>();
            floorStr = floorStr.Replace('，', ',').Replace('B', '-').Replace("F", "").Replace("M", "").Replace(" ", "");
            var hs = new HashSet<int>();
            foreach (var s in floorStr.Split(','))
            {
                if (string.IsNullOrEmpty(s))
                    continue;
                var m = Regex.Match(s, @"(\-?\d+)-(\-?\d+)");
                if (m.Success)
                {
                    var v1 = int.Parse(m.Groups[1].Value);
                    var v2 = int.Parse(m.Groups[2].Value);
                    var min = Math.Min(v1, v2);
                    var max = Math.Max(v1, v2);
                    for (int i = min; i <= max; i++)
                    {
                        hs.Add(i);
                    }

                    continue;
                }

                m = Regex.Match(s, @"\-?\d+");
                if (m.Success)
                {
                    hs.Add(int.Parse(m.Value));
                }
            }

            hs.Remove(0);
            return hs.OrderBy(x => x).ToList();
        }
    }
}
