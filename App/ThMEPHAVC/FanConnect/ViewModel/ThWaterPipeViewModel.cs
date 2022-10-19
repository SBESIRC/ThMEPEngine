using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

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
        public string FrictionCoeff
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.FrictionCoeff; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.FrictionCoeff = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsACPipeDim
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.IsACPipeDim; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.IsACPipeDim = value;
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<ThACPipeDimConfigFile> ACPipeDimConfigFileList
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.ACPipeDimConfigFileList; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.ACPipeDimConfigFileList = value;
                this.RaisePropertyChanged();
            }
        }
        public ThACPipeDimConfigFile ACPipeDimConfigFile
        {
            get { return WaterPipeConfigInfo.WaterSystemConfigInfo.ACPipeDimConfigFile; }
            set
            {
                WaterPipeConfigInfo.WaterSystemConfigInfo.ACPipeDimConfigFile = value;
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
        public string MapScale
        {
            get { return WaterPipeConfigInfo.WaterValveConfigInfo.MapScale; }
            set
            {
                WaterPipeConfigInfo.WaterValveConfigInfo.MapScale = value;
                this.RaisePropertyChanged();
            }
        }
        public string RoomCount
        {
            get
            {
                return WaterPipeConfigInfo.WaterValveConfigInfo.RoomObb.Count.ToString();
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }

        public ICommand ACConfigFileCmd => new RelayCommand(ACConfigFile);
        private void ACConfigFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Title = "请选择冷媒管管径数据文件";
            dialog.Filter = string.Format("冷媒管管径数据文件(*.{0})|*.{0}", "xlsx");
           
            if (dialog.ShowDialog() == true)
            {
                string fullPath = dialog.FileName;
                if (ACPipeDimConfigFileList.Where(x => x.FullPath == fullPath).Any() == false)
                {
                    var newFile = new ThACPipeDimConfigFile(fullPath);
                    ACPipeDimConfigFileList.Add(newFile);
                    ACPipeDimConfigFile = ACPipeDimConfigFileList.Last();
                }
            }
        }
    }
}
