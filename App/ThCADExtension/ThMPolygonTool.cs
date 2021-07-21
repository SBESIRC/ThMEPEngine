using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThCADExtension
{
    public static class ThMPolygonTool
    {
        public static void Initialize()
        {
            string ver = Application.GetSystemVariable("ACADVER").ToString().Substring(0, 2);
            SystemObjects.DynamicLinker.LoadModule("AcMPolygonObj" + ver + ".dbx", false, false);
        }

        public static MPolygon CreateMPolygon(Curve shell)
        {
            return CreateMPolygon(shell, new List<Curve>());
        }

        public static MPolygon CreateMPolygon(Curve shell, List<Curve> holes)
        {
            MPolygon mPolygon = new MPolygon();
            if (shell is Polyline polyline)
            {
                if (polyline.Area > 0.0)
                {
                    mPolygon.AppendLoopFromBoundary(polyline, false, 0.0);
                }
            }
            else if (shell is Polyline2d polyline2d)
            {
                mPolygon.AppendLoopFromBoundary(polyline2d, false, 0.0);
            }
            else if (shell is Circle circle)
            {
                mPolygon.AppendLoopFromBoundary(circle, false, 0.0);
            }
            else
            {
                throw new NotSupportedException();
            }

            holes.ForEach(o =>
            {
                if (o is Polyline innerPolyline)
                {
                    mPolygon.AppendLoopFromBoundary(innerPolyline, false, 0.0);
                }
                else if (o is Polyline2d innerPolyline2d)
                {
                    mPolygon.AppendLoopFromBoundary(innerPolyline2d, false, 0.0);
                }
                else if (o is Circle innerCircle)
                {
                    mPolygon.AppendLoopFromBoundary(innerCircle, false, 0.0);
                }
                else
                {
                    throw new NotSupportedException();
                }
            });

            mPolygon.SetLoopDirection(0, LoopDirection.Exterior);
            if (holes.Count > 0)
            {
                for (int i = 1; i <= holes.Count; i++)
                {
                    mPolygon.SetLoopDirection(i, LoopDirection.Interior);
                }
            }
            return mPolygon;
        }

        public static MPolygon CreateMPolygon(DBObjectCollection curves)
        {
            return CreateMPolygon(curves[0] as Curve, curves.Cast<Curve>().Skip(1).ToList());
        }

        public static MPolygon CreateMPolygon(List<DBObjectCollection> loops)
        {
            MPolygon mPolygon = new MPolygon();
            loops.ForEach(o =>
            {
                var item = CreateMPolygon(o);
                for (int i = 0; i < item.NumMPolygonLoops; i++)
                {
                    mPolygon.AppendMPolygonLoop(item.GetMPolygonLoopAt(i), false, 0.0);
                }
            });
            return mPolygon;
        }
    }
}
