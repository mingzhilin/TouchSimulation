﻿using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Runtime.InteropServices;
using YamlDotNet.RepresentationModel;
using HardwareSimulator;

namespace TouchSimulation
{
    class Program
    {
        public static string chipModel;
        public static string outputFolder;
        public static string logFolder;

        public static string firmwarePath;
        public static string rawImagePath;
        public static string logPath;
        public static int imageWidth;
        public static int imageHeight;
        public static int imageSize;

        static TouchInputImage touchInput;
        static Image touchOutput;
        static Graphics comboGraphic;

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
        public delegate void UpdatePositiveRegionFunctionPointer(int threshold);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UpdateNegativeRegionFunctionPointer(int threshold);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void SaveTouchOutputImageFunctionPointer(int frameNo,
                                                                 int touchOutputCount,
                                                                 [MarshalAs(UnmanagedType.LPArray, SizeConst = 10)] int[] x,
                                                                 [MarshalAs(UnmanagedType.LPArray, SizeConst = 10)] int[] y);

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

        public static void UpdatePositiveRegion(int threshold)
        {
            touchInput.UpdatePositiveRegion(threshold);
        }

        public static void UpdateNegativeRegion(int threshold)
        {
            touchInput.UpdateNegativeRegion(threshold);
        }

        public static void SaveTouchOutputImage(int frameNo, int touchOutputCount, int[] touchOutputX, int[] touchOutputY)
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

        static void Main(string[] args)
        {
            Console.WriteLine("+++++ TouchSimulation +++++");

            using (var reader = new StreamReader("TouchSimulationSettings.yaml"))
            {
                var yaml = new YamlStream();
                yaml.Load(reader);
                var root = (YamlMappingNode)yaml.Documents[0].RootNode;
                var commonNodes = (YamlMappingNode)root.Children[new YamlScalarNode("Common")];
                chipModel = (string)commonNodes.Children["ChipModel"];
                outputFolder = (string)commonNodes.Children["OutputFolder"];
                logFolder = (string)commonNodes.Children["LogFolder"];

                var chipNodes = (YamlMappingNode)root.Children[new YamlScalarNode(chipModel)];
                firmwarePath = (string)chipNodes.Children["FirmwarePath"];
                rawImagePath = (string)chipNodes.Children["RawImagePath"];
                logPath = (string)chipNodes.Children["LogPath"];
                imageWidth = Int32.Parse((string)chipNodes.Children["ImageWidth"]);
                imageHeight = Int32.Parse((string)chipNodes.Children["ImageHeight"]);
                imageSize = imageWidth * imageHeight;
            }

            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, true);
            }

            if (Directory.Exists(logFolder))
            {
                Directory.Delete(logFolder, true);
            }

            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(logFolder);

            touchInput = new TouchInputImage(rawImagePath, imageWidth, imageHeight);

            touchOutput = new Bitmap(1920, 1080);
            comboGraphic = Graphics.FromImage(touchOutput);
            comboGraphic.DrawRectangle(new Pen(Color.White, 5), new Rectangle(0, 0, 1920, 1080));

            touchInput.GetHeader();
            //touchPanel.GetNextFrame();
            //touchPanel.UpdateReferenceImage();

            LoadParameterFromFirmwareBinary(firmwarePath);

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

            touchOutput.Save(Path.Combine(outputFolder, string.Format("TouchOutput.bmp")));

            Console.WriteLine("----- TouchSimulation -----");
        }
    }
}
