using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

// Kinect includes
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

// EmguCV includes
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Cuda;

namespace videoRadar
{
    public partial class Form1 : Form
    {
        // Kinect member variables
        private KinectSensor kSensor;

        // EmguCV member variables
        Mat originalImage;
        Mat processedImage;
        Mat hsvImage;
        Mat upper_red_hue_range;
        Mat lower_red_hue_range;

        // create text file
        TextWriter datafile;

        short[] depth_data;
        int depth_data_width;


        int x_pos;
        int y_pos;
        int z_pos;

        int x_prev = 0, y_prev = 0, z_prev = 0;

        static int tick_rate = 3;
        int tick = tick_rate;

        int color_save_num = 0;
        int depth_save_num = 0;


        public Form1()
        {
            InitializeComponent();
        }

        void ProcessFrame()
        {
            // Create camera instance
            // originalImage = captureCamera.QueryFrame();

            // Create instances
            hsvImage = new Mat(originalImage.Size, DepthType.Cv8U, 3);
            upper_red_hue_range = new Mat(originalImage.Size, DepthType.Cv8U, 1);
            lower_red_hue_range = new Mat(originalImage.Size, DepthType.Cv8U, 1);
            processedImage = new Mat(originalImage.Size, DepthType.Cv8U, 1);

            // Convert to HSV image
            CvInvoke.CvtColor(originalImage, hsvImage, ColorConversion.Bgr2Hsv);

            // Create range of hue value for red and then add both to the processedImage
            CvInvoke.InRange(hsvImage, new ScalarArray(new MCvScalar(0, 155, 155)), new ScalarArray(new MCvScalar(20, 255, 255)), lower_red_hue_range);
            CvInvoke.InRange(hsvImage, new ScalarArray(new MCvScalar(160, 155, 155)), new ScalarArray(new MCvScalar(179, 255, 255)), upper_red_hue_range);

            CvInvoke.Add(lower_red_hue_range, upper_red_hue_range, processedImage);
            CvInvoke.GaussianBlur(processedImage, processedImage, new Size(3, 3), 0);

            Mat structuringElement = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));

            //CvInvoke.Threshold(processedImage, processedImage, 10, 255, ThresholdType.Binary);
            //CvInvoke.BitwiseAnd(mask, s, mask, null);

