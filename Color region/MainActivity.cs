using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Java.IO;
using Environment = Android.OS.Environment;
using Uri = Android.Net.Uri;
using Android.Views;
using OpenCV.ImgProc;
using OpenCV.Core;
using OpenCV.Android;
using MonoDroid.ColorPickers;
using Android.Util;
using Android.Graphics.Drawables;

namespace Color_region
{
    public static class App
    {
        public static File _file;
        public static File _dir;
        public static Bitmap bitmap;
    }
    [Activity(Label = "Color_region", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, View.IOnTouchListener, ILoaderCallbackInterface
    {
        private ColorPickerPanelView _panelNoAlpha;
        public bool OnTouch(View v, MotionEvent e)
        {
            if (App.bitmap != null)
            {
                float eventX = e.GetX();
                float eventY = e.GetY();
                float[] eventXY = new float[] { eventX, eventY };
                Matrix invertMatrix = new Matrix();
                bool check = ((ImageView)v).ImageMatrix.Invert(invertMatrix);
                invertMatrix.MapPoints(eventXY);
                int x = (int)eventXY[0];
                int y = (int)eventXY[1];
                //Drawable imgDrawable = ((ImageView)v).Drawable;
                //Bitmap bitmap = ((BitmapDrawable)imgDrawable).Bitmap;
                if (!OpenCVLoader.InitDebug())
                {
                    OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion242, this, this);
                }
                else
                {
                    OnManagerConnected(LoaderCallbackInterface.Success);
                }
                Bitmap bitmap = App.bitmap.Copy(Bitmap.Config.Argb8888, true);
                if (x < 0)
                {
                    x = 0;
                }
                else if (x > bitmap.Width - 1)
                {
                    x = bitmap.Width - 1;
                }

                if (y < 0)
                {
                    y = 0;
                }
                else if (y > bitmap.Height - 1)
                {
                    y = bitmap.Height - 1;
                }
                bitmap = BitmapHelpers.PaintWall(App.bitmap,x,y,_panelNoAlpha.Color);
                _imageView.SetImageBitmap(bitmap);
            }
            return true;
        }
        private ImageView _imageView;

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                App.bitmap = (Bitmap)data.Extras.Get("data");
                _imageView.SetImageBitmap(App.bitmap);
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            if (IsThereAnAppToTakePictures())
            {
                Button button = FindViewById<Button>(Resource.Id.MyButton);
                Button colorPickerButton = FindViewById<Button>(Resource.Id.ColorPickerButton);
                _panelNoAlpha = FindViewById<ColorPickerPanelView>(Resource.Id.PanelColorNoAlpha);
                _panelNoAlpha.Color = Color.Black;
                _imageView = FindViewById<ImageView>(Resource.Id.imageView1);
                _imageView.BuildDrawingCache();
                button.Click += TakeAPicture;
                colorPickerButton.Click += BtNoAlphaOnClick;
                _imageView.SetOnTouchListener(this);
            }
        }

        private void CreateDirectoryForPictures()
        {
            App._dir = new File(
                Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryPictures), "CameraAppDemo");
            if (!App._dir.Exists())
            {
                App._dir.Mkdirs();
            }
        }

        private bool IsThereAnAppToTakePictures()
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities =
                PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }

        private void BtNoAlphaOnClick(object sender, EventArgs eventArgs)
        {
            using (var colorPickerDialog = new ColorPickerDialog(this, _panelNoAlpha.Color))
            {
                colorPickerDialog.AlphaSliderVisible = true;
                colorPickerDialog.ColorChanged += (o, args) => _panelNoAlpha.Color = args.Color;
                colorPickerDialog.Show();
            }
        }

        private void TakeAPicture(object sender, EventArgs eventArgs)
        {
            Intent intent = new Intent(MediaStore.ActionImageCapture);

            StartActivityForResult(intent, 0);
        }

        public void OnManagerConnected(int p0)
        {

        }

        public void OnPackageInstall(int p0, IInstallCallbackInterface p1)
        {
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (!OpenCVLoader.InitDebug())
            {
                OpenCVLoader.InitAsync(OpenCVLoader.OpencvVersion242, this, this);
            }
            else
            {
                OnManagerConnected(LoaderCallbackInterface.Success);
            }
        }
    }
}

