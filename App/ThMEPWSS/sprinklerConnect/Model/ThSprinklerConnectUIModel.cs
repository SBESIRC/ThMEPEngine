using System.Linq;
using System.ComponentModel;
using ThMEPWSS.Sprinkler.Analysis;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ThMEPWSS.SprinklerConnect.Model
{
    public class ThSprinklerConnectUIModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> LayoutDirections { get; set; }

        public ThSprinklerConnectUIModel()
        {
            var items = new List<string>{ "垂直","平行"};
            LayoutDirections = new ObservableCollection<string>(items);
            layoutDirection = items[0];
        }

        private string layoutDirection = "";
        /// <summary>
        /// 布置方向
        /// </summary>
        public string LayoutDirection
        {
            get
            {
                return layoutDirection;
            }
            set
            {
                layoutDirection = value;
                RaisePropertyChanged("LayoutDirection");
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
