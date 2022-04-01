using System.Collections.ObjectModel;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class WallColumnReinforceSetVM
    {
        public ThWallColumnReinforceSetModel Model { get; set; }
        public ObservableCollection<string> ConcreteStrengthGrades { get; set; }
        public ObservableCollection<string> AntiSeismicGrades { get; set; }
        public ObservableCollection<string> Frames { get; set; }
        public ObservableCollection<string> DrawScales { get; set; }
        public ObservableCollection<string> GbzPlaces { get; set; }
        public WallColumnReinforceSetVM()
        {
            Model = new ThWallColumnReinforceSetModel();
            GbzPlaces = new ObservableCollection<string>() { "底部加强区", "其它部位" };
            Frames = new ObservableCollection<string>() { "A0", "A1", "A2", "A3" };
            AntiSeismicGrades = new ObservableCollection<string>() { "一级", "二级", "三级", "四级" };
            DrawScales = new ObservableCollection<string>() { "1:1", "1:10", "1:20", "1:25", "1:30", "1:50" };
            ConcreteStrengthGrades = new ObservableCollection<string>() { "C35", "C40", "C45", "C50", "C55", "C60" }; 
        }
        public void Set()
        {
            Model.Set();
        }
        public void Reset()
        {
            Model.Reset();
        }       
    }
}
