using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaarLikeDetector.Adam
{
    class RealBoostClassifier
    {
        #region Training

        /// <summary>
        /// Naucz klasyfikator na podstawie bazy przykladow.
        /// </summary>
        /// <param name="data">Zbior przykladow uczacych.</param>
        /// <param name="T">Liczba iteracji/rund boostingu.</param>
        /// <returns>'true' jesli procedura sie powiodla, 'false' jesli nie.</returns>
        public bool TrainClassifier(IReadOnlyList<double[]> data, int T)
        {
            return TrainClassifier(data, T, 16);
            //            return TrainClassifier(data, T, 32);
        }

        /// <summary>
        /// Naucz klasyfikator na podstawie bazy przykladow.
        /// </summary>
        /// <param name="data">Zbior przykladow uczacych.</param>
        /// <param name="T">Liczba iteracji/rund boostingu.</param>
        /// <param name="B">Ile koszykow dla kazdej cechy.</param>
        /// <returns>'true' jesli procedura sie powiodla, 'false' jesli nie.</returns>
        public bool TrainClassifier(IReadOnlyList<double[]> data, int T, int B)
        {
            if (data == null || data.Count <= 0) return false;
            int[] arrB = new int[data[0].Length - 1];
            for (int i = 0; i < arrB.Length; i++) arrB[i] = B;
            return TrainClassifier(data, T, arrB);
        }

        /// <summary>
        /// Naucz klasyfikator na podstawie bazy przykladow.
        /// </summary>
        /// <param name="data">Zbior przykladow uczacych.</param>
        /// <param name="T">Liczba iteracji/rund boostingu.</param>
        /// <param name="B">Ile koszykow dla kazdej cechy.</param>
        /// <returns>'true' jesli procedura sie powiodla, 'false' jesli nie.</returns>
        public bool TrainClassifier(IReadOnlyList<double[]> data, int T, int[] B)
        {
            if (data == null || data.Count <= 0) return false;
            if (T <= 0) return false;
            if (B == null || B.Length != data[0].Length - 1) return false;

            // data - lista przykładów: wartości cech Haar'a + informacja czy poprawny, czy nie
            // m - liczba przykładów
            // n - liczba cech Haar'a
            int m = data.Count;
            int n = data[0].Length - 1;

            // Klasyfikator zbiorowy.
            var F = new List<double[]>();

            // Wagi.
            var w = new double[m];
            double initW = 1d / m; // poczatkowa wartosc dla wszystkich wag
            for (int i = 0; i < m; i++) w[i] = initW;

            // Zakresy cech.
            var a1 = new double[n]; // dolne ograniczenia
            var a2 = new double[n]; // gorne ograniczenia
            for (int ftId = 0; ftId < data[0].Length - 1; ftId++)
            {
                a1[ftId] = data[0][ftId];
                a2[ftId] = data[0][ftId];
            }
            for (int exId = 1; exId < data.Count; exId++)
            {
                for (int ftId = 0; ftId < data[exId].Length - 1; ftId++)
                {
                    if (data[exId][ftId] < a1[ftId])
                        a1[ftId] = data[exId][ftId];
                    if (data[exId][ftId] > a2[ftId])
                        a2[ftId] = data[exId][ftId];
                }
            }
            // Sprawdzenie, czy wszystkie cechy maja zakres, czy tez maja stala wartosc we wszystkich przykladach.
            for (int ftId = 0; ftId < a1.Length; ftId++)
            {
                if (a1[ftId] == a2[ftId])
                    throw new Exception("Cannot divide into buckets! FeatureId: " + ftId);
            }
            Console.WriteLine("A1: " + string.Join(", ", a1));
            Console.WriteLine("A2: " + string.Join(", ", a2));

            // --- Uczenie ----------------------
            Console.WriteLine(
                @"Start training: ExCt({0}), FtCt({1}), IterationCt({2}), StartTime({3}).",
                m, n, T, DateTime.Now);
            for (int t = 0; t < T; t++)
            {
                double Z_best = double.PositiveInfinity;
                int ftId_best = -1; // Najlepsza cecha.
                double[] f_best = null; // 1x(B+1), gdzie B - liczba cech Haar'a; ostatnia wartosc to id cechy
                double[] wP; // 1xB, koszyki pozytywne
                double[] wN; // 1xB, koszyki negatywne

                for (int ftId = 0; ftId < n; ftId++) // cechy
                {
                    double Z = 0d; // Kryterium wykladnicze

                    wP = new double[B[ftId]];
                    wN = new double[B[ftId]];
                    for (int exId = 0; exId < m; exId++) // przyklady
                    {
                        int b = GetBucket(data, B, a1, a2, exId, ftId);

                        // Ostatnia wartosc mowi czy przyklad jest poprawny czy nie (1 - jest, -1 - nie)
                        if (data[exId][n] > 0)
                            wP[b] += w[exId];
                        else
                            wN[b] += w[exId];
                    }

                    var f = new double[B[ftId] + 1];
                    for (int b = 0; b < B[ftId]; b++)
                    {
                        if (wP[b] == 0)
                        {
                            if (wN[b] == 0d)
                                f[b] = 0d;
                            else
                                f[b] = -2d;
                        }
                        else if (wN[b] == 0d)
                            f[b] = 2d;
                        else
                        {
                            f[b] = 0.5d * Math.Log(wP[b] / wN[b]);
                            if (f[b] > 2d) f[b] = 2d;
                            else if (f[b] < -2d) f[b] = -2d;
                        }
                    }

                    for (int exId = 0; exId < m; exId++)
                    {
                        int b = GetBucket(data, B, a1, a2, exId, ftId);
                        Z += w[exId] * Math.Exp(-data[exId][n] * f[b]);
                    }

                    if (Z < Z_best)
                    {
                        // Przypisanie jako najlepszej cechy.
                        Z_best = Z;
                        ftId_best = ftId;
                        f_best = f;
                    }
                }

                if (ftId_best < 0)
                {
                    throw new Exception("Invalid ftId_best!");
                    continue;
                }
                if (f_best == null)
                {
                    throw new Exception("f_best is null!");
                    continue;
                }

                double epsilon = 0d;
                for (int exId = 0; exId < m; exId++)
                {
                    int b = GetBucket(data, B, a1, a2, exId, ftId_best);
                    var ans = f_best[b];
                    if(ans * data[exId][n] <= 0)
                        epsilon += w[exId];
                }



                // Wpisanie Id cechy na ostatnie miejsce w tablicy klasyfikatora.
                f_best[f_best.Length - 1] = ftId_best;
                // Dopisanie slabego klasyfikatora do klasyfikatora zbiorowego.
                F.Add(f_best);

                var epsilonF = 0d;
                for (int exId = 0; exId < m; exId++)
                {
                    var ans = Classify(F, B, a1, a2, 0, data[exId]) ? 1 : -1;
                    if (ans * data[exId][n] <= 0)
                        epsilonF += initW;
                }
                // Aktualizacja wag przykladow.
                for (int exId = 0; exId < m; exId++)
                {
                    int b = GetBucket(data, B, a1, a2, exId, ftId_best);
                    w[exId] = w[exId] * Math.Exp(-data[exId][n] * f_best[b]) / Z_best;
                }

                Console.WriteLine(
                    @"Iteration finished: It({0}), BestFtId({1}), BestZ({2}), Epsilon({3}), EpsilonF({4}), f({5}).",
                    t, ftId_best, Z_best, epsilon, epsilonF, string.Join(", ", f_best));
            }

            // Uczenie zakonczone. Przypisanie rezultatow.
            Classifier = F;
            this.B = B;
            A1 = a1;
            A2 = a2;

            Console.WriteLine(
                @"Training finished: IterationCt({0}), EndTime({1}).",
                T, DateTime.Now);
            return true;
        }

        /// <summary>
        /// Sprawdz w ktorym koszyku lezy wartosc cechy.
        /// </summary>
        /// <param name="data">Baza przykladow.</param>
        /// <param name="B">Liczba koszykow dla poszczegolnych cech.</param>
        /// <param name="a1">Dolne ograniczenia wartosci cech.</param>
        /// <param name="a2">Gorne ograniczenia wartosci cech.</param>
        /// <param name="exId">Id przykladu.</param>
        /// <param name="ftId">Id cechy.</param>
        /// <returns>Id koszyka do ktorego nalezy cecha.</returns>
        private static int GetBucket(
            IReadOnlyList<double[]> data, int[] B,
            double[] a1, double[] a2,
            int exId, int ftId)
        {
            if (a2[ftId] == a1[ftId]) return 0;

            var b = (int)Math.Ceiling(
                B[ftId] * (data[exId][ftId] - a1[ftId]) / (a2[ftId] - a1[ftId]))
                - 1; // bo indeksy od 0

            //if (data[exId][ftId] <= a1[ftId])
            //    return 0;
            //if (data[exId][ftId] > a2[ftId])
            //    return B[ftId] - 1;

            if (b < 0) return 0;
            if (b > B[ftId] - 1) return B[ftId] - 1;
            return b;
        }

        #endregion


        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX


        #region Classification (training must be completed)

        /// <summary>
        /// Klasyfikuj obiekt o podanych cechach.
        /// </summary>
        /// <param name="featureVals">Wartosci cech przykladu</param>
        /// <returns>'true' - klasa 1, 'false' - klasa -1.</returns>
        public bool Classify(double[] featureVals)
        {
            return Classify(Classifier, B, A1, A2, 0d, featureVals);
        }

        /// <summary>
        /// Klasyfikuj obiekt o podanych cechach.
        /// </summary>
        /// <param name="classifier">Klasyfikator zbiorowy.</param>
        /// <param name="B">Liczba koszykow dla poszczegolnych cech.</param>
        /// <param name="a1">Dolne ograniczenia wartosci cech.</param>
        /// <param name="a2">Gorne ograniczenia wartosci cech.</param>
        /// <param name="theta">Modyfikator sumy. Standardowo 0. Zwiekszenie tego 
        /// parametru ogranicza liczbe wskazan pozytywnych.</param>
        /// <param name="featureVals">Wartosci cech przykladu</param>
        /// <returns>'true' - klasa 1, 'false' - klasa -1.</returns>
        public static bool Classify(
            IReadOnlyList<double[]> classifier,
            int[] B,
            double[] a1, double[] a2,
            double theta,
            double[] featureVals)
        {
            if (classifier == null || classifier.Count <= 0)
                throw new Exception("classifier is null or empty!");
            if (B == null || B.Length <= 0)
                throw new Exception("B null or empty!");
            if (a1 == null || a1.Length <= 0)
                throw new Exception("a1 null or empty!");
            if (a2 == null || a2.Length <= 0)
                throw new Exception("a2 null or empty!");
            if (featureVals == null || featureVals.Length <= 0)
                throw new Exception("featureVals null or empty!");

            //if (featureVals.Length != classifier[0].Length - 1)
            //{
            //    throw new Exception(string.Format(
            //        "Invalid feature count! FtValCt({0}), ClassifierFtCt({1}).",
            //        featureVals.Length, classifier[0].Length));
            //}

            double sum = 0d;
            foreach (var weak in classifier)
            {
                // Id cechy wykorzystanej w slabym klasyfikatorze.
                var ftId = (int)weak[weak.Length - 1];
                // W ktorym koszyku lezy wartosc cechy.
                var b = GetBucket(featureVals[ftId], B[ftId], a1[ftId], a2[ftId]);
                sum += weak[b];
            }
            bool result = (sum - theta) > 0;

            // Zakomentowac, gdyby za mocno spamowalo po konsoli.
            Console.WriteLine(
                "Object classified: Sum({0}), Theta({1}), Result({2}).",
                sum, theta, result);
            return result;
        }

        //-------------------------------------------------------------------------------

        private static int GetBucket(
            double ftVal,
            int B,
            double a1,
            double a2)
        {
            //int b; // id koszyka do ktorego wpada wartosc cechy
            //if (ftVal <= a1)
            //    b = 0;
            //else if (ftVal > a2)
            //    b = B - 1;
            //else
            //    b = (int)Math.Ceiling(B * (ftVal - a1) / (a2 - a1)) - 1;
            //return b;
            if (a1 == a2) return 0;
            var b = (int)Math.Ceiling(B * (ftVal - a1) / (a2 - a1)) - 1;
            if (b < 0) return 0;
            if (b > B - 1)
                return B - 1;
            return b;
        }

        #endregion


        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX


        #region Files

        public bool SaveToFiles(
            string pathClassifier,
            string pathB,
            string pathA1,
            string pathA2)
        {
            return SaveToFiles(
                pathClassifier, pathB, pathA1, pathA2, Classifier, B, A1, A2);
        }

        public static bool SaveToFiles(
            string pathClassifier,
            string pathB,
            string pathA1,
            string pathA2,
            IReadOnlyList<double[]> classifier,
            int[] bCounts,
            double[] a1,
            double[] a2)
        {
            if (string.IsNullOrWhiteSpace(pathClassifier)
                || string.IsNullOrWhiteSpace(pathB)
                || string.IsNullOrWhiteSpace(pathA1)
                || string.IsNullOrWhiteSpace(pathA2)
                || classifier == null
                || bCounts == null
                || a1 == null
                || a2 == null)
                return false;

            var sb = new StringBuilder();

            // Classifier.
            for (int i = 0; i < classifier.Count; i++)
            {
                for (int j = 0; j < classifier[i].Length; j++)
                {
                    sb.Append(classifier[i][j].ToString(CultureInfo.InvariantCulture));
                    sb.Append(';');
                }
                sb.AppendLine();
            }
            File.WriteAllText(pathClassifier, sb.ToString());
            sb.Clear();

            // B.
            for (int j = 0; j < bCounts.Length; j++)
            {
                sb.Append(bCounts[j].ToString(CultureInfo.InvariantCulture));
                sb.Append(';');
            }
            File.WriteAllText(pathB, sb.ToString());
            sb.Clear();

            // A1.
            for (int j = 0; j < a1.Length; j++)
            {
                sb.Append(a1[j].ToString(CultureInfo.InvariantCulture));
                sb.Append(';');
            }
            File.WriteAllText(pathA1, sb.ToString());
            sb.Clear();

            // A2.
            for (int j = 0; j < a2.Length; j++)
            {
                sb.Append(a2[j].ToString(CultureInfo.InvariantCulture));
                sb.Append(';');
            }
            File.WriteAllText(pathA2, sb.ToString());
            sb.Clear();

            return true;
        }

        //-------------------------------------------------------------------------------

        public bool Read(
            string pathClassifier,
            string pathB,
            string pathA1,
            string pathA2)
        {
            if (!File.Exists(pathClassifier)
                || !File.Exists(pathB)
                || !File.Exists(pathA1)
                || !File.Exists(pathA2))
                return false;
            string[] splitRow;

            // Classifier.
            var fileLines = File.ReadAllLines(pathClassifier);
            var classifier = new List<double[]>(fileLines.Length);
            for (int i = 0; i < fileLines.Length; i++)
            {
                if (fileLines[i].Length <= 0) continue;
                splitRow = fileLines[i].Split(
                    new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                var weakClassifier = new double[splitRow.Length];
                for (int j = 0; j < splitRow.Length; j++)
                {
                    weakClassifier[j] = double.Parse(
                        splitRow[j], CultureInfo.InvariantCulture);
                }
                classifier.Add(weakClassifier);
            }

            // B.
            fileLines = File.ReadAllLines(pathB);
            if (fileLines.Length != 1) return false;
            var bCounts = new int[fileLines[0].Length];
            splitRow = fileLines[0].Split(
                new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < fileLines[0].Length; j++)
            {
                bCounts[j] = int.Parse(
                    splitRow[j], CultureInfo.InvariantCulture);
            }

            // A1.
            fileLines = File.ReadAllLines(pathA1);
            if (fileLines.Length != 1) return false;
            var a1 = new double[fileLines[0].Length];
            splitRow = fileLines[0].Split(
                new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < fileLines[0].Length; j++)
            {
                a1[j] = double.Parse(
                    splitRow[j], CultureInfo.InvariantCulture);
            }

            // A1.
            fileLines = File.ReadAllLines(pathA2);
            if (fileLines.Length != 1) return false;
            var a2 = new double[fileLines[0].Length];
            splitRow = fileLines[0].Split(
                new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < fileLines[0].Length; j++)
            {
                a2[j] = double.Parse(
                    splitRow[j], CultureInfo.InvariantCulture);
            }

            Classifier = classifier;
            B = bCounts;
            A1 = a1;
            A2 = a2;
            return true;
        }

        #endregion


        //XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX


        #region ACCESSORS

        /// <summary>
        /// Klasyfikator zbiorowy.
        /// </summary>
        public IReadOnlyList<double[]> Classifier { get; private set; }

        public int[] B { get; private set; }

        public double[] A1 { get; private set; }

        public double[] A2 { get; private set; }

        #endregion
    }
}
