using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ЛабРабКомГраф
{
    public partial class Form1 : Form
    {
        Bitmap image;
        public Form1()
        {
            InitializeComponent();
        }

        abstract class Filters
        {
            protected abstract Color calcNewPixelColor(Bitmap im, int x, int y);

            public virtual Bitmap ProcessImage(Bitmap im, BackgroundWorker bw)
            {
                Bitmap res = new Bitmap(im.Width, im.Height);
                for (int i = 0; i < im.Width; i++)
                {
                    bw.ReportProgress((int)((float)i / res.Width * 100));
                    if (bw.CancellationPending)
                        return null;
                    for (int j = 0; j < im.Height; j++)
                    {
                        res.SetPixel(i, j, calcNewPixelColor(im, i, j));
                    }
                }
                return res;
            }

            public int Clamp(int val, int min, int max)
            {
                if (val < min)
                    return min;
                if (val > max)
                    return max;
                return val;
            }

            public double Intens(Color color)
            {
                return 0.36 * color.R + 0.53 * color.G + 0.11 * color.B;
            }
        }

        class InvertFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                Color resColor = Color.FromArgb(255 - sColor.R, 255 - sColor.G, 255 - sColor.B);
                return resColor;
            }
        }

        class GrayScaleFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                int intens = (int)(0.299 * sColor.R + 0.587 * sColor.G + 0.114 * sColor.B);
                Color resColor = Color.FromArgb(intens, intens, intens);
                return resColor;
            }
        }

        class SepiaFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                int k = 50;
                int intens = (int)(0.299 * sColor.R + 0.587 * sColor.G + 0.114 * sColor.B);
                double resR = intens + 2 * k;
                double resG = intens + 0.5 * k;
                double resB = intens - 1 * k;
                Color resColor = Color.FromArgb(Clamp((int)resR, 0, 255), Clamp((int)resG, 0, 255), Clamp((int)resB, 0, 255));
                return resColor;
            }
        }

        class UpBrightnessFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                int k = 50;
                Color resColor = Color.FromArgb(Clamp((int)sColor.R + k, 0, 255), Clamp((int)sColor.G + k, 0, 255), Clamp((int)sColor.B + k, 0, 255));
                return resColor;
            }
        }

        class GrayWorldFilter : Filters
        {
            double R;
            double G;
            double B;
            double AvC;
            int norm;
            public GrayWorldFilter(Bitmap im)
            {
                R = 0;
                G = 0;
                B = 0;
                norm = 0;
                for (int i = 0; i < im.Width; i++)
                    for (int j = 0; j < im.Height; j++)
                    {
                        Color tmpColor = im.GetPixel(i, j);
                        R += tmpColor.R;
                        G += tmpColor.G;
                        B += tmpColor.B;
                        norm++;
                    }
                R /= norm;
                G /= norm;
                B /= norm;
                AvC = (R + G + B) / 3;
            }
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {

                Color CurColor = im.GetPixel(x, y);
                return Color.FromArgb(
                    Clamp((int)(CurColor.R * AvC / R), 0, 255),
                     Clamp((int)(CurColor.G * AvC / G), 0, 255),
                      Clamp((int)(CurColor.G * AvC / G), 0, 255));
            }
        }

        class MedianFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                int rad = 2;
                if (x < rad || x >= im.Width - 1 - rad || y < rad || y >= im.Height - 1 - rad)
                    return im.GetPixel(x, y);
                double[] valCol = new double[(rad * 2 + 1) * (rad * 2 + 1)];
                Color[] col = new Color[(rad * 2 + 1) * (rad * 2 + 1)];

                for (int i = -rad; i <= rad; i++)
                    for (int j = -rad; j <= rad; j++)
                    {
                        valCol[(i + rad) * (rad * 2 + 1) + j + rad] = Intens(im.GetPixel(x + i, y + j));
                        col[(i + rad) * (rad * 2 + 1) + j + rad] = im.GetPixel(x + i, y + j);
                    }
                bool f = false;
                for (int i = 0; i < valCol.Length; i++)
                {
                    for (int j = 1; j < valCol.Length; j++)
                    {
                        if (valCol[j] < valCol[j - 1])
                        {
                            double tmp;
                            Color tmpCol;
                            tmp = valCol[j];
                            valCol[j] = valCol[j - 1];
                            valCol[j - 1] = tmp;
                            tmpCol = col[j];
                            col[j] = col[j - 1];
                            col[j - 1] = tmpCol;
                            f = true;
                        }
                    }
                    if (f == false)
                        break;
                }
                return col[col.Length / 2];
            }
        }

        class MatrixFilter : Filters
        {
            protected float[,] ker = null;
            protected MatrixFilter() { }
            public MatrixFilter(float[,] kernel)
            {
                this.ker = kernel;
            }
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                int radX = ker.GetLength(0) / 2;
                int radY = ker.GetLength(1) / 2;
                float resR = 0;
                float resG = 0;
                float resB = 0;
                for (int i = -radY; i <= radY; i++)
                    for (int j = -radX; j <= radX; j++)
                    {
                        int idX = Clamp(x + i, 0, im.Width - 1);
                        int idY = Clamp(y + j, 0, im.Height - 1);
                        Color nearColor = im.GetPixel(idX, idY);
                        resR += nearColor.R * ker[j + radX, i + radY];
                        resG += nearColor.G * ker[j + radX, i + radY];
                        resB += nearColor.B * ker[j + radX, i + radY];
                    }
                return Color.FromArgb(Clamp((int)resR, 0, 255), Clamp((int)resG, 0, 255), Clamp((int)resB, 0, 255));
            }
        }

        class BlurFilter : MatrixFilter
        {
            public BlurFilter()
            {
                int sizeX = 3;
                int sizeY = 3;
                ker = new float[sizeX, sizeY];
                for (int i = 0; i < sizeX; i++)
                    for (int j = 0; j < sizeY; j++)
                        ker[i, j] = 1.0f / (float)(sizeY * sizeX);
            }
        }

        class GaussianFilter : MatrixFilter
        {
            public void createGaussianKernel(int rad, float sigma)
            {
                int size = 2 * rad + 1;
                ker = new float[size, size];
                float norm = 0;
                for (int i = -rad; i <= rad; i++)
                    for (int j = -rad; j <= rad; j++)
                    {
                        ker[i + rad, j + rad] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                        norm += ker[i + rad, j + rad];
                    }
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        ker[i, j] /= norm;
            }
            public GaussianFilter()
            {
                createGaussianKernel(3, 2);
            }
        }

        class SobelsFilter : MatrixFilter
        {
            public SobelsFilter()
            {
                int sizeX = 3;
                int sizeY = 3;
                ker = new float[sizeX, sizeY];
                ker[0, 0] = -1;
                ker[0, 1] = -2;
                ker[0, 2] = -1;
                ker[1, 0] = 0;
                ker[1, 1] = 0;
                ker[1, 2] = 0;
                ker[2, 0] = 1;
                ker[2, 1] = 2;
                ker[2, 2] = 1;
            }
        }

        class UpSharpnessFilter : MatrixFilter
        {
            public UpSharpnessFilter()
            {
                int sizex = 3;
                int sizey = 3;
                ker = new float[sizex, sizey];
                ker[0, 0] = 0;
                ker[0, 1] = -1;
                ker[0, 2] = 0;
                ker[1, 0] = -1;
                ker[1, 1] = 5;
                ker[1, 2] = -1;
                ker[2, 0] = 0;
                ker[2, 1] = -1;
                ker[2, 2] = 0;
            }
        }

        class DilationFilter : Filters
        {
            int width = 3;
            int height = 3;
            int[,] m = { { 1, 1, 1 },
                         { 1, 1, 1 },
                         { 1, 1, 1 } };
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color max = Color.Black;
                double maxIntens = -100000;

                for (int j = -height / 2; j <= height / 2; j++)
                    for (int i = -width / 2; i <= width / 2; i++)
                    {
                        //обработка краевого случая
                        int nx = Clamp(x + i, 0, im.Width - 1);
                        int ny = Clamp(y + j, 0, im.Height - 1);

                        if ((m[width / 2 + i, height / 2 + j] != 0) && (Intens(im.GetPixel(nx, ny)) > maxIntens))
                        {
                            max = im.GetPixel(nx, ny);
                            maxIntens = Intens(max);
                        }
                    }
                return max;
            }
        }

        class ErosionFilter : Filters
        {
            int width = 3;
            int height = 3;
            int[,] m = { { 1, 1, 1 },
                         { 1, 1, 1 },
                         { 1, 1, 1 } };
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color min = Color.Black;
                double minIntens = 100000;

                for (int j = -height / 2; j <= height / 2; j++)
                    for (int i = -width / 2; i <= width / 2; i++)
                    {
                        //обработка краевого случая
                        int nx = Clamp(x + i, 0, im.Width - 1);
                        int ny = Clamp(y + j, 0, im.Height - 1);

                        if ((m[width / 2 + i, height / 2 + j] != 0) && (Intens(im.GetPixel(nx, ny)) < minIntens))
                        {
                            min = im.GetPixel(nx, ny);
                            minIntens = Intens(min);
                        }
                    }
                return min;
            }
        }
        //Под вопросом
        class TopHatFilter : Filters
        {
            Bitmap Im;
            int width = 3;
            int height = 3;
            int[,] m = { { 1, 1, 1 },
                         { 1, 1, 1 },
                         { 1, 1, 1 } };
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color color = Im.GetPixel(x, y);
                if (color.R >= 250 && color.G >= 250 && color.B >= 250)
                    return Color.Black;
                return im.GetPixel(x, y);

            }
            public override Bitmap ProcessImage(Bitmap im, BackgroundWorker bw)
            {
                OpeningFilter op = new OpeningFilter();
                Im = op.ProcessImage(im, bw);
                return base.ProcessImage(im, bw);
            }
        }
        //
        class OpeningFilter : Filters
        {
            DilationFilter dilfil = new DilationFilter();
            ErosionFilter erfil = new ErosionFilter();
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                if (im == null)
                    throw new ArgumentNullException(nameof(im));
                return Color.White;
            }
            public override Bitmap ProcessImage(Bitmap im, BackgroundWorker bw)
            {
                Bitmap res = erfil.ProcessImage(im, bw);
                Bitmap final = dilfil.ProcessImage(im, bw);
                return final;
            }

        }

        class ClosingFilter : Filters
        {
            DilationFilter dilfil = new DilationFilter();
            ErosionFilter erfil = new ErosionFilter();
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                if (im == null)
                    throw new ArgumentNullException(nameof(im));
                return Color.White;
            }
            public override Bitmap ProcessImage(Bitmap im, BackgroundWorker bw)
            {
                Bitmap res = dilfil.ProcessImage(im, bw);
                Bitmap final = erfil.ProcessImage(im, bw);
                return final;
            }
        }

        private void файлToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files|*.png;*.jpeg;*.jpg;*.bmp*|All files(*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                image = new Bitmap(dialog.FileName);
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
        }

        private void инверсияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvertFilter filter = new InvertFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap NewIm = ((Filters)e.Argument).ProcessImage(image, backgroundWorker1);
            if (backgroundWorker1.CancellationPending != true)
                image = NewIm;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
            progressBar1.Value = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void размытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new BlurFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void гауссовоРазмытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GaussianFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void оттенкиСерогоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GrayScaleFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void сепияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new SepiaFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void увеличитьЯркостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new UpBrightnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void собеляToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new SobelsFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void повыситьРезкостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new UpSharpnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void наращиваниеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new DilationFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void эрозияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new ErosionFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void размыканиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new OpeningFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void замыканиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new ClosingFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void серыйМирToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GrayWorldFilter(image);
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void blackHatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new TopHatFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void медианыйФильтрToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new MedianFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }
    }
}
