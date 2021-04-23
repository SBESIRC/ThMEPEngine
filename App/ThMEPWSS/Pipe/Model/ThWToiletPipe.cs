using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Pipe.Service;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWToiletPipeParameters
    {
        public List<Tuple<string, double>> Identifier { get; set; }     
        public ThWToiletPipeParameters(bool separation_water, bool caisson, int floor)
        {
            Identifier = new List<Tuple<string, double>>();
            if (!separation_water)
            {
                if (!caisson)
                {              
                    Identifier.Add(Tuple.Create(string.Format("通气TLx1"), ThTagParametersService.ToiletTpipe));
                    Identifier.Add(Tuple.Create(string.Format("污废PLx1"), ThTagParametersService.ToiletWpipe));
                }
                else
                {      
                    Identifier.Add(Tuple.Create(string.Format("沉箱DLx1"), ThTagParametersService.KaTFpipe));
                    Identifier.Add(Tuple.Create(string.Format("通气TLx1"), ThTagParametersService.ToiletTpipe));
                    Identifier.Add(Tuple.Create(string.Format("污废PLx1"),ThTagParametersService.ToiletWpipe));
                }
            }
            else
            {
                if (!caisson)
                {                   
                    Identifier.Add(Tuple.Create(string.Format("污水WLx1"), ThTagParametersService.ToiletWpipe));
                    Identifier.Add(Tuple.Create(string.Format("通气TLx1"), ThTagParametersService.ToiletTpipe));
                    Identifier.Add(Tuple.Create(string.Format("水FLx1"), ThTagParametersService.KaTFpipe));

                }
                else
                {                   
                    Identifier.Add(Tuple.Create(string.Format("沉箱DLx1"), ThTagParametersService.KaTFpipe));
                    Identifier.Add(Tuple.Create(string.Format("污水WLx1"), ThTagParametersService.ToiletWpipe));
                    Identifier.Add(Tuple.Create(string.Format("通气TLx1"), ThTagParametersService.ToiletTpipe));
                    Identifier.Add(Tuple.Create(string.Format("水FLx1"), ThTagParametersService.KaTFpipe));
                }
            }
        }
    }

    public class ThWToiletPipe : ThWPipe
    {
        public Point3d Center { get; set; }
    }
}
