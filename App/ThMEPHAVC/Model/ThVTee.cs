using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThVTee
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public double W { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public double H { get; set; }

        /// <summary>
        /// 中心点到起始点的距离为K * 100
        /// </summary>
        public double K { get; set; }

        public ThVTee(double width, double height, double step)
        {
            K = step;
            W = width;
            H = height;
        }

        public void RunVTeeDrawEngine(ThDbModelFan fanmodel, string linetype, double angle, Vector3d dis_vec)
        {
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            string flangeLinerLayer = ThDuctUtils.FlangeLayerName(modelLayer);
            List<ThIfcDistributionElement> OutletVTeeSeg = new List<ThIfcDistributionElement>();
            SetVTee(dis_vec, OutletVTeeSeg, angle);
            DrawVTeeDWG(OutletVTeeSeg, ductLayer, flangeLinerLayer, linetype);
        }
        public ThIfcDistributionElement CreateVTeeBlock()
        {
            return new ThIfcDistributionElement()
            {
                FlangeLine = CreateVTeeFlange(),
                Representation = CreateVTeeGeo()
            };
        }
        public DBObjectCollection CreateVTeeFlange()
        {
            double extCoef = K > 20 ? 20 : K;
            double hw = W / 2;
            double hh = H / 2;
            double ext = 45;

            Point3d dL = new Point3d(-hw - ext, -hh - ext, 0);
            Point3d uL = new Point3d(-hw - ext, hh + ext, 0);
            Point3d dR = new Point3d(hw + ext, -hh - ext, 0);
            Point3d uR = new Point3d(hw + ext, hh + ext, 0);

            var points = new Point3dCollection() { uR, dR, dL, uL };
            var frame = new Polyline() { Closed = true };
            frame.CreatePolyline(points);
            Line intverL = new Line(uL + new Vector3d(-ext, 0, 0),
                                    dL + new Vector3d(-ext, 0, 0));
            double wallLen = extCoef * 100 + 350;//350-> is half fan len
            DBObjectCollection dbobj1 = new DBObjectCollection() { frame, intverL };
            foreach (Curve c in dbobj1)
            {
                c.TransformBy(Matrix3d.Displacement(new Vector3d(wallLen, 0, 0)));
            }
            Polyline cframe = frame.Clone() as Polyline;
            Line cintverL = intverL.Clone() as Line;
            DBObjectCollection dbobj2 = new DBObjectCollection() { cframe, cintverL };

            foreach (Curve c in dbobj2)
            {
                c.TransformBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, hh, 0), new Point3d(0, -hh, 0))));
            }
            return new DBObjectCollection() { frame, intverL, cframe, cintverL };
        }
        public DBObjectCollection CreateVTeeGeo()
        {
            double extCoef = K > 20 ? 20 : K;
            double hw = W / 2;
            double hh = H / 2;

            Point3d dL = new Point3d(-hw, -hh, 0);
            Point3d uL = new Point3d(-hw, hh, 0);
            Point3d dR = new Point3d(hw, -hh, 0);
            Point3d uR = new Point3d(hw, hh, 0);
            var points = new Point3dCollection() { uR, dL, dR, uL };
            var frame = new Polyline() { Closed = true };
            frame.CreatePolyline(points);
            Line closeL1 = new Line(uL, dL);
            Line closeL2 = new Line(uR, dR);

            double wallLen = extCoef * 100 + 350;//350-> is half fan len
            Line w1 = new Line(dL, new Point3d(-wallLen, -hh, 0));
            Line w2 = new Line(uL, new Point3d(-wallLen, hh, 0));
            DBObjectCollection dbobj1 = new DBObjectCollection() { frame, closeL1, closeL2, w1, w2 };
            foreach (Curve c in dbobj1)
            {
                c.TransformBy(Matrix3d.Displacement(new Vector3d(wallLen, 0, 0)));
            }
            Polyline cframe = frame.Clone() as Polyline;
            Line ccloseL1 = closeL1.Clone() as Line;
            Line ccloseL2 = closeL2.Clone() as Line;
            Line cw1 = w1.Clone() as Line;
            Line cw2 = w2.Clone() as Line;
            DBObjectCollection dbobj2 = new DBObjectCollection() { cframe, ccloseL1, ccloseL2, cw1, cw2 };

            foreach (Curve c in dbobj2)
            {
                c.TransformBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, hh, 0), new Point3d(0, -hh, 0))));
            }

            return new DBObjectCollection() { frame, closeL1, closeL2, w1, w2,
                                             cframe, ccloseL1, ccloseL2, cw1, cw2 };
        }
        private void SetVTee(Vector3d disVec, List<ThIfcDistributionElement> OutletVTeeSeg, double angle)
        {
            var ductSegment = CreateVTeeBlock();
            ductSegment.Matrix = Matrix3d.Displacement(disVec) *
                                 Matrix3d.Rotation(angle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            OutletVTeeSeg.Add(ductSegment);
        }
        private ObjectId CreateLayer(string name)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(name);
                acadDatabase.Database.UnOffLayer(name);
                acadDatabase.Database.UnLockLayer(name);
                acadDatabase.Database.UnPrintLayer(name);
                acadDatabase.Database.UnFrozenLayer(name);
                return acadDatabase.Layers.ElementOrDefault(name).ObjectId;
            }
        }
        private ObjectId CreateVTeelinetype(string linetype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLinetype(linetype, true);
                return acadDatabase.Linetypes.ElementOrDefault(linetype).ObjectId;
            }
        }
        private void DrawVTeeDWG(List<ThIfcDistributionElement> DuctSegments, 
                                 string ductlayer, 
                                 string flangelayer,
                                 string linetype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var linetypeId = CreateVTeelinetype(linetype);
                foreach (var Segment in DuctSegments)
                {
                    // 绘制风管
                    var layerId = CreateLayer(ductlayer);
                    foreach (Curve dbobj in Segment.Representation)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }
                    // 绘制法兰线
                    layerId = CreateLayer(flangelayer);
                    foreach (Curve dbobj in Segment.FlangeLine)
                    {
                        dbobj.ColorIndex = 256;
                        dbobj.LayerId = layerId;
                        dbobj.LinetypeId = linetypeId;
                        dbobj.TransformBy(Segment.Matrix);
                        acadDatabase.ModelSpace.Add(dbobj);
                        dbobj.SetDatabaseDefaults();
                    }
                }
            }
        }
    }
}
