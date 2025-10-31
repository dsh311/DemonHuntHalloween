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

namespace DemonHuntHalloween.Enemies
{
    public class ReaperEnemy : Enemy
    {
        private static Random _random = new Random();
        string imageFileName = "Reaper.gif";

        public ReaperEnemy(Canvas canvas)
        {
            Speed = 1800;
            Name = "Reaper";

            ContainerWidth = canvas.ActualWidth;
            ContainerHeight = canvas.ActualHeight;

            // Random direction
            double angle = ChooseGoodAngleForMoving();

            // Convert angle to vector
            Velocity = new Vector(Math.Cos(angle), Math.Sin(angle)) * Speed;

            bool itWorked = base.PrepareEnemyImage(imageFileName, canvas);
        }

        public override void TakeDamage(int amount)
        {
            if (isDead) { return; }

            // Subract from health
            Health -= amount;

            // Check if no more health
            if (Health <= 0)
            {
                Die();
                return;
            }

            // Change angle after being hit
            double angle = ChooseGoodAngleForMoving();
            // Speed up is too hard, so skip
            // Change direction
            Velocity = new Vector(Math.Cos(angle), Math.Sin(angle)) * Speed;


            // Random appear after every hit is too hard
            /*
            double enemyWidth = ImageControl.Width;
            double enemyHeight = ImageControl.Height;

            // Ensure it stays fully inside the container
            double randomX = _random.NextDouble() * (ContainerWidth - enemyWidth);
            double randomY = _random.NextDouble() * (ContainerHeight - enemyHeight);

            Position = new Point(randomX, randomY);
            */

        }

    }
}
