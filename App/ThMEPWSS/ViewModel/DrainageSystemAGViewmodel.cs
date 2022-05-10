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
            var scaleList = CommonUtil.EnumDescriptionToList(typeof(EnumDrawingScale));
            scaleList.ForEach(c => ScaleListItems.Add(c));
            ScaleSelectItem = ScaleListItems.Where(c => c.Value == (int)EnumDrawingScale.DrawingScale1_100).FirstOrDefault();

            //污废立管、通气立管和阳台废水的规格
            //可选项为DN100、DN125、DN150和DN200，初始选项为DN100。
            List<int> values = new List<int>
            {
                (int)EnumPipeDiameter.DN100,
                (int)EnumPipeDiameter.DN125,
                (int)EnumPipeDiameter.DN150,
                (int)EnumPipeDiameter.DN200,
            };
            var raiseDim = CommonUtil.EnumDescriptionToList(typeof(EnumPipeDiameter), values);
            //WSVPipeDiameterListItems.Add(new UListItemData("无",-1));
            foreach (var raise in raiseDim) 
            {
                WSWPipeDiameterListItems.Add(raise);
                WSVPipeDiameterListItems.Add(raise);
                BWWPipeDiameterListItems.Add(raise);
                CaissonRiserListItems.Add(raise);
            }
            WSWPipeDiameterSelectItem = WSWPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();
            WSVPipeDiameterSelectItem = WSVPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();
            BWWPipeDiameterSelectItem = BWWPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();
            CaissonRiseSelectItem = CaissonRiserListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();

            //卫生间沉箱初始不勾选。
            ToiletIsCaisson = false;

            //设置屋顶雨水立管的规格。选项为DN100、DN125、DN150和DN200，初始选项为DN100。
            var roofValues = new List<int>
            {
                (int)EnumPipeDiameter.DN100,
                (int)EnumPipeDiameter.DN125,
                (int)EnumPipeDiameter.DN150,
                (int)EnumPipeDiameter.DN200,
            };
            var roofRainRaise = CommonUtil.EnumDescriptionToList(typeof(EnumPipeDiameter), roofValues);
            roofRainRaise.ForEach(c => RPipeDiameterListItems.Add(c));
            RPipeDiameterSelectItem = RPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();

            //设置阳台立管的管径。选项为DN100、DN125、DN150和DN200，初始选项为DN100。
            var balcValues = new List<int>
            {
                (int)EnumPipeDiameter.DN100,
                (int)EnumPipeDiameter.DN125,
                (int)EnumPipeDiameter.DN150,
                (int)EnumPipeDiameter.DN200,
            };
            var balcRaise = CommonUtil.EnumDescriptionToList(typeof(EnumPipeDiameter), balcValues);
            balcRaise.ForEach(c => BPipeDiameterListItems.Add(c));
            BPipeDiameterSelectItem = BPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();


            //冷凝立管 可选项为DN50、DN75和DN100，初始选项为DN50。
            var corrValues = new List<int>
            {
                (int)EnumPipeDiameter.DN50,
                (int)EnumPipeDiameter.DN75,
                (int)EnumPipeDiameter.DN100,
            };
            var corrRaise = CommonUtil.EnumDescriptionToList(typeof(EnumPipeDiameter), corrValues);
            corrRaise.ForEach(c => CPipeDiameterListItems.Add(c));
            CPipeDiameterSelectItem = CPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN50).FirstOrDefault();


            //设置大屋面上的重力流雨水斗和侧排水雨水斗的规格。选项为DN75、DN100、DN125和DN150，初始选项为DN100。
            var maxRoofValues = new List<int>
            {
                (int)EnumPipeDiameter.DN75,
                (int)EnumPipeDiameter.DN100,
                (int)EnumPipeDiameter.DN125,
                (int)EnumPipeDiameter.DN150,
            };
            var maxRoofRaise = CommonUtil.EnumDescriptionToList(typeof(EnumPipeDiameter), maxRoofValues);
            maxRoofRaise.ForEach(c =>
            {
                MRGPipeDiameterListItems.Add(c);
                MRSPipeDiameterListItems.Add(c);
            });
            MRGPipeDiameterSelectItem = MRGPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();
            MRSPipeDiameterSelectItem = MRSPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN100).FirstOrDefault();

            //设置小屋面上的重力流雨水斗和侧排水雨水斗的规格。选项为DN75、DN100、DN125和DN150，初始选项为DN100。
            var minRoofValues = new List<int>
            {
                (int)EnumPipeDiameter.DN75,
                (int)EnumPipeDiameter.DN100,
                (int)EnumPipeDiameter.DN125,
                (int)EnumPipeDiameter.DN150,
            };
            var minRoofRaise = CommonUtil.EnumDescriptionToList(typeof(EnumPipeDiameter), minRoofValues);
            maxRoofRaise.ForEach(c =>
            {
                MIRGPipeDiameterListItems.Add(c);
                MIRSPipeDiameterListItems.Add(c);
            });
            MIRGPipeDiameterSelectItem = MIRGPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN75).FirstOrDefault();
            MIRSPipeDiameterSelectItem = MIRSPipeDiameterListItems.Where(c => c.Value == (int)EnumPipeDiameter.DN75).FirstOrDefault();
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
        /// <summary>
        /// 废污合流立管直径选中项
        /// </summary>
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
        /// <summary>
        /// 废污合流通气立管直径 wasteSewageVentilationRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 废污合流通气立管直径 选中项
        /// </summary>
        public UListItemData WSVPipeDiameterSelectItem
        {
            get { return _wsvPipeDiameterSelectItem; }
            set
            {
                _wsvPipeDiameterSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _caissonRiserListItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> CaissonRiserListItems 
        {
            get { return _caissonRiserListItems; }
            set 
            {
                _caissonRiserListItems = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _caissonRiseSelectItem { get; set; }
        public UListItemData CaissonRiseSelectItem 
        {
            get { return _caissonRiseSelectItem; }
            set 
            {
                _caissonRiseSelectItem = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 卫生间是否沉箱
        /// </summary>
        private bool _toiletIsCaisson { get; set; }
        /// <summary>
        /// 卫生间是否沉箱
        /// </summary>
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
        /// <summary>
        /// 阳台废水立管直径 balconyWasteWaterRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 阳台废水立管直径 选中项
        /// </summary>
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
        /// <summary>
        /// 阳台立管直径 balconyRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 阳台立管直径 选中项
        /// </summary>
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
        /// <summary>
        /// 冷凝立管直径 condensingRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 冷凝立管直径 选中项
        /// </summary>
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
        /// <summary>
        /// 屋面雨水立管直径 roofRainRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 屋面雨水立管直径 选中项
        /// </summary>
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
        /// <summary>
        /// 大屋面重力雨水斗直径 maxRoofGravityRainBucketRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 大屋面重力雨水斗直径 选中项
        /// </summary>
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
        /// <summary>
        /// 大屋面侧排雨水斗直径 maxRoofSideDrainRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 大屋面侧排雨水斗直径 选中项
        /// </summary>
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
        /// <summary>
        /// 小屋面重力雨水斗直径 minRoofGravityRainBucketRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 小屋面重力雨水斗直径 选中项
        /// </summary>
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
        /// <summary>
        /// 小屋面侧排雨水斗直径 minRoofSideDrainRiserPipeDiameter
        /// </summary>
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
        /// <summary>
        /// 小屋面侧排雨水斗直径 选中项
        /// </summary>
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
