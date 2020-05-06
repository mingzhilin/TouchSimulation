using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using HardwareSimulator;

static class Constants
{
    public const string FirmwarePath = "U:\\WeidaHiTech\\TestData\\WDT875X_Flash_512k_2020_0505_000.bin";
    public const string RawImagePath = "U:\\WeidaHiTech\\TestData\\WDT875X_ImageDump_2020_0505_000.csv";
    public const string OutputFolderName = ".\\Output";
    public const string LogFolderName = ".\\Log";
    public const string LogFileName = "TouchSimulation.log";
    public const int ImageWidth = 46;
    public const int ImageHeight = 22;
    public const int ImageSize = ImageWidth * ImageHeight;
}

namespace TouchSimulation
{
    class Program
    {
        static TouchInputImage touchInput;
        static Image touchOutput;
        static Graphics graphics;

#if false
        [DllImport("RegionLabeling.dll")]
        public static extern void RegionLabeling(string path, int threshold);
#endif

        [DllImport("FirmwareSimulator.dll")]
        public static extern void LoadParameterFromFirmwareBinary(string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool GetNextFrameFunctionPointer();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate short GetPositiveImageFunctionPointer(int startRow, int startCol, int currRow, int currCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate short GetNegativeImageFunctionPointer(int startRow, int startCol, int currRow, int currCol);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetRegionCountFunctionPointer(bool isPositiveRegion);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetRegionLeftFunctionPointer(bool isPositiveRegion, int regionNo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetRegionTopFunctionPointer(bool isPositiveRegion, int regionNo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetRegionRightFunctionPointer(bool isPositiveRegion, int regionNo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int GetRegionBottomFunctionPointer(bool isPositiveRegion, int regionNo);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdateReferenceImageFunctionPointer();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdatePositiveImageFunctionPointer();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdateNegativeImageFunctionPointer();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdatePositiveRegionFunctionPointer();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdateNegativeRegionFunctionPointer();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SaveTouchOutputImageFunctionPointer(int frameNo, int x, int y);

        [DllImport("FirmwareSimulator.dll")]
        public static extern void SetupCallbackFunctions([MarshalAs(UnmanagedType.FunctionPtr)] GetNextFrameFunctionPointer GetNextFrame,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetPositiveImageFunctionPointer GetPositiveImage,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetNegativeImageFunctionPointer GetNegativeImage,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetRegionCountFunctionPointer GetRegionCount,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetRegionLeftFunctionPointer GetRegionLeft,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetRegionTopFunctionPointer GetRegionTop,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetRegionRightFunctionPointer GetRegionRight,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] GetRegionBottomFunctionPointer GetRegionBottom,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] UpdateReferenceImageFunctionPointer UpdateReferenceImage,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] UpdatePositiveImageFunctionPointer UpdatePositiveImage,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] UpdateNegativeImageFunctionPointer UpdateNegativeImag,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] UpdatePositiveRegionFunctionPointer UpdatePositiveRegion,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] UpdateNegativeRegionFunctionPointer UpdateNegativeRegion,
                                                         [MarshalAs(UnmanagedType.FunctionPtr)] SaveTouchOutputImageFunctionPointer SaveTouchOuputImage);

        [DllImport("FirmwareSimulator.dll")]
        public static extern void StartProcessTouchSignal();

        public static bool GetNextFrame()
        {
            return touchInput.GetNextFrame();
        }

        public static short GetPositiveImage(int startRow, int startCol, int currRow, int currCol)
        {
            return touchInput.positiveImage[currRow - startRow, currCol - startCol];
        }

        public static short GetNegativeImage(int startRow, int startCol, int currRow, int currCol)
        {
            return touchInput.negativeImage[currRow - startRow, currCol - startCol];
        }

        public static int GetRegionCount(bool isPositiveRegion)
        {
            if (isPositiveRegion == true)
            {
                return touchInput.positiveRegion.Count;
            }
            else
            {
                return touchInput.negativeRegion.Count;
            }
        }

