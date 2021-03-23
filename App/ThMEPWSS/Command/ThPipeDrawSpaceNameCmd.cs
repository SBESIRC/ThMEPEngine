using System;
using Linq2Acad;
using AcHelper.Commands;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Command
{
    public class ThPipeDrawSpaceNameCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var Drawing = new ThDrawDbSpaceNameService();
                Drawing.Draw();
            }
        }
    }
}
