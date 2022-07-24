namespace ThMEPTCH.TCHArchDataConvert
{
    class TCHArchSQL
    {
        public static string GetWallSQL()
        {
            return @"select wall.ID, wall.MidPointZ, 
                            wall.MidPointY, wall.MidPointX,
                            wall.StartPointX,wall.StartPointY,wall.StartPointZ,
                            wall.EndPointX,wall.EndPointY,wall.EndPointZ,
                            wall.LeftWidth,wall.leftInsulateWidth,
                            wall.RightWidth,wall.rightInsulateWidth,
                            wall.IsArc,wall.Bulge,
                            wall.Height,
                            wall.Elevtion,
                            wall.Layer,wall.LineType,wall.DocScale,
                            wallM.Name as WallMaterialName,
                            wallU.Name as WallUsageName,
                            wallF.Name as WallFireproofName
                        from TArchWall wall
                        left join TArchWallMaterial wallM ON wall.MaterialID = wallM.ID
                        left join TArchWallFireProof wallF ON wall.FireproofID = wallF.ID
                        left join TArchWallUsage wallU on wall.UsageID = wallU.ID";
        }
        public static string GetWindowSQL() 
        {
            return @"select window.ID,
                            window.BasePointX,window.BasePointY,window.BasePointZ,
                            window.TextPointX,window.TextPointY,window.TextPointZ,
                            window.Width,window.Height,window.dThickness as Thickness,
                            window.Number,
                            window.StyleID,
                            window.Kind,
                            window.Rotation,
                            window.SubKind
                    from TArchWindow window";
        }
        public static string GetDoorSQL() 
        {
            return @"select door.ID,
                            door.BasePointX,door.BasePointY,door.BasePointZ,
                            door.TextPointX,door.TextPointY,door.TextPointZ,
                            door.Width,
                            door.Height,
                            door.dThickness as Thickness,
                            door.StyleID,
                            door.Kind,
                            door.Rotation,
                            door.SubKind
                    from TArchDoor door";
        }
    }
}