        public static int GetRegionLeft(bool isPositiveRegion, int regionNo)
        {
            if (isPositiveRegion == true)
            {
                return touchInput.positiveRegion[regionNo].Left;
            }
            else
            {
                return touchInput.negativeRegion[regionNo].Left;
            }
        }

        public static int GetRegionTop(bool isPositiveRegion, int regionNo)
        {
            if (isPositiveRegion == true)
            {
                return touchInput.positiveRegion[regionNo].Top;
            }
            else
            {
                return touchInput.negativeRegion[regionNo].Top;
            }
        }

        public static int GetRegionRight(bool isPositiveRegion, int regionNo)
        {
            if (isPositiveRegion == true)
            {
                return touchInput.positiveRegion[regionNo].Right;
            }
            else
            {
                return touchInput.negativeRegion[regionNo].Right;
            }
        }

        public static int GetRegionBottom(bool isPositiveRegion, int regionNo)
        {
            if (isPositiveRegion == true)
            {
                return touchInput.positiveRegion[regionNo].Bottom;
            }
            else
            {
                return touchInput.negativeRegion[regionNo].Bottom;
            }
        }

        public static void UpdateReferenceImage()
        {
            touchInput.UpdateReferenceImage();
        }

        public static void UpdatePositiveImage()
        {
            touchInput.UpdatePositiveImage();
        }

        public static void UpdateNegativeImage()
        {
            touchInput.UpdateNegativeImage();
        }

        public static void UpdatePositiveRegion()
        {
            touchInput.UpdatePositiveRegion();
        }

        public static void UpdateNegativeRegion()
        {
            touchInput.UpdateNegativeRegion();
        }

        public static void SaveTouchOutputImage(int frameNo, int x, int y)
        {
#if true
            float xScale = (float)32767 / 1920;
            float yScale = (float)32767 / 1080;
            x = (int)(x / xScale);
            y = (int)(y / yScale);
            graphics.FillRectangle(Brushes.Red, x, y, (int)xScale, (int)yScale);
#endif
#if false
            Image touchOutput = new Bitmap(1920, 1080);
            Graphics graphic = Graphics.FromImage(touchOutput);
            graphic.FillRectangle(Brushes.White, 0, 0, 1920, 1080);

            //float xScale = (float)32767 / 1920;
            //float yScale = (float)32767 / 1080;
            x = (int)(x / xScale);
            y = (int)(y / yScale);
            graphic.FillRectangle(Brushes.Red, x, y, (int)xScale, (int)yScale);

            touchOutput.Save(Path.Combine(Constants.OutputFolderName, string.Format("TouchOutput{0:0000}.bmp", frameNo)));
#endif
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            touchInput = new TouchInputImage(Constants.RawImagePath, Constants.ImageWidth, Constants.ImageHeight);

            touchOutput = new Bitmap(1920, 1080);
            graphics = Graphics.FromImage(touchOutput);
            graphics.DrawRectangle(new Pen(Color.White, 5), new Rectangle(0, 0, 1920, 1080));

            touchInput.GetHeader();
            //touchPanel.GetNextFrame();
            //touchPanel.UpdateReferenceImage();

            LoadParameterFromFirmwareBinary(Constants.FirmwarePath);

            SetupCallbackFunctions(GetNextFrame,
                                   GetPositiveImage,
                                   GetNegativeImage,
                                   GetRegionCount,
                                   GetRegionLeft,
                                   GetRegionTop,
                                   GetRegionRight,
                                   GetRegionBottom,
                                   UpdateReferenceImage,
                                   UpdatePositiveImage,
                                   UpdateNegativeImage,
                                   UpdatePositiveRegion,
                                   UpdateNegativeRegion,
                                   SaveTouchOutputImage);

            Thread processTouchSignalThread = new Thread(new ThreadStart(StartProcessTouchSignal));
            processTouchSignalThread.Start();
            processTouchSignalThread.Join();

            touchOutput.Save(Path.Combine(Constants.OutputFolderName, string.Format("TouchOutput.bmp")));
        }
    }
}
