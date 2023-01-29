using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace okienko
{
    public partial class Form1 : Form
    { 
        string imgPath = "";
        string ImageName = "";
        int referenceNumber = 0;
        int threadNumber = 1;
        int depth=0;
        int depth2=0;
        int numberOfThreadstoFinish=0;
        byte[] sourceBuffer;
        byte[] destinationBuffer;
        private static Semaphore sem = new Semaphore(initialCount: 1, maximumCount: 1,"semafor");
        Stopwatch sw = new Stopwatch();
        Bitmap sourceBitmap, finishedBitmap;
        
        public Form1()
        {
            InitializeComponent();
        }
        private void recalculateHorizontal(int sourceWidth, int row, int sourceHeight)
        {
            int[] pixelTab = new int[24];
            int currentRow = row/2;
            int currentPosition = 0;
            int loadedPixels = 0;
            int numberOfPixelsToWrite = 0;

            for (int i = 0; i < sourceWidth; i++)
            {
                var offset = ((row * sourceWidth) + i) * depth;

                pixelTab[loadedPixels] = (int)sourceBuffer[offset];
                pixelTab[loadedPixels + 1] = (int)sourceBuffer[offset + 1];
                pixelTab[loadedPixels + 2] = (int)sourceBuffer[offset + 2];
                offset = ((row * sourceWidth) + i) * depth;

                pixelTab[loadedPixels + 3] = (int)sourceBuffer[offset];
                pixelTab[loadedPixels + 4] = (int)sourceBuffer[offset + 1];
                pixelTab[loadedPixels + 5] = (int)sourceBuffer[offset + 2];

                numberOfPixelsToWrite++;
                loadedPixels += 6;
                if (numberOfPixelsToWrite == 4)
                {
                    loadedPixels = 0;
                    if (AsmDll.Checked)
                    {//ASM
                        //[DllImport(@"../../../../../x64/Debug/JAAsm.dll")]
                        [DllImport(@"JAAsm.dll")]
                        static extern int ScaleDown(int[] pixelTab);
                        ScaleDown(pixelTab);
                    }
                    else if (cppDll.Checked)
                    {//C++
                        //[DllImport(@"../../../../../x64/Debug/Dll1.dll")]
                        [DllImport(@"Dll1.dll")]
                        static extern int ScaleDown(int[] pixelTab);
                        ScaleDown(pixelTab);
                    }

                    for (int q = 0; q < numberOfPixelsToWrite; q++)
                    {
                        offset = (((currentRow ) * sourceWidth) + currentPosition) * depth2;

                        destinationBuffer[offset] = (byte)pixelTab[0 + (q * 3)];
                        destinationBuffer[offset + 1] = (byte)pixelTab[1 + (q * 3)];
                        destinationBuffer[offset + 2] = (byte)pixelTab[2 + (q * 3)];
                        if (depth2 == 4) destinationBuffer[offset + 3] = 255;
                        currentPosition++;
                    }
                    numberOfPixelsToWrite = 0;
                }
            }
            for (int q = 0; q < numberOfPixelsToWrite; q++)
            {
                var offset = (((currentRow) * sourceWidth) + currentPosition) * depth2;

                destinationBuffer[offset] = (byte)pixelTab[0 + (q * 3)];
                destinationBuffer[offset + 1] = (byte)pixelTab[1 + (q * 3)];
                destinationBuffer[offset + 2] =(byte)pixelTab[2 + (q * 3)];
                if (depth2 == 4) destinationBuffer[offset + 3] = 255; 
                currentPosition++;
            }
        }
        private void DoAHorizontalCut()
        {
            
            int width, height, sourceWidth, sourceHeight;
            if (sourceBitmap.Width % 2 != 0)
            {
                width = (sourceBitmap.Width - 1);
                sourceWidth = sourceBitmap.Width - 1;
            }
            else
            {
                width = sourceBitmap.Width;
                sourceWidth = sourceBitmap.Width;
            }
            if (sourceBitmap.Height % 2 != 0)
            {
                height = (sourceBitmap.Height - 1);
                sourceHeight = sourceBitmap.Height - 1;
            }
            else
            {
                height = sourceBitmap.Height;
                sourceHeight = sourceBitmap.Height;
            }
            //########## Threads buffors
            var rect = new Rectangle(0, 0, sourceWidth, sourceHeight);
            var data = sourceBitmap.LockBits(rect, ImageLockMode.ReadWrite, sourceBitmap.PixelFormat);
            depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; //bytes per pixel
            sourceBuffer = new byte[data.Width * data.Height * depth];
            Marshal.Copy(data.Scan0, sourceBuffer, 0, sourceBuffer.Length);
            //##########
            finishedBitmap = new Bitmap(width, height/2);
            var rect2 = new Rectangle(0, 0, width, height/2);
            var data2 = finishedBitmap.LockBits(rect2, ImageLockMode.ReadWrite, finishedBitmap.PixelFormat);
            depth2 = Bitmap.GetPixelFormatSize(data2.PixelFormat) / 8; //bytes per pixel
            destinationBuffer = new byte[data2.Width * data2.Height * depth2];
            //##########
            referenceNumber = 0;
            numberOfThreadstoFinish = threadNumber;
            for (int i = 0; i < threadNumber; i++)
            {
                Thread newThread = new Thread(() => TransformBitmap(sourceHeight, sourceWidth, false));
                newThread.Start();
            }
            while (numberOfThreadstoFinish > 0)
                Thread.Sleep(100);

            Marshal.Copy(destinationBuffer, 0, data2.Scan0, destinationBuffer.Length);

            sourceBitmap.UnlockBits(data);
            finishedBitmap.UnlockBits(data2);
        }
        private void recalculateVertical(int sourceHeight, int column,int sourceWidth)
        {
            int[] pixelTab = new int[25];
            int currentColumn = column / 2;
            int currentPosition = 0;
            int loadedPixels = 0;
            int numberOfPixelsToWrite = 0;
            
            for (int j = 0; j < sourceHeight; j++)
            {
                var offset=((j * sourceWidth) + column) *depth;

                pixelTab[loadedPixels] = (int)sourceBuffer[offset];
                pixelTab[loadedPixels + 1] = (int)sourceBuffer[offset + 1];
                pixelTab[loadedPixels + 2] = (int)sourceBuffer[offset + 2];
                offset = ((j * sourceWidth) + column+1) * depth;

                pixelTab[loadedPixels + 3] = (int)sourceBuffer[offset];
                pixelTab[loadedPixels + 4] = (int)sourceBuffer[offset + 1];
                pixelTab[loadedPixels + 5] = (int)sourceBuffer[offset + 2];

                numberOfPixelsToWrite++;
                loadedPixels += 6;
                if (numberOfPixelsToWrite == 4)
                {
                    loadedPixels = 0;
                    if (AsmDll.Checked)
                    {//ASM
                        [DllImport(@"../../../../../x64/Debug/JAAsm.dll")]
                        static extern int ScaleDown(int[] pixelTab);

                        ScaleDown(pixelTab);
                    }
                    else if (cppDll.Checked)
                    {//C++
                        [DllImport(@"../../../../../x64/Debug/Dll1.dll")]
                        static extern int ScaleDown(int[] pixelTab);
                        ScaleDown(pixelTab);
                    }

                    for (int q = 0; q < numberOfPixelsToWrite; q++)
                    {
                        offset = ((currentPosition * (sourceWidth/2)) + currentColumn) * depth2;

                        destinationBuffer[offset] = (byte)pixelTab[0 + (q * 3)];
                        destinationBuffer[offset + 1] = (byte)pixelTab[1 + (q * 3)];
                        destinationBuffer[offset + 2] =(byte)pixelTab[2 + (q * 3)];
                        if(depth2==4)destinationBuffer[offset + 3] = 255; ;
                        currentPosition++;
   

                    }
                    numberOfPixelsToWrite = 0;
                }
            }
            for (int q = 0; q < numberOfPixelsToWrite; q++)
            {
                var offset = ((currentPosition * (sourceWidth / 2)) + currentColumn) * depth2;
                destinationBuffer[offset] = (byte)pixelTab[0 + (q * 3)];
                destinationBuffer[offset + 1] = (byte)pixelTab[1 + (q * 3)];
                destinationBuffer[offset + 2] = (byte)pixelTab[2 + (q * 3)];
                if (depth2 == 4) destinationBuffer[offset + 3] = 255; ;
               currentPosition++;
            }
        }
        private void DoAVerticalCut()
        {
            int width, height, sourceWidth, sourceHeight;
            if (sourceBitmap.Width % 2 != 0)
            {
                width = (sourceBitmap.Width - 1);
                sourceWidth = sourceBitmap.Width - 1;
            }
            else
            {
                width = sourceBitmap.Width;
                sourceWidth = sourceBitmap.Width;
            }
            if (sourceBitmap.Height % 2 != 0)
            {
                height = (sourceBitmap.Height - 1);
                sourceHeight = sourceBitmap.Height - 1;
            }
            else
            {
                height = sourceBitmap.Height;   
                sourceHeight = sourceBitmap.Height;
            }
            
            //########## Threads buffors
            var rect = new Rectangle(0, 0,sourceWidth,sourceHeight);
            var data = sourceBitmap.LockBits(rect, ImageLockMode.ReadWrite, sourceBitmap.PixelFormat);
            depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; //bytes per pixel
            sourceBuffer = new byte[data.Width * data.Height * depth];
            Marshal.Copy(data.Scan0, sourceBuffer, 0, sourceBuffer.Length);
            //##########
            finishedBitmap =new Bitmap(width / 2, height);
            var rect2 = new Rectangle(0, 0, width / 2, height);
            var data2 = finishedBitmap.LockBits(rect2, ImageLockMode.ReadWrite, finishedBitmap.PixelFormat);
            depth2 = Bitmap.GetPixelFormatSize(data2.PixelFormat) / 8; //bytes per pixel
            destinationBuffer = new byte[data2.Width * data2.Height * depth2];
            //##########

            referenceNumber = 0;
            numberOfThreadstoFinish = threadNumber;


            for (int i = 0; i < threadNumber; i++)
            {
                    Thread newThread = new Thread(()
                        => TransformBitmap(
                                sourceWidth, sourceHeight, true));
                    newThread.Start();
            }
            while (numberOfThreadstoFinish > 0)
                Thread.Sleep(100);
            

            Marshal.Copy(destinationBuffer, 0, data2.Scan0, destinationBuffer.Length);

            sourceBitmap.UnlockBits(data);
            finishedBitmap.UnlockBits(data2);

        }
        private void TransformBitmap(int boundry, int packageLength, bool vertical)
        {
            int tempnum;
            int currentNumber;
            while (referenceNumber < boundry)
            {
                sem.WaitOne();
                currentNumber=referenceNumber;
                Interlocked.Add(ref referenceNumber, 2);
                tempnum=referenceNumber;
                sem.Release();
                if (currentNumber >= boundry) break;
                if (tempnum-currentNumber > 2)
                    Interlocked.Exchange(ref referenceNumber, tempnum);

                if (vertical) recalculateVertical(packageLength, currentNumber, boundry);
                else recalculateHorizontal(packageLength, currentNumber, boundry);

            }
            Interlocked.Decrement(ref numberOfThreadstoFinish);
        }

        private void openFileDialogBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            if (openFile.ShowDialog() == DialogResult.OK)
            {
                string temp = openFile.FileName.ToString();
                sourcePathBoX.Text = temp;
                string extension=temp.Substring(temp.Length - 4);
                destinationPathBox.Text = temp.Substring(0, temp.Length - 4) + "_Scaled" + extension;
            }
        }

        private void numberOfThreads_ValueChanged(object sender, EventArgs e)
        {
            trackBar1.Value =(int)numberOfThreads.Value;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            numberOfThreads.Value=trackBar1.Value;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            //############# Droping Data To .txt ##############
            //List< List < Double>> mainList = new List<List<Double>>();
            //List<String>stringList=new List<String>();
            //ThreadsCount being  
            //for (int t = 1; t < 65; t*=2)
            //{
            //    mainList.Add(new List<Double>());
            //
            //    100 tests per thread count
            //    for (int i = 0; i < 100; i++)
            //    {
            //       threadNumber = t;
            //       ############# Droping Data To .txt ##############

            threadNumber = (int)numberOfThreads.Value;
            
            if (sourcePathBoX.Text == "")
                        notificationLabel.Text = "Source is empty";
            else if (destinationPathBox.Text == "")
                        notificationLabel.Text = "destination is empty";
            else
               {
                FileInfo fi = new FileInfo(sourcePathBoX.Text);
                if (!File.Exists(sourcePathBoX.Text))
                    notificationLabel.Text = "Wrong input";
                else if (!(fi.Extension == ".png") && !(fi.Extension == ".jpg"))
                    notificationLabel.Text = "Wrong file extension";
                else
                { 
                        imgPath =sourcePathBoX.Text;
                        ImageName=destinationPathBox.Text;

                        try
                           {
                            sourceBitmap = new Bitmap(imgPath);
                           

                            sw.Restart();
                            sw.Start();
                            if (verticalCheckbox.Checked && horizontalCheckbox.Checked)
                            {

                                DoAVerticalCut();
                                sourceBitmap.Dispose();
                                sourceBitmap = finishedBitmap;
                                DoAHorizontalCut();
                                finishedBitmap.Save(ImageName);
                            }
                            else if (verticalCheckbox.Checked == true)
                            {
                                DoAVerticalCut();
                                finishedBitmap.Save(ImageName);
                            }
                            else if (horizontalCheckbox.Checked == true)
                            {
                                DoAHorizontalCut();
                                finishedBitmap.Save(ImageName);
                            }
                            sw.Stop();
                            TimeInSeconds.Text = sw.Elapsed.ToString();
                            notificationLabel.Text = "Elapsed time";
                            //############# Droping Data To .txt ##############
                            //mainList[mainList.Count-1].Add(sw.Elapsed.TotalSeconds);
                            //############# Droping Data To .txt ##############

                        }
                        catch (Exception ex)
                        {
                            notificationLabel.Text = "Unknown error ocurred";
                            
                        }
                        finally
                        {
                           if(verticalCheckbox.Checked ||horizontalCheckbox.Checked) finishedBitmap.Dispose();
                            sourceBitmap.Dispose();
                        }

                     }
                    }

                }
            }
    //############# Droping Data To .txt ##############
    //.txt Organization:
    //[ThreadCount][AVG][Min][Max]
    //Possible thread count (1,2,4,8,16,32,64)
    //try {
    //    int x = 0;
    //    foreach (var dataSet in mainList)
    //    {
    //        var AVG = dataSet.Sum() / dataSet.Count;
    //        var Min = dataSet.Min();
    //        var Max = dataSet.Max();
    //        string temp = Math.Pow(2, x).ToString() + "\t" + AVG.ToString() + "\t" + Min.ToString() + "\t" + Max.ToString();
    //        stringList.Add(temp);
    //        x++;
    //    }
    //    string[] str = stringList.ToArray();
    //    File.WriteAllLines("results.txt", str);
    //}
    //catch (Exception ex)
    //{
    //    notificationLabel.Text = "ERRRROR: "+ex.Message;
    //
    //}


    //}

    //}
    //############# Droping Data To .txt ##############
}