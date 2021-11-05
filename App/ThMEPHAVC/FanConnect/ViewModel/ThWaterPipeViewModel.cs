﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.FanConnect.ViewModel
{
    public class ThWaterPipeViewModel : NotifyPropertyChangedBase, ICloneable
    {
        public ThWaterPipeConfigInfo WaterPipeConfigInfo { set; get; }
        public object Clone()
        {
            throw new NotImplementedException();
        }
        public ThWaterPipeViewModel()
        {
            WaterPipeConfigInfo = new ThWaterPipeConfigInfo();
        }
        public int SystemType
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.SystemType; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.SystemType = value;
                this.RaisePropertyChanged();
            }
        }
        public int HorizontalType
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.HorizontalType; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.HorizontalType = value;
                this.RaisePropertyChanged();
            }
        }
        public int PipeSystemType
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.PipeSystemType; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.PipeSystemType = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsCodeAndHotPipe
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.IsCodeAndHotPipe = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsCWPipe
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.IsCWPipe; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.IsCWPipe = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsCoolPipe
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.IsCoolPipe; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.IsCoolPipe = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsGenerValve
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.IsGenerValve; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.IsGenerValve = value;
                this.RaisePropertyChanged();
            }
        }
        public double FrictionCoeff
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.FrictionCoeff; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.FrictionCoeff = value;
                this.RaisePropertyChanged();
            }
        }
        public double MarkHeigth
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.MarkHeigth; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.MarkHeigth = value;
                this.RaisePropertyChanged();
            }
        }
        public string FeedPipeValve
        {
            get { return WaterPipeConfigInfo.WaterValveConfigInfo.FeedPipeValve; }
            set
            {
                WaterPipeConfigInfo.WaterValveConfigInfo.FeedPipeValve = value;
                this.RaisePropertyChanged();
            }
        }
        public string ReturnPipeValeve
        {
            get { return WaterPipeConfigInfo.WaterValveConfigInfo.ReturnPipeValeve; }
            set
            {
                WaterPipeConfigInfo.WaterValveConfigInfo.ReturnPipeValeve = value;
                this.RaisePropertyChanged();
            }
        }
        public string MapScale
        {
            get { return WaterPipeConfigInfo.WaterValveConfigInfo.MapScale; }
            set
            {
                WaterPipeConfigInfo.WaterValveConfigInfo.MapScale = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
