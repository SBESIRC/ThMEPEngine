using AcHelper;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHAVC.Duct.PipeFitting
{
    class DrawPipeEngine
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly DrawPipeEngine instance = new DrawPipeEngine();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static DrawPipeEngine() { }
        internal DrawPipeEngine() { }
        public static DrawPipeEngine Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        public PipeingFittings FittingsInPipe { get; set; }
        public List<ThReducing> Reducings { get; set; }
        public List<ThElbow> Elbows { get; set; }
        public List<ThTee> Tees { get; set; }
        public List<ThFourWay> FourWays { get; set; }


        public void DrawPipeFittingsInCAD(Point3d position)
        {
            var reducingworker = new PipeFittingDrawWorker();
            Matrix3d mt = Active.Editor.CurrentUserCoordinateSystem;
            foreach (var reducing in Reducings)
            {
                mt = mt * Matrix3d.Displacement(position - reducing.Parameters.StartCenterPoint) * Matrix3d.Rotation(reducing.Parameters.RotateAngle * Math.PI / 180, Vector3d.ZAxis, reducing.Parameters.StartCenterPoint);
                reducingworker.DrawPipeFitting(reducing, mt);
            }
            foreach (var elbow in Elbows)
            {
                mt = mt * Matrix3d.Displacement(position - elbow.Parameters.CornerPoint) * Matrix3d.Rotation(elbow.Parameters.RotateAngle * Math.PI / 180, Vector3d.ZAxis, elbow.Parameters.CornerPoint);
                reducingworker.DrawPipeFitting(elbow, mt);
            }
            foreach (var tee in Tees)
            {
                mt = mt * Matrix3d.Displacement(position - tee.Parameters.CenterPoint) * Matrix3d.Rotation(tee.Parameters.RotateAngle * Math.PI / 180, Vector3d.ZAxis, tee.Parameters.CenterPoint);
                reducingworker.DrawPipeFitting(tee, mt);
            }
            foreach (var fourway in FourWays)
            {
                mt = mt * Matrix3d.Displacement(position - fourway.Parameters.FourWayCenter) * Matrix3d.Rotation(fourway.Parameters.RotateAngle * Math.PI / 180, Vector3d.ZAxis, fourway.Parameters.FourWayCenter);
                reducingworker.DrawPipeFitting(fourway, mt);
            }

        }

        public void InitializeEngine()
        {
            Reducings = new List<ThReducing>();
            Elbows = new List<ThElbow>();
            Tees = new List<ThTee>();
            FourWays = new List<ThFourWay>();
        }
    }

    class PipeingFittings
    {
        public List<ThReducing> Reducings { get; set; }
        public List<ThElbow> Elbows { get; set; }
        public List<ThTee> Tees { get; set; }
        public List<ThFourWay> FourWays { get; set; }

    }
}
