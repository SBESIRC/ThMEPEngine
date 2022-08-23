﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPWSS.SprinklerDim.Model;

namespace ThMEPWSS.SprinklerDim.Service
{
    public class ThDataTransformService
    {

        public static ThCADCoreNTSSpatialIndex GenerateSpatialIndex(List<Polyline> reference)
        {
            DBObjectCollection dboc = ChangeToDboc(reference);
            ThCADCoreNTSSpatialIndex si = new ThCADCoreNTSSpatialIndex(dboc);
            return si;
        }

        public static ThCADCoreNTSSpatialIndex GenerateSpatialIndex(List<Line> references)
        {
            // 把参考物拆解为线,做成空间索引
            DBObjectCollection dboc = ChangeToDboc(references);
            ThCADCoreNTSSpatialIndex si = new ThCADCoreNTSSpatialIndex(dboc);
            return si;
        }

        public static List<Polyline> GetBothPolylinesAndLines(DBObjectCollection dboc)
        {
            List<Polyline> polylines = new List<Polyline>();

            polylines.AddRange(GetPolylines(dboc));
            polylines.AddRange(Change(GetLines(dboc)));
            
            return polylines;
        }

        public static List<Line> GetLines(DBObjectCollection dboc)
        {
            List<Line> lines = new List<Line>();
            foreach (Line l in dboc.OfType<Line>())
            {
                lines.Add(l);
            }

            return lines;
        }

        public static List<Polyline> GetPolylines(DBObjectCollection dboc)
        {
            List<Polyline> polylines = new List<Polyline>();
            foreach (Polyline p in dboc.OfType<Polyline>())
            {
                polylines.Add(p);
            }

            return polylines;
        }

        public static List<Point3d> GetPoints(List<Point3d> pts, List<int> idxs)
        {
            List<Point3d> line = new List<Point3d>();

            foreach (int i in idxs)
            {
                line.Add(pts[i]);
            }

            return line;
        }

        public static List<Polyline> Change(List<Line> lines)
        {
            List<Polyline> polylines = new List<Polyline>();
            foreach (var l in lines)
            {
                Polyline p = new Polyline();
                p.AddVertexAt(0, l.StartPoint.ToPoint2d(), 0, 0, 0);
                p.AddVertexAt(1, l.EndPoint.ToPoint2d(), 0, 0, 0);
                polylines.Add(p);
            }

            return polylines;
        }

        public static List<Line> Change(List<Polyline> polylines)
        {
            List<Line> lines = new List<Line>();

            foreach (Polyline p in polylines)
            {
                for (int i = 0; i < p.NumberOfVertices-1; i++)
                {
                    lines.Add(new Line(p.GetPoint3dAt(i), p.GetPoint3dAt(i + 1)));
                }
                if(p.Closed)
                    lines.Add(new Line(p.GetPoint3dAt(0), p.GetPoint3dAt(p.NumberOfVertices-1)));
            }

            return lines;
        }

        public static List<Polyline> Change(List<MPolygon> mPolygons)
        {
            List<Polyline> lines = new List<Polyline>();

            foreach (MPolygon mPolygon in mPolygons)
            {
                lines.AddRange(Change(mPolygon));
            }

            return lines;
        }

        public static List<Polyline> Change(MPolygon mPolygon)
        {
            List<Polyline> lines = new List<Polyline>();

            lines.Add(mPolygon.Shell());
            foreach(Polyline hole in mPolygon.Holes())
            {
                lines.Add(hole);
            }

            return lines;
        }

        public static DBObjectCollection ChangeToDboc(List<Polyline> polylines)
        {
            DBObjectCollection dboc = new DBObjectCollection();

            foreach(Polyline polyline in polylines)
            {
                dboc.Add(polyline);
            }

            return dboc;
        }

        public static DBObjectCollection ChangeToDboc(List<Line> lines)
        {
            DBObjectCollection dboc = new DBObjectCollection();

            foreach (Line line in lines)
            {
                dboc.Add(line);
            }

            return dboc;
        }

        public static List<List<int>> Mix(List<List<List<int>>> arrLists)
        {
            List<List<int>> list = new List<List<int>>();

            foreach (List<List<int>> l in arrLists)
            {
                list.AddRange(l);
            }

            return list;
        }

        public static List<int> Mix(List<List<int>> arrLists)
        {
            List<int> list = new List<int>();

            foreach (List<int> l in arrLists)
            {
                list.AddRange(l);
            }

            return list;
        }

        public static List<int> GetDim(List<ThSprinklerDimGroup> dimGroup)
        {
            return dimGroup.Select(d => d.pt).ToList();
        }

        public static List<int> GetDimedPts(List<ThSprinklerDimGroup> dimGroup)
        {
            return Mix(dimGroup.Select(d => d.PtsDimed).ToList());
        }


    }
}