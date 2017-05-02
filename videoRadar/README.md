<h1>IMPORTANT:</h1>

<h2>Hardware requirements:</h2>

1. Xbox 360 Kinect
2. USB adapter for the Kinect

<h2>Software requirements:</h2>

1. Kinect for Windows SDK v1.8 - https://www.microsoft.com/en-us/download/details.aspx?id=40278

2. EmguCV library 3.1.0.2504 - https://sourceforge.net/projects/emgucv/

3. Used for development: Visual studio 2015 community version (.dll is specified for this version)

<h2>Steps to compile:</h2>

1. Install Kinect SDK v1.8

2. Install EmguCV library v 3.1.0.2504 and set up environment

        (a) Set system envirnment variable path to C:\Emgu\emgucv-windesktop 3.1.0.2504\bin\x86

        (b) Add ImageBox (instructions - http://www.emgu.com/wiki/index.php/Add_ImageBox_Control)

3. Open the project solution and run


##### *for testing purposes, google an image of a red ball on phone and use that.
##### *lighting plays a factor when detecting objects and may have an impact on on detecting accuracy.
