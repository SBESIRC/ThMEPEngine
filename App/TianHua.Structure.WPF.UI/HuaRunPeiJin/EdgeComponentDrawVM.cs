using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Structure.WPF.UI.HuaRunPeiJin
{
    internal class EdgeComponentDrawVM
    {
        public ObservableCollection<EdgeComponentInfo> EdgeComponents { get; set; }
        public EdgeComponentDrawModel DrawModel { get; set; }
        public ObservableCollection<string> DwgSources { get; set; }
        public ObservableCollection<string> SortWays { get; set; }
        public ObservableCollection<string> LeaderTypes { get; set; }
        public ObservableCollection<string> MarkPositions { get; set; }
        public EdgeComponentDrawVM()
        {
            DrawModel = new EdgeComponentDrawModel();
            DwgSources = new ObservableCollection<string>() { "YJK"};
            SortWays = new ObservableCollection<string>() { "从左到右，从下到上" };
            LeaderTypes = new ObservableCollection<string>() { "折现引出" };
            MarkPositions = new ObservableCollection<string>() { "右上", "右下", "左上", "左下" };
            EdgeComponents = new ObservableCollection<EdgeComponentInfo>();
        }
        public void Select()
        {
            //ToDO
        }
        public void Clear()
        {
            //TODO
        }
        public void Merge()
        {
            //TODO
        }
        public void Draw()
        {
            //TODO
        }
    }
}
