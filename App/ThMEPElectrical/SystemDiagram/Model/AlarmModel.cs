using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPElectrical.SystemDiagram.Model
{
    public class AlarmModel
    {
        public AlarmModel()
        {
            AlarmList = new List<Tuple<string, Point3d>>();
            UiAlarmList = new List<Tuple<string, ObjectId>>();
        }
        public Document Doc { get; set; }
        public List<Tuple<string, Point3d>> AlarmList { get; set; }
        public List<Tuple<string,ObjectId>> UiAlarmList { get;set; }
    }
}
