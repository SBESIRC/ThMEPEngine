using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model.Algorithm
{
    internal class GeneticAlgorithm
    {
        public GeneticAlgorithm(List<List<ThBeamTopologyNode>> fixedSpace, List<ThBeamTopologyNode> nodes)
        {
            chromosome.bitsCount = nodes.Count;
            bitsCount = nodes.Count;
            Nodes = nodes;
            FixedSpace = fixedSpace;
        }



        /// <summary>
        /// 染色体;
        /// </summary>
        private class chromosome
        {
            public static int bitsCount = 0;

            /// <summary>
            /// 用bit数组对染色体进行编码;
            /// </summary>
            public int[] bits = new int[bitsCount];

            /// <summary>
            /// 适应值;
            /// </summary>
            public int fitValue;

            /// <summary>
            /// 选择概率;
            /// </summary>
            public double fitValuePercent;

            /// <summary>
            /// 累积概率;
            /// </summary>
            public double probability;

            public chromosome Clone()
            {
                chromosome c = new chromosome();
                for (int i = 0; i < bits.Length; i++)
                {
                    c.bits[i] = bits[i];
                }
                c.fitValue = fitValue;
                c.fitValuePercent = fitValuePercent;
                c.probability = probability;
                return c;
            }
        }

        /// <summary>
        /// 染色体组;
        /// </summary>
        private List<chromosome> chromosomes = new List<chromosome>();

        private List<chromosome> chromosomesChild = new List<chromosome>();

        private List<List<ThBeamTopologyNode>> FixedSpace = new List<List<ThBeamTopologyNode>>();
        private List<ThBeamTopologyNode> Nodes = new List<ThBeamTopologyNode>();
        private int bitsCount = 0;
        private int PopulationSize = 30;
        private Random random = new Random();

        private enum ChooseType
        {
            Bubble,//冒泡;
            Roulette,//轮盘赌;
        }

        private ChooseType chooseType = ChooseType.Bubble;// ChooseType.Roulette;

        /// <summary>
        /// 入口函数;
        /// </summary>
        /// <param name="args"></param>
        public void Run()
        {
            // 遗传算法
            // 迭代次数;
            int totalTime = 150;
            //初始化;
            Init();
            for (int i = 0; i < totalTime; i++)
            {
                //Console.WriteLine("当前迭代次数: " + i);

                //重新计算fit值;;
                UpdateFitValue();

                // 挑选染色体;
                //Console.WriteLine("挑选:");

                switch (chooseType)
                {
                    case ChooseType.Bubble:
                        // 排序;
                        //Console.WriteLine("排序:");
                        ChooseChromosome();
                        break;

                    default:
                        //轮盘赌;
                        //Console.WriteLine("轮盘赌:");
                        UpdateNext();
                        break;
                }
                //Print(true);

                //淘汰劣质基因
                DisuseOperate();

                //交叉得到新个体;
                //Console.WriteLine("交叉:");
                CrossOperate();
                //Print();

                //变异操作;
                //Console.WriteLine("变异:");
                VariationOperate();
                //Print();
            }
            UpdateFitValue();
            //int maxfit = chromosomes[0].fitValue;
            //for (int i = 1; i < chromosomes.Count; i++)
            //{
            //    if (chromosomes[i].fitValue > maxfit)
            //    {
            //        maxfit = chromosomes[i].fitValue;
            //    }
            //}
            //Console.WriteLine("最大值为: " + maxfit);
            //Console.ReadLine();
        }

        /// <summary>
        /// 打印;
        /// </summary>
        private void Print(bool bLoadPercent = false)
        {
            Console.WriteLine("=========================");
            for (int i = 0; i < chromosomes.Count; i++)
            {
                Console.Write("第" + i + "条" + " bits: ");
                for (int j = 0; j < chromosomes[i].bits.Length; j++)
                {
                    Console.Write(" " + chromosomes[i].bits[j]);
                }
                //int x = DeCode(chromosomes[i].bits);
                //Console.Write(" x: " + x);
                Console.Write(" y: " + chromosomes[i].fitValue);
                if (bLoadPercent)
                {
                    Console.Write(" 选择概率: " + chromosomes[i].fitValuePercent);
                    //Console.Write(" 累积概率: " + chromosomes[i].probability);
                }
                Console.WriteLine();
            }
            Console.WriteLine("=========================");
        }

        /// <summary>
        /// 初始化;
        /// </summary>
        private void Init()
        {
            chromosomes.Clear();
            // 染色体数量;
            int length = PopulationSize;

            for (int i = 0; i < length; i++)
            {
                chromosome chromosome = new chromosome();
                for (int j = 0; j < chromosome.bits.Length; j++)
                {
                    // 随机出0或者1;
                    //int seed = (i + j) * 100 / 3;//种子;
                    int bitValue = random.Next(0, 2);
                    chromosome.bits[j] = bitValue;
                }
                //获得十进制的值;
                int y = EvaluationModel.Evaluation(chromosome.bits, Nodes, FixedSpace);
                chromosome.fitValue = y;
                chromosomes.Add(chromosome);
            }
        }

        /// <summary>
        /// 更新下一代;
        /// 基于轮盘赌选择方法，进行基因型的选择;
        /// </summary>
        private void UpdateNext()
        {
            // 获取总的fit;
            double totalFitValue = chromosomes.Sum(o => o.fitValue);
            //Console.WriteLine("totalFitValue " + totalFitValue);

            //算出每个的fit percent;
            for (int i = 0; i < chromosomes.Count; i++)
            {
                chromosomes[i].fitValuePercent = chromosomes[i].fitValue / totalFitValue;
                //Console.WriteLine("fitValuePercent " + i + " " + chromosomes[i].fitValuePercent);
            }

            //计算累积概率;
            //第一个的累计概率就是自己的概率;
            chromosomes[0].probability = chromosomes[0].fitValuePercent;
            //Console.WriteLine("probability 0 " + chromosomes[0].probability);
            double probability = chromosomes[0].probability;
            for (int i = 1; i < chromosomes.Count; i++)
            {
                if (chromosomes[i].fitValuePercent != 0)
                {
                    chromosomes[i].probability = chromosomes[i].fitValuePercent + probability;
                    probability = chromosomes[i].probability;
                }
                //Console.WriteLine("probability " + i + " " + chromosomes[i].probability);
            }
            chromosomesChild.Clear();
            //轮盘赌选择方法,用于选出前两个;
            for (int i = 0; i < chromosomes.Count; i++)
            {
                //产生0-1之前的随机数;
                //int seed = i * 100 / 3;
                double rand = random.NextDouble();//0.0-1.0
                //Console.WriteLine("挑选的rand " + rand);
                if (rand < chromosomes[0].probability)
                {
                    chromosomesChild.Add(chromosomes[0].Clone());
                }
                else
                {
                    for (int j = 0; j < chromosomes.Count - 1; j++)
                    {
                        if (chromosomes[j].probability <= rand && rand <= chromosomes[j + 1].probability)
                        {
                            chromosomesChild.Add(chromosomes[j + 1].Clone());
                        }
                    }
                }
            }
            for (int i = 0; i < chromosomes.Count; i++)
            {
                chromosomes[i] = chromosomesChild[i];
            }
        }

        /// <summary>
        /// 选择染色体;
        /// </summary>
        private void ChooseChromosome()
        {
            // 从大到小排序;
            chromosomes.Sort((a, b) => { return b.fitValue.CompareTo(a.fitValue); });
        }

        /// <summary>
        /// 优胜劣汰，淘汰劣质基因
        /// </summary>
        private void DisuseOperate()
        {
            chromosomes = chromosomes.Take(10).ToList();
        }

        /// <summary>
        /// 交叉操作;
        /// </summary>
        private void CrossOperate()
        {
            /**         bit[5]~bit[0]   fit
             * 4        000 110         12
             * 3        001 010         9
             * child1   000 010         14
             * child2   001 110         5
             */
            //int rand1 = random.Next(0, bitsCount);//0-5;
            //int rand2 = random.Next(0, bitsCount);//0-5;
            //if (rand1 > rand2)
            //{
            //    var t = rand1;
            //    rand1 = rand2;
            //    rand2 = t;
            //}
            ////Console.WriteLine("交叉的rand " + rand1 + " - " + rand2);
            //for (int j = 0; j < chromosomes.Count; j = j + 2)
            //{
            //    for (int i = rand1; i <= rand2; i++)
            //    {
            //        //将第0个给第2个;
            //        var t = chromosomes[j].bits[i];
            //        chromosomes[j].bits[i] = chromosomes[j + 1].bits[i];//第一条和第三条交叉;
            //        chromosomes[j + 1].bits[i] = t;
            //    }
            //    chromosomes[j].fitValue = GetFitValue(DeCode(chromosomes[j].bits));
            //    chromosomes[j + 1].fitValue = GetFitValue(DeCode(chromosomes[j + 1].bits));
            //}
            var count = chromosomes.Count;
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    var rand = random.Next(3);//0-2
                    if (j <4 || rand < 1)//鼓励前几个优质解 进行杂交
                    {
                        chromosomes.Add(Crossover(chromosomes[i], chromosomes[j]));
                    }
                }
            }
        }

        /// <summary>
        /// 杂交
        /// </summary>
        /// <param name="chromosome1"></param>
        /// <param name="chromosome2"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private chromosome Crossover(chromosome chromosome1, chromosome chromosome2)
        {
            chromosome chromosome = new chromosome();
            for (int i = 0; i < bitsCount; i++)
            {
                var rand = random.Next(2);//0-1
                chromosome.bits[i] = rand>0 ? chromosome1.bits[i] : chromosome2.bits[i];
            }
            return chromosome;
        }

        /// <summary>
        /// 变异操作;
        /// </summary>
        private void VariationOperate()
        {
            int rand = random.Next(0, 100);
            //Console.WriteLine("变异的rand " + rand);
            if (rand < 5)//1/50 = 0.02的概率进行变异;rand==25;
            {
                //Console.WriteLine("开始变异");
                rand = random.Next(chromosomes.Count);
                while (rand-- >0)
                {
                    int col = random.Next(0, bitsCount);
                    int row = random.Next(10, chromosomes.Count);
                    //Console.WriteLine("变异的位置 " + row + "  " + col);
                    // 0变为1,1变为0;
                    chromosomes[row].bits[col] =  (chromosomes[row].bits[col] + 1) % 2;
                    //chromosomes[row].fitValue = GetFitValue(DeCode(chromosomes[row].bits));
                }
            }
        }

        /// <summary>
        /// 重新计算fit值;
        /// </summary>
        private void UpdateFitValue()
        {
            for (int i = 0; i < chromosomes.Count; i++)
            {
                chromosomes[i].fitValue = EvaluationModel.Evaluation(chromosomes[i].bits, Nodes, FixedSpace);
            }
        }
    }
}
