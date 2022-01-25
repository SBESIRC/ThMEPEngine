using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.JsonExtensionsNs;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{
    public class FireHydrantSystemUIViewModel : NotifyPropertyChangedBase
    {
        public static readonly FireHydrantSystemUIViewModel Singleton = new FireHydrantSystemUIViewModel()
        {
            cbLabelRing = ViewModel.FireHydrantSystemViewModel.InsertLoopMark,
            cbLabelNode = ViewModel.FireHydrantSystemViewModel.InsertSubLoopMark,
            cbGenerate = ThMEPWSS.FireNumFlatDiagramNs.FlatDiagramService.cbGenerate,
        };
        NumberingMethodEnum _NumberingMethod;
        public NumberingMethodEnum NumberingMethod
        {
            get => _NumberingMethod;
            set
            {
                if (value != _NumberingMethod) { _NumberingMethod = value; OnPropertyChanged(nameof(NumberingMethod)); }
            }
        }
        ProcessingObjectEnum _ProcessingObject;
        public ProcessingObjectEnum ProcessingObject
        {
            get => _ProcessingObject;
            set
            {
                if (value != _ProcessingObject) { _ProcessingObject = value; OnPropertyChanged(nameof(ProcessingObject)); }
            }
        }
        string _CurrentDwgRatio = "1:150";
        public string CurrentDwgRatio
        {
            get => _CurrentDwgRatio;
            set
            {
                if (value != _CurrentDwgRatio) { _CurrentDwgRatio = value; OnPropertyChanged(nameof(CurrentDwgRatio)); }
            }
        }
        string _Prefix = "D1X1L-";
        public string Prefix
        {
            get => _Prefix;
            set
            {
                if (value != _Prefix) { _Prefix = value; OnPropertyChanged(nameof(Prefix)); }
            }
        }
        int _StartNum = 1;
        public int StartNum
        {
            get => _StartNum;
            set
            {
                if (value != _StartNum) { _StartNum = value; OnPropertyChanged(nameof(StartNum)); }
            }
        }
        public Action cbLabelRing;
        public Action cbLabelNode;
        public Action cbGenerate;
        public enum NumberingMethodEnum
        {
            Whole, Single,
        }
        public enum ProcessingObjectEnum
        {
            Whole, Single,
        }
    }
}