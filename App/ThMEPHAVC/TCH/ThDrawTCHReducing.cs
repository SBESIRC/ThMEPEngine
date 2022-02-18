using System.Collections.Generic;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.TCH
{
    public class ThDrawTCHReducing
    {
        private ThSQLiteHelper sqliteHelper;
        private TCHReducingParam reducingParam;
        
        public ThDrawTCHReducing(ThSQLiteHelper sqliteHelper)
        {
            this.sqliteHelper = sqliteHelper;
        }
        public void Draw(List<ReducingInfo> segInfos, ref ulong gId)
        {

        }
        
    }
}