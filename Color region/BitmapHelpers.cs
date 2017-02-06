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
        public static Mat LoadMatFromFile(this string fileName, int reqWidth, int reqHeight)
        {
            // First we get the the dimensions of the file on disk
            BitmapFactory.Options options = new BitmapFactory.Options();
            BitmapFactory.DecodeFile(fileName, options);
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {

                int halfHeight = height / 2;
                int halfWidth = width / 2;

                // Calculate the largest inSampleSize value that is a power of 2 and keeps both
                // height and width larger than the requested height and width.
                while ((halfHeight / inSampleSize) >= reqHeight
                        && (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            options.InSampleSize = inSampleSize;
            Bitmap bitmap = BitmapFactory.DecodeFile(fileName, options);
            Mat mat = new Mat();
            Utils.BitmapToMat(bitmap, mat);
            return mat;
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
            double highThres = Imgproc.Threshold(mSrc, new Mat(), 0, 255, Imgproc.ThreshBinary | Imgproc.ThreshOtsu);
            double lowThres = 0.5 * highThres;
            Imgproc.Canny(mCanny, mCanny, lowThres, highThres, 3, false);
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

        public static Bitmap PaintWall2(Bitmap srcBitmap, int xCor, int yCor, Color color)
        {
            Mat mSrc = new Mat(), mLaplacian = new Mat(), mSharp = new Mat(), mBinary = new Mat(), mDist = new Mat(), mResult = new Mat();
            Utils.BitmapToMat(srcBitmap, mSrc);
            Imgproc.CvtColor(mSrc, mSrc, Imgproc.ColorRgba2rgb);
            Mat kernel = new Mat(3, 3, CvType.Cv32f);
            kernel.Put(0, 0, new float[] { 1, 1, 1, 1, -8, 1, 1, 1, 1 });
            mSharp = mSrc.Clone();
            Imgproc.Filter2D(mSharp, mLaplacian, CvType.Cv32f, kernel);
            mSrc.ConvertTo(mSharp, CvType.Cv32f);
            Core.Subtract(mSharp, mLaplacian, mResult);
            mResult.ConvertTo(mResult, CvType.Cv8uc3);
            mSrc = mResult.Clone();
            Imgproc.CvtColor(mSrc, mBinary, Imgproc.ColorRgba2gray);
            Imgproc.Threshold(mBinary, mBinary, 40, 255, Imgproc.ThreshBinary | Imgproc.ThreshOtsu);
            Imgproc.DistanceTransform(mBinary, mDist, Imgproc.CvDistL2, 3);
            Core.Normalize(mDist, mDist, 0, 1, Core.NormMinmax);
            Imgproc.Threshold(mDist, mDist, 0.4, 1, Imgproc.ThreshBinary);
            Mat kernel1 = Mat.Ones(3, 3, CvType.Cv8uc1);
            Imgproc.Dilate(mDist, mDist, kernel1);
            Mat mDist8u = new Mat();
            mDist.ConvertTo(mDist8u, CvType.Cv8u);
            JavaList<MatOfPoint> lstContours = new JavaList<MatOfPoint>();
            Imgproc.FindContours(mDist8u, lstContours, new Mat(), Imgproc.RetrExternal, Imgproc.ChainApproxSimple);
            Mat mMarker = Mat.Zeros(mDist.Size(), CvType.Cv32sc1);
            for (int i = 0; i < lstContours.Size(); i++)
            {
                Imgproc.DrawContours(mMarker, lstContours, i, Scalar.All(i + 1), -1);
            }
            Imgproc.Circle(mMarker, new OpenCV.Core.Point(xCor, yCor), 3, Scalar.All(255), -1);
            Imgproc.Watershed(mSrc, mMarker);
            Mat mMark = new Mat(mMarker.Size(), CvType.Cv8uc1);
            mMarker.ConvertTo(mMark, CvType.Cv8uc1);
            Core.Bitwise_not(mMark, mMark);
            Mat mWaterShed = new Mat(mMarker.Size(), CvType.Cv8uc3);
            Random rnd = new Random();
            int rows = mMarker.Rows();
            int cols = mMarker.Cols();
            int numberContours = lstContours.Size();
            if (mSrc.Dims() > 0)
            {
                Core.AddWeighted(mWaterShed, 0.5, mSrc, 0.5, 0.0, mWaterShed);
            }
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {

                }
            }
            Bitmap bitmap = Bitmap.CreateBitmap((int)mSrc.Size().Width, (int)mSrc.Size().Height, Bitmap.Config.Argb8888);
            Utils.MatToBitmap(mMark, bitmap);
            return bitmap;
        }

        public static Mat WaterShed2(Mat markerMask, Mat img0)
        {
            JavaList<MatOfPoint> lstContours = new JavaList<MatOfPoint>();
            Imgproc.FindContours(markerMask, lstContours, new Mat(), Imgproc.RetrCcomp, Imgproc.ChainApproxSimple);
            Mat markers = new Mat(markerMask.Size(), CvType.Cv32s);
            markers.SetTo(Scalar.All(0));
            for (int i = 0; i < lstContours.Size(); i++)
            {
                Imgproc.DrawContours(markers, lstContours, i, Scalar.All(i + 1), -1);
            }
            Imgproc.Watershed(img0, markers);
            lstContours.Clear();
            Imgproc.FindContours(markers, lstContours, new Mat(), Imgproc.RetrCcomp, Imgproc.ChainApproxSimple);
            Random rnd = new Random();
            markers.ConvertTo(markers, CvType.Cv32f);
            Imgproc.CvtColor(markers, markers, Imgproc.ColorGray2rgba);
            for (int i = 0; i < lstContours.Size(); i++)
            {
                Imgproc.DrawContours(markers, lstContours, i, new Scalar(rnd.Next(0, 255), rnd.Next(0, 255), rnd.Next(0, 255)), -1);
            }
            markers.ConvertTo(markers, CvType.Cv8u);
            return markers;
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