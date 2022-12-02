using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Diagram.ViewModel;

namespace ThMEPWSS.ExerciseProj
{
    
    public class Data
    {
        public List<VerticlePipe> VerticlePipes { get; set; }
        public List<Pump> pumps
        {
            get;set;
        }
        public List<HorizontalPipe> horizontalPipes { get; set; }
        public List<BlkRain> blkRains { get; set; }
        public Data()
        {
            
        }
    }
    public class ExerciseFunc
    {
        public ExerciseFunc()
        {

        }
        public List<Entity> GetHorizontalPipe()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {

                var ents = acadDatabase.ModelSpace.OfType<Entity>().ToList();//取出cad数据库的所有元素的列表A
                var ents_in_block_PSHG = ents.Where(e =>
                {
                    var cond_matchlayer = e.Layer.Equals("W-FRPT-DRAI-PIPE");
                    cond_matchlayer = cond_matchlayer || (e.Layer.Contains("W-") && e.Layer.Contains("-DRAI-") && e.Layer.Contains("-PIPE"));
                    return cond_matchlayer;
                }).ToList();
                var ents_in_block_PSHG_aim = ents_in_block_PSHG.Where(e =>
                {
                    var cond_match = e is Line || e is Polyline || IsTianZhengElement(e);
                    return cond_match;
                }).ToList();
                return ents_in_block_PSHG_aim;

            }
        }
        public List<Circle> GetVerticalPipe()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var ents = acadDatabase.ModelSpace.OfType<Entity>().ToList();//取出cad数据库的所有元素的列表A
                var ents_in_block_PSLG = ents.Where(e =>
                {
                    var cond_matchlayer = e.Layer.Equals("W-DRAI-EQPM") || e.Layer.Equals("W-RAIN-EQPM");
                    return cond_matchlayer;
                }).Where(e => e is BlockReference).Select(e => (BlockReference)e).ToList();
                ents_in_block_PSLG = ents_in_block_PSLG.Where(e => e.GetEffectiveName().Contains("带定位立管")).ToList();
                ents_in_block_PSLG = ents_in_block_PSLG.Where(e =>
                {
                    return true;
                }).ToList();
                var ents_in_block_Circle = ents.Where(e =>
                {
                    if (e is Circle)
                        return true;
                    return false;
                }).Select(e => (Circle)e).Where(e => e.Radius <= 300).ToList();

                return ents_in_block_Circle;
            }
        }
        public List<BlockReference> GetRainBlk()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var ents = acadDatabase.ModelSpace.OfType<Entity>().ToList();//取出cad数据库的所有元素的列表A
                var ents_for_rain = ents.Where(e =>
                {
                    var ents_match = e.Layer.Equals("重力流雨水井编号") || e.Layer.Equals("压力流雨水井编号");
                    return ents_match;
                }).Where(e => e is BlockReference).Select(e => (BlockReference)e).ToList();
                return ents_for_rain;
            }
        }
        public List<BlockReference> GetDivingDump()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var ents = acadDatabase.ModelSpace.OfType<Entity>().ToList();//取出cad数据库的所有元素的列表A
                var ents_for_divingpump = ents.Where(e =>
                {
                   
                    var ents_match = (e.Layer.Equals("潜水泵-AI") || e.Layer.Equals("潜水泵"));
                    return ents_match;
                }).Where(e => e is BlockReference).Select(e => (BlockReference)e).ToList();
                ents_for_divingpump = ents_for_divingpump.Where(e =>
                {
                    return true;
                }).ToList();

                return ents_for_divingpump;
            }
        }

        public  bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }
        


    }

    public class CCCCC
    {
        public CCCCC(int index)
        {
            Index = index;
        }

        public int Index { get; }
        public int Count { get; set; }
    }

    public class ExerciseService
    {
        public Data Datas { get; set; }
        public ExerciseViewmodel Viewmodel { get; set; }
        public ExerciseService()
        {
            

        }
        public void Process()
        {
            Datas = new Data();
            ExerciseFunc func=new ExerciseFunc();
            Datas.horizontalPipes = func.GetHorizontalPipe().Select(e => new HorizontalPipe(e)).ToList();
            Datas.VerticlePipes = func.GetVerticalPipe().Select(e => new VerticlePipe(e)).ToList();
            Datas.blkRains = func.GetRainBlk().Select(e => new BlkRain(e)).ToList();
            Datas.pumps = func.GetDivingDump().Select(e => new Pump(e)).ToList();

        }
        public static bool IsTianZhengElement(Entity ent)
        {
            return ThMEPEngineCore.Algorithm.ThMEPTCHService.IsTCHElement(ent);
        }
    }
}
