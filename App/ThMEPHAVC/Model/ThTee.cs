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
    public class ThTee
    {
        public double Angle { get; set; }
        // 中心点
        public Point3d CP { get; set; }
        // 支路管道直径
        public double BDmtr { get; set; }
        // 主路大端管道直径
        public double MBDmtr { get; set; }
        // 主路小端管道直径
        public double MSDmtr { get; set; }

        public ThTee(Point3d center_point, 
                     double branch_diameter, 
                     double main_big_diameter, 
                     double main_small_diameter)
        {
            CP = center_point;
            BDmtr = branch_diameter;
            MBDmtr = main_big_diameter;
            MSDmtr = main_small_diameter;
        }
        public void RunTeeDrawEngine(ThDbModelFan fanmodel, Matrix3d mat)
        {
            List<ThIfcDistributionElement> TeeSegments = new List<ThIfcDistributionElement>();
            DBObjectCollection Flg = CreateTeeFlangeline();
            DBObjectCollection Rep = CreateTeeGeometries(Flg);
            ThIfcDistributionElement a = new ThIfcDistributionElement
            {
                FlangeLine = Flg,
                Representation = Rep,
                Matrix = mat
            };
            TeeSegments.Add(a);
            string modelLayer = fanmodel.Data.BlockLayer;
            string ductLayer = ThDuctUtils.DuctLayerName(modelLayer);
            string flangeLinerLayer = ThDuctUtils.FlangeLayerName(modelLayer);

            DrawTeeDWG(TeeSegments, ductLayer, flangeLinerLayer);
        }

        private ObjectId CreateTeelinetype(string linetype)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLinetype(linetype, true);
                return acadDatabase.Linetypes.ElementOrDefault(linetype).ObjectId;
            }
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
        private void DrawTeeDWG(List<ThIfcDistributionElement> DuctSegments, 
                             string ductlayer, 
                             string flangelayer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var linetypeId = CreateTeelinetype("CONTINUOUS");
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
        private DBObjectCollection CreateTeeFlangeline()
        {
            //创建支路端线
            double xOft = (MBDmtr+ BDmtr) * 0.5 + 50;
            double yOft = 0.5 * BDmtr;
            double ext = 45;
            Line branchEndLine = new Line()
            {
                StartPoint = new Point3d(xOft, yOft + ext, 0),
                EndPoint = new Point3d(xOft, -yOft - ext, 0),
            };
            xOft = 0.5 * MSDmtr;
            yOft = 0.5 * BDmtr + 100;
            //创建主路小端端线
            Line mainSmallEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };
            xOft = 0.5 * MBDmtr;
            yOft = -BDmtr - 50;
            //创建主路大端端线
            Line mainBigEndLine = new Line()
            {
                StartPoint = new Point3d(xOft + ext, yOft, 0),
                EndPoint = new Point3d(-xOft - ext, yOft, 0),
            };

            return new DBObjectCollection()
            {
                branchEndLine,
                mainBigEndLine,
                mainSmallEndLine
            };
        }

        private DBObjectCollection CreateTeeGeometries(DBObjectCollection endLines)
        {
            double ext = 45;
            Line branchEndLine = endLines[0].Clone() as Line;
            branchEndLine.StartPoint = branchEndLine.StartPoint + new Vector3d(0, -ext, 0);
            branchEndLine.EndPoint = branchEndLine.EndPoint + new Vector3d(0, ext, 0);
            Line mainBigEndLine = endLines[1].Clone() as Line;
            mainBigEndLine.StartPoint = mainBigEndLine.StartPoint + new Vector3d(-ext, 0, 0);
            mainBigEndLine.EndPoint = mainBigEndLine.EndPoint + new Vector3d(ext, 0, 0);
            Line mainSmallEndLine = endLines[2].Clone() as Line;
            mainSmallEndLine.StartPoint = mainSmallEndLine.StartPoint + new Vector3d(-ext, 0, 0);
            mainSmallEndLine.EndPoint = mainSmallEndLine.EndPoint + new Vector3d(ext, 0, 0);

            //创建支路50mm直管段
            Line branchUpStraightLine = new Line()
            {
                StartPoint = branchEndLine.StartPoint,
                EndPoint = branchEndLine.StartPoint + new Vector3d(-50, 0, 0),
            };
            Line branchBelowStraightLine = new Line()
            {
                StartPoint = branchEndLine.EndPoint,
                EndPoint = branchEndLine.EndPoint + new Vector3d(-50, 0, 0),
            };

            //创建支路下侧圆弧过渡段
            Point3d circleCenter = new Point3d(0.5 * (MBDmtr + BDmtr), -BDmtr, 0);
            Arc branchInnerArc = new Arc(circleCenter, 0.5 * BDmtr, 0.5 * Math.PI, Math.PI);

            //创建支路上侧圆弧过渡段
            //首先创建主路上端小管道的内侧线作为辅助线以便于后续计算圆弧交点
            Ray branchAuxiliaryRay = new Ray()
            {
                BasePoint = mainSmallEndLine.StartPoint,
                SecondPoint = mainSmallEndLine.StartPoint + new Vector3d(0, -5000, 0)
            };
            Circle branchAuxiliaryCircle = new Circle()
            {
                Center = circleCenter,
                Radius = 1.5 * BDmtr
            };
            Point3dCollection Intersectpoints = new Point3dCollection();
            IntPtr ptr = new IntPtr();
            branchAuxiliaryRay.IntersectWith(branchAuxiliaryCircle, Intersect.OnBothOperands, Intersectpoints, ptr, ptr);
            Arc branchOuterArc = new Arc();
            if (Intersectpoints.Count != 0)
            {
                Point3d Intersectpointinarc = Intersectpoints[0];
                foreach (Point3d point in Intersectpoints)
                {
                    if (point.Y > Intersectpointinarc.Y)
                    {
                        Intersectpointinarc = point;
                    }
                }
                branchOuterArc.CreateArcSCE(branchUpStraightLine.EndPoint, circleCenter, Intersectpointinarc);
            }

            //创建主路外侧管线
            Line outerStraightLine = new Line()
            {
                StartPoint = mainBigEndLine.EndPoint,
                EndPoint = mainBigEndLine.EndPoint + new Vector3d(0, 50, 0),
            };
            Line outerObliqueLine = new Line()
            {
                StartPoint = outerStraightLine.EndPoint,
                EndPoint = mainSmallEndLine.EndPoint,
            };

            //创建主路内侧管线
            Line innerUpLine = new Line()
            {
                StartPoint = mainSmallEndLine.StartPoint,
                EndPoint = branchOuterArc.EndPoint,
            };
            Line innerBelowLine = new Line()
            {
                StartPoint = mainBigEndLine.StartPoint,
                EndPoint = branchInnerArc.EndPoint,
            };

            return new DBObjectCollection()
            {
                branchUpStraightLine,
                branchBelowStraightLine,
                branchInnerArc,
                branchOuterArc,
                outerStraightLine,
                outerObliqueLine,
                innerUpLine,
                innerBelowLine
            };
        }
    }
}
