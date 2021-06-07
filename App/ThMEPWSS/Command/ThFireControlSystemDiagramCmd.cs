using AcHelper.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.Command
{
    public class ThFireControlSystemDiagramCmd : IAcadCommand, IDisposable
    {
        FireControlSystemDiagramViewModel _vm;
        public ThFireControlSystemDiagramCmd(FireControlSystemDiagramViewModel vm = null)
        {
            _vm = vm;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Execute()
        {
            //todo: implement
            //_vmS
            
        }
    }
}
