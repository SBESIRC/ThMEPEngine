using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using Linq2Acad;
using ThCADCore.NTS;

namespace ThMEPLighting.ParkingStall.CAD
{
    public class InfoReader
    {
        private Point3dCollection m_previewWindow;

        public List<Polyline> ParkingStallPolys
        {
            get;
            set;
        } = new List<Polyline>();

        public InfoReader(Point3dCollection preViewWindow)
        {
            m_previewWindow = preViewWindow;
        }

        public static List<Polyline> MakeParkingStallPolys(Point3dCollection preViewWindow)
        {
            var infoReader = new InfoReader(preViewWindow);
            infoReader.Do();
            return infoReader.ParkingStallPolys;
        }

        public void Do()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var parkingStallRecognitionEngine = new ThParkingStallRecognitionEngine();
                parkingStallRecognitionEngine.Recognize(acadDb.Database, m_previewWindow);

                foreach (var space in parkingStallRecognitionEngine.Spaces)
                {
                    if (space.Boundary is Polyline polyline)
                    {
                        foreach (var entity in polyline.Buffer(ParkingStallCommon.ParkingPolyEnlargeLength))
                        {
                            if (entity is Polyline poly && poly.Closed)
                                ParkingStallPolys.Add(poly);
                        }
                    }
                }
            }
        }
    }
}