            CvInvoke.Dilate(processedImage, processedImage, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0, 0, 0));
            CvInvoke.Erode(processedImage, processedImage, structuringElement, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0, 0, 0));

            CircleF[] circles = CvInvoke.HoughCircles(processedImage, HoughType.Gradient, 2.0, processedImage.Rows / 4, 100, 50, 0, 0);

            // Drawing circles around objects
            foreach (CircleF circle in circles)
            {
                CvInvoke.Circle(originalImage, new Point((int)circle.Center.X, (int)circle.Center.Y), (int)circle.Radius, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Circle(originalImage, new Point((int)circle.Center.X, (int)circle.Center.Y), 3, new MCvScalar(0, 255, 0), -1);
            }

            tick--;
            if (circles != null && circles.Length != 0)
            {
                //this.speed_val_label.Text = ((ushort)depth_data[(depth_data_width * (int)circles[0].Center.Y) + ((int)circles[0].Center.X)]>>3).ToString();

                x_pos = (int)circles[0].Center.X;
                y_pos = (int)circles[0].Center.Y;
                z_pos = (ushort)depth_data[(depth_data_width * (int)circles[0].Center.Y) + ((int)circles[0].Center.X)] >> 3;

                double x_disp, y_disp, z_disp;

                if (tick <= 0)
                {

                    if (textBox1.Text != "")
                    {                         // if we are not on the first line in the text box
                        textBox1.AppendText(Environment.NewLine);         // then insert a new line char
                    }

                    textBox1.AppendText("ball position x = " + x_pos.ToString() + ", y = " + y_pos.ToString() + ", z = " + z_pos.ToString());
                    textBox1.ScrollToCaret();             // scroll down in text box so most recent line added (at the bottom) will be shown


                    tick = tick_rate;

                    x_disp = (x_pos - x_prev) * z_pos;  //147*2250 = 310*1097
                    y_disp = (y_pos - y_prev) * z_pos;
                    z_disp = Math.Pow(z_pos - z_prev, 2);  //1 ft = 304.8mm
                    double speed = ((Math.Sqrt(Math.Pow(x_disp, 2) + Math.Pow(y_disp, 2) + Math.Pow(z_disp, 2))) / Math.Pow(304.8, 2) * (30/tick_rate));
					//sqrt(x^2+y^2+z^2) * pixels-to-feet

                    this.speed_val_label.Text = speed.ToString();

                    x_prev = x_pos;
                    y_prev = y_pos;
                    z_prev = z_pos;

                    // write lines of text to the file
                    datafile.WriteLine(x_pos.ToString() + "," + y_pos.ToString() + "," + z_pos.ToString() + "," + speed.ToString());
                }
            }

            imageBox1.Image = originalImage;
            //imageBox2.Image = upper_red_hue_range;
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void FinalVideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //throw new NotImplementedException();
            Bitmap image = (Bitmap)eventArgs.Frame.Clone(); //bitmap boxing
            pictureBox1.Image = image;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //stop webcam when form closes
            if (kSensor != null && kSensor.IsRunning)
            {
                kSensor.Stop();

            }

            // close the stream     
            datafile.Close();

        }

        private void kinect_stream_Click(object sender, EventArgs e)
        {
            if (btn_kinect_stream.Text == "Stream")
            {
                if (KinectSensor.KinectSensors.Count > 0)
                {
                    this.btn_kinect_stream.Text = "Stop";
                    kSensor = KinectSensor.KinectSensors[0];
                    KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
                }

                kSensor.Start();

                datafile = new StreamWriter("../../../data.txt");

                kSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                //kSensor.ColorFrameReady += KSensor_ColorFrameReady;

                kSensor.DepthStream.Enable();
                kSensor.DepthStream.Range = DepthRange.Default;
                //kSensor.DepthFrameReady += KSensor_DepthFrameReady;
                kSensor.AllFramesReady += KSensor_AllFramesReady;

                if (!Directory.Exists("./recording"))
                {
                    Directory.CreateDirectory("./recording");
                }

            }
            else
            {
                if(kSensor != null && kSensor.IsRunning)
                {
                    kSensor.Stop();
                    this.btn_kinect_stream.Text = "Stream";
                    this.pictureBox1.Image = null;
                }
            }

            
        }

        private void KSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if(depthFrame != null)
                {
                    //pictureBox1.Image = CreateColorBitmapFromDepth(depthFrame);

                    byte[] pixels = GenerateColoredBytes(depthFrame);

                    //number of bytes per row width * 4 (B,G,R, Empty)
                    int stride = depthFrame.Width * 4;

                    //create image
                    pictureBox1.Image = BitmapFromSource( BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride) );
                }
            }
        }

        private void KSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
           

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if(depthFrame != null)
                {
                    //pictureBox1.Image = CreateColorBitmapFromDepth(depthFrame);

                    byte[] pixels = GenerateColoredBytes(depthFrame);

                    depth_data_width = depthFrame.Width;
                    depth_data = new short[depthFrame.PixelDataLength];

                    depthFrame.CopyPixelDataTo(depth_data);

                    var arrayHandle = System.Runtime.InteropServices.GCHandle.Alloc(depth_data, System.Runtime.InteropServices.GCHandleType.Pinned);

                    Bitmap bmp = new Bitmap(depthFrame.Width, depthFrame.Height, // 2x2 pixels
                                        2*depthFrame.Width,                     // RGB32 => 8 bytes stride
                                        System.Drawing.Imaging.PixelFormat.Format16bppArgb1555,
                                        arrayHandle.AddrOfPinnedObject()
                                        );
                    //bmp.Save("./recording/depth_save_image_" + depth_save_num.ToString() + ".bmp");
                    //depth_save_num++;


                    //number of bytes per row width * 4 (B,G,R, Empty)
                    int stride = depthFrame.Width * 4;

                    //create image
                    pictureBox1.Image = BitmapFromSource(BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride));
                }
            }

            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    //create bitmap from sensor
                    Bitmap bitmap = CreateBitmapFromSensor(frame);
                    Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(bitmap); //Image Class from Emgu.CV
                    originalImage = imageCV.Mat; //This is your Image converted to Mat
                    ProcessFrame();

                    //bitmap.Save("./recording/color_save_image" + color_save_num.ToString() + ".bmp");
                    //color_save_num++;
                }
            }

        }

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {

            //get min and max reliable depth
            int minDepth = depthFrame.MinDepth;
            int maxDepth = depthFrame.MaxDepth;

            //get the raw data from kinect with the depth for evey pixel
            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            //using the depthFrame to creat the image to display on-screen
            //depthFrame contains color information for all pixel in image
            //Height x Width x 4 (Red, Green, Blue, empty byte)
            Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            //Bgr32  - Blue, Green, Red. empty byte
            //Bgra32 - Blue, Green, Red, transparency
            //you must set transparency for Bgra as .NET defaults a byte to 0 = fully transparent

            //hardcoded location to Blue, Green, Red (BGR) index postitions
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;

            //loop through all distances
            //pick a RGB color based on distance
            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixels.Length;
                depthIndex++, colorIndex += 4)
            {
                //get the depth value
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                //equal coloring for the monochromatic histogram
                byte intensity = CalculateIntesityFromDepth(depth, minDepth, maxDepth);
                pixels[colorIndex + BlueIndex] = intensity;
                pixels[colorIndex + GreenIndex] = intensity;
                pixels[colorIndex + RedIndex] = intensity;
            }

            return pixels;
        }

        private byte CalculateIntesityFromDepth(int distance, int MinDepthDistance, int MaxDepthDistance)
        {
            //formula for calculating monochrome intensity for histogram
            return (byte)(255 - (255 * Math.Max(distance - MinDepthDistance, 0)
                / (MaxDepthDistance)));
        }

        private void KSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    //create bitmap from sensor
                    Bitmap bitmap = CreateBitmapFromSensor(frame);
                    Image<Bgr, Byte> imageCV = new Image<Bgr, byte>(bitmap); //Image Class from Emgu.CV
                    originalImage = imageCV.Mat; //This is your Image converted to Mat
                    ProcessFrame();
                }
            }
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.lblStatus.Text = kSensor.Status.ToString();
        }

        private Bitmap CreateBitmapFromSensor(ColorImageFrame frame)
        {
            //WriteableBitmap map;
            var pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);

            // return pixelData.ToBitmap(frame.Width, frame.Height);
            var stride = frame.Width * frame.BytesPerPixel;

            var bmpFrame = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bmpData = bmpFrame.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmpFrame.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bmpData.Scan0, frame.PixelDataLength);

            bmpFrame.UnlockBits(bmpData);

            return bmpFrame;
        }

        private Bitmap CreateColorBitmapFromDepth(DepthImageFrame frame)
        {
            //WriteableBitmap colorBitmap;
            DepthImagePixel[] depthPixels;
            byte[] colorPixels;

            depthPixels = new DepthImagePixel[kSensor.DepthStream.FramePixelDataLength];
            colorPixels = new byte[kSensor.DepthStream.FramePixelDataLength * sizeof(int)];

            frame.CopyDepthImagePixelDataTo(depthPixels);

            //get min and max reliable depth
            int minDepth = frame.MinDepth;
            int maxDepth = frame.MaxDepth;

            //convert depth to RGB
            int colorPixelIndex = 0;
            for(int i = 0; i < depthPixels.Length; ++i)
            {
                //get depth for this pixel
                short depth = depthPixels[i].Depth;

                // To convert to a byte, we're discarding the most-significant
                // rather than least-significant bits.
                // We're preserving detail, although the intensity will "wrap."
                // Values outside the reliable depth range are mapped to 0 (black).

                // Note: Using conditionals in this loop could degrade performance.
                // Consider using a lookup table instead when writing production code.
                // See the KinectDepthViewer class used by the KinectExplorer sample
                // for a lookup table example.

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                //write out blue byte
                colorPixels[colorPixelIndex++] = intensity;

                //write out green byte
                colorPixels[colorPixelIndex++] = intensity;

                //write out red byte
                colorPixels[colorPixelIndex++] = intensity;

                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                // If we were outputting BGRA, we would write alpha here.
                ++colorPixelIndex;
            }

            var stride = frame.Width * frame.BytesPerPixel;

            var bmpFrame = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bmpData = bmpFrame.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmpFrame.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(colorPixels, 0, bmpData.Scan0, colorPixels.Length);

            bmpFrame.UnlockBits(bmpData);

            return bmpFrame;

        }

        private System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();

                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }

            return bitmap;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}