using System.Collections.ObjectModel;
using ThMEPStructure.Reinforcement.Model;

namespace TianHua.Structure.WPF.UI.Reinforcement
{
    public class ColumnReinforceSetVM
    {
        public ThColumnReinforceSetModel Model { get; set; }
        public ObservableCollection<string> ConcreteStrengthGrades { get; set; }
        public ObservableCollection<string> AntiSeismicGrades { get; set; }
        public ObservableCollection<string> Frames { get; set; }
        public ObservableCollection<string> DrawScales { get; set; }
        public ObservableCollection<string> WallLocations { get; set; }
        public ColumnReinforceSetVM()
        {
            Model = new ThColumnReinforceSetModel();
            Frames = new ObservableCollection<string>(ThColumnReinforceConfig.Instance.Frames);
            DrawScales = new ObservableCollection<string>(ThColumnReinforceConfig.Instance.DrawScales);                       
            AntiSeismicGrades = new ObservableCollection<string>(ThColumnReinforceConfig.Instance.AntiSeismicGrades);            
            ConcreteStrengthGrades = new ObservableCollection<string>(ThColumnReinforceConfig.Instance.ConcreteStrengthGrades); 
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
