using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.CADExtensionsNs;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    /// <summary>
    /// 管径标注
    /// </summary>
    public class ThDimModel : ThBaseModel
    {
        //文本
        public string StrText { set; get; }
        public Point3d CentralPoint;
        public ThDimModel()
        {
            StrText = "";
        }
        public override void Initialization(Entity entity)
        {
            if(entity is DBText dbText)
            {
                StrText = dbText.TextString;
                Position = dbText.Position.ToPoint2D().ToPoint3d();
            }
            else if (IsTianZhengElement(entity))
            {
                List<Entity>ents=new List<Entity>() { entity};
                List<DBText> texts = new List<DBText>();
                while (true)
                {
                    bool found=false;
                    for (int i = 0; i < ents.Count; i++)
                    {
                        if (ents[i] is DBText text)
                        {
                            texts.Add(text);
                            ents.RemoveAt(i);
                            i--;
                        }
                        else if (IsTianZhengElement(ents[i]))
                        {
                            found = true;
                            var ent = ents[i];
                            ents.RemoveAt(i);
                            ents.AddRange(ent.ExplodeToDBObjectCollection().OfType<Entity>());
                            break;
                        }
                    }
                    if (!found) break;
                }
                if (texts.Count > 0)
                {
                    StrText = texts[0].TextString;
                    Position = texts[0].Position.ToPoint2D().ToPoint3d();
                }
            }
        }
    }
}
