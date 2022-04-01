using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSDBTextCollection
    {
        public Point3d FirstPosition { get; set; }
        public Vector3d Direction { get; set; }
        public List<Tuple<List<string>, ObjectId>> Texts { get; set; }
    }
}
