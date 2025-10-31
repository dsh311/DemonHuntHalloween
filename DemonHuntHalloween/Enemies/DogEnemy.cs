//DEMONHUNTHALLOWEEN
//MIT License
//
//Copyright (c) 2025 David S. Shelley
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace DemonHuntHalloween.Enemies
{
    internal class DogEnemy : Enemy
    {
        private static Random _random = new Random();
        string imageFileName = "DogLaugh.gif";
        public DogEnemy(Canvas canvas, Image DogLaughsImage)
        {
            Speed = 3;
            Name = "Dog";

            ContainerWidth = canvas.ActualWidth;
            ContainerHeight = canvas.ActualHeight;

            bool itWorked = PrepareDogImage(
                imageFileName,
                canvas,
                DogLaughsImage);
        }

        public bool PrepareDogImage(
            string enemyName,
            Canvas canvas,
            Image DogLaughsImage)
        {
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            string localFileImagePath = System.IO.Path.Combine(basePath, enemyName);

            // Load file into BitmapImage
            BitmapImage? enemyImage = LoadGifForDetailsOnly(localFileImagePath);

            // Ensure the WPF control is proper width and height
            if (enemyImage != null)
            {
                DogLaughsImage.Width = enemyImage.PixelWidth;
                DogLaughsImage.Height = enemyImage.PixelHeight;
                DogLaughsImage.Stretch = Stretch.None;
                DogLaughsImage.Visibility = Visibility.Hidden;
            }


            var frames = LoadGifFrames(localFileImagePath);
            StartGifAnimation(DogLaughsImage, frames, 100);

            DogLaughsImage.Visibility = Visibility.Hidden;

            return true;
        }

    }
}
