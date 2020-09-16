using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.CAD
{
    public class BeamTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BeamComponentType) ? true : false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string s = (string)reader.Value;
            if (s == "主梁")
            {
                return BeamComponentType.PrimaryBeam;
            }
            if (s == "半主梁")
            {
                return BeamComponentType.HalfPrimaryBeam;
            }
            if (s == "悬挑主梁")
            {
                return BeamComponentType.OverhangingPrimaryBeam;
            }
            if (s == "次梁")
            {
                return BeamComponentType.SecondaryBeam;
            }
            return BeamComponentType.Undefined;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            BeamComponentType beamType = (BeamComponentType)value;
            switch (beamType)
            {
                case BeamComponentType.PrimaryBeam:
                    writer.WriteValue("主梁");
                    break;
                case BeamComponentType.HalfPrimaryBeam:
                    writer.WriteValue("半主梁");
                    break;
                case BeamComponentType.OverhangingPrimaryBeam:
                    writer.WriteValue("悬挑主梁");
                    break;
                case BeamComponentType.SecondaryBeam:
                    writer.WriteValue("次梁");
                    break;
            }            
        }
    }
}
