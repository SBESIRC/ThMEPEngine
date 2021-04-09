using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.SplitMPolygon.Interface
{
    public interface ISplitMPolygon:IDisposable
    {
        List<Polyline> Split(Polyline shell,List<Polyline> holes);
    }
}
