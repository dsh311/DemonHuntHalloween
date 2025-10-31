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

using DemonHuntHalloween.Enemies;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Media;

namespace DemonHuntHalloween
{
    public class GameWorld
    {
        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver
        }

        public GameState _currentState = GameState.MainMenu;
        public List<string> levels = new() { "Pumpkin", "Booger", "GhostGirl", "Skeleton", "Reaper" };
        public int currentLevelIndex = 1;

        private DateTime _lastRenderTime;
        private Stopwatch _gameRunLengthTimer = new Stopwatch();
        private DispatcherTimer _leveltimer = null!;

        private Random _rand = new Random();
        private bool _isShowingMiss = false;

        private const int MaxHealth = 10;
        private int currentHealth = 10;

        // Game Stats
        private int numShotsFired = 0;
        private int numHits = 0;
        private int numMisses = 0;

        // Countdown
        private int _maxCountdown = 10;
        private int _countdown = 10;
        

        // Set the target time (in seconds)
        private const double MaxTimeSeconds = 40.0;

        // Sound effects --------------------
        private readonly Dictionary<string, SoundPlayer> _sounds = new()
        {
            { "shoot", new SoundPlayer("Assets/shoot.wav") },
            { "win", new SoundPlayer("Assets/win.wav") },
            { "lose", new SoundPlayer("Assets/lose.wav") }
        };


        private readonly Canvas _playArea;
        private readonly Ellipse _dot;

        private readonly List<Enemy> _enemies = new();
        private readonly DogEnemy _dogEnemy;

        private readonly TextBox _gameStats;
        private readonly TextBlock _gameStateTextBlock;
        private readonly TextBlock _statusText;
        private readonly TextBlock _enemyName;
        
        private readonly Image _mainmenuImage;
        private readonly Image _dogLaughsImage;

        private readonly StackPanel _healthBar;
        private readonly Rectangle _leftBar;
        private readonly Rectangle _rightBar;

        public GameWorld(Canvas playArea,
            Ellipse dot,
            TextBox gameStats,
            TextBlock gameStateTextBlock,
            TextBlock statusText,
            TextBlock enemyName,
            Image mainmenuImage,
            Image dogLaughsImage,
            StackPanel healthBar,
            Rectangle leftBar,
            Rectangle rightBar)
        {
            _playArea = playArea;
            _dot = dot;
            _gameStats = gameStats;
            _gameStateTextBlock = gameStateTextBlock;
            _statusText = statusText;
            _enemyName = enemyName;
            _mainmenuImage = mainmenuImage;
            _dogLaughsImage = dogLaughsImage;
            _healthBar = healthBar;
            _leftBar = leftBar;
            _rightBar = rightBar;

            SetState(GameState.MainMenu);

            _gameStateTextBlock.Visibility = Visibility.Visible;
            _gameStateTextBlock.Text = "";
            _gameStats.Text = "";
            _gameStats.Visibility = Visibility.Hidden;

            InitializeSounds();

            // Load the dog image
            _dogEnemy = new DogEnemy(_playArea, dogLaughsImage);
        }

        private void InitializeSounds()
        {
            // Play intro sound to prime the player or it can be delayed on first sound
            if (_sounds.TryGetValue("lose", out var player))
            {
                // Preload the sound into memory
                player.Load();  // synchronous, blocks until loaded
                player.Play();
            }
        }

        private void PlaySound(string key)
        {
            if (_sounds.TryGetValue(key, out var player))
            {
                player.Play();
            }
        }

        private void UpdateHealthBar()
        {
            // Get the stats
            List<(int, string)> enemyStats = GetEnemyStats();

            // Exit if there are no enemies
            if (enemyStats.Count == 0)
            {
                return;
            }

            (int firstEnemyHealth, string firstEnemyName) = enemyStats.First();

            _enemyName.Text = firstEnemyName;

            for (int i = 0; i < _healthBar.Children.Count; i++)
            {
                var rect = (Rectangle)_healthBar.Children[i];
                rect.Fill = i < firstEnemyHealth ? Brushes.LimeGreen : Brushes.Gray;
            }
        }

