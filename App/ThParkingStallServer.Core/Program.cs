﻿using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.OInterProcess;

namespace ThParkingStallServer.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = dir + "\\dataWraper.txt";
            ReadDataWraperService readDataWraperService = new ReadDataWraperService(path);
            var dataWraper = readDataWraperService.Read();

            //run GA
            int fileSize = 64; // 64Mb
            var nbytes = fileSize * 1024 * 1024;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("DataWraper", nbytes))
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, dataWraper);
                }
                var GA_Engine = new ServerGAGenerator(dataWraper);
                //GA_Engine.Logger = Logger;
                //GA_Engine.DisplayLogger = DisplayLogger;
                //GA_Engine.displayInfo = displayInfos.Last();
                var Solution = GA_Engine.Run().First();
            }

            return;
        }
    }
}
