using System;
using System.ComponentModel;

using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.ViewModel
{
    public class HydrantConnectPipeViewModel : NotifyPropertyChangedBase, ICloneable
    {
        private ThHydrantConnectPipeConfigInfo ConfigInfo = new ThHydrantConnectPipeConfigInfo();
        public ThHydrantConnectPipeConfigInfo GetConfigInfo()
        {
            return ConfigInfo;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public string MapScale
        {
            get
            {
                return ConfigInfo.strMapScale;
            }
            set
            {
                ConfigInfo.strMapScale = value;
                this.RaisePropertyChanged();
            }
        }
        public bool SetupValve
        {
            get
            {
                return ConfigInfo.isSetupValve;
            }
            set
            {
                ConfigInfo.isSetupValve = value;
                this.RaisePropertyChanged();
            }
        }
        public bool MarkSpecif
        {
            get
            {
                return ConfigInfo.isMarkSpecif;
            }
            set
            {
                ConfigInfo.isMarkSpecif = value;
                this.RaisePropertyChanged();
            }
        }
        public bool CoveredGraph
        {
            get
            {
                return ConfigInfo.isCoveredGraph;
            }
            set
            {
                ConfigInfo.isCoveredGraph = value;
                this.RaisePropertyChanged();
            }
        }
        public OutputType OutputType
        {
            get
            {
                return ConfigInfo.isTchPipe;
            }
            set
            {
                ConfigInfo.isTchPipe = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public enum OutputType
    {
        /// <summary>
        /// CAD
        /// </summary>
        [Description("CAD")]
        CAD = 0,
        /// <summary>
        /// TCH
        /// </summary>
        [Description("TCH")]
        TCH = 1,
        /// <summary>
        /// by main ring
        /// </summary>
        [Description("ByMainRing")]
        ByMainRing = 2,
    }
}
