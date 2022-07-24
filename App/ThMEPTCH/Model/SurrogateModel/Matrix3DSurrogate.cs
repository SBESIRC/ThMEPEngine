using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPTCH.Model.SurrogateModel
{
    [ProtoContract]
    public struct Matrix3DSurrogate
    {
        public Matrix3DSurrogate(double[] data) : this()
        {
            this.Data = data;
        }

        [ProtoMember(1)]
        public double[] Data { get; set; }

        public static implicit operator Matrix3d(Matrix3DSurrogate surrogate)
        {
            return new Matrix3d(surrogate.Data);
        }

        public static implicit operator Matrix3DSurrogate(Matrix3d matrix)
        {
            return new Matrix3DSurrogate(matrix.ToArray());
        }
    }
}
