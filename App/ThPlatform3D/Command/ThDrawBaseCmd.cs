using System;
using System.Linq;
using AcHelper;
using Linq2Acad;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using ThMEPEngineCore;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThPlatform3D.Command
{
    public abstract class ThDrawBaseCmd : IAcadCommand, IDisposable
    {
        protected DBObjectCollection _collectObjs;
        protected Autodesk.AutoCAD.Colors.Color _ceColor;
        protected ObjectId _celtype;
        protected LineWeight _celweight;

        public ThDrawBaseCmd()
        {
            // 记录Cad默认的颜色、线型和线宽
            _ceColor = Active.Database.Cecolor;
            _celtype = Active.Database.Celtype;
            _celweight = Active.Database.Celweight;
            _collectObjs = new DBObjectCollection();
        }

        public virtual void Dispose()
        {
            // 重置Cad默认的颜色、线型和线宽
            Active.Database.Cecolor = _ceColor;
            Active.Database.Celtype = _celtype;
            Active.Database.Celweight = _celweight;
        }

        public abstract void Execute();

        protected void SetCurrentDbConfig(LayerTableRecord ltr)
        {
            Active.Database.Cecolor = ltr.Color;
            Active.Database.Celtype = ltr.LinetypeObjectId;
            Active.Database.Celweight = ltr.LineWeight;
        }

        protected void SetStyle(string layerName)
        {
            if(_collectObjs.Count>0)
            {
                using (var acadDb = AcadDatabase.Active())
                {
                    _collectObjs.OfType<Entity>().ForEach(o =>
                    {
                        acadDb.Element<Curve>(o.ObjectId, true);
                        o.Layer = layerName;
                        o.Linetype = "ByLayer";
                        o.LineWeight = LineWeight.ByLayer;
                        o.ColorIndex = (int)ColorIndex.BYLAYER;
                    });
                }
            }
        }

        protected void OpenLayer(List<string> layers)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                layers.ForEach(o => acadDb.Database.OpenAILayer(o));
            }
        }

        protected LayerTableRecord GetLTR(string layerName)
        {
            using (var acdb = AcadDatabase.Active())
            {
                if(acdb.Layers.Contains(layerName))
                {
                    return acdb.Layers.Element(layerName);
                }
                else
                {
                    return null;
                }
            }
        }

        protected void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is Curve)
            {
                _collectObjs.Add(e.DBObject);
            }
        }
    }
}
