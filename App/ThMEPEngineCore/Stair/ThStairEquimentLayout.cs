﻿using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Stair
{
    public class ThStairEquimentLayout
    {
        public Dictionary<Point3d, double> StairNormalLighting(Database database, List<Polyline> rooms, List<Polyline> obstacle, Point3dCollection points, double scale)
        {
            var equiment = "E-BL302";
            var engine = new ThStairLayoutEngine();
            return engine.Layout(database, rooms, obstacle, points, scale, equiment, false);
        }

        public Dictionary<Point3d, double> StairEvacuationLighting(Database database, List<Polyline> rooms, List<Polyline> obstacle, Point3dCollection points, double scale)
        {
            var equiment = "E-BFEL800";
            var engine = new ThStairLayoutEngine();
            return engine.Layout(database, rooms, obstacle, points, scale, equiment, false);
        }

        public Dictionary<Point3d, double> StairFireDetector(Database database, List<Polyline> rooms, List<Polyline> obstacle, Point3dCollection points, double scale)
        {
            var equiment = "E-BFAS110";
            var engine = new ThStairLayoutEngine();
            return engine.Layout(database, rooms, obstacle, points, scale, equiment, true);
        }

        public Dictionary<Point3d, double> StairStoreyMark(Database database, List<Polyline> rooms, List<Polyline> obstacle, Point3dCollection points, double scale)
        {
            var equiment = "E-BFEL110";
            var engine = new ThStairLayoutEngine();
            return engine.Layout(database, rooms, obstacle, points, scale, equiment, true);
        }

        public Dictionary<Point3d, double> StairBroadcast(Database database, List<Polyline> rooms, List<Polyline> obstacle, Point3dCollection points, double scale)
        {
            var equiment = "E-BFAS410-4";
            var engine = new ThStairLayoutEngine();
            return engine.Layout(database, rooms, obstacle, points, scale, equiment, true);
        }
    }
}
