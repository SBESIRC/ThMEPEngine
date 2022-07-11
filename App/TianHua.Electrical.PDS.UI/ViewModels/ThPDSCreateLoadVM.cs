﻿using System;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.ViewModels
{
    public sealed class ThPDSCreateLoadVM : ObservableObject
    {
        private readonly ImageSourceConverter cvt = new();
        public ObservableCollection<ThPDSLoadItemTypeVM> Types { get; private set; }

        public ThPDSCreateLoadVM()
        {
            Types = new ObservableCollection<ThPDSLoadItemTypeVM>()
            {
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.AL,
                    Image = LoadImage("Lighting Distribution Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.AP,
                    Image = LoadImage("Power Distribution Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.ALE,
                    Image = LoadImage("Emergency Lighting Distribution Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.APE,
                    Image = LoadImage("Emergency Power Distribution Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.FEL,
                    Image = LoadImage("Fire Emergency Lighting Distribution Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.AW,
                    Image = LoadImage("Electrical Meter Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.ACB,
                    Image = LoadImage("AC Charger.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.RS,
                    Image = LoadImage("Roller Shutter.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.INT,
                    Image = LoadImage("Isolation Switch Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.RD,
                    Image = LoadImage("Residential Distribution Panel.png"),
                },
                //new ThPDSLoadItemTypeVM()
                //{
                //    Type = ImageLoadType.AX,
                //    Image = 

                //},
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.Light,
                    Image = LoadImage("Luminaire.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.Socket,
                    Image = LoadImage("Socket.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.AC,
                    Image = LoadImage("Electrical Control Panel.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.DC,
                    Image = LoadImage("DC Charger.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.Motor,
                    Image = LoadImage("Motor.png"),
                },
                new ThPDSLoadItemTypeVM()
                {
                    Type = ImageLoadType.Pump,
                    Image = LoadImage("Pump.png"),
                },
            };

            // 默认设备类型
            Type = Types[0];
        }

        private ImageSource LoadImage(string name)
        {
            var uri = string.Format("pack://application:,,,/ThControlLibraryWPF;component/Images/{0}", name);
            return (ImageSource)cvt.ConvertFrom(new Uri(uri));
        }

        /// <summary>
        /// 设备类型
        /// </summary>
        public ThPDSLoadItemTypeVM Type { get; private set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double RatedVoltage { get; set; }

        /// <summary>
        /// 编号
        /// </summary>
        private string _Number;
        public string Number
        {
            get => _Number;
            set => SetProperty(ref _Number, value);
        }

        /// <summary>
        /// 功率
        /// </summary>
        private double _Power;
        public double Power
        {
            get => _Power;
            set => SetProperty(ref _Power, value);
        }

        /// <summary>
        /// 描述
        /// </summary>
        private string _Description;
        public string Description
        {
            get => _Description;
            set => SetProperty(ref _Description, value);
        }

        /// <summary>
        /// 楼层
        /// </summary>
        private string _Storey;
        public string Storey
        {
            get => _Storey;
            set => SetProperty(ref _Storey, value);
        }

        /// <summary>
        /// 消防设备
        /// </summary>
        private bool _FireLoad;
        public bool FireLoad
        {
            get => _FireLoad;
            set => SetProperty(ref _FireLoad, value);
        }
    }
}
