using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace StPet_TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            RouteSPb TravTable = new RouteSPb();
            string s = "";

            // Ввод данных из текстового файла
            using (StreamReader sr = new StreamReader("InpData.txt"))
            {
                string[] str_line;

                // Чтение первого блока данных (от кол-во дней до исходного положения)
                s = sr.ReadLine(); TravTable.DaysNum = ushort.Parse(s.Substring(s.IndexOf('=') + 1));
                s = sr.ReadLine(); TravTable.FirstDayStart = double.Parse(s.Substring(s.IndexOf('=') + 1));
                s = sr.ReadLine(); TravTable.TimeToSleep = double.Parse(s.Substring(s.IndexOf('=') + 1));
                s = sr.ReadLine(); TravTable.TimeToWakeUp = double.Parse(s.Substring(s.IndexOf('=') + 1));

                // Чтение блока данных с описанием достопримечательностей
                sr.ReadLine();
                sr.ReadLine();
                s = sr.ReadLine();
                while (s != "-")
                {
                    str_line = s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries); //в качестве разделителя здесь используется ';'
                    TravTable.Sights.Add(new Sight() // добавление достопримечательности в список
                    {
                        Priority = ushort.Parse(str_line[0]),
                        Name = str_line[2],
                        OpeningTime = double.Parse(str_line[4]),
                        ClosingTime = double.Parse(str_line[6]),
                        TimeToVisit = double.Parse(str_line[8])
                    });
                    s = sr.ReadLine();
                }

                // Чтение блока данных с матрицей расстояний
                sr.ReadLine();
                s = sr.ReadLine();
                TravTable.WeightMatrix = new double[TravTable.Sights.Count][];
                for (int i = 0; i < TravTable.WeightMatrix.GetLength(0); i++)
                    TravTable.WeightMatrix[i] = new double[TravTable.Sights.Count - i];
                int k = 0;
                while(s != "-")
                {
                    str_line = s.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries); // разделителями являются пробел и Tab
                    for (int i = 0; i < str_line.Length; i++)
                        TravTable.WeightMatrix[k][i] = double.Parse(str_line[i]);
                    s = sr.ReadLine();
                    k++;
                }
            }

            // Формирование расписания
            TravTable.FormedTimeTable();

            // Вывод сформированного расписания в файл и на консоль
            using (StreamWriter sw = new StreamWriter("OutData.txt"))
            {
                for (int i = 0; i < TravTable.DaysNum; i++)
                {
                    sw.WriteLine("-> Day: {0}", i + 1);
                    Console.WriteLine("-> Day: {0}", i + 1);
                    foreach (Sight sg in TravTable[i])
                    {
                        if (sg.Equals(TravTable[i].First())) continue;
                        s = string.Format("--> с {0}:{1:f0} до {2}:{3:f0} - \'{4}\' в течении {5}:{6:f0} ч", 
                            Math.Truncate(sg.OpeningTime), 60 * (sg.OpeningTime - Math.Truncate(sg.OpeningTime)),
                            Math.Truncate(sg.ClosingTime), 60 * (sg.ClosingTime - Math.Truncate(sg.ClosingTime)),
                            sg.Name, Math.Truncate(sg.TimeToVisit), 60 * (sg.TimeToVisit - Math.Truncate(sg.TimeToVisit)));
                        sw.WriteLine(s);
                        Console.WriteLine(s);
                    }
                }
            }

            Console.WriteLine("Нажмите любую клавишу...");
            Console.Read();

        }
    }
}
