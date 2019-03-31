using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StPet_TestApp
{
    // Место
    struct Sight
    {
        public int Priority { get; set; } // приоритет
        public string Name { get; set; } // имя
        public double OpeningTime { get; set; } // время открытия, ч
        public double ClosingTime { get; set; } // время закрытия, ч
        public double TimeToVisit { get; set; } // общее время на посещение, ч

        public Sight(int pr, string name, double optime, double cltime, double vistime) : this()
        {
            Priority = pr; Name = name; OpeningTime = optime;
            ClosingTime = cltime; TimeToVisit = vistime;
        }

        public static IComparer<Sight> SortByOpTimeAndPriority
        {
            get
            {
                return new SightCompareByOpTimeAndPriority();
            }
        }

        public override string ToString()
        {
            return string.Format("Priority: {0}; Name: {1}; Open: {2}; Close: {3}; Visit, h: {4}", Priority, Name, OpeningTime, ClosingTime, TimeToVisit);
        }
    }
    class SightCompareByOpTimeAndPriority : IComparer<Sight>
    {
        public int Compare(Sight x, Sight y)
        {
            if (x.ClosingTime - x.OpeningTime == 24) x.OpeningTime = 24;
            if (y.ClosingTime - y.OpeningTime == 24) y.OpeningTime = 24;
            if (x.OpeningTime < y.OpeningTime)
                return -1;

            if (Math.Abs(x.OpeningTime - y.OpeningTime) < 1E-6)
            {
                if (x.Priority < y.Priority) return -1;
                if (x.Priority == y.Priority) return 0;
            }

            return 1;

        }
    }

    // Расписание
    internal class RouteSPb
    {
        public List<Sight> Sights { get; set; }               // Список достопр. для посещения
        public double[][] WeightMatrix { get; set; }          // Матрица смежности с временами маршрутов
        public int DaysNum { get; set; }                   // Количество дней
        public double FirstDayStart { get; set; }             // Время начала первого дня, ч
        public double TimeToSleep { get; set; }               // Время отхода ко сну, ч
        public double TimeToWakeUp { get; set; }              // Время пробуждения, ч
        private List<Sight>[] TimeTable;                      // Расписание
        private bool[] fVisited;

        public RouteSPb() => Sights = new List<Sight>();

        // Формирование расписания
        public void FormedTimeTable()
        {
            TimeTable = new List<Sight>[DaysNum];
            for (int i = 0; i < DaysNum; i++) TimeTable[i] = new List<Sight>();
            fVisited = new bool[Sights.Count];
            for (int i = 0; i < fVisited.Length; i++) fVisited[i] = false;

            // Первый этап: добавление мест в порядке приоритета одним посещением
            for (int day = 1; day <= DaysNum; day++) // Цикл по дням
            {
                if (day == 1)
                    FormedThisDayTimeTable(FirstDayStart, TimeToSleep, day);
                else
                    FormedThisDayTimeTable(TimeToWakeUp, TimeToSleep, day);
            }

            // Второй этап: добавление не посещенных мест в порядке приоритета, разделяя посещение
            // на несколько раз
            double ts, tf, ts1, tf1;
            double t1_path, t2_path;
            double t_vis;
            int l, m;
            List<int>[] n = new List<int>[DaysNum];
            List<int>[] nlen = new List<int>[DaysNum];

            for (int i = 0; i < Sights.Count; i++)
            {
                List<Sight>[] FreeTime = new List<Sight>[DaysNum];
                if (!fVisited[i])
                {
                    t_vis = Sights[i].TimeToVisit;
                    for (int j = 0; j < DaysNum; j++)
                    {
                        n[j] = new List<int>();
                        nlen[j] = new List<int>();
                        FreeTime[j] = new List<Sight>();
                        for (int k = 0; k < TimeTable[j].Count - 2; k++)
                        {
                            if (TimeTable[j][k].Name == "Свободное время")
                            {
                                ts = TimeTable[j][k].OpeningTime;
                                tf = TimeTable[j][k + 2].OpeningTime;
                                l = Math.Min(TimeTable[j][k].Priority, Sights[i].Priority);
                                m = Math.Max(TimeTable[j][k].Priority, Sights[i].Priority) - (Math.Min(TimeTable[j][k].Priority, Sights[i].Priority) + 1);
                                t1_path = WeightMatrix[l][m];
                                l = Math.Min(TimeTable[j][k + 2].Priority, Sights[i].Priority);
                                m = Math.Max(TimeTable[j][k + 2].Priority, Sights[i].Priority) - (Math.Min(TimeTable[j][k + 2].Priority, Sights[i].Priority) + 1);
                                t2_path = WeightMatrix[l][m];

                                if (Sights[i].OpeningTime < tf - t2_path && Sights[i].ClosingTime > ts + t1_path)
                                {
                                    tf1 = Math.Min(Sights[i].ClosingTime, tf - t2_path);
                                    ts1 = Math.Max(Sights[i].OpeningTime, ts + t1_path);
                                    if (ts1 > tf1) continue;
                                    nlen[j].Add(0);
                                    if (ts1 > ts + t1_path)
                                    {
                                        FreeTime[j].Add(new Sight(TimeTable[j][k].Priority, "Свободное время", ts, ts1 - t1_path, ts1 - t1_path - ts));
                                        nlen[j][nlen[j].Count - 1]++;
                                    }
                                    FreeTime[j].Add(new Sight(TimeTable[j][k].Priority, "Дорога", ts1 - t1_path, ts1, t1_path));
                                    nlen[j][nlen[j].Count - 1]++;

                                    if (t_vis >= tf1 - ts1)
                                    {
                                        FreeTime[j].Add(new Sight(Sights[i].Priority, Sights[i].Name, ts1, tf1, tf1 - ts1));
                                        nlen[j][nlen[j].Count - 1]++;
                                    }
                                    else
                                    {
                                        FreeTime[j].Add(new Sight(Sights[i].Priority, Sights[i].Name, ts1, ts1 + t_vis, t_vis));
                                        nlen[j][nlen[j].Count - 1]++;
                                        tf1 = ts1 + t_vis;
                                    }

                                    if (tf1 < tf - t2_path)
                                    {
                                        FreeTime[j].Add(new Sight(Sights[i].Priority, "Свободное время", tf1, tf - t2_path, tf - t2_path - tf1));
                                        nlen[j][nlen[j].Count - 1]++;
                                    }

                                    FreeTime[j].Add(new Sight(Sights[i].Priority, "Дорога", tf - t2_path, tf, t2_path));
                                    nlen[j][nlen[j].Count - 1]++;
                                    t_vis -= tf1 - ts1;
                                    n[j].Add(k);
                                }
                            }
                        }
                        if (t_vis <= 1E-6) break;
                    }
                    if(t_vis <= 1E-6)
                    {
                        int s;
                        for (int j = 0; j < DaysNum; j++)
                        {
                            s = 0;
                            for (int k = 0; k < n[j].Count; k++)
                            {
                                for (int p = s; p < s + nlen[j][k]; p++)
                                    TimeTable[j].Insert(n[j][k] + (p - s), FreeTime[j][p]);

                                if (k + 1 < n[j].Count) n[j][k + 1] += nlen[j][k] - 2;
                                s += nlen[j][k];

                                TimeTable[j].RemoveAt(n[j][k] + nlen[j][k]);
                                TimeTable[j].RemoveAt(n[j][k] + nlen[j][k]);
                            }
                        }
                    }
                }
            }
        }

        // Формирование расписания на заданный день
        private void FormedThisDayTimeTable(double ts, double tf, int day) // время начала и конца дня, номер дня
        {
            Sights.Sort(Sight.SortByOpTimeAndPriority); // сортируем список мест по времени открытия и приоритету

            double t = ts;
            double t_path;
            int indSg = 0;
            int ind_node, ind_nextnode;
            int k, l;
            TimeTable[day - 1].Add(new Sight() {Priority = 0, Name = "Стартовая точка"});

            while (t < tf) // Цикл по времени в текущий день
            {
                // Массив мест пройден (все доступные места были перебраны в текущий день)
                if (indSg == Sights.Count)
                {
                    t_path = WeightMatrix[0][TimeTable[day - 1].Last().Priority - 1];
                    TimeTable[day - 1].Add(new Sight(TimeTable[day - 1].Last().Priority, "Свободное время", t, TimeToSleep - t_path, TimeToSleep - t_path - t));
                    TimeTable[day - 1].Add(new Sight(TimeTable[day - 1].Last().Priority, "Дорога", TimeToSleep - t_path, TimeToSleep, t_path));
                    TimeTable[day - 1].Add(new Sight(0, "Стартовая точка", TimeToSleep, TimeToWakeUp, 8));
                    break;
                }

                ind_node = TimeTable[day - 1].Last().Priority; // текущий узел
                ind_nextnode = Sights[indSg].Priority; // узел для добавления

                // если рассматриваются одинаковые узлы
                if (ind_node == ind_nextnode) 
                {
                    indSg++;
                    continue;
                }

                // получаем время в пути из текущего места в следующее
                k = Math.Min(ind_node, ind_nextnode);
                l = Math.Max(ind_node, ind_nextnode) - (Math.Min(ind_node, ind_nextnode) + 1);
                t_path = WeightMatrix[k][l]; 

                // Если место посещено
                if(fVisited[indSg])
                {
                    indSg++;
                    continue;
                }

                // Если время приезда из текущего места в следующее слишком раннее
                if(t + t_path < Sights[indSg].OpeningTime)
                {
                    TimeTable[day - 1].Add(new Sight(TimeTable[day - 1].Last().Priority, "Свободное время", t, 
                        Sights[indSg].OpeningTime - t_path, Sights[indSg].OpeningTime - t_path - t));
                    t = Sights[indSg].OpeningTime - t_path;
                }
                else // Приезд вовремя
                {
                    // Посещение места умещается в отведенное время
                    if((t + t_path + Sights[indSg].TimeToVisit <= Sights[indSg].ClosingTime) &&
                       (t + t_path + Sights[indSg].TimeToVisit + WeightMatrix[0][Sights[indSg].Priority - 1] <= TimeToSleep))
                    {
                        TimeTable[day - 1].Add(new Sight(TimeTable[day - 1].Last().Priority, "Дорога", t, t + t_path, t_path));
                        TimeTable[day - 1].Add(new Sight()
                        {
                            Priority = Sights[indSg].Priority,
                            Name = Sights[indSg].Name,
                            OpeningTime = t + t_path,
                            ClosingTime = t + t_path + Sights[indSg].TimeToVisit,
                            TimeToVisit = Sights[indSg].TimeToVisit
                        });
                        t += t_path + Sights[indSg].TimeToVisit;
                        fVisited[indSg] = true;
                    }

                    indSg++;
                }
            }
        }

        public List<Sight> this[int index] => TimeTable[index];
    }
}
