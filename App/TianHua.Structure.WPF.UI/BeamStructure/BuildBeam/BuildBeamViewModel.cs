using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Structure.WPF.UI.BeamStructure.BuildBeam
{
    public class BuildBeamViewModel : NotifyPropertyChangedBase
    {
        public BuildBeamViewModel(BuildBeamConfigModel model)
        {
            TopPlate = new ObservableCollection<DynamicBuildBeamModelData>();
            TopPlate.Add(new DynamicBuildBeamModelData() { L = "≤4", H = model.TableTop1.H, B = model.TableTop1.B });
            TopPlate.Add(new DynamicBuildBeamModelData() { L = "(4,6]", H = model.TableTop2.H, B = model.TableTop2.B });
            TopPlate.Add(new DynamicBuildBeamModelData() { L = "(6,7]", H = model.TableTop3.H, B = model.TableTop3.B });
            TopPlate.Add(new DynamicBuildBeamModelData() { L = "(7,8]", H = model.TableTop4.H, B = model.TableTop4.B });
            TopPlate.Add(new DynamicBuildBeamModelData() { L = "(8,9]", H = model.TableTop5.H, B = model.TableTop5.B });
            TopPlate.Add(new DynamicBuildBeamModelData() { L = "(9,10]", H = model.TableTop6.H, B = model.TableTop6.B });
            TopPlate.Add(new DynamicBuildBeamModelData() { L = ">10", H = model.TableTop7.H, B = model.TableTop7.B });

            MiddlePlateA = new ObservableCollection<DynamicBuildBeamModelData>();
            MiddlePlateA.Add(new DynamicBuildBeamModelData() { L = "≤6", H = model.TableMiddleA1.H, B = model.TableMiddleA1.B });
            MiddlePlateA.Add(new DynamicBuildBeamModelData() { L = "(6,7]", H = model.TableMiddleA2.H, B = model.TableMiddleA2.B });
            MiddlePlateA.Add(new DynamicBuildBeamModelData() { L = "(7,8]", H = model.TableMiddleA3.H, B = model.TableMiddleA3.B });
            MiddlePlateA.Add(new DynamicBuildBeamModelData() { L = "(8,9]", H = model.TableMiddleA4.H, B = model.TableMiddleA4.B });
            MiddlePlateA.Add(new DynamicBuildBeamModelData() { L = "(9,10]", H = model.TableMiddleA5.H, B = model.TableMiddleA5.B });
            MiddlePlateA.Add(new DynamicBuildBeamModelData() { L = ">10", H = model.TableMiddleA6.H, B = model.TableMiddleA6.B });

            MiddlePlateB = new ObservableCollection<DynamicBuildBeamModelData>();
            MiddlePlateB.Add(new DynamicBuildBeamModelData() { L = "≤6", H = model.TableMiddleB1.H, B = model.TableMiddleB1.B });
            MiddlePlateB.Add(new DynamicBuildBeamModelData() { L = "(6,7]", H = model.TableMiddleB2.H, B = model.TableMiddleB2.B });
            MiddlePlateB.Add(new DynamicBuildBeamModelData() { L = "(7,8]", H = model.TableMiddleB3.H, B = model.TableMiddleB3.B });
            MiddlePlateB.Add(new DynamicBuildBeamModelData() { L = "(8,9]", H = model.TableMiddleB4.H, B = model.TableMiddleB4.B });
            MiddlePlateB.Add(new DynamicBuildBeamModelData() { L = "(9,10]", H = model.TableMiddleB5.H, B = model.TableMiddleB5.B });
            MiddlePlateB.Add(new DynamicBuildBeamModelData() { L = ">10", H = model.TableMiddleB6.H, B = model.TableMiddleB6.B });
        }

        private ObservableCollection<DynamicBuildBeamModelData> topPlate { get; set; }
        public ObservableCollection<DynamicBuildBeamModelData> TopPlate
        {
            get { return topPlate; }
            set
            {
                topPlate = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<DynamicBuildBeamModelData> middlePlateA { get; set; }
        public ObservableCollection<DynamicBuildBeamModelData> MiddlePlateA
        {
            get { return middlePlateA; }
            set
            {
                middlePlateA = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<DynamicBuildBeamModelData> middlePlateB { get; set; }
        public ObservableCollection<DynamicBuildBeamModelData> MiddlePlateB
        {
            get { return middlePlateB; }
            set
            {
                middlePlateB = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public class DynamicBuildBeamModelData
    {
        public string L { get; set; }
        public int H { get; set; }
        public int B { get; set; }
    }
}