        private void UpdateTimerBar(double maxValue, double currentValue)
        {
            if (maxValue <= 0) return;

            double fraction = currentValue / maxValue;
            fraction = Math.Clamp(fraction, 0, 1); // ensures between 0 and 1

            double barHeight = fraction * _playArea.ActualHeight;

            // Update left bar
            _leftBar.Height = barHeight;
            Canvas.SetTop(_leftBar, _playArea.ActualHeight - barHeight);

            // Update right bar
            _rightBar.Height = barHeight;
            Canvas.SetLeft(_rightBar, _playArea.ActualWidth - _rightBar.Width);
            Canvas.SetTop(_rightBar, _playArea.ActualHeight - barHeight);
        }

        private void RedrawEnemyStats()
        {
            _enemyName.Text = "";
            _healthBar.Children.Clear();

            for (int i = 0; i < MaxHealth; i++)
            {
                var bar = new Rectangle
                {
                    Width = 20,
                    Height = 30,
                    Fill = Brushes.LimeGreen,
                    Margin = new Thickness(4, 0, 4, 0),
                    RadiusX = 3,
                    RadiusY = 3
                };
                _healthBar.Children.Add(bar);
            }

        }
        private async Task ShowReadyPlayerCountdown()
        {
            _gameStats.Text = "";
            _gameStats.Visibility = Visibility.Hidden;

            _gameStateTextBlock.Visibility = Visibility.Visible;
            // Show the "Get Ready" text underneath the number
            _gameStateTextBlock.Text = "3\nGet Ready!";
            await Task.Delay(1000);
            _gameStateTextBlock.Text = "2\nGet Ready!";
            await Task.Delay(1000);
            _gameStateTextBlock.Text = "1\nGet Ready!";
            await Task.Delay(1000);

            _gameStateTextBlock.Text = "";
        }

        public async Task StartGame()
        {
            try
            {
                _playArea.Background = new SolidColorBrush(Colors.Black);

                // Prepare the 10 seconds timer so its ready to start
                PrepLevelCountdownTimer();

                StopGameLoop();

                ClearEnemies();

                _gameStateTextBlock.Text = "";
                _gameStats.Text = "";
                _gameStats.Visibility = Visibility.Hidden;

                // Start level 1 and enemies have have max health
                currentLevelIndex = 1;
                _countdown = 10;
                currentHealth = MaxHealth;

                _mainmenuImage.Visibility = Visibility.Visible;

                // Show the user the ready player count down
                await ShowReadyPlayerCountdown();

                _mainmenuImage.Visibility = Visibility.Hidden;
                _playArea.Visibility = Visibility.Visible;

                // Reset stats
                numHits = 0;
                numShotsFired = 0;
                numMisses = 0;

                // Resets and starts the timer
                _gameRunLengthTimer.Reset();
                _gameRunLengthTimer.Start();

                // Clear again in case they pressed "Start Game" during countdown
                ClearEnemies();

                LoadLevel(currentLevelIndex);
            }
            catch (Exception ex)
            {
                _statusText.Text = $"Error: {ex.Message}";
            }
        }

        public void InitializeLevel(int levelNumber)
        {
            List<Enemy> enemiesToAdd = new List<Enemy>();

            switch (levelNumber)
            {
                case 1:
                    enemiesToAdd.Add(new PumpkinEnemy(_playArea));
                    break;
                case 2:
                    enemiesToAdd.Add(new BoogerEnemy(_playArea));
                    break;
                case 3:
                    enemiesToAdd.Add(new GhostGirlEnemy(_playArea));
                    break;
                case 4:
                    enemiesToAdd.Add(new SkeletonEnemy(_playArea));
                    break;
                case 5:
                    enemiesToAdd.Add(new ReaperEnemy(_playArea));
                    break;
                default:
                    throw new ArgumentException($"Invalid level number: {levelNumber}");
            }

            // Bats made it too hard using the wiimote
            //enemiesToAdd.Add(new BatEnemy(_playArea));

            if (enemiesToAdd.Count > 0)
            {
                _enemies.Clear();
                _enemies.AddRange(enemiesToAdd);
            }
        }

