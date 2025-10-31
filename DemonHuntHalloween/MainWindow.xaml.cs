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
using System.Windows.Input;
using System.Windows.Threading;
using WiimoteLib;

namespace DemonHuntHalloween
{
    public partial class MainWindow : Window
    {
        private GameWorld _gameworld;
        private Wiimote _wiimote = new();
        private bool _wiimoteIsConnected = false;
        
        // Prevent holding the trigger on wiimote
        private bool _wasBPresseed = false;

        public MainWindow()
        {
            InitializeComponent();

            // Create the game world and allow access to UI controls
            // Pass everything instead of binding since most things are visual effects
            _gameworld = new GameWorld(
                PlayArea,
                Dot,
                GameStats,
                GameStateTextBlock,
                StatusText,
                EnemyName,
                MainmenuImage,
                DogLaughsImage,
                HealthBar,
                LeftBar,
                RightBar);
        }

        private void ConnectToWiimote_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_wiimote.WiimoteState == null || !_wiimote.WiimoteState.Rumble)
                {
                    _wiimote.Connect();
                    _wiimoteIsConnected = true;
                    _wiimote.SetReportType(InputReport.IRAccel, true);
                    _wiimote.WiimoteChanged += WiimoteOnChanged;
                    StatusText.Text = "Connected.";
                    ConnectToWiimote.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void WiimoteOnChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Check if dispatcher is still running. This is needed to fix error when closing UI window.
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            {
                return;
            }

            try
            {
                var state = e.WiimoteState;
                Dispatcher.Invoke(() =>
                {
                    if (state.IRState.IRSensors[0].Found)
                    {
                        BadAim.Visibility = Visibility.Hidden;
                        Warning.Visibility = Visibility.Hidden;
                        EnemyName.Visibility = Visibility.Visible;

                        int thisLevelIndex = (_gameworld.currentLevelIndex - 1) % _gameworld.levels.Count;
                        //EnemyName.Text = _gameworld.levels[thisLevelIndex];

                        // Hide aim warning when
                        

                        PlayArea.Background = System.Windows.Media.Brushes.Black;
                        // IR tracking mode
                        double nx = 1 - state.IRState.IRSensors[0].Position.X;
                        double ny = state.IRState.IRSensors[0].Position.Y;
                        MoveDot(nx, ny);
                    }
                    else
                    {
                        BadAim.Visibility = Visibility.Visible;
                        EnemyName.Visibility = Visibility.Hidden;
                        Warning.Visibility = Visibility.Visible;
                        Warning.Text = "AIM AIM AIM AIM AIM";
                    }


                    // Handle Button Presses (New Logic)
                    // Check the A button (the main trigger)
                    if (state.ButtonState.A)
                    {
                        // The 'A' button is currently being held down
                        // Example: Change background color again or perform an action
                        // For demonstration: change the PlayArea to Blue when 'A' is pressed
                        PlayArea.Background = System.Windows.Media.Brushes.Blue;
                    }

                    var bPressed = state.ButtonState.B;

                    if (bPressed && !_wasBPresseed)
                    {
                        OnTriggerPressed();
                    }

                    // Update the previous state, so if they released B, we will now realize they released B
                    _wasBPresseed = bPressed;

                    // Check the Home button
                    if (state.ButtonState.Home)
                    {
                        _gameworld.StartGame();
                        return;
                    }

                    // Check D-Pad Left
                    if (state.ButtonState.Down)
                    {
                        ToggleFullscreen(false);
                    }

                    // Check D-Pad Up
                    if (state.ButtonState.Up)
                    {
                        ToggleFullscreen(true);
                    }

                });
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }

        }

        private void ToggleFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanMinimize;
                Topmost = false;
                WindowState = WindowState.Normal;
            }
        }

        private void shutdownGame()
        {
            _wiimote.SetRumble(false);
            _wiimote.Disconnect();
            Application.Current.Shutdown(0);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            shutdownGame();
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            _gameworld.StartGame();
        }

        private void OnTriggerPressed()
        {
            RumbleForOneSecond(150);
            bool somethingHit = _gameworld.ProcessShot();
        }

        private void PlayArea_MouseMove(object sender, MouseEventArgs e)
        {
            // If not connected to wi remote, use mouse location instead
            if (_gameworld._currentState == GameWorld.GameState.Playing && !_wiimoteIsConnected)
            {
                Mouse.OverrideCursor = Cursors.None;

                // Get the mouse position relative to the Canvas
                System.Windows.Point pos = e.GetPosition(PlayArea);

                // Center the ellipse on the cursor
                double left = pos.X - (Dot.Width / 2);
                double top = pos.Y - (Dot.Height / 2);

                // Move the Dot
                Canvas.SetLeft(Dot, left);
                Canvas.SetTop(Dot, top);

                // Stop the event from reaching other elements
                e.Handled = true;
            }
            else
            {
                // Let the event continue to other elements or handlers
                e.Handled = false;
            }
        }

        private void PlayArea_Click(object sender, MouseButtonEventArgs e)
        {
            // If not connected to wi remote, use mouse location instead
            if (_gameworld._currentState == GameWorld.GameState.Playing && !_wiimoteIsConnected)
            {
                PlayArea.Background = System.Windows.Media.Brushes.Black;
                OnTriggerPressed();
            }

        }

        private async void RumbleForOneSecond(int milisecondsToRumble)
        {
            _wiimote.SetRumble(true);
            await Task.Delay(milisecondsToRumble);
            _wiimote.SetRumble(false);
        }

        private void MoveDot(double normX, double normY)
        {
            double canvasWidth = PlayArea.ActualWidth;
            double canvasHeight = PlayArea.ActualHeight;

            double newX = Math.Clamp(normX * canvasWidth - Dot.Width / 2, 0, canvasWidth - Dot.Width);
            double newY = Math.Clamp(normY * canvasHeight - Dot.Height / 2, 0, canvasHeight - Dot.Height);

            Canvas.SetLeft(Dot, newX);
            Canvas.SetTop(Dot, newY);
        }


    }
}