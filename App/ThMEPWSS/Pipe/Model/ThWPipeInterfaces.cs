using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
namespace ThMEPWSS.Pipe.Model
{
    public interface IThWDraw
    {
        void Draw(Point3d basePt, Matrix3d mat);
        void Draw(Point3d basePt);
    } 
}
