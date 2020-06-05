using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;

static class Constants
{
    //public const string InputFolder = ".\\Input";
    public const string OutputFolderName = ".\\Output";
    public const string LogFolderName = ".\\Log";
    public const string PositiveImageFileName = "PositiveImage.csv";
    public const string NegativeImageFileName = "NegativeImage.csv";
    public const string LogFileName = "HardwareWT0014.log";
    public const string HeaderTag = "# CoolTouch image dump file";
    public const string FrameTag = "# Raw Image";
}

namespace HardwareWT0014
{
    public class TouchInputImage
    {
        [DllImport("RegionLabeling.dll")]
        public static extern int RegionLabeling(string path, int threshold);

        [DllImport("RegionLabeling.dll")]
        public static extern int GetTopLeftX(int regionNo);

        [DllImport("RegionLabeling.dll")]
        public static extern int GetTopLeftY(int regionNo);

        [DllImport("RegionLabeling.dll")]
        public static extern int GetBottomRightX(int regionNo);

        [DllImport("RegionLabeling.dll")]
        public static extern int GetBottomRightY(int regionNo);

        public string imagePath;
        public int imageWidth;
        public int imageHeight;

        public StreamReader rawImageReader;
        public StreamWriter positiveImageWriter;
        public StreamWriter negativeImageWriter;
        public StreamWriter logWriter;

        public short[,] currentImage;
        public short[,] referenceImage;
        public short[,] positiveImage;
        public short[,] negativeImage;
        public short[,] positiveRegionImage;
        public short[,] negativeRegionImage;

        public Dictionary<int, Rectangle> positiveRegion;
        public Dictionary<int, Rectangle> negativeRegion;

        public TouchInputImage(string path, int width, int height)
        {
            imagePath = path;
            imageWidth = width;
            imageHeight = height;

            rawImageReader = new StreamReader(path);
            positiveImageWriter = new StreamWriter(Path.Combine(Constants.OutputFolderName, Constants.PositiveImageFileName));
            negativeImageWriter = new StreamWriter(Path.Combine(Constants.OutputFolderName, Constants.NegativeImageFileName));
            logWriter = new StreamWriter(Path.Combine(Constants.LogFolderName, Constants.LogFileName));

            currentImage = new short[height, width];
            referenceImage = new short[height, width];
            positiveImage = new short[height, width];
            negativeImage = new short[height, width];
            positiveRegionImage = new short[height, width];
            negativeRegionImage = new short[height, width];

            positiveRegion = new Dictionary<int, Rectangle>();
            negativeRegion = new Dictionary<int, Rectangle>();
        }

        public void GetHeader()
        {
            string line = rawImageReader.ReadLine();
            if (line == Constants.HeaderTag)
            {
                while (line != "")
                {
                    line = rawImageReader.ReadLine();
                    Console.WriteLine(line);
                }
            }
        }

        public bool GetNextFrame()
        {
            string line = "";
            while (line.Contains(Constants.FrameTag) == false)
            {
                if ((line = rawImageReader.ReadLine()) == null)
                {
                    return false;
                }
            }

            // Read the line of CS00,CS01,CS02....
            rawImageReader.ReadLine();

            for (int row = 0; row < imageHeight; row++)
            {
                line = rawImageReader.ReadLine();
                string[] words = line.Split(',');
                if (words[0].Contains("CD"))
                {
                    for (int col = 0; col < imageWidth; col++)
                    {
                        currentImage[row, col] = Convert.ToInt16(words[col + 1]);
                    }
                }
            }

            return true;
        }

        public void UpdateReferenceImage()
        {
            //GetNextFrame();
            Array.Copy(currentImage, referenceImage, imageWidth * imageHeight);
        }

        public void UpdatePositiveImage()
        {
            for (int row = 0; row < imageHeight; row++)
            {
                StringBuilder line = new StringBuilder();
                for (int col = 0; col < imageWidth; col++)
                {
                    positiveImage[row, col] = (short)(referenceImage[row, col] - currentImage[row, col]);
                    if (positiveImage[row, col] < 0)
                    {
                        positiveImage[row, col] = 0;
                    }
                    line.Append(positiveImage[row, col]);
                    line.Append(",");
                }
                positiveImageWriter.WriteLine(line);
            }
            positiveImageWriter.WriteLine("");
            positiveImageWriter.Flush();
        }

