using NetTopologySuite.Geometries;
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
            //Read Datawraper
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = dir + "\\dataWraper.txt";
            ReadDataWraperService readDataWraperService = new ReadDataWraperService(path);
            var dataWraper = new DataWraper();
            try
            {
                dataWraper = readDataWraperService.Read();
            }
            catch (Exception ex)
            {
                return;
            }
            //run GA
            OInterParameter.Init(dataWraper);
            int fileSize = 64; // 64Mb
            var nbytes = fileSize * 1024 * 1024;
            Genome Solution = new Genome();
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
                Solution = GA_Engine.Run().First();
            }
            //Serialize Genome
            path = dir + "\\genome.txt";
            FileStream fileStream = new FileStream(path, FileMode.Create);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, Solution); //序列化 参数：流 对象
            fileStream.Close();

            Console.WriteLine("success.");
            Console.ReadKey();
            return;
        }
    }
}
