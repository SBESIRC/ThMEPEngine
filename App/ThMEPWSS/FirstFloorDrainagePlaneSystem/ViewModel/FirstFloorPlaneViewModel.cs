using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel
{
    public class FirstFloorPlaneViewModel : NotifyPropertyChangedBase
    {
        public FirstFloorPlaneViewModel()
        {
            SetDirvepipeDimensionType();
            SetBlockScaleListType();
        }

        /// <summary>
        /// 套管标高
        /// </summary>
        private double _drivepipeLevel = -0.55;
        public double DrivepipeLevel
        {
            get
            {
                return _drivepipeLevel;
            }
            set
            {
                _drivepipeLevel = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 出图比例
        /// </summary>
        private UListItemData _blockScale { get; set; }
        public UListItemData BlockScale
        {
            get { return _blockScale; }
            set
            {
                _blockScale = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _blockScaleList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> BlockScaleList
        {
            get { return _blockScaleList; }
            set
            {
                _blockScaleList = value;
                this.RaisePropertyChanged();
            }
        }

        private void SetBlockScaleListType()
        {
            BlockScaleList.Add(new UListItemData("1:50", 0, 0.5));
            BlockScaleList.Add(new UListItemData("1:100", 1, 1));
            BlockScaleList.Add(new UListItemData("1:150", 2, 1.5));
            BlockScaleList.Add(new UListItemData("1:200", 3, 2));
            BlockScale = BlockScaleList[1];
        }

        /// <summary>
        /// 污废水(合流\分流)
        /// </summary>
        private UListItemData _dirvepipeDimensionType { get; set; }
        public UListItemData DirvepipeDimensionType
        {
            get { return _dirvepipeDimensionType; }
            set
            {
                _dirvepipeDimensionType = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _dirvepipeDimensionList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> DrvepipeDimensionList
        {
            get { return _dirvepipeDimensionList; }
            set
            {
                _dirvepipeDimensionList = value;
                this.RaisePropertyChanged();
            }
        }

        private void SetDirvepipeDimensionType()
        {
            DrvepipeDimensionList.Add(new UListItemData("普通钢套管", 0, DirvepipeDimensionTypeEnum.OrdinarySteel));
            DrvepipeDimensionList.Add(new UListItemData("B型钢防水套管", 1, DirvepipeDimensionTypeEnum.BSteelWaterproof));
            DrvepipeDimensionList.Add(new UListItemData("A型柔性防水套管", 2, DirvepipeDimensionTypeEnum.AFlexibleWaterproof));
            DrvepipeDimensionList.Add(new UListItemData("A型防护密闭套管", 3, DirvepipeDimensionTypeEnum.AProtectiveSealing));
            DrvepipeDimensionList.Add(new UListItemData("C型防护密闭套管", 4, DirvepipeDimensionTypeEnum.CProtectiveSealing));
            DrvepipeDimensionList.Add(new UListItemData("E型防护密闭套管", 5, DirvepipeDimensionTypeEnum.EProtectiveSealing));
            DirvepipeDimensionType = DrvepipeDimensionList[1];
        }
    }

    public enum DirvepipeDimensionTypeEnum
    {
        /// <summary>
        /// 普通钢套管
        /// </summary>
        OrdinarySteel = 0,

        /// <summary>
        /// B型钢防水套管
        /// </summary>
        BSteelWaterproof = 1,

        /// <summary>
        /// A型柔性防水套管
        /// </summary>
        AFlexibleWaterproof = 2,

        /// <summary>
        /// A型防护密闭套管
        /// </summary>
        AProtectiveSealing = 3,

        // <summary>
        /// C型防护密闭套管
        /// </summary>
        CProtectiveSealing = 4,

        // <summary>
        /// E型防护密闭套管
        /// </summary>
        EProtectiveSealing = 5,
    }
}
