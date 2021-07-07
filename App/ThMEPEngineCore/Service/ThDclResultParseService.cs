using System.IO;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThDclResultParseService
    {
        public static List<DclInfo> Parse(string content)
        {
            var results = new List<DclInfo>();
            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(content))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                var features = serializer.Deserialize<FeatureCollection>(jsonReader);
                features.ForEach(f =>
                {
                    var info = new DclInfo();
                    if (f.Geometry != null)
                    {
                        if (f.Geometry is Point point)
                        {
                            info.Position = point.ToAcGePoint3d();
                        }
                    }
                    if (f.Attributes != null)
                    {
                        if (f.Attributes.Exists("Class"))
                        {
                            info.Class = f.Attributes["Class"].ToString();
                        }
                        if (f.Attributes.Exists("Floor"))
                        {
                            info.Floor = f.Attributes["Floor"].ToString();
                        }
                        if (f.Attributes.Exists("Id"))
                        {
                            info.Id = f.Attributes["Id"].ToString();
                        }
                        if (f.Attributes.Exists("Pinned"))
                        {
                            info.Pinned = f.Attributes["Pinned"].ToString();
                        }
                        if (f.Attributes.Exists("Cond"))
                        {
                            info.Cond = f.Attributes["Cond"].ToString();
                        }
                    }
                    results.Add(info);
                });
            }
            return results;
        }
    }
    public class DclInfo
    {
        public Point3d Position { get; set; }
        /// <summary>
        /// class分AB两类
        /// A类表示从上面引至本层
        /// B类表示从本层引下去
        /// </summary>
        public string Class { get; set; } = "";
        /// <summary>
        /// Floor代表所在层数
        /// 普通楼层用F+楼层号表示，大屋面用R表示
        /// </summary>
        public string Floor { get; set; } = "";
        /// <summary>
        /// 引下线编号
        /// </summary>
        public string Id { get; set; } = "";
        /// <summary>
        /// A类引下线位点会额外多出一个Pinned属性，
        /// 这是一个中间信息，代表该位点是否从楼上引下来，
        /// 或者距离某凸点最近，不能被算法淘汰掉
        /// </summary>
        public string Pinned { get; set; } = "";
        /// <summary>
        /// B类引下线位点会多出一个Cond属性，Cond分三类，
        /// THROUGH表示能直接贯通到下层，
        /// SHIFT表示竖向构件可以贯通，但是位点坐标需要移动，
        /// DISRUPT表示竖向构件不贯通，是通过横梁找到新的竖向构件布置的位点
        /// </summary>
        public string Cond { get; set; } = "";

        public MText CreateText()
        {
            var text = new MText();            
            text.Location = this.Position;
            text.Contents =
                "Class" + Class + "\n" +
                "Floor" + Floor + "\n" +
                "Id" + Id + "\n" +
                "Pinned" + Pinned + "\n" +
                "Cond" + Cond;
            text.Width = 50;
            text.Linetype = "ByLayer";
            text.ColorIndex = (int)ColorIndex.BYLAYER;
            text.LineWeight = LineWeight.ByLayer;
            return text;
        }
    }
}
