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
    public class BatEnemy : Enemy
    {
        private static Random _random = new Random();
        string imageFileName = "Bat.gif";
        public BatEnemy(Canvas canvas)
        {
            Speed = 1300;
            Name = "Bat";

            ContainerWidth = canvas.ActualWidth;
            ContainerHeight = canvas.ActualHeight;

            // Random direction
            double angle = ChooseGoodAngleForMoving();

            // Convert angle to vector
            Velocity = new Vector(Math.Cos(angle), Math.Sin(angle)) * Speed;


            bool itWorked = base.PrepareEnemyImage(imageFileName, canvas, 20);
            if (base.ImageControl != null)
            {
                base.ImageControl.Visibility = Visibility.Visible;
            }

        }

    }
    
}
