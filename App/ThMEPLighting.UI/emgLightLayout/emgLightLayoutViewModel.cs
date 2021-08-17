using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ThControlLibraryWPF.ControlUtils;
using ThMEPLighting.EmgLight;

namespace ThMEPLighting.UI.emgLightLayout
{
    public partial class emgLightLayoutViewModel : NotifyPropertyChangedBase
    {
        private SideLayoutEnum _singleLayout { get; set; }
        public SideLayoutEnum singleLayout
        {
            get
            {
                return _singleLayout;
            }
            set
            {
                _singleLayout = value;
                this.RaisePropertyChanged();
            }
        }
        private BlkTypeEnum _blkType { get; set; }
        public BlkTypeEnum blkType
        {
            get
            {
                return _blkType;
            }
            set
            {
                _blkType = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _scaleListItem = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> scaleListItems
        {
            get { return _scaleListItem; }
            set
            {
                _scaleListItem = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _scale { get; set; }

        public UListItemData scaleItem
        {
            get { return _scale; }
            set
            {
                _scale = value;
                this.RaisePropertyChanged();
            }
        }

        //public double scale { 
        //    get
        //    {
        //        return _scale;
        //    }
        //    set
        //    {
        //        _scale = value;
        //        RaisePropertyChanged();
        //    }
        //}
        public emgLightLayoutViewModel()
        {
            this.singleLayout = SideLayoutEnum.DoubleSide;
            this.blkType = BlkTypeEnum.onWall;
            // this.scale = EmgLightCommon.BlockScaleNum;
            scaleListItems.Add(new UListItemData("100", 0, (double)100));
            scaleListItems.Add(new UListItemData("150", 1, (double)150));
            scaleItem = scaleListItems.FirstOrDefault();
        }

    }
    public enum SideLayoutEnum
    {
        DoubleSide = 0,
        SingleSide = 1,
    }
    public enum BlkTypeEnum
    {
        onWall = 0,
        doubleDir = 1
    }
}
