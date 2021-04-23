using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BuildRoom.Interface
{
    public interface IRoomBuilder: IDisposable
    {
        List<Entity> Outlines { get;}

        List<Entity> FindRooms(Point3d pt);

        void Build(IRoomBuildData roomData);
    }
}
