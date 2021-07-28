﻿using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThMEPElectrical.Model
{
    public class ThCapitalConverterModel : INotifyPropertyChanged
    {        
        public ThCapitalConverterModel()
        {
            blkScale = "1:100";
            equipOps = CapitalOP.All;
            havcOps = true;
            wssOps = true;
            BlkScales = new ObservableCollection<string>(new List<string> { "1:100", "1:150" });
        }
        public ObservableCollection<string> BlkScales { get; set; }

        private bool havcOps;
        public bool HavcOps
        {
            get
            {
                return havcOps;
            }
            set
            {
                havcOps = value;
                RaisePropertyChanged("HavcOps");
            }
        }

        private bool wssOps;
        public bool WssOps
        {
            get
            {
                return wssOps;
            }
            set
            {
                wssOps = value;
                RaisePropertyChanged("WssOps");
            }
        }

        private CapitalOP equipOps;
        public CapitalOP EquipOps
        {
            get
            {
                return equipOps;
            }
            set
            {
                equipOps = value;
                RaisePropertyChanged("EquipOps");
            }
        }

        private string blkScale = "";
        public string BlkScale
        {
            get
            {
                return blkScale;
            }
            set
            {
                blkScale = value;
                RaisePropertyChanged("BlkScale");
            }
        }

        public double BlkScaleValue
        {
            get
            {
                string[] values = blkScale.Split(':');
                return double.Parse(values[1]);
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
    public enum CapitalOP
    {
        Strong,
        Weak,
        All,
    }
}
