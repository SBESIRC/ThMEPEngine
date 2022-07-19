using ProtoBuf;
using System.Collections.Generic;

namespace ThMEPTCH.Model.SurrogateModel
{
    [ProtoContract]
    public struct Point3DCollectionSurrogate
    {
        public Point3DCollectionSurrogate(List<Point3DSurrogate> pts) : this()
        {
            this.Points = pts;
        }

        [ProtoMember(1)]
        public List<Point3DSurrogate> Points { get; set; }
    }
}
