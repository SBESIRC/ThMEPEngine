using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    public class ModelValidator
    {
        //返回false时是不需要去校验的，返回true时是需要去校验的
        public bool CheckModel(dynamic model)
        {
            if (Convert.ToInt32(model.Load) == 0)
            { return false; }
            else
            { return true; }
        }

        //返回false时，是不需要去变颜色的
        public bool CheckColorModel(dynamic model)
        {
            if (Convert.ToInt32(model.Load) == 1)
            {
                if (model.OverAk < 3.2)
                {
                    if (model.TotalVolume < 0.75 * model.AAAA || model.TotalVolume > 0.75 * model.BBBB)
                    {
                        return true;
                    }
                    else return false;
                }
                else
                {
                    if (model.TotalVolume < model.AAAA || model.TotalVolume > model.BBBB)
                    {
                        return true;
                    }
                    else return false;
                }

            }
            else
            {
                if (model.OverAk < 3.2)
                {
                    if (model.TotalVolume < 0.75 * model.CCCC || model.TotalVolume > 0.75 * model.DDDD)
                    {
                        return true;
                    }
                    else return false;
                }
                else
                {
                    if (model.TotalVolume < model.CCCC || model.TotalVolume > model.DDDD)
                    {
                        return true;
                    }
                    else return false;
                }
            }
        }

        //返回false时是变成红色，返回true时是绿色
        public bool CheckRedOrGreenModel(dynamic model)
        {
            if (Convert.ToInt32(model.Load) == 1)
            {
                if (model.OverAk < 3.2)
                {
                    if (model.TotalVolume < 0.75 * model.AAAA)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    if (model.TotalVolume < model.BBBB)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (model.OverAk < 3.2)
                {
                    if (model.TotalVolume < 0.75 * model.CCCC)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    if (model.TotalVolume < model.DDDD)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
    }
           
}
