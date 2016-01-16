using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaarLikeDetector.Komar
{
    class Haar
    {
        //data
        public double[,] iImage;
        int width, height, p, s;
        List<Elementator> keys = new List<Elementator>();

        int window_width, window_height;
        int x_step, y_step, x_start, y_start;
        double window_ratio;
        int window_max_size;
        int window_size_step;
        double window_move_step;
        int size, x, y;
        // Initialize
        Bitmap RGB2Mono(Bitmap Image, byte Conversion_Type = 1)
        {
            /* Conversion type:
             *  0= RGB 2 Grey 2
             *  1= RGB 2 Grey 1
             *  2= Hue
             */
            Bitmap mImage = new Bitmap(Image.Width, Image.Height);
            double mono;
            switch (Conversion_Type)
            {
                case 0:
                    for (int x = 0; x < Image.Width; x++)
                    {
                        for (int y = 0; y < Image.Height; y++)
                        {
                            mono = 0.299 * Image.GetPixel(x, y).R + 0.587 * Image.GetPixel(x, y).G + 0.114 * Image.GetPixel(x, y).B;
                            mImage.SetPixel(x, y, Color.FromArgb((int)mono, (int)mono, (int)mono));
                        }
                    }
                    break;
                case 1:
                    for (int x = 0; x < Image.Width; x++)
                    {
                        for (int y = 0; y < Image.Height; y++)
                        {
                            mono = 0.2126 * Image.GetPixel(x, y).R + 0.7152 * Image.GetPixel(x, y).G + 0.0722 * Image.GetPixel(x, y).B;
                            mImage.SetPixel(x, y, Color.FromArgb((int)mono, (int)mono, (int)mono));
                        }
                    }
                    break;
                case 2:
                    for (int x = 0; x < Image.Width; x++)
                    {
                        for (int y = 0; y < Image.Height; y++)
                        {
                            mono = Image.GetPixel(x, y).GetHue();
                            mImage.SetPixel(x, y, Color.FromArgb((int)mono, (int)mono, (int)mono));
                        }
                    }
                    break;
                default:
                    break;
            };


            return mImage;
        }
        double[,] IntegralImage(Bitmap MonoImage)
        {
            double[,] iImage = new double[MonoImage.Width, MonoImage.Height];
            double[] k = new double[MonoImage.Height];
            for (int x = 0; x < MonoImage.Width; x++)
            {
                for (int y = 0; y < MonoImage.Height; y++)
                {
                    k[y] = MonoImage.GetPixel(x, y).R;

                    if (y > 0)
                    {
                        k[y] += k[y - 1];
                    }
                    iImage[x, y] = k[y];
                    if (x > 0)
                    {
                        iImage[x, y] += iImage[x - 1, y];
                    }

                }
            }
            return iImage;
        }

        public void InicializeImage(Bitmap IMG, byte Conversion_Type = 0)
        {
            iImage = IntegralImage(RGB2Mono(IMG, Conversion_Type));
            width = IMG.Width;
            height = IMG.Height;
        }
        public void InicializeGenerator(int s, int p)
        {
            int px, py, sx, sy;
            this.p = p;
            this.s = s;
            //Elementator temp = new Elementator();
            keys.Clear();
            for (px = -(p - 1); px <= (p - 1); px++)
            {
                //temp.px = px;
                for (py = -(p - 1); py <= (p - 1); py++)
                {
                    //temp.py = py;
                    //scale 
                    for (sx = 0; sx <= s - 1; sx++)
                    {
                        //temp.sx = sx;
                        for (sy = 0; sy <= s - 1; sy++)
                        {
                            //temp.sy = sy;
                            keys.Add(new Elementator(px, py, sx, sy, genMaskDualHorizonta));
                            keys.Add(new Elementator(px, py, sx, sy, genMaskDualVertical));
                            keys.Add(new Elementator(px, py, sx, sy, genMaskTripleHorizonta));
                            keys.Add(new Elementator(px, py, sx, sy, genMaskTripleVertical));
                            keys.Add(new Elementator(px, py, sx, sy, genMaskQuadCheess));
                            keys.Add(new Elementator(px, py, sx, sy, genMaskCenter));
                            keys.Add(new Elementator(px, py, sx, sy, genMask2x3Cheess));
                            keys.Add(new Elementator(px, py, sx, sy, genMask3x2Cheess));

                            /*
                            temp.Mask = new genMask(genMaskDualHorizonta);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMaskDualVertical);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMaskTripleHorizonta);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMaskTripleVertical);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMaskQuadCheess);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMaskCenter);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMask2x3Cheess);
                            keys.Add(temp);
                            temp.Mask = new genMask(genMask3x2Cheess);
                            keys.Add(temp);
                            */
                        }

                    }
                }
            }
        }
        //Haar
        int[,] genRectangle(int cx, int cy, double sw, double sh)
        {
            int[,] corners = new int[2, 2];
            //ox
            corners[0, 0] = cx - (int)sw;
            if (corners[0, 0] < 1)
            {
                corners[0, 0] = 1;
            }

            corners[0, 1] = cx + (int)sw;
            if (corners[0, 1] > width - 2)
            {
                corners[0, 1] = width - 2;
            }
            //oy
            corners[1, 0] = cy - (int)sh;
            if (corners[1, 0] < 1)
            {
                corners[1, 0] = 1;
            }

            corners[1, 1] = cy + (int)sh;
            if (corners[1, 1] > height - 2)
            {
                corners[1, 1] = height - 2;
            }
            return corners;
        }
        double sumInRange(int x1, int x2, int y1, int y2)
        {
            double sum;
            if (x2 >= width)
            {
                x2 = width - 1;
            }
            if (y2 >= height)
            {
                y2 = height - 1;
            }
            if ((x1 <= 0) && (y1 <= 0))
            {
                sum= iImage[x2, y2];
            }
            else {
                if ((x1 <= 0))
                {
                    sum = iImage[x2, y2] - iImage[x2, y1 - 1];
                }
                else
                {
                    if ((y1 <= 0))
                    {
                        sum = iImage[x2, y2] - iImage[x1 - 1, y2] ;
                    }
                    else
                    {
                        sum = iImage[x2, y2] - iImage[x1 - 1, y2] - iImage[x2, y1 - 1] + iImage[x1 - 1, y1 - 1];
                    }
                }
            }
            return sum;
        }
        public double[] allHarrFeatures()
        {
            return this.allHarrFeatures(0, width - 1, 0, height - 1);
        }

        public double[] allHarrFeatures(int x1, int x2, int y1, int y2)
        {
            double[] harr = new double[keys.Count()];
            int cx, cy;
            double max_size_x = (x2 - x1) / (p * 2);
            double max_size_y = (y2 - y1) / (p * 2);
            double max_size = Math.Min(max_size_x, max_size_y);
            double sw, sh;
            int[,] active_rect;
            int k = 0;
            //mount point
            foreach (Elementator element in keys)
            {
                cx = (int)Math.Round((x2 - x1) / 2.0 + element.px * (x2 - x1) / ((p + 1) * 2));
                cy = (int)Math.Round((y2 - y1) / 2.0 + element.py * (y2 - y1) / ((p + 1) * 2));
                sw = max_size * (Math.Pow(Math.Sqrt(2) / 2, element.sx));
                sh = max_size * (Math.Pow(Math.Sqrt(2) / 2, element.sy));

                active_rect = genRectangle(cx, cy, sw, sh);
                harr[k++] = element.Mask(active_rect);
            }
            return harr;
        }
        public double[] selectedHarrFeatures(int x1, int x2, int y1, int y2, List<int> key_filter)
        {
            double[] harr = new double[keys.Count()];
            int cx, cy;
            double max_size_x = (x2 - x1) / (p * 2);
            double max_size_y = (y2 - y1) / (p * 2);
            double max_size = Math.Min(max_size_x, max_size_y);
            double sw, sh;
            int[,] active_rect;
            int k = 0;
            //mount point
            Elementator element;
            foreach (int key in key_filter)
            {
                element = keys.ElementAt(key);
                cx = (int)Math.Round((x2 - x1) / 2.0 + element.px * (x2 - x1) / ((p + 1) * 2));
                cy = (int)Math.Round((y2 - y1) / 2.0 + element.py * (y2 - y1) / ((p + 1) * 2));
                sw = max_size * (Math.Pow(Math.Sqrt(2) / 2, element.sx));
                sh = max_size * (Math.Pow(Math.Sqrt(2) / 2, element.sy));
                active_rect = genRectangle(cx, cy, sw, sh);
                harr[key] = element.Mask(active_rect);
            }
            return harr;
        }

        /*public Dictionary<string,double[]> allHarrScanImage(int start_x,int start_y,double window_ratio, int window_min_size,
                                       int window_max_size, int window_size_step, double window_move_step)
        {
            //ratio width/height
            Dictionary<string, double[]> PicFeatures = new Dictionary<string, double[]>();
            int window_width, window_height;
            int x_step, y_step;
            int size, x, y;
            string key;
            for(size = window_min_size; size < window_max_size; size += window_size_step)
            {
                window_width = size;
                window_height = (int)Math.Round(window_width / window_ratio);
                x_step = (int)Math.Round(window_width * window_move_step);
                y_step = (int)Math.Round(window_height * window_move_step);
                for (x=start_x;x + window_width < width ; x+=x_step)
                {
                    Console.WriteLine(x.ToString());
                    for (y = start_y; y + window_height < height; y += y_step)
                    {
                        key = size.ToString() + " " + x.ToString() + " "  + y.ToString();
                        PicFeatures.Add(key, allHarrFeatures(x, x + window_width, y, y + window_height));
                    }
                }
            }
            return PicFeatures;
        }*/
        //mask generator
        double genMaskDualHorizonta(int[,] corners)
        {
            /*
                    --
                    ++
            */
            double val;
            /*val = - sumInRange(corners[0,0], corners[0, 1], corners[1,0],(int)Math.Round(corners[1,1]/2.0)) 
                + sumInRange(corners[0, 0], corners[0, 1], (int)Math.Round(corners[1, 1] / 2.0)+1, corners[1 ,1]);
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0]));
            */
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0));
            negativeQuantity += ((corners[0, 1] - corners[0, 0] + 1) * ((int)Math.Round(corners[1, 1] / 2.0) - corners[1, 0] + 1));

            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;
            return val;
        }
        double genMaskDualVertical(int[,] corners)
        {
            /*
                    +-
                    +-
            */
            double val;
            /*val = sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), corners[1, 0], corners[1, 1] ) 
                - sumInRange(1+(int)Math.Round(corners[0, 1] / 2.0), corners[0, 1], corners[1, 0], corners[1, 1]);
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0]));*/
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(1 + (int)Math.Round(corners[0, 1] / 2.0), corners[0, 1], corners[1, 0], corners[1, 1]);
            negativeQuantity += ((corners[0, 1] - ((int)Math.Round(corners[0, 1] / 2.0) + 1) + 1) * (corners[1, 1] - corners[1, 0] + 1));

            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;

            return val;
        }
        double genMaskTripleHorizonta(int[,] corners)
        {
            /*
                    ---
                    +++
                    ---
            */
            double val;
            /*val = -sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], (int)Math.Round(corners[1, 1] / 3.0)) 
                + sumInRange(corners[0, 0], corners[0, 1], (int)Math.Round(corners[1, 1] / 3.0)+1, 2*(int)Math.Round(corners[1, 1] / 3.0))
                - sumInRange(corners[0, 0], corners[0, 1], 2*(int)Math.Round(corners[1, 1] / 3.0) + 1, corners[1, 1] );
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0]));*/
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], (int)Math.Round(corners[1, 1] / 3.0));
            negativeQuantity += ((corners[0, 1] - corners[0, 0] + 1) * ((int)Math.Round(corners[1, 1] / 3.0) - corners[1, 0] + 1));
            negative += sumInRange(corners[0, 0], corners[0, 1], 2 * (int)Math.Round(corners[1, 1] / 3.0) + 1, corners[1, 1]);
            negativeQuantity += ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - (2 * (int)Math.Round(corners[1, 1] / 3.0) + 1) + 1));
            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;

            return val;
        }
        double genMaskTripleVertical(int[,] corners)
        {
            /*
                    -+-
                    -+-
                    -+-
            */
            double val;
            /*val = -sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 3.0), corners[1,0], corners[1, 1])
                 + sumInRange((int)Math.Round(corners[0, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[0, 1] / 3.0), corners[1, 0], corners[1, 1])
                 - sumInRange(2 * (int)Math.Round(corners[0, 1] / 3.0) + 1, corners[0, 1], corners[1, 0], corners[1, 1]);
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0])); */
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 3.0), corners[1, 0], corners[1, 1]);
            negativeQuantity += (((int)Math.Round(corners[0, 1] / 3.0) - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            negative += sumInRange(2 * (int)Math.Round(corners[0, 1] / 3.0) + 1, corners[0, 1], corners[1, 0], corners[1, 1]);
            negativeQuantity += ((corners[0, 1] - (2 * (int)Math.Round(corners[0, 1] / 3.0) + 1) + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;


            return val;
        }
        double genMaskQuadCheess(int[,] corners)
        {
            /*
                    -+
                    +-
            */
            double val;
            /*val = -sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0))
                + sumInRange((int)Math.Round(corners[0, 1] / 2.0)+1, corners[0, 1] , corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0))
                - sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), (int)Math.Round(corners[1, 1] / 2.0), corners[1, 1] )
                + sumInRange((int)Math.Round(corners[0, 1] / 2.0) + 1, corners[0, 1], (int)Math.Round(corners[1, 1] / 2.0), corners[1, 1] );
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0])); */
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0));
            negativeQuantity += (((int)Math.Round(corners[0, 1] / 2.0) - corners[0, 0] + 1) * ((int)Math.Round(corners[1, 1] / 2.0) - corners[1, 0] + 1));
            negative += sumInRange((int)Math.Round(corners[0, 1] / 2.0) + 1, corners[0, 1], (int)Math.Round(corners[1, 1] / 2.0) + 1, corners[1, 1]);
            negativeQuantity += ((corners[0, 1] - ((int)Math.Round(corners[0, 1] / 2.0) + 1) + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;



            return val;
        }
        double genMaskCenter(int[,] corners)
        {
            /*
                    ---
                    -+-
                    ---
            */
            double val;
            /*val = -sumInRange(corners[0, 0], corners[0, 1] , corners[1, 0], corners[1, 1])
                + 2* sumInRange((int)Math.Round(corners[0, 1] / 4.0) + 1, 3*(int)Math.Round(corners[0, 1] / 4.0) ,
                    (int)Math.Round(corners[1, 1] / 4.0) + 1, 3 * (int)Math.Round(corners[1, 1] / 4.0));
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0]));*/
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double positiveQuantity = 0;
            double positive = 0;
            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //pos
            positive += sumInRange((int)Math.Round(corners[0, 1] / 4.0) + 1, 3 * (int)Math.Round(corners[0, 1] / 4.0),
                    (int)Math.Round(corners[1, 1] / 4.0) + 1, 3 * (int)Math.Round(corners[1, 1] / 4.0));
            positiveQuantity += (((3 * (int)Math.Round(corners[0, 1] / 4.0)) - ((int)Math.Round(corners[0, 1] / 4.0) + 1) + 1) *
                    ((3 * (int)Math.Round(corners[1, 1] / 4.0))) - ((int)Math.Round(corners[1, 1] / 4.0) + 1) + 1);
            //final
            positive = total - negative;
            val = positive / positiveQuantity - negative / (FullQuatity - positiveQuantity);
            return val;
        }
        double genMask2x3Cheess(int[,] corners)
        {
            /*
                    -+-
                    +-+
            */
            double val;
            /*val = -sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 3.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0))
                    + sumInRange((int)Math.Round(corners[0, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[0, 1] / 3.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0))
                    - sumInRange(2 * (int)Math.Round(corners[0, 1] / 3.0) + 1, corners[1, 0], corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0))
                    + sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 3.0), (int)Math.Round(corners[1, 1] / 2.0) + 1, corners[1, 1])
                    - sumInRange((int)Math.Round(corners[0, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[0, 1] / 3.0), (int)Math.Round(corners[1, 1] / 2.0) + 1, corners[1, 1])
                    + sumInRange(2 * (int)Math.Round(corners[0, 1] / 3.0) + 1, corners[0, 1], (int)Math.Round(corners[1, 1] / 2.0) + 1, corners[1, 1]); */
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 3.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0));
            negativeQuantity += (((int)Math.Round(corners[0, 1] / 3.0) - corners[0, 0] + 1) * (((int)Math.Round(corners[1, 1] / 2.0)) - corners[1, 0] + 1));

            negative += sumInRange(2 * (int)Math.Round(corners[0, 1] / 3.0) + 1, corners[1, 0], corners[1, 0], (int)Math.Round(corners[1, 1] / 2.0));
            negativeQuantity += ((corners[0, 1] - (2 * (int)Math.Round(corners[0, 1] / 3.0) + 1) + 1) * (corners[1, 1] - ((int)Math.Round(corners[1, 1] / 2.0) + 1)));

            negative += sumInRange((int)Math.Round(corners[0, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[0, 1] / 3.0), (int)Math.Round(corners[1, 1] / 2.0) + 1, corners[1, 1]);
            negativeQuantity += (((2 * (int)Math.Round(corners[0, 1] / 3.0)) - ((int)Math.Round(corners[0, 1] / 3.0) + 1) + 1) * (corners[1, 1] - ((int)Math.Round(corners[1, 1] / 2.0) + 1) + 1));

            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;

            return val;
        }
        double genMask3x2Cheess(int[,] corners)
        {
            /*
                    -+
                    +-
                    -+
            */
            double val;
            /*val = -sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 3.0))
                    + sumInRange(corners[0,0], (int)Math.Round(corners[0, 1] / 2.0), (int)Math.Round(corners[1, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[1, 1] / 3.0))
                    - sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), 2 * (int)Math.Round(corners[1, 1] / 3.0) + 1, corners[1, 1])
                    + sumInRange((int)Math.Round(corners[0, 1] / 2.0) + 1, corners[0, 1], corners[1, 0], (int)Math.Round(corners[1, 1] / 3.0))
                    - sumInRange((int)Math.Round(corners[0, 1] / 2.0) + 1, corners[0, 1], (int)Math.Round(corners[1, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[1,1] / 3.0))
                    + sumInRange((int)Math.Round(corners[0, 1] / 2.0) + 1, corners[0, 1], 2 * (int)Math.Round(corners[1, 1] / 3.0) + 1, corners[1, 1]);
            val = val / ((corners[0, 1] - corners[0, 0]) * (corners[1, 1] - corners[1, 0])); */
            double total = 0;
            double FullQuatity = 0;
            double negative = 0;
            double negativeQuantity = 0;
            double positive = 0;

            total = sumInRange(corners[0, 0], corners[0, 1], corners[1, 0], corners[1, 1]);
            FullQuatity = ((corners[0, 1] - corners[0, 0] + 1) * (corners[1, 1] - corners[1, 0] + 1));
            //neg
            negative += sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), corners[1, 0], (int)Math.Round(corners[1, 1] / 3.0));
            negativeQuantity += ((((int)Math.Round(corners[0, 1] / 2.0)) - corners[0, 0] + 1) * (((int)Math.Round(corners[1, 1] / 3.0)) - corners[1, 0] + 1));

            negative += sumInRange(corners[0, 0], (int)Math.Round(corners[0, 1] / 2.0), 2 * (int)Math.Round(corners[1, 1] / 3.0) + 1, corners[1, 1]);
            negativeQuantity += ((((int)Math.Round(corners[0, 1] / 2.0)) - corners[0, 0] + 1) * (corners[1, 1] - (2 * (int)Math.Round(corners[1, 1] / 3.0) + 1) + 1));

            negative += sumInRange((int)Math.Round(corners[0, 1] / 2.0) + 1, corners[0, 1], (int)Math.Round(corners[1, 1] / 3.0) + 1, 2 * (int)Math.Round(corners[1, 1] / 3.0));
            negativeQuantity += ((corners[0, 1] - ((int)Math.Round(corners[0, 1] / 2.0) + 1) + 1) * ((2 * (int)Math.Round(corners[1, 1] / 3.0)) - ((int)Math.Round(corners[1, 1] / 3.0) + 1) + 1));

            //final
            positive = total - negative;
            val = positive / (FullQuatity - negativeQuantity) - negative / negativeQuantity;


            return val;
        }

        public void allHarrScanImageStart(int start_x, int start_y, double window_ratio, int window_min_size,
                                       int window_max_size, int window_size_step, double window_move_step)
        {
            size = window_min_size;

            this.window_max_size = window_max_size;
            this.window_size_step = window_size_step;
            this.window_move_step = window_move_step;
            this.window_ratio = window_ratio;
            this.x_start = start_x;
            this.y_start = start_y;
            x = x_start;
            y = y_start;
            return ;
        }
        public double[] allHarrScanImageStep()
        {
            double[] ArgH;
            window_width = size;
            window_height = (int)Math.Round(window_width / window_ratio);
            x_step = (int)Math.Round(window_width * window_move_step);
            y_step = (int)Math.Round(window_height * window_move_step);
            ArgH = allHarrFeatures(x, x + window_width, y, y + window_height);
            x += x_step;
            if (x + window_width > width)
            {
                x = x_start;
                y += y_step;

            }
            return ArgH;
        }
        public double[] allHarrScanImageStep(List<int> key_filter)
        {
            double[] ArgH;
            window_width = size;
            window_height = (int)Math.Round(window_width / window_ratio);
            x_step = (int)Math.Round(window_width * window_move_step);
            y_step = (int)Math.Round(window_height * window_move_step);
            ArgH = selectedHarrFeatures(x, x + window_width, y, y + window_height,key_filter);
            x += x_step;
            if (x + window_width > width)
            {
                x = x_start;
                y += y_step;

            }
            return ArgH;
        }
        public bool isContinue()
        {
            bool ans = true;
            if (y + window_height > height)
            {
                window_width = window_width + window_size_step;
                window_height = (int)Math.Round(window_width / window_ratio);
                x = x_start;
                y = y_start;
            }
            if (window_width> window_max_size) { 
                ans = false;
            }
            return ans;
        }
    }
}
