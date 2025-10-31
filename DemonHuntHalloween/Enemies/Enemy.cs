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

using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using System;

namespace DemonHuntHalloween.Enemies
{
    public abstract class Enemy
    {
        public Guid Id { get; } = Guid.NewGuid();

        public Image? ImageControl { get; protected set; }
        public Point Position { get; set; }

        public double ContainerWidth {  get; set; }
        public double ContainerHeight { get; set; }
        
        public Vector Velocity { get; set; } // pixels per second
        public double Speed { get; set; } = 100; // pixels per second
        public int Health { get; set; } = 10;
        public string Name { get; set; } = String.Empty;

        public bool isDead = false;

        private List<BitmapFrame> _frames;
        private int _currentFrame = 0;
        private DispatcherTimer _gifTimer;


        public List<BitmapFrame> LoadGifFrames(string path)
        {
            var frames = new List<BitmapFrame>();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                frames.AddRange(decoder.Frames);
            }

            return frames;
        }

        // For debugging gif animations.
        public void SaveGifFrames(List<BitmapFrame> frames, string outputFolder, string baseName = "frame")
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            for (int i = 0; i < frames.Count; i++)
            {
                string filePath = Path.Combine(outputFolder, $"{baseName}_{i + 1}.png");

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(frames[i]);
                    encoder.Save(stream);
                }
            }
        }

        public void StartGifAnimation(
                            Image imageControl,
                            List<BitmapFrame> frames,
                            int frameDelayMs = 100)
        {
            ImageControl = imageControl;

            _frames = frames;
            _currentFrame = 0;

            _gifTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(frameDelayMs) };
            _gifTimer.Tick += (s, e) =>
            {
                ImageControl.Source = _frames[_currentFrame];
                _currentFrame = (_currentFrame + 1) % _frames.Count;
            };
            _gifTimer.Start();
        }

        public void StopGifAnimation()
        {
            _gifTimer?.Stop();
        }

        public void RestartGifAnimation()
        {
            if (_frames == null || _frames.Count == 0 || ImageControl == null)
            {
                return;
            }

            // Stop any ongoing animation
            _gifTimer?.Stop();

            // Reset to the first frame since image control would show the last frame
            _currentFrame = 0;

            // Show the first frame immediately (so there’s no visual delay)
            ImageControl.Source = _frames[_currentFrame];

            // Restart the timer
            _gifTimer?.Start();
        }

        // Used to get metrics on the size of the image
        public static BitmapImage? LoadGifForDetailsOnly(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public double ChooseGoodAngleForMoving()
        {
            Random random = new Random();

            // Allowed angles (degrees)
            double[] allowedAngles = { 45, 40, 35, 30 };

            // Pick a random angle from the list
            double angleDeg = allowedAngles[random.Next(allowedAngles.Length)];

            // Convert to radians
            double angleRad = angleDeg * (Math.PI / 180.0);

            // Randomly flip horizontal and vertical direction
            if (random.Next(2) == 0) angleRad = -angleRad;  // flip vertically
            if (random.Next(2) == 0) angleRad = Math.PI - angleRad; // flip horizontally

            return angleRad;
        }

        public virtual bool PrepareEnemyImage(
            string enemyName,
            Canvas canvas,
            int animateSpeed = 200)
        {
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            string localFileImagePath = System.IO.Path.Combine(basePath, enemyName);

            // Ensure ImageControl exists
            if (ImageControl == null)
            {
                ImageControl = new Image
                {
                    Width = 64,
                    Height = 64,
                    Tag = Id
                };
            }

            // Load file into BitmapImage
            BitmapImage? enemyImage = LoadGifForDetailsOnly(localFileImagePath);
            
            // Ensure the WPF control is proper width and height
            if (enemyImage != null)
            {
                ImageControl.Width = enemyImage.PixelWidth;
                ImageControl.Height = enemyImage.PixelHeight;
                ImageControl.Stretch = Stretch.None;
                ImageControl.Visibility = Visibility.Visible;
            }

            // Save image to the control
            //ImageBehavior.SetAnimatedSource(ImageControl, enemyImage);

            var frames = LoadGifFrames(localFileImagePath);
            StartGifAnimation(ImageControl, frames, animateSpeed);



            ImageControl.Visibility = Visibility.Hidden;

            // Add ImageControl to canvas, as first element so Dot is on top
            canvas.Children.Insert(0, ImageControl);

            // Position enemy to start in the center
            double centerX = canvas.ActualWidth / 2;
            double centerY = canvas.ActualHeight / 2;
            Position = new Point(centerX, centerY);

            return true;
        }

        // Draw the image at its current location
        public virtual void Render(Canvas canvas)
        {
            // Try to find an existing Image with this ID
            var image = canvas.Children
                .OfType<Image>()
                .FirstOrDefault(i => i.Tag is Guid guid && guid == Id);

            if (image == null)
            {
                return;
            }
            else
            {
                ImageControl = image;
            }

            // Ensure it is visible since it is hidden when created
            image.Visibility = Visibility.Visible;

            // Update position
            double halfActualWidth = image.ActualWidth / 2;
            double halfActualHeight = image.ActualHeight / 2;

            double finalX = Position.X - halfActualWidth;
            double finalY = Position.Y - halfActualHeight;

            Canvas.SetLeft(image, finalX);
            Canvas.SetTop(image, finalY);
        }

        public virtual void Update(
            double deltaTime,
            double areaWidth,
            double areaHeight)
        {
            // Compute new position based on velocity
            double newX = Position.X + Velocity.X * deltaTime;
            double newY = Position.Y + Velocity.Y * deltaTime;

            // Check horizontal walls
            if (newX <= 0)
            {
                newX = 0;
                Velocity = new Vector(-Velocity.X, Velocity.Y); // reflect X
            }
            else if (newX >= areaWidth)
            {
                newX = areaWidth;
                Velocity = new Vector(-Velocity.X, Velocity.Y); // reflect X
            }

            // Check vertical walls
            if (newY <= 0)
            {
                newY = 0;
                Velocity = new Vector(Velocity.X, -Velocity.Y); // reflect Y
            }
            else if (newY >= areaHeight)
            {
                newY = areaHeight;
                Velocity = new Vector(Velocity.X, -Velocity.Y); // reflect Y
            }

            // Update position
            Position = new Point(newX, newY);
        }

        public bool ProcessShot(
            double dotLeftX,
            double dotTopY,
            double dotWidth,
            double dotHeight,
            int damageAmount)
        {
            if (ImageControl == null) { return false; }

            double imgLeftX = Canvas.GetLeft(ImageControl);
            double imgTopY = Canvas.GetTop(ImageControl);
            double imgWidth = ImageControl.Width;
            double imgHeight = ImageControl.Height;

            bool hit =
                dotLeftX < imgLeftX + imgWidth &&
                dotLeftX + dotWidth > imgLeftX &&
                dotTopY < imgTopY + imgHeight &&
                dotTopY + dotHeight > imgTopY;

            if (hit)
            {
                TakeDamage(damageAmount); // Or whatever damage the dot does
            }

            return hit;
        }

        public virtual void TakeDamage(int amount)
        {
            if (isDead) { return; }
            
            // Subract from health
            Health -= amount;

            // Check if no more health
            if (Health <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            isDead = true;
            if (ImageControl != null)
            {
                ImageControl.Visibility = Visibility.Hidden;
            }

        }
    }


}
