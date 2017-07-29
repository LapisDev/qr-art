using Lapis.QRCode.Encoding;
using Lapis.QRCode.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lapis.QRCode.Art
{
    public interface IBinarizer
    {
        BitMatrix Binarize(IRgb24BitmapBase bitmap, int rowCount, int columnCount);
    }

    public class Binarizer : IBinarizer
    {
        public BitMatrix Binarize(IRgb24BitmapBase bitmap, int rowCount, int columnCount)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            var bitMatrix = new BitMatrix(rowCount, columnCount);

            int[,] rgb24s = Sample(bitmap, rowCount, columnCount);
            int[,] grays = ToGrays(rgb24s);
            int[] histGram = GetHistGram(grays);
            int threhold = GetThreshold(histGram);
            for (int i = 0; i < grays.GetLength(0); i++)
            {
                for (int j = 0; j < grays.GetLength(1); j++)
                {
                    bitMatrix[i, j] = grays[i, j] < threhold;
                }
            }
            return bitMatrix;
        }    

        private int[,] Sample(IRgb24BitmapBase bitmap, int rowCount, int columnCount)
        {
            float height = Convert.ToSingle(bitmap.Height);
            float width = Convert.ToSingle(bitmap.Width);
            float rowLength = Convert.ToSingle(rowCount);
            float columnLength = Convert.ToSingle(columnCount);
            int[,] rgb24s = new int[rowCount, columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                for (int j = 0; j < rowCount; j++)
                {
                    int x = Convert.ToInt32(width / columnLength * i);
                    int y = Convert.ToInt32(height / rowLength * j);
                    int color = bitmap.GetPixel(x, y);
                    rgb24s[j, i] = color;
                }
            }
            return rgb24s;
        }

        private int[,] ToGrays(int[,] rgb24s)
        {
            int[,] grays = new int[rgb24s.GetLength(0), rgb24s.GetLength(1)];
            for (int i = 0; i < rgb24s.GetLength(0); i++)
            {
                for (int j = 0; j < rgb24s.GetLength(1); j++)
                {
                    int r = (rgb24s[i, j] & 0xFF0000) >> 16;
                    int g = (rgb24s[i, j] & 0xFF00) >> 8;
                    int b = rgb24s[i, j] & 0xFF;
                    int grayscale = Convert.ToInt32(0.2126 * r + 0.7152 * g + 0.722 * b);
                    grays[i, j] = grayscale;
                }
            }
            return grays;
        }

        private int[] GetHistGram(int[,] grays)
        {
            int[] histGram = new int[256];
            for (int i = 0; i < grays.GetLength(0); i++)
            {
                for (int j = 0; j < grays.GetLength(1); j++)
                {
                    histGram[grays[i, j] & 0xFF] += 1;
                }
            }
            return histGram;
        }

        private int GetThreshold(int[] histGram)
        {
            int y, amount = 0;
            int pixelBack = 0, pixelFore = 0, pixelIntegralBack = 0, pixelIntegralFore = 0, pixelIntegral = 0;
            double omegaBack, omegaFore, microBack, microFore, sigmaB, sigma;
            int minValue, maxValue;
            int threshold = 0;

            for (minValue = 0; minValue < 256 && histGram[minValue] == 0; minValue++) ;
            for (maxValue = 255; maxValue > minValue && histGram[minValue] == 0; maxValue--) ;
            if (maxValue == minValue)
                return maxValue;
            if (minValue + 1 == maxValue)
                return minValue;

            for (y = minValue; y <= maxValue; y++)
                amount += histGram[y];

            pixelIntegral = 0;
            for (y = minValue; y <= maxValue; y++)
                pixelIntegral += histGram[y] * y;
            sigmaB = -1;
            for (y = minValue; y < maxValue; y++)
            {
                pixelBack = pixelBack + histGram[y];
                pixelFore = amount - pixelBack;
                omegaBack = (double)pixelBack / amount;
                omegaFore = (double)pixelFore / amount;
                pixelIntegralBack += histGram[y] * y;
                pixelIntegralFore = pixelIntegral - pixelIntegralBack;
                microBack = (double)pixelIntegralBack / pixelBack;
                microFore = (double)pixelIntegralFore / pixelFore;
                sigma = omegaBack * omegaFore * (microBack - microFore) * (microBack - microFore);
                if (sigma > sigmaB)
                {
                    sigmaB = sigma;
                    threshold = y;
                }
            }
            return threshold;
        }
    }
}