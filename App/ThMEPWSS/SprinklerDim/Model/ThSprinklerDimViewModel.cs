using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

using Autodesk.AutoCAD.ApplicationServices;

using AcHelper;
using AcHelper.Commands;
using ThControlLibraryWPF.ControlUtils;

using ThMEPWSS.SprinklerDim.Cmd;


namespace ThMEPWSS.SprinklerDim.Model
{
    public class ThSprinklerDimViewModel : NotifyPropertyChangedBase
    {
        private int _UseTCHDim { get; set; }
        public int UseTCHDim
        {
            get { return _UseTCHDim; }
            set
            {
                _UseTCHDim = value;
                this.RaisePropertyChanged();
            }
        }

        private int _Scale { get; set; }
        public int Scale
        {
            get { return _Scale; }
            set
            {
                _Scale = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<int> _ScaleList { get; set; }
        public ObservableCollection<int> ScaleList
        {
            get { return _ScaleList; }
            set
            {
                _ScaleList = value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand SprinklerDimLayoutCmd => new RelayCommand(SprinklerDimLayout);
        private void SprinklerDimLayout()
        {
            ThMEPWSS.Common.Utils.FocusToCAD();
            using (var cmd = new ThSprinklerDimCmd(this))
            {
                cmd.Execute();
                ThMEPWSS.Common.Utils.FocusToCAD();

                if (UseTCHDim == 1)
                {
                    //CommandHandlerBase.ExecuteFromCommandLine(false, "THTCHPIPIMP");
                    Active.Document.SendCommand("THTCHPIPIMP" + "\n");
                    ThMEPWSS.Common.Utils.FocusToCAD();
                    DeleteDBFile();
                }
            }
        }

        public ICommand SprinklerDimHelpCmd => new RelayCommand(SprinklerDimHelp);
        private void SprinklerDimHelp()
        {
            //System.Diagnostics.Process.Start(@"");
        }


        public string TCHDBPath { get; set; }
        public void DeleteDBFile()
        {
            if (string.IsNullOrEmpty(TCHDBPath) || !File.Exists(TCHDBPath))
                return;
            if (File.Exists(TCHDBPath))
                File.Delete(TCHDBPath);
        }
        public ThSprinklerDimViewModel()
        {
            UseTCHDim = 1;
            Scale = 100;
            ScaleList = new ObservableCollection<int>() { 100, 150 };
            TCHDBPath = Path.GetTempPath() + "TG20.db";
        }
    }
}
