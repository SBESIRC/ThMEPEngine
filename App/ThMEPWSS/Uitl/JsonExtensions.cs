using Autodesk.AutoCAD.Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ThMEPWSS.JsonExtensionsNs
{
  public static class JsonExtensions
  {
    public static string ToJson(this Point3d p) => $"{{x:{p.X},y:{p.Y},z:{p.Z}}}";
    public static Point3d JsonToPoint3d(this string json)
    {
      var jo = JsonConvert.DeserializeObject<JObject>(json);
      return new Point3d(jo["x"].ToObject<double>(), jo["y"].ToObject<double>(), jo["z"].ToObject<double>());
    }
    public static string ToJson(this object obj)
    {
      return JsonConvert.SerializeObject(obj);
    }
    public static T FromJson<T>(this string json)
    {
      return JsonConvert.DeserializeObject<T>(json);
    }
  }
}
