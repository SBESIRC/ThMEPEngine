using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShowProgressBar
{
    // references:
    //  https://drive-cad-with-code.blogspot.com/2015/04/showing-progress-for-long-code.html
    public class ProcessingProgressBar : IDisposable
    {
        private dlgProgress _dlg = null;
        private ILongProcessingObject _executingObject;

        public ProcessingProgressBar(ILongProcessingObject executingObj)
        {
            _executingObject = executingObj;
        }

        public void Start()
        {
            _dlg = new dlgProgress(_executingObject);
            _dlg.ShowDialog();
        }

        public void Dispose()
        {
            if (_dlg!=null)
            {
                _dlg.Dispose();
            }
        }
    }
}
