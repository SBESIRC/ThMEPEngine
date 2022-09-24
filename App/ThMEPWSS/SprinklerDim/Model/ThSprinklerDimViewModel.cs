using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
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
            TCHDBPath = Path.GetTempPath() + "TG20.db";
        }
    }
}
