﻿namespace ThMEPWSS.Pipe.Model
{
    public class ThWCompositeRoom : ThWRoom
    {
        public ThWToiletRoom Toilet { get; set; }
        public ThWKitchenRoom Kitchen { get; set; }

        public ThWCompositeRoom(ThWKitchenRoom kitchen, ThWToiletRoom toilet)
        {
            Toilet = toilet;
            Kitchen = kitchen;
        }
    }
}