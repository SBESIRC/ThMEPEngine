using System;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;

namespace TianHua.Hvac.UI.ViewModels
{
    class DragCalcViewModel : NotifyPropertyChangedBase
    {
        public DragCalcModel calcModel { get; }
        public DragCalcViewModel(DragCalcModel model) 
        {
            calcModel = model;
            CalcDuckDrag();
            CalcDuckAllDrag();
            CalcDuckTypeAllDrag();
        }
        /// <summary>
        /// 风管长度：小数点后最多1位
        /// </summary>
        public double DuctLength
        {
            get { return calcModel.DuctLength; }
            set 
            {
                calcModel.DuctLength = value;
                CalcDuckDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 比摩阻：小数点后最多1位
        /// </summary>
        public double Friction 
        { 
            get { return calcModel.Friction; }
            set 
            {
                calcModel.Friction = value;
                CalcDuckDrag();
                this.RaisePropertyChanged();
            } 
        }

        /// <summary>
        /// 局部阻力倍数：小数点后最多1位
        /// </summary>
        public double LocRes
        {
            get { return calcModel.LocRes; }
            set
            {
                calcModel.LocRes = value;
                CalcDuckDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 消音器阻力：正整数
        /// </summary>
        public int Damper 
        {
            get { return calcModel.Damper; }
            set
            {
                calcModel.Damper = value;
                CalcDuckAllDrag();
                CalcDuckTypeAllDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风管阻力
        /// </summary>
        public int DuctResistance 
        {
            get { return calcModel.DuctResistance; }
            set
            {
                calcModel.DuctResistance = value;
                CalcDuckAllDrag();
                CalcDuckTypeAllDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 末端预留风压
        /// </summary>
        public int EndReservedAirPressure 
        {
            get { return calcModel.EndReservedAirPressure; }
            set
            {
                calcModel.EndReservedAirPressure = value;
                CalcDuckAllDrag();
                CalcDuckTypeAllDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 动压
        /// </summary>
        public int DynPress 
        {
            get { return calcModel.DynPress; }
            set
            {
                calcModel.DynPress = value;
                CalcDuckAllDrag();
                CalcDuckTypeAllDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 计算总阻力
        /// </summary>
        public double CalcResistance 
        {
            get { return calcModel.CalcResistance; }
            set
            {
                calcModel.CalcResistance = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 选型系数
        /// </summary>
        public double SelectionFactor 
        {
            get { return calcModel.SelectionFactor; }
            set
            {
                calcModel.SelectionFactor = value;
                CalcDuckTypeAllDrag();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风阻：正整数
        /// </summary>
        public int WindResis
        {
            get { return calcModel.WindResis; }
            set
            {
                calcModel.WindResis = value;
                this.RaisePropertyChanged();
            }
        }
        void CalcDuckDrag() 
        {
            //计算风管阻力
            DuctResistance = calcModel.CalcDuckDrag();
        }
        void CalcDuckAllDrag() 
        {
            //计算总阻力
            CalcResistance = calcModel.CalcDuckAllDrag();
        }
        void CalcDuckTypeAllDrag() 
        {
            //计算选型总阻力
            WindResis = calcModel.CalcDuckTypeAllDrag();
        }
    }
}
