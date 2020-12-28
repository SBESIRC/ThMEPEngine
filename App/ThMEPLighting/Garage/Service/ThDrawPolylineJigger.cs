using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.GraphicsInterface;
using MgdAcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace ThMEPLighting.Garage.Service
{
    public class ThDrawPolylineJigger : DrawJig
    {
        private Point3dCollection mAllVertexes = new Point3dCollection();
        private Point3d mLastVertex;
        public ThDrawPolylineJigger()
        {
        }
        ~ThDrawPolylineJigger()
        {
        }
        public Point3dCollection AllVertexes
        { 
            get
            {
                return mAllVertexes;
            }
        }

        public Point3d LastVertex
        {
            get { return mLastVertex; }
            set { mLastVertex = value; }
        }
        private Editor Editor
        {
            get
            {
                return MgdAcApplication.DocumentManager.MdiActiveDocument.Editor;
            }
        }
        public Matrix3d UCS
        {
            get
            {
                return MgdAcApplication.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
            }
        }

        public Point3dCollection WcsVertexes
        {
            get
            {
                var pts = new Point3dCollection();
                foreach (Point3d pt in mAllVertexes)
                {
                    pts.Add(pt.TransformBy(UCS));
                }
                return pts;
            }
        }

        protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
        {
            WorldGeometry geo = draw.Geometry;
            if (geo != null)
            {
                geo.PushModelTransform(UCS);

                Point3dCollection tempPts = new Point3dCollection();
                foreach (Point3d pt in mAllVertexes)
                {
                    tempPts.Add(pt);
                }
                if (mLastVertex != null)
                    tempPts.Add(mLastVertex);
                if (tempPts.Count > 1)
                    geo.Polyline(tempPts, Vector3d.ZAxis, IntPtr.Zero);

                geo.PopModelTransform();
            }

            return true;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\n请指定下一点或[确定(Enter)]");
            if(mAllVertexes.Count==0)
            {
                prOptions1.UserInputControls =
                UserInputControls.NullResponseAccepted |
                UserInputControls.Accept3dCoordinates |
                UserInputControls.GovernedByUCSDetect;
            }
            else
            {
                prOptions1.UserInputControls =
                UserInputControls.NullResponseAccepted |
                UserInputControls.Accept3dCoordinates |
                UserInputControls.GovernedByOrthoMode |
                UserInputControls.GovernedByUCSDetect;
            }
            PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
            if (prResult1.Status == PromptStatus.Cancel || 
                prResult1.Status == PromptStatus.Error)
            {
                return SamplerStatus.Cancel;
            }
            Point3d tempPt = prResult1.Value.TransformBy(UCS.Inverse());
            if (tempPt.DistanceTo(mLastVertex) <= 1.0)
            {
                return SamplerStatus.NoChange;
            }
            mLastVertex = tempPt;
            return SamplerStatus.OK;
        }
    }
}
