using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Interface
{
    interface ISpatialIndex: IDisposable
    {
        DBObjectCollection SelectCrossingPolygon(Polyline polyline);
        DBObjectCollection SelectCrossingPolygon(Point3dCollection polygon);
        DBObjectCollection SelectCrossingPolygon(MPolygon mPolygon);
        DBObjectCollection SelectCrossingWindow(Point3d pt1, Point3d pt2);
        DBObjectCollection SelectWindowPolygon(Polyline polyline);
        DBObjectCollection SelectWindowPolygon(MPolygon mPolygon);
        DBObjectCollection SelectFence(Polyline polyline);
        DBObjectCollection SelectFence(Line line);
        DBObjectCollection SelectAll();
    }
}