        public void UpdateNegativeImage()
        {
            for (int row = 0; row < imageHeight; row++)
            {
                StringBuilder line = new StringBuilder();
                for (int col = 0; col < imageWidth; col++)
                {
                    negativeImage[row, col] = (short)(referenceImage[row, col] - currentImage[row, col]);
                    if (negativeImage[row, col] > 0)
                    {
                        negativeImage[row, col] = 0;
                    }
                    line.Append(negativeImage[row, col]);
                    line.Append(",");
                }
                negativeImageWriter.WriteLine(line);
            }
            negativeImageWriter.WriteLine("");
            negativeImageWriter.Flush();
        }

        public void UpdatePositiveRegion()
        {
            positiveRegion.Clear();

            using (StreamWriter writer = new StreamWriter("RegionLabelingInput.csv"))
            {
                for (int row = 0; row < imageHeight; row++)
                {
                    StringBuilder line = new StringBuilder();
                    for (int col = 0; col < imageWidth; col++)
                    {
                        line.Append(positiveImage[row, col]);
                        line.Append(",");
                    }
                    writer.WriteLine(line);
                }
            }

            int regionCount = RegionLabeling("RegionLabelingInput.csv", 120);

            string log = string.Format("PositiveRegionCount = {0}", regionCount);
            logWriter.WriteLine(log);

            if (regionCount > 0)
            {
                for (int regionNo = 0; regionNo < regionCount; regionNo++)
                {
                    int topLeftX = GetTopLeftX(regionNo);
                    int topLeftY = GetTopLeftY(regionNo);
                    int width = GetBottomRightX(regionNo) - topLeftX;
                    int height = GetBottomRightY(regionNo) - topLeftY;
                    positiveRegion.Add(regionNo, new Rectangle(topLeftX, topLeftY, width, height));
                }
            }

            if (regionCount > 0)
            {
                for (int regionNo = 0; regionNo < regionCount; regionNo++)
                {
                    int topLeftX = positiveRegion[regionNo].Left;
                    int topLeftY = positiveRegion[regionNo].Top;
                    int bottomRightX = positiveRegion[regionNo].Right;
                    int bottomRightY = positiveRegion[regionNo].Bottom;
                    log = string.Format("PositiveRrgion[{0}]: ({1}, {2}) -> ({3}, {4})", regionNo, topLeftX, topLeftY, bottomRightX, bottomRightY);
                    logWriter.WriteLine(log);
                }
            }

            logWriter.Flush();
        }

        public void UpdateNegativeRegion()
        {
            negativeRegion.Clear();

            using (StreamWriter writer = new StreamWriter("RegionLabelingInput.csv"))
            {
                for (int row = 0; row < imageHeight; row++)
                {
                    StringBuilder line = new StringBuilder();
                    for (int col = 0; col < imageWidth; col++)
                    {
                        line.Append(Math.Abs(negativeImage[row, col]));
                        line.Append(",");
                    }
                    writer.WriteLine(line);
                }
            }

            int regionCount = RegionLabeling("RegionLabelingInput.csv", 120);

            string log = string.Format("NegativeRegionCount = {0}", regionCount);
            logWriter.WriteLine(log);

            if (regionCount > 0)
            {
                for (int regionNo = 0; regionNo < regionCount; regionNo++)
                {
                    int topLeftX = GetTopLeftX(regionNo);
                    int topLeftY = GetTopLeftY(regionNo);
                    int bottomRightX = GetBottomRightX(regionNo);
                    int bottomRightY = GetBottomRightY(regionNo);

                    negativeRegion.Add(regionNo, new Rectangle(topLeftX, topLeftY, bottomRightX, bottomRightY));

                    log = string.Format("{0} {1} {2} {3}", topLeftX, topLeftY, bottomRightX, bottomRightY);
                    logWriter.WriteLine(log);
                }
            }

            logWriter.Flush();
        }
    }
}