        private void displayStatsWithMsg(string theMsg)
        {
            int shotCount = numShotsFired;
            int missCount = numMisses;
            int hitCount = numHits;
            double elapsedTime = _gameRunLengthTimer.Elapsed.TotalSeconds;
            int beatGameBonus = 0;

            // Figure out how many levels they beat
            int levelsBeat = (currentLevelIndex > levels.Count) ? levels.Count : currentLevelIndex - 1;

            // If they bead the game, see how long it took
            if (currentLevelIndex >= levels.Count)
            {
                // Check if the player beat the max time
                if (elapsedTime < MaxTimeSeconds)
                {
                    // Calculate the time difference (how much faster they were)
                    double timeDifference = MaxTimeSeconds - elapsedTime;

                    // Use Math.Ceiling to round the score up to the nearest whole number (as requested previously)
                    beatGameBonus = 100 * (int)Math.Ceiling(timeDifference);
                }
                else
                {
                    Console.WriteLine($"No score awarded. Time was {elapsedTime:F2} seconds (must be under {MaxTimeSeconds}s).");
                }
            }


            //Accuracy
            double accuracy = 0;
            if (numShotsFired > 0)
            {
                int numHits = shotCount - missCount;
                accuracy = Math.Ceiling((double)numHits / numShotsFired * 100.0);
            }

            _gameStats.Visibility = Visibility.Visible;

            int finalScore = (int)(((beatGameBonus + accuracy) + (levelsBeat * 100)) - (missCount * 2));
            int roundedElaspedTime = (int)Math.Ceiling(elapsedTime);
            string statsMsg = $"Acurracy: {accuracy}%\nHit count: {hitCount}\nMiss count: {missCount}\nLevels beat: {levelsBeat}\nElasped seconds: {roundedElaspedTime}\n-----\nSCORE: {finalScore}";
            _gameStats.Text = statsMsg;

            _gameStateTextBlock.Text = "";
            _gameStateTextBlock.Inlines.Clear();
            _gameStateTextBlock.Inlines.Add(new Run(theMsg));
            _gameStateTextBlock.Inlines.Add(new LineBreak());
            _gameStateTextBlock.Inlines.Add(new Run($"Score: {finalScore}"));
        }

        private void StopAndResetLevelCountdown()
        {
            _leveltimer.Stop();
            _countdown = _maxCountdown;
        }

        private void PrepLevelCountdownTimer()
        {
            if (_leveltimer != null)
            {
                _leveltimer.Stop();
            }

            _countdown = _maxCountdown;

            if (_leveltimer == null)
            {
                _leveltimer = new DispatcherTimer();
                _leveltimer.Interval = TimeSpan.FromSeconds(1);
                _leveltimer.Tick += LevelCountdownTimer_Tick;
            }
        }

