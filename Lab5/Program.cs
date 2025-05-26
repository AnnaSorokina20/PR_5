using System;
using System.Collections.Generic;
using System.Linq;

namespace HungarianSolver
{
    internal class Program
    {
        static int[,] ReadMatrix(int n)
        {
            Console.WriteLine($"Введіть матрицю вартостей {n}×{n}:");
            var a = new int[n, n];
            for (int i = 0; i < n; i++)
            {
                while (true)
                {
                    Console.Write($"  рядок {i + 1}: ");
                    var parts = Console.ReadLine()!
                                .Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != n) { Console.WriteLine("    ❌ Кількість елементів ≠ n, спробуйте ще раз."); continue; }
                    try
                    {
                        for (int j = 0; j < n; j++) a[i, j] = int.Parse(parts[j]);
                        break;
                    }
                    catch { Console.WriteLine("    ❌ Невірний формат числа, спробуйте ще раз."); }
                }
            }
            return a;
        }

        static void PrintMatrix(int[,] m, string name, string fmt = "{0,3}")
        {
            int n = m.GetLength(0);
            Console.WriteLine($"{name} :=");
            for (int i = 0; i < n; i++)
            {
                Console.Write("   ");
                for (int j = 0; j < n; j++)
                    Console.Write(string.Format(fmt, m[i, j]) + (j < n - 1 ? " " : ""));
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        //УГОРСЬКИЙ АЛГОРИТМ
        static (int[,] assign, int cost) Hungarian(int[,] c0)
        {
            int n = c0.GetLength(0);
            var c = (int[,])c0.Clone();                // робоча копія
            Console.WriteLine("\n--- Угорський метод ---");

            // Крок 1: віднімання мінімумів рядків
            Console.WriteLine("1) Віднімаємо мінімум кожного рядка:");
            for (int i = 0; i < n; i++)
            {
                int min = Enumerable.Range(0, n).Min(j => c[i, j]);
                for (int j = 0; j < n; j++) c[i, j] -= min;
            }
            PrintMatrix(c, "Після рядків");

            // Крок 2: віднімання мінімумів стовпців
            Console.WriteLine("2) Віднімаємо мінімум кожного стовпця:");
            for (int j = 0; j < n; j++)
            {
                int min = Enumerable.Range(0, n).Min(i => c[i, j]);
                for (int i = 0; i < n; i++) c[i, j] -= min;
            }
            PrintMatrix(c, "Після стовпців");

            // Допоміжні структури
            var rowCovered = new bool[n];
            var colCovered = new bool[n];
            var starred = new bool[n, n];   // початкові "зірочки"
            var primed = new bool[n, n];   // тимчасові "риски"

            // Функція: к-ть покритих рядків/стовпців
            int CoverZeros()
            {
                Array.Fill(rowCovered, false);
                Array.Fill(colCovered, false);

                
                // покриваємо всі стовпці, що містять зірочку
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (starred[i, j]) colCovered[j] = true;

                return colCovered.Count(b => b);
            }

            // Початково ставимо зірочку в перший нуль кожного рядка
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (c[i, j] == 0 &&
                        !Enumerable.Range(0, n).Any(k => starred[i, k]) &&   
                        !Enumerable.Range(0, n).Any(k => starred[k, j]))     
                    {                                                      
                        starred[i, j] = true;
                        break;                                              
                    }
                }
            }

            // Крок 3-5: ітерації, поки не покриємо N стовпців
            while (CoverZeros() < n)
            {
                while (true)
                {
                    // шукаємо нуль серед непокритих клітинок
                    int zRow = -1, zCol = -1;
                    for (int i = 0; i < n && zRow < 0; i++)
                        if (!rowCovered[i])
                            for (int j = 0; j < n; j++)
                                if (!colCovered[j] && c[i, j] == 0)
                                { zRow = i; zCol = j; break; }

                    if (zRow == -1)                           // (5) немає нулів
                    {
                        int min = int.MaxValue;
                        for (int i = 0; i < n; i++)
                            if (!rowCovered[i])
                                for (int j = 0; j < n; j++)
                                    if (!colCovered[j])
                                        min = Math.Min(min, c[i, j]);

                        // додаємо min до подвійно покритих, віднімаємо від непокритих
                        for (int i = 0; i < n; i++)
                            for (int j = 0; j < n; j++)
                            {
                                if (rowCovered[i] && colCovered[j]) c[i, j] += min;
                                else if (!rowCovered[i] && !colCovered[j]) c[i, j] -= min;
                            }
                        Console.WriteLine($"   Додаємо/віднімаємо min={min} -> нові нулі");
                        continue;                              // повертаємося шукати нулі
                    }

                    // ставимо риску в знайдений 0
                    primed[zRow, zCol] = true;

                    // є зірочка в цьому рядку?
                    int starCol = Enumerable.Range(0, n).FirstOrDefault(j => starred[zRow, j]);
                    if (!starred[zRow, starCol])
                    {
                        // будуємо черг. ланцюг «ризка-зірка» й перевертаємо позначки
                        var seq = new List<(int r, int c)>();
                        seq.Add((zRow, zCol));      // починаємо з ризки
                        int r = zRow, col = zCol;
                        while (true)
                        {
                            // шукаємо зірочку в цьому стовпці
                            int rStar = Enumerable.Range(0, n).FirstOrDefault(i => starred[i, col]);
                            if (!starred[rStar, col]) break;
                            seq.Add((rStar, col));     // зірочка
                            // шукаємо ризку в цьому рядку
                            int cPrime = Enumerable.Range(0, n).First(j => primed[rStar, j]);
                            seq.Add((rStar, cPrime));
                            r = rStar; col = cPrime;
                        }
                        // перевертаємо позначки
                        foreach (var (ri, ci) in seq)
                            starred[ri, ci] = !starred[ri, ci];
                        Array.Clear(primed);          // прибираємо всі риски
                        Array.Fill(rowCovered, false);
                        Array.Fill(colCovered, false);
                        break;                        // знову рахуємо покриття
                    }
                    else
                    {
                        // покриваємо цей рядок, відкриваємо стовпець зі зірочкою
                        rowCovered[zRow] = true;
                        colCovered[starCol] = false;
                    }
                }
            }

            // формуємо матрицю призначень
            int[,] assign = new int[n, n];
            int cost = 0;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (starred[i, j])
                    { assign[i, j] = 1; cost += c0[i, j]; }

            return (assign, cost);
        }

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.Write("n (розмір квадратної матриці): ");
            int n = int.Parse(Console.ReadLine()!);

            int[,] c = ReadMatrix(n);
            PrintMatrix(c, "Початкова матриця");

            var (assign, cost) = Hungarian(c);

            Console.WriteLine("\nМатриця призначень:");
            PrintMatrix(assign, "X", "{0,2}");
            Console.WriteLine($"Загальна вартість робіт S = {cost}");

            Console.WriteLine("\nНатисніть Enter, щоб завершити …");
            Console.ReadLine();
        }
    }
}
