using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.CAD
{
    public class Point3dConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Point3d) ? true : false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string s = (string)reader.Value;
            string[] values= s.Split(',');
            if(values.Length==3)
            {
                double x = Convert.ToDouble(values[0]);
                double y = Convert.ToDouble(values[1]);
                double z = Convert.ToDouble(values[2]);
                return new Point3d(x,y,z);
            }
            return Point3d.Origin;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Point3d pt = (Point3d)value;
            string x = String.Format("{0:0.0000}", pt.X);
            string y = String.Format("{0:0.0000}", pt.Y);
            string z = String.Format("{0:0.0000}", pt.Z);
            writer.WriteValue(x+","+y+","+z);
        }
    }
}