        private void LevelCountdownTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimerBar(_maxCountdown, _countdown);
            _countdown--;
            if (_countdown <= 0)
            {
                _leveltimer.Stop();
                _gameRunLengthTimer.Stop();
                OnCountdownFinished();
            }
        }

        private void StartGameLoop()
        {
            // Initialize last render time
            _lastRenderTime = DateTime.Now;

            // Subscribe to the CompositionTarget.Rendering event
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopGameLoop()
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            // Compute deltaTime in seconds
            DateTime now = DateTime.Now;
            double deltaTime = (now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;

            // ---------------- UPDATE ----------------
            Update(deltaTime);
            // ---------------------------------------

            // ---------------- RENDER ----------------
            UpdateTimerBar(_maxCountdown, _countdown);
            UpdateHealthBar();
            Render(_playArea);
            // ---------------------------------------
        }

        private void OnCountdownFinished()
        {
            PlaySound("lose");
            displayStatsWithMsg("TRY AGAIN!!!");

            StopGameLoop();

            ClearEnemies();

            SetState(GameWorld.GameState.GameOver);

            _enemyName.Visibility = Visibility.Hidden;
            _healthBar.Visibility = Visibility.Hidden;
            
            _mainmenuImage.Visibility = Visibility.Visible;
            _playArea.Visibility = Visibility.Hidden;

            Mouse.OverrideCursor = Cursors.Arrow;
        }

        // Call this to start the countdown
        private void StartLevelCountdownTimer()
        {
            if (_leveltimer != null && _leveltimer.IsEnabled)
            {
                _leveltimer.Stop();
            }

            _leveltimer.Start();
        }

        public void LoadLevel(int loadThisLevel)
        {
            _enemyName.Visibility = Visibility.Visible;
            _healthBar.Visibility = Visibility.Visible;
            _playArea.Visibility = Visibility.Visible;

            // If level they want is pased the number of supported levels
            if (loadThisLevel > levels.Count)
            {
                _gameRunLengthTimer.Stop();

                PlaySound("win");
                displayStatsWithMsg("YOU WIN!!!");

                StopGameLoop();

                ClearEnemies();
                SetState(GameWorld.GameState.MainMenu);

                _enemyName.Visibility = Visibility.Hidden;
                _healthBar.Visibility = Visibility.Hidden;

                _mainmenuImage.Visibility = Visibility.Visible;
                _playArea.Visibility = Visibility.Hidden;

                StopAndResetLevelCountdown();

                Mouse.OverrideCursor = Cursors.Arrow;
                return;
            }

            RedrawEnemyStats();

            StopAndResetLevelCountdown();

            SetState(GameState.Playing);

            // Prepare game for level 1
            InitializeLevel(loadThisLevel);

            // Start game loop timer
            StartGameLoop();

            //Stop level countdown timer
            StartLevelCountdownTimer();
        }

        public void SetState(GameState newState)
        {
            _currentState = newState;

            switch (_currentState)
            {
                case GameState.MainMenu:
                    break;

                case GameState.Playing:
                    break;

                case GameState.Paused:
                    break;

                case GameState.GameOver:
                    break;
            }
        }

        public void ClearEnemies()
        {
            _enemies.Clear();

            var imagesToRemove = _playArea.Children
                .OfType<Image>()
                .ToList(); // The crucial .ToList() creates the copy

            foreach (var img in imagesToRemove)
            {
                _playArea.Children.Remove(img);
            }

        }

        public List<(int,string)> GetEnemyStats()
        {
            List<(int, string)> EnemyHealth = new List<(int, string)>();
            foreach (var enemy in _enemies)
            {
                EnemyHealth.Add((enemy.Health, enemy.Name));
            }

            return EnemyHealth;
        }


        // UPDATE ------------------------------------
        public void Update(double deltaTime)
        {
            foreach (var enemy in _enemies)
            {
                enemy.Update(
                    deltaTime,
                    _playArea.ActualWidth,
                    _playArea.ActualHeight);
            }
        }
        // -------------------------------------------


        // RENDER ------------------------------------
        public void Render(Canvas canvas)
        {
            foreach (var enemy in _enemies)
            {
                enemy.Render(canvas);
            }
        }
        // -------------------------------------------


        public bool ProcessShot()
        {
            if (_currentState != GameWorld.GameState.Playing)
            {
                return false;
            }

            numShotsFired++;

            // Get dot position
            double dotLeftX = Canvas.GetLeft(_dot);
            double dotTopY = Canvas.GetTop(_dot);
            double dotWidth = _dot.ActualWidth;
            double dotHeight = _dot.ActualHeight;
            double dotX = dotLeftX;
            double dotY = dotTopY;
            double dotW = _dot.Width;
            double dotH = _dot.Height;

            int damageAmount = 1;

            bool someEnemyHit = false;
            List<Guid> hitDeadEnemyGuids = new List<Guid>();

            PlaySound("shoot");

            // Check all enemies for hits
            foreach (var aEnemy in _enemies)
            {
                bool hit = aEnemy.ProcessShot(dotLeftX, dotTopY, dotWidth, dotHeight, damageAmount);
                someEnemyHit |= hit;
                if (hit && aEnemy.isDead)
                {
                    hitDeadEnemyGuids.Add(aEnemy.Id);
                }
            }

            if (hitDeadEnemyGuids.Count > 0)
            {
                // Remove matching images from the canvas in a single pass
                foreach (var img in _playArea.Children.OfType<Image>()
                                 .Where(img => img.Tag is Guid id && hitDeadEnemyGuids.Contains(id))
                                 .ToList()) // ToList avoids modifying collection while iterating
                {
                    _playArea.Children.Remove(img);
                }

                // Remove the Enemy objects
                _enemies.RemoveAll(e => hitDeadEnemyGuids.Contains(e.Id));
            }

            if (someEnemyHit)
            {
                numHits++;
            }
            else
            {
                numMisses++;
            }


            if (_enemies.Count == 0)
            {
                currentLevelIndex++;
                LoadLevel(currentLevelIndex);
            }
            else
            {
                if (!someEnemyHit)
                {
                    ShowMiss();
                }
            }

            return someEnemyHit;
        }

        private async void ShowMiss()
        {
            // If already showing, do nothing
            if (_isShowingMiss)
            {
                return;
            }

            _isShowingMiss = true;

            try
            {
                _dogEnemy.RestartGifAnimation();
                _dogLaughsImage.Visibility = Visibility.Visible;

                // Let the GIF play for some seconds
                await Task.Delay(1100);

                // Hide the image afterward
                _dogLaughsImage.Visibility = Visibility.Hidden;
            }
            finally
            {
                _isShowingMiss = false;
            }
        }


    }


}
