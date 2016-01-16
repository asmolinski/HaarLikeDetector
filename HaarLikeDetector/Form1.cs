using HaarLikeDetector.Adam;
using HaarLikeDetector.Komar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HaarLikeDetector
{
    public partial class Form1 : Form
    {
        private int s; 
        private int p;
        private double scaling; 
        private double ratio;
        private bool paralel;

        public Form1()
        {
            InitializeComponent();
            paralel = true;

        }

        /// <summary>
        /// Przycisk o Programie
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            new AboutBox().Show();
        }

        /// <summary>
        /// Odpalenie ObjectMakera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void narzedzieObjectMakerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"ObjectMaker\objectmarker.exe");
        }

        /// <summary>
        /// Wycinanie z obrazka
        /// </summary>
        /// <param name="source"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        private Bitmap CropImage(Bitmap source, Rectangle section)
        {
            // An empty bitmap which will hold the cropped image
            Bitmap bmp = new Bitmap(section.Width, section.Height);

            Graphics g = Graphics.FromImage(bmp);

            // Draw the given area (section) of the source image
            // at location 0,0 on the empty bitmap (bmp)
            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            return bmp;
        }

        /// <summary>
        /// Ekstrahowanie pojedynczej twarzy z obrazu
        /// </summary>
        /// <param name="Picture">Obraz źródłowy</param>
        /// <param name="parameters">tablica [x, y, width, heigh] opisująca twarz</param>
        /// <returns></returns>
        private Bitmap getSingleFace(Bitmap Picture, double[] parameters)
        {
            int width = Picture.Width, height = Picture.Height, x = 0, y = 0;

            if (Convert.ToDouble(parameters[2]) / Convert.ToDouble(parameters[3]) < ratio)
            {
                //rozszerz width
                width = Convert.ToInt32(ratio * Convert.ToInt32(parameters[3]));
                height = Convert.ToInt32(parameters[3]);
                x = Convert.ToInt32(parameters[1]) - (width - Convert.ToInt32(parameters[2])) / 2;
                //warunki brzegowe
                if (x < 0)
                    x = 0; //jeżeli rozszerzając wyjdziemy za lewą krawędź
                if (x + width > Picture.Width)
                    x = Picture.Width - width; //jeżeli wyjdziemy za prawą krawędź
                y = Convert.ToInt32(parameters[1]);
            }
            else
            {
                //rozszerz height
                width = Convert.ToInt32(parameters[2]);
                height = Convert.ToInt32(Convert.ToInt32(parameters[2]) / ratio);
                x = Convert.ToInt32(parameters[0]);
                y = Convert.ToInt32(parameters[1]) - (height - Convert.ToInt32(parameters[3])) / 2;
                if (y < 0)
                    y = 0;
                if (y + height > Picture.Height)
                    y = Picture.Height - height;
            }
            Rectangle section = new Rectangle(
                 new Point(x, y),
                 new Size(Convert.ToInt32(width * scaling), Convert.ToInt32(height * scaling))
            );
            return CropImage(Picture, section);
        }

        /// <summary>
        /// Wyliczenie cech Haara dla pliku
        /// </summary>
        /// <param name="Picture">Plik obrazu</param>
        /// <param name="parameters">parametry z ObjectMakera</param>
        /// <returns></returns>
        private List<double[]> HaarFeatures(string Picture, string[] parameters)
        {
            List<double[]> ArgH = new List<double[]>();
            Haar twarz = new Haar();
            twarz.InicializeGenerator(this.s, this.p);
            for (int i = 0 ; i < Convert.ToInt32(parameters[1]); i++)
            {
                try {
                    twarz.InicializeImage(this.getSingleFace(new Bitmap(Picture), new double[] {
                        Convert.ToDouble(parameters[4*i+2]),  Convert.ToDouble(parameters[4 * i + 3]),  Convert.ToDouble(parameters[4 * i + 4]), Convert.ToDouble(parameters[4 * i + 5])
                    }));
                }
                catch { MessageBox.Show("Niepoprawne parametry dla pliku: " + Picture); }
            ArgH.Add(twarz.allHarrFeatures());
            }
            return ArgH;
        }

        /// <summary>
        /// Przycisk wyliczający cechy Haara
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void extrachujCechyHarraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            s = Convert.ToInt32(this.toolStripTextBox5.Text);
            p = Convert.ToInt32(this.toolStripTextBox6.Text);
            scaling = Convert.ToDouble(this.toolStripTextBox2.Text);
            ratio = Convert.ToDouble(this.toolStripTextBox4.Text);
            this.toolStripStatusLabel1.Text = "Analiza bazy zdjęć:";
            Cursor.Current = Cursors.WaitCursor;
            this.Update();
            List<double[]> ArgH = new List<double[]>();
            string line = "";
            string[] parameters;
            int images=0;
            try {
                images = File.ReadAllLines(@"info.txt").Length;
                StreamReader file = new StreamReader(@"info.txt");
                //Czytanie danych z ObjectMakera
                if (paralel)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Parallel.For(0, images, i =>
                        {
                        //this.toolStripProgressBar1.Value = i * 100 / images;
                        line = file.ReadLine();
                            parameters = line.Split('.');
                            ArgH.AddRange(this.HaarFeatures(parameters[0] + ".bmp", parameters[1].Split(' ')));

                        });
                    }).Wait();
                }
                else
                {
                    int currentline = 0;
                    while ((line = file.ReadLine()) != null)
                    {
                        currentline++;
                        this.toolStripProgressBar1.Value = currentline * 100 / images;
                        parameters = line.Split('.');

                        ArgH = ArgH.Concat(this.HaarFeatures(parameters[0] + ".bmp", parameters[1].Split(' '))).ToList();
                    }
                }
                file.Close();
            }
            catch
            {
                MessageBox.Show("brak pliku info.txt - uruchom program objectmaker by go wygenerować.");
            }

            //Zapis wyestrahowanych cech do pliku
            try
            {
                StreamWriter writer = new StreamWriter("cechy.txt");
                foreach (double[] cecha in ArgH)
                {
                    writer.WriteLine(String.Join(" ", cecha));
                }
                writer.Close();
            }
            catch { MessageBox.Show("Nie udało się zapisać cech Haara do pliku :("); }


            Cursor.Current = Cursors.Default;
            MessageBox.Show("Przeanalizowano "+ ArgH.Count +" twarzy z "+images+" obrazów.");
            this.toolStripStatusLabel1.Text = "Gotowe.";
        }
        
        /// <summary>
        /// Załadowanie obrazu do detekcji
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void załadujObrazToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Title = "Wybierz plik do detekcji";
            dlg.Filter = "bmp files (*.bmp)|*.bmp";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                this.pictureBox1.Image = new Bitmap(dlg.FileName);
            }

            dlg.Dispose();
        }

        /// <summary>
        /// Uczenie klasyfikatora
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel1.Text = "Wczytywanie cech:";
            this.Update();
            List<double[]> ArgH = new List<double[]>(800);
            string line;
            //Wczytaj cechy Haara z pliku
            //Task.Run(delegate {
                //try
                {
                    StreamReader file = new StreamReader(@"cechy.txt");
                    //int lineCount = File.ReadAllLines(@"cechy.txt").Length;
                    int currentLine = 0;
                    //Czytanie danych z ObjectMakera
               
                        while ((line = file.ReadLine()) != null)
                        {
                            currentLine++;
                            //this.toolStripProgressBar1.Value = currentLine * 100 / lineCount;
                            line += " 1"; //oznaczenie przykładu pozytywnego
                            ArgH.Add(Array.ConvertAll(line.Split(' '), Double.Parse));
                    if (currentLine > 200)
                        break;
                        }
                
                    file.Close();
                }
                //catch { MessageBox.Show("Brak pliku cechy.txt - uruchom program Ekstraktor cech Haara by go wygenerować."); }
            //});
            this.toolStripStatusLabel1.Text = "Wczytywanie negatywnych:";
            this.Update();
            //Task.Run(delegate {
                //try
                {
                    StreamReader file = new StreamReader(@"negative.txt");
                    //int lineCount = File.ReadAllLines(@"negative.txt").Length;
                    int currentLine = 0;
                    //Czytanie danych z ObjectMakera

                    while ((line = file.ReadLine()) != null)
                    {
                        currentLine++;
                        //this.toolStripProgressBar1.Value = currentLine * 100 / lineCount;
                        line += " -1"; //oznaczenie przykładu pozytywnego
                        ArgH.Add(Array.ConvertAll(line.Split(' '), Double.Parse));
                    if (currentLine > 600)
                        break;
                    }

                    file.Close();
                }
                //catch { MessageBox.Show("Brak pliku negative.txt - uruchom generowanie przykładów negatywnych."); }
            //});

            //Odpal funkcję uczącą Adama
                Cursor.Current = Cursors.WaitCursor;
                this.toolStripStatusLabel1.Text = "Uczenie klasyfikatora";
                this.Update();
                RealBoostClassifier klasyfikator = new RealBoostClassifier();
                klasyfikator.TrainClassifier(ArgH, Convert.ToInt32(this.toolStripTextBox1.Text), Convert.ToInt32(this.toolStripTextBox3.Text));
                
            //Zapisz wynik uczenia
                this.toolStripStatusLabel1.Text = "Zapis wyniku uczenia";
                this.Update();
                klasyfikator.SaveToFiles("clasifier.txt", "B.txt", "A1.txt", "A2.txt");

                Cursor.Current = Cursors.Default;
                this.toolStripStatusLabel1.Text = "Gotowe.";
               
         }

        /// <summary>
        /// Otworzenie w explorerze folderu programu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            Process.Start(@".\");
        }

        /// <summary>
        /// Konwersja obrazu na bmp
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public Bitmap ConvertToBitmap(string fileName)
        {
            Bitmap bitmap;
            using (Stream bmpStream = System.IO.File.Open(fileName, System.IO.FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);
                bitmap = new Bitmap(image);
            }
            return bitmap;
        }

        /// <summary>
        /// Wygenerowanie cech haara dla przykładów negatywnych
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel1.Text = "Analiza przykładów negatywnych:";
            Cursor.Current = Cursors.WaitCursor;
            this.Update();
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles("negative\\");
            List<double[]> ArgH = new List<double[]>();
            s = Convert.ToInt32(this.toolStripTextBox5.Text);
            p = Convert.ToInt32(this.toolStripTextBox6.Text);
            scaling = Convert.ToDouble(this.toolStripTextBox2.Text);
            ratio = Convert.ToDouble(this.toolStripTextBox4.Text);
            Haar twarz = new Haar();
            twarz.InicializeGenerator(this.s, this.p);

            if (paralel)
            {
                Task.Factory.StartNew(() =>
                {
                    Parallel.For(0, fileEntries.Length, i =>
                    {
                        try
                        {
                            //this.toolStripProgressBar1.Value = i * 100 / images;
                            Bitmap pic = ConvertToBitmap(fileEntries[i]);
                            for (int k = 0; k < 3; k++)
                                for (int l = 0; l < 3; l++)
                                {
                                    twarz.InicializeImage(this.getSingleFace(pic, new double[] { pic.Width / 3 * k, pic.Height / 3 * l, pic.Width / 3, pic.Height / 3 }));
                                    ArgH.Add(twarz.allHarrFeatures());
                                }
                        }
                        catch {
                            //MessageBox.Show("Jakiś problem ze zrównolegleniem.");
                        }
                    });
                }).Wait();
            }
            else
            {
                int currentline = 0;
                foreach (string fileName in fileEntries)
                {
                    currentline++;
                    this.toolStripProgressBar1.Value = currentline * 100 / fileEntries.Length;
                    Bitmap pic = ConvertToBitmap(fileName);
                    for (int k = 0; k < 3; k++)
                        for (int l = 0; l < 3; l++)
                        {
                            twarz.InicializeImage(this.getSingleFace(pic, new double[] { pic.Width / 3 * k, pic.Height / 3 * l, pic.Width / 3, pic.Height / 3 }));
                            ArgH.Add(twarz.allHarrFeatures());
                        }
                }
            }
        
        //Zapis wyestrahowanych cech do pliku
            try
            {
                StreamWriter writer = new StreamWriter("negative.txt");
                foreach (double[] cecha in ArgH)
                {
                    writer.WriteLine(String.Join(" ", cecha));
                }
                writer.Close();
            }
            catch { MessageBox.Show("Nie udało się zapisać cech Haara do pliku :("); }


            Cursor.Current = Cursors.Default;
            MessageBox.Show("Przeanalizowano "+ ArgH.Count +" twarzy z "+ fileEntries .Length+ " obrazów.");
            this.toolStripStatusLabel1.Text = "Gotowe.";

        }
    }
}
