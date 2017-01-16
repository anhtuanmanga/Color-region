using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using OpenCV.Android;
using OpenCV.Core;
using OpenCV.ImgProc;

namespace Color_region
{
    public static class BitmapHelpers
    {
        public static Bitmap BitmapResizer(Bitmap bitmap, int newWidth, int newHeight)
        {
            Bitmap scaledBitmap = Bitmap.CreateBitmap(newWidth, newHeight, Bitmap.Config.Argb8888);

            float ratioX = newWidth / (float)bitmap.Width;
            float ratioY = newHeight / (float)bitmap.Height;
            float middleX = newWidth / 2.0f;
            float middleY = newHeight / 2.0f;

            Matrix scaleMatrix = new Matrix();
            scaleMatrix.SetScale(ratioX, ratioY, middleX, middleY);

            Canvas canvas = new Canvas(scaledBitmap);
            canvas.Matrix = scaleMatrix;
            canvas.DrawBitmap(bitmap, middleX - bitmap.Width / 2, middleY - bitmap.Height / 2, new Paint(PaintFlags.FilterBitmap));
            return scaledBitmap;
        }
        public static Bitmap LoadBitmapFromFile(this string fileName)
        {
            // First we get the the dimensions of the file on disk
            BitmapFactory.Options options = new BitmapFactory.Options();
            BitmapFactory.DecodeFile(fileName, options);
            Bitmap bitmap = BitmapFactory.DecodeFile(fileName, options);

            return bitmap;
        }

        public static Bitmap PaintWall(Bitmap srcBitmap, int x, int y, Color color)
        {
            Bitmap bitmap = srcBitmap.Copy(Bitmap.Config.Argb8888, true);
            Mat mSrc = new Mat();
            Mat mCanny = new Mat();
            
            Utils.BitmapToMat(bitmap, mSrc);
            Imgproc.CvtColor(mSrc, mSrc, Imgproc.ColorRgba2rgb);
            Imgproc.CvtColor(mSrc, mCanny, Imgproc.ColorRgba2gray);
            Imgproc.Blur(mCanny, mCanny, new Size(3, 3));
            Imgproc.Canny(mCanny, mCanny, 80, 90, 5, true);
            Mat mMask = Mat.Zeros(mCanny.Rows() + 2, mCanny.Cols() + 2, CvType.Cv8uc1);
            Imgproc.FloodFill(mCanny, mMask, new OpenCV.Core.Point(x, y), new Scalar(0, 0, 0), new OpenCV.Core.Rect(0, 0, mCanny.Cols(), mCanny.Rows()), new Scalar(20, 20, 20), new Scalar(20, 20, 20), 4 | Imgproc.FloodfillMaskOnly | (255 << 8));
            Mat mMaskColor = mMask.Submat(new OpenCV.Core.Rect(1, 1, mSrc.Cols(), mSrc.Rows()));
            mMaskColor.ConvertTo(mMaskColor, CvType.Cv8uc3);
            Core.Bitwise_not(mMaskColor, mMaskColor);
            Mat mResult = new Mat();
            mSrc.CopyTo(mResult, mMaskColor);
            Imgproc.FloodFill(mResult, mMask, new OpenCV.Core.Point(x, y), new Scalar(color.R, color.G, color.B));
            Utils.MatToBitmap(mResult, bitmap);
            return bitmap;
        }

        public static Bitmap WaterShed(Bitmap srcBitmap)
        {
            Bitmap bitmap = srcBitmap.Copy(Bitmap.Config.Argb8888, true);
            Mat mRBG = new Mat();
            Mat mBinary = new Mat();
            Utils.BitmapToMat(bitmap, mRBG);
            Imgproc.CvtColor(mRBG, mRBG, Imgproc.ColorRgba2rgb);
            Imgproc.CvtColor(mRBG, mBinary, Imgproc.ColorRgb2gray);
            Imgproc.Threshold(mBinary, mBinary, 100, 255, Imgproc.ThreshBinary);
            Mat mFg = new Mat();
            Imgproc.Erode(mBinary, mFg, new Mat(), new OpenCV.Core.Point(-1, -1), 2);
            Mat mBg = new Mat();
            Imgproc.Dilate(mBinary, mBg, new Mat(), new OpenCV.Core.Point(-1, -1), 3);
            //Imgproc.Threshold(mBg, mBg, 1, 128, Imgproc.ThreshBinaryInv);
            Imgproc.Threshold(mBg, mBg, 1, 128, Imgproc.ThreshBinaryInv);
            Mat mMarker = new Mat(mBinary.Size(), CvType.Cv8u, new Scalar(0));
            Core.Add(mFg, mBg, mMarker);
            mMarker.ConvertTo(mMarker, CvType.Cv32s);
            Imgproc.Watershed(mRBG, mMarker);
            mMarker.ConvertTo(mMarker, CvType.Cv8u);
            Utils.MatToBitmap(mMarker, bitmap);
            return bitmap;
        }
    }
}