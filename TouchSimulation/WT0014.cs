using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;
using System.IO;
using HardwareWT0014;

namespace TouchSimulation
{
    class WT0014
    {
        private readonly TouchInputImage touchInput;

        private readonly Image touchOutput;
        private readonly Graphics comboGraphic;

        private readonly string outputFolder;

        public WT0014(string input, int width, int height, string output)
        {
            touchInput = new TouchInputImage(input, width, height);

            touchOutput = new Bitmap(1920, 1080);
            comboGraphic = Graphics.FromImage(touchOutput);
            comboGraphic.DrawRectangle(new Pen(Color.White, 5), new Rectangle(0, 0, 1920, 1080));

            outputFolder = output;
        }

        #region LoadParameters
        [DllImport("FirmwareWT0014.dll")]
        public static extern void LoadParameterFromFirmwareBinary(string path);
        public void LoadParameters(string path)
        {
            LoadParameterFromFirmwareBinary(path);
        }
        #endregion

        public void SaveTouchOutputImage(int frameNo, int touchOutputCount, int[] touchOutputX, int[] touchOutputY)
        {
            Pen touchOutputPen = new Pen(Color.Red, 5);
            float xScale = (float)32767 / 1920;
            float yScale = (float)32767 / 1080;

            Image frameImage = new Bitmap(1920, 1080);
            Graphics frameGraphic = Graphics.FromImage(frameImage);
            frameGraphic.DrawRectangle(new Pen(Color.White, 5), new Rectangle(0, 0, 1920, 1080));

            for (int i = 0; i < touchOutputCount; i++)
            {
                int x = (int)(touchOutputX[i] / xScale);
                int y = (int)(touchOutputY[i] / yScale);
                frameGraphic.DrawEllipse(touchOutputPen, x, y, 20, 20);
                comboGraphic.DrawEllipse(touchOutputPen, x, y, 20, 20);
            }

            frameImage.Save(Path.Combine(outputFolder, string.Format("TouchOutput{0:0000}.bmp", frameNo)));
        }

        #region StartProcessTouchSignal
        [DllImport("FirmwareWT0014.dll")]
        public static extern void ProcessTouchSignal();
        public void StartProcessTouchSignal()
        {
            Thread processTouchSignalThread = new Thread(new ThreadStart(ProcessTouchSignal));

            processTouchSignalThread.Start();
            processTouchSignalThread.Join();

            //touchOutput.Save(Path.Combine(outputFolder, string.Format("TouchOutput.bmp")));
        }
        #endregion

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
        public delegate void SaveTouchOutputImageFunctionPointer(int frameNo,
                                                                 int touchOutputCount,
                                                                 [MarshalAs(UnmanagedType.LPArray, SizeConst = 10)] int[] x,
                                                                 [MarshalAs(UnmanagedType.LPArray, SizeConst = 10)] int[] y);

        [DllImport("FirmwareWT0014.dll")]
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

        public bool GetNextFrame()
        {
            return touchInput.GetNextFrame();
        }

        public short GetPositiveImage(int startRow, int startCol, int currRow, int currCol)
        {
            return touchInput.positiveImage[currRow - startRow, currCol - startCol];
        }

        public short GetNegativeImage(int startRow, int startCol, int currRow, int currCol)
        {
            return touchInput.negativeImage[currRow - startRow, currCol - startCol];
        }

        public int GetRegionCount(bool isPositiveRegion)
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

        public int GetRegionLeft(bool isPositiveRegion, int regionNo)
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

        public int GetRegionTop(bool isPositiveRegion, int regionNo)
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

        public int GetRegionRight(bool isPositiveRegion, int regionNo)
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

        public int GetRegionBottom(bool isPositiveRegion, int regionNo)
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

        public void UpdateReferenceImage()
        {
            touchInput.UpdateReferenceImage();
        }

        public void UpdatePositiveImage()
        {
            touchInput.UpdatePositiveImage();
        }

        public void UpdateNegativeImage()
        {
            touchInput.UpdateNegativeImage();
        }

        public void UpdatePositiveRegion()
        {
            touchInput.UpdatePositiveRegion();
        }

        public void UpdateNegativeRegion()
        {
            touchInput.UpdateNegativeRegion();
        }

        public void SetupCallbackFunctions()
        {
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
        }
    }
}
