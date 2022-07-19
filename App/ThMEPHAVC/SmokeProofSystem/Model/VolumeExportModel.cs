using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.Model
{
    public class VolumeExportModel
    {
        public string ScenarioTitle { get; set; }

        public BaseSmokeProofViewModel baseSmokeProofViewModel { get; set; }
    }
}
