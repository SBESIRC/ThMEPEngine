using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.Model;

namespace ThMEPWSS.ViewModel
{
    public class DrainageSystemAGViewmodel: NotifyPropertyChangedBase
    {
        public DrainageSystemAGViewmodel() 
        {
            //初始化数据
            //图纸比例 在1: 50、1:100和1: 150之间单选，初始值1: 100



        }
        /// <summary>
        /// 图纸比例下拉选择数据
        /// </summary>
        private ObservableCollection<UListItemData> _scaleListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ScaleListItems
        {
            get 
            {
                return _scaleListItems;
            }
            set 
            {
                _scaleListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 图纸比例选中项
        /// </summary>
        private UListItemData _scaleSelectItem { get; set; }
        public UListItemData ScaleSelectItem 
        {
            get { return _scaleSelectItem; }
            set 
            {
                _scaleSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 废污合流立管直径wasteSewageWaterRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _wswPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> WSWPipeDiameterListItems
        {
            get
            {
                return _wswPipeDiameterListItems;
            }
            set
            {
                _wswPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 废污合流立管直径选中项
        /// </summary>
        private UListItemData _wswPipeDiameterSelectItem { get; set; }
        public UListItemData WSWPipeDiameterSelectItem
        {
            get { return _wswPipeDiameterSelectItem; }
            set
            {
                _wswPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        // <summary>
        /// 废污合流通气立管直径 wasteSewageVentilationRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _wsvPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> WSVPipeDiameterListItems
        {
            get
            {
                return _wsvPipeDiameterListItems;
            }
            set
            {
                _wsvPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 废污合流通气立管直径 选中项
        /// </summary>
        private UListItemData _wsvPipeDiameterSelectItem { get; set; }
        public UListItemData WSVPipeDiameterSelectItem
        {
            get { return _wsvPipeDiameterSelectItem; }
            set
            {
                _wsvPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 卫生间是否沉箱
        /// </summary>
        private bool _toiletIsCaisson { get; set; }
        public bool ToiletIsCaisson 
        {
            get { return _toiletIsCaisson; }
            set 
            {
                _toiletIsCaisson = value;
                this.RaisePropertyChanged();
            }
        }

        // <summary>
        /// 阳台废水立管直径 balconyWasteWaterRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _bwwPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> BWWPipeDiameterListItems
        {
            get
            {
                return _bwwPipeDiameterListItems;
            }
            set
            {
                _bwwPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 阳台废水立管直径 选中项
        /// </summary>
        private UListItemData _bwwPipeDiameterSelectItem { get; set; }
        public UListItemData BWWPipeDiameterSelectItem
        {
            get { return _bwwPipeDiameterSelectItem; }
            set
            {
                _bwwPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 阳台立管直径 balconyRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _bPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> BPipeDiameterListItems
        {
            get
            {
                return _bPipeDiameterListItems;
            }
            set
            {
                _bPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 阳台立管直径 选中项
        /// </summary>
        private UListItemData _bPipeDiameterSelectItem { get; set; }
        public UListItemData BPipeDiameterSelectItem
        {
            get { return _bPipeDiameterSelectItem; }
            set
            {
                _bPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 冷凝立管直径 condensingRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _cPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> CPipeDiameterListItems
        {
            get
            {
                return _cPipeDiameterListItems;
            }
            set
            {
                _cPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 冷凝立管直径 选中项
        /// </summary>
        private UListItemData _cPipeDiameterSelectItem { get; set; }
        public UListItemData CPipeDiameterSelectItem
        {
            get { return _cPipeDiameterSelectItem; }
            set
            {
                _cPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 屋面雨水立管直径 roofRainRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _rPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> RPipeDiameterListItems
        {
            get
            {
                return _rPipeDiameterListItems;
            }
            set
            {
                _rPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 屋面雨水立管直径 选中项
        /// </summary>
        private UListItemData _rPipeDiameterSelectItem { get; set; }
        public UListItemData RPipeDiameterSelectItem
        {
            get { return _rPipeDiameterSelectItem; }
            set
            {
                _rPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 大屋面重力雨水斗直径 maxRoofGravityRainBucketRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _mRGPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> MRGPipeDiameterListItems
        {
            get
            {
                return _mRGPipeDiameterListItems;
            }
            set
            {
                _mRGPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 大屋面重力雨水斗直径 选中项
        /// </summary>
        private UListItemData _mRGPipeDiameterSelectItem { get; set; }
        public UListItemData MRGPipeDiameterSelectItem
        {
            get { return _mRGPipeDiameterSelectItem; }
            set
            {
                _mRGPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 大屋面侧排雨水斗直径 maxRoofSideDrainRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _mRSPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> MRSPipeDiameterListItems
        {
            get
            {
                return _mRSPipeDiameterListItems;
            }
            set
            {
                _mRSPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 大屋面侧排雨水斗直径 选中项
        /// </summary>
        private UListItemData _mRSPipeDiameterSelectItem { get; set; }
        public UListItemData MRSPipeDiameterSelectItem
        {
            get { return _mRSPipeDiameterSelectItem; }
            set
            {
                _mRSPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 小屋面重力雨水斗直径 minRoofGravityRainBucketRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _miRGPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> MIRGPipeDiameterListItems
        {
            get
            {
                return _miRGPipeDiameterListItems;
            }
            set
            {
                _miRGPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 小屋面重力雨水斗直径 选中项
        /// </summary>
        private UListItemData _miRGPipeDiameterSelectItem { get; set; }
        public UListItemData MIRGPipeDiameterSelectItem
        {
            get { return _miRGPipeDiameterSelectItem; }
            set
            {
                _miRGPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }



        /// <summary>
        /// 小屋面侧排雨水斗直径 minRoofSideDrainRiserPipeDiameter
        /// </summary>
        private ObservableCollection<UListItemData> _miRSPipeDiameterListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> MIRSPipeDiameterListItems
        {
            get
            {
                return _miRSPipeDiameterListItems;
            }
            set
            {
                _miRGPipeDiameterListItems = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 小屋面侧排雨水斗直径 选中项
        /// </summary>
        private UListItemData _miRSPipeDiameterSelectItem { get; set; }
        public UListItemData MIRSPipeDiameterSelectItem
        {
            get { return _miRSPipeDiameterSelectItem; }
            set
            {
                _miRSPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
