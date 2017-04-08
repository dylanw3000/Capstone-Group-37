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

using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;

namespace WindowsFormsApplication3
{
    public partial class Form1 : Form
    {
        private KinectSensor kSensor;

        public Form1()
        {
            InitializeComponent();
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

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

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
                kSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                kSensor.ColorFrameReady += KSensor_ColorFrameReady;

                kSensor.DepthStream.Enable();
                kSensor.DepthStream.Range = DepthRange.Default;
                kSensor.DepthFrameReady += KSensor_DepthFrameReady;

                //kSensor.AllFramesReady += KSensor_AllFramesReady;
                //kSensor.SkeletonStream.Enable();
                //kSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

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
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    //pictureBox2.Image = CreateBitmapFromSensor(frame);
                }
            }

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if(depthFrame != null)
                {
                    //byte[] pixels = GenerateColoredBytes(depthFrame);

                    //number of bytes per row width * 4 (B,G,R, Empty)
                    int stride = depthFrame.Width * 4;

                    //create image
                    //pictureBox1.Image = BitmapFromSource( BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride) );
                    //pictureBox1.Image = CreateColorBitmapFromDepth(depthFrame);


                }
            }

            /*
            using (var frame = e.OpenSkeletonFrame())
            {
                if(frame == null)
                {
                    return;
                }

                var skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                var trackedSkeleton = skeletons.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
                if(trackedSkeleton == null)
                {
                    return;
                }

                var position = trackedSkeleton.Joints[JointType.HandRight].Position;

                


            }
            //*/
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

                /*
                //.9M or 2.95
                if(depth <= 900)
                {
                    //we are very close
                    pixels[colorIndex + BlueIndex] = 255;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;
                }
                //.9M - 2M or 2.95' - 6.56'
                else if(depth > 900 && depth < 2000)
                {
                    //we are a bit further away
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 255;
                    pixels[colorIndex + RedIndex] = 0;
                }
                //2M+ or 6.56'+
                else if(depth > 200)
                {
                    //we are the farthest
                    pixels[colorIndex + BlueIndex] = 255;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;
                }
                //*/

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
                    pictureBox2.Image = CreateBitmapFromSensor(frame);
                }
            }
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.lblStatus.Text = kSensor.Status.ToString();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        private Bitmap CreateBitmapFromSensor(ColorImageFrame frame)
        {
            //WriteableBitmap map;
            var pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);

           // return pixelData.ToBitmap(frame.Width, frame.Height);

            //*
            var stride = frame.Width * frame.BytesPerPixel;

            var bmpFrame = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            var bmpData = bmpFrame.LockBits(new Rectangle(0, 0, frame.Width, frame.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmpFrame.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bmpData.Scan0, frame.PixelDataLength);

            bmpFrame.UnlockBits(bmpData);

            return bmpFrame;
            //*/
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

    }
}
