using THMEPCore3D.Model;
using System.Collections.Generic;

namespace THMEPCore3D.Interface
{
    interface IQuery
    {
        void Query(List<ThModelCode> codes);
    }
}
