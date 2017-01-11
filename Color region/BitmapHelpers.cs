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
        public static Bitmap LoadAndResizeBitmap(this string fileName, int width, int height)
        {
            // First we get the the dimensions of the file on disk
            BitmapFactory.Options options = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(fileName, options);

            // Next we calculate the ratio that we need to resize the image by
            // in order to fit the requested dimensions.
            int outHeight = options.OutHeight;
            int outWidth = options.OutWidth;
            int inSampleSize = 1;

            if (outHeight > height || outWidth > width)
            {
                inSampleSize = outWidth > outHeight
                                   ? outHeight / height
                                   : outWidth / width;
            }

            // Now we will load the image and have BitmapFactory resize it for us.
            options.InSampleSize = inSampleSize;
            options.InJustDecodeBounds = false;
            Bitmap resizedBitmap = BitmapFactory.DecodeFile(fileName, options);

            return resizedBitmap;
        }

        public static Bitmap PaintWall(Bitmap srcBitmap, int x, int y, Color color)
        {
            Bitmap bitmap = srcBitmap.Copy(Bitmap.Config.Argb8888, true);
            Mat mSrc = new Mat();
            Mat mCanny = new Mat();
            Utils.BitmapToMat(bitmap, mSrc);
            Imgproc.CvtColor(mSrc, mSrc, Imgproc.ColorRgba2rgb);
            Imgproc.CvtColor(mSrc, mCanny, Imgproc.ColorRgba2gray);
            Imgproc.Canny(mCanny, mCanny, 15, 30, 3, false);
            Imgproc.Laplacian(mCanny, mCanny, CvType.Cv8uc3);
            Mat mMask = Mat.Zeros(mCanny.Rows() + 2, mCanny.Cols() + 2, CvType.Cv8uc1);
            Imgproc.FloodFill(mCanny, mMask, new OpenCV.Core.Point(x, y), new Scalar(0, 0, 0), new OpenCV.Core.Rect(0, 0, mCanny.Cols(), mCanny.Rows()), new Scalar(20, 20, 20), new Scalar(20, 20, 20), 4 | Imgproc.FloodfillMaskOnly | (255 << 8));
            Mat mMaskColor = mMask.Submat(new OpenCV.Core.Rect(1, 1, mSrc.Cols(), mSrc.Rows()));
            mMaskColor.ConvertTo(mMaskColor, CvType.Cv8uc3);
            JavaList<MatOfPoint> listContours = new JavaList<MatOfPoint>();
            Imgproc.FindContours(mMaskColor, listContours, new Mat(), Imgproc.RetrCcomp, Imgproc.ChainApproxSimple);
            for (int i = 0; i < listContours.Size(); i++)
            {
                Imgproc.DrawContours(mMaskColor, listContours, i, new Scalar(255, 255, 255), -1);
            }
            Core.Bitwise_not(mMaskColor, mMaskColor);
            //Mat kernel = Imgproc.GetStructuringElement(Imgproc.MorphEllipse, new Size(11, 11));
            //Imgproc.MorphologyEx(mMaskColor, mMaskColor, Imgproc.MorphOpen, kernel);
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
            Imgproc.Threshold(mBinary, mBinary, 10, 255, Imgproc.ThreshBinary + Imgproc.ThreshOtsu);
            Imgproc.MorphologyEx(mBinary, mBinary, Imgproc.MorphOpen, Imgproc.GetStructuringElement(2, new Size(23, 23)));
            Mat mFg = new Mat();
            Imgproc.Erode(mBinary, mFg, new Mat(), new OpenCV.Core.Point(-1, -1), 2);
            Mat mBg = new Mat();
            Imgproc.Dilate(mBinary, mBg, new Mat(), new OpenCV.Core.Point(-1, -1), 3);
            //Imgproc.Threshold(mBg, mBg, 1, 128, Imgproc.ThreshBinaryInv);
            Imgproc.Threshold(mBg, mBg, 1, 128, Imgproc.ThreshBinary + Imgproc.ThreshOtsu);
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