using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json;
using System;

namespace ThMEPEngineCore.CAD
{
    public class Vector3dConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
           return objectType == typeof(Vector3d) ? true : false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string s = (string)reader.Value;
            string[] values = s.Split(',');
            if (values.Length == 3)
            {
                double x = Convert.ToDouble(values[0]);
                double y = Convert.ToDouble(values[1]);
                double z = Convert.ToDouble(values[2]);
                return new Vector3d(x, y, z);
            }
            return Vector3d.XAxis;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Vector3d vec = (Vector3d)value;
            writer.WriteValue(vec.ToString());
        }
    }
}
