using System.Collections.ObjectModel;
using ThMEPStructure.Reinforcement.Model;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class WallColumnReinforceSetVM
    {
        public ThWallColumnReinforceSetModel Model { get; set; }
        public ObservableCollection<string> ConcreteStrengthGrades { get; set; }
        public ObservableCollection<string> AntiSeismicGrades { get; set; }
        public ObservableCollection<string> Frames { get; set; }
        public ObservableCollection<string> DrawScales { get; set; }
        public ObservableCollection<string> WallLocations { get; set; }
        public WallColumnReinforceSetVM()
        {
            Model = new ThWallColumnReinforceSetModel();
            Frames = new ObservableCollection<string>(ThWallColumnReinforceConfig.Instance.Frames);
            DrawScales = new ObservableCollection<string>(ThWallColumnReinforceConfig.Instance.DrawScales);
            WallLocations = new ObservableCollection<string>(ThWallColumnReinforceConfig.Instance.WallLocations);            
            AntiSeismicGrades = new ObservableCollection<string>(ThWallColumnReinforceConfig.Instance.AntiSeismicGrades);            
            ConcreteStrengthGrades = new ObservableCollection<string>(ThWallColumnReinforceConfig.Instance.ConcreteStrengthGrades); 
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
