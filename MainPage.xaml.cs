using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Graphics;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        private int[,] board = new int[4, 4];
        private Border[,] tiles = new Border[4, 4];
        private int score = 0;
        private int bestScore = 0;
        private Random random = new Random();
        private bool isAnimating = false;

        public MainPage()
        {
            InitializeComponent();
            LoadBestScore();
            InitializeBoard();
            StartNewGame();
        }

        private void LoadBestScore()
        {
            bestScore = Preferences.Get("BestScore", 0);
            BestScoreLabel.Text = bestScore.ToString();
        }

        private void SaveBestScore()
        {
            if (score > bestScore)
            {
                bestScore = score;
                Preferences.Set("BestScore", bestScore);
                BestScoreLabel.Text = bestScore.ToString();
                AnimateScoreUpdate(BestScoreLabel);
            }
        }

        private void InitializeBoard()
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var border = new Border
                    {
                        BackgroundColor = Color.FromArgb("#cdc1b4"),
                        StrokeThickness = 0,
                        Padding = 0
                    };

                    var label = new Label
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        FontSize = 32,
                        FontAttributes = FontAttributes.Bold
                    };

                    border.Content = label;
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    GameBoard.Children.Add(border);
                    tiles[row, col] = border;
                }
            }
        }

        private void StartNewGame()
        {
            score = 0;
            ScoreLabel.Text = "0";
            
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    board[row, col] = 0;
                }
            }

            AddRandomTile();
            AddRandomTile();
            UpdateUI();
        }

        private async void AddRandomTile()
        {
            var emptyCells = new List<(int row, int col)>();
            
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (board[row, col] == 0)
                    {
                        emptyCells.Add((row, col));
                    }
                }
            }

            if (emptyCells.Count > 0)
            {
                var cell = emptyCells[random.Next(emptyCells.Count)];
                board[cell.row, cell.col] = random.Next(10) < 9 ? 2 : 4;
                
                // Animate new tile
                var tile = tiles[cell.row, cell.col];
                tile.Scale = 0;
                await tile.ScaleToAsync(1, 200, Easing.SpringOut);
            }
        }

        private void UpdateUI()
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var value = board[row, col];
                    var tile = tiles[row, col];
                    if (tile?.Content is Label label)
                    {
                        if (value == 0)
                        {
                            label.Text = "";
                            tile.BackgroundColor = Color.FromArgb("#cdc1b4");
                        }
                        else
                        {
                            label.Text = value.ToString();
                            tile.BackgroundColor = GetTileColor(value);
                            label.TextColor = value <= 4 ? Color.FromArgb("#776e65") : Colors.White;
                            
                            // Adjust font size for larger numbers
                            label.FontSize = value >= 1024 ? 24 : (value >= 128 ? 28 : 32);
                        }
                    }
                }
            }
        }

        private Color GetTileColor(int value)
        {
            return value switch
            {
                2 => Color.FromArgb("#eee4da"),
                4 => Color.FromArgb("#ede0c8"),
                8 => Color.FromArgb("#f2b179"),
                16 => Color.FromArgb("#f59563"),
                32 => Color.FromArgb("#f67c5f"),
                64 => Color.FromArgb("#f65e3b"),
                128 => Color.FromArgb("#edcf72"),
                256 => Color.FromArgb("#edcc61"),
                512 => Color.FromArgb("#edc850"),
                1024 => Color.FromArgb("#edc53f"),
                2048 => Color.FromArgb("#edc22e"),
                _ => Color.FromArgb("#3c3a32")
            };
        }

        private async void OnSwiped(object? sender, SwipedEventArgs e)
        {
            if (isAnimating) return;
            
            isAnimating = true;
            bool moved = false;

            switch (e.Direction)
            {
                case SwipeDirection.Up:
                    moved = MoveUp();
                    break;
                case SwipeDirection.Down:
                    moved = MoveDown();
                    break;
                case SwipeDirection.Left:
                    moved = MoveLeft();
                    break;
                case SwipeDirection.Right:
                    moved = MoveRight();
                    break;
            }

            if (moved)
            {
                UpdateUI();
                await Task.Delay(100);
                AddRandomTile();
                await Task.Delay(200);
                
                ScoreLabel.Text = score.ToString();
                AnimateScoreUpdate(ScoreLabel);
                SaveBestScore();

                if (CheckWin())
                {
                    await DisplayAlertAsync("🎉 Parabéns!", "Você chegou ao 2048!", "Continuar");
                }
                else if (CheckGameOver())
                {
                    await DisplayAlertAsync("Game Over", $"Pontuação final: {score}\nMelhor: {bestScore}", "Novo Jogo");
                    StartNewGame();
                }
            }
            
            isAnimating = false;
        }

        private async void AnimateScoreUpdate(Label label)
        {
            await label.ScaleToAsync(1.2, 100);
            await label.ScaleToAsync(1, 100);
        }

        private bool MoveLeft()
        {
            bool moved = false;
            
            for (int row = 0; row < 4; row++)
            {
                int[] line = new int[4];
                for (int col = 0; col < 4; col++)
                {
                    line[col] = board[row, col];
                }

                int[] newLine = MergeLine(line);
                
                for (int col = 0; col < 4; col++)
                {
                    if (board[row, col] != newLine[col])
                    {
                        moved = true;
                        board[row, col] = newLine[col];
                    }
                }
            }
            
            return moved;
        }

        private bool MoveRight()
        {
            bool moved = false;
            
            for (int row = 0; row < 4; row++)
            {
                int[] line = new int[4];
                for (int col = 0; col < 4; col++)
                {
                    line[col] = board[row, 3 - col];
                }

                int[] newLine = MergeLine(line);
                
                for (int col = 0; col < 4; col++)
                {
                    if (board[row, 3 - col] != newLine[col])
                    {
                        moved = true;
                        board[row, 3 - col] = newLine[col];
                    }
                }
            }
            
            return moved;
        }

        private bool MoveUp()
        {
            bool moved = false;
            
            for (int col = 0; col < 4; col++)
            {
                int[] line = new int[4];
                for (int row = 0; row < 4; row++)
                {
                    line[row] = board[row, col];
                }

                int[] newLine = MergeLine(line);
                
                for (int row = 0; row < 4; row++)
                {
                    if (board[row, col] != newLine[row])
                    {
                        moved = true;
                        board[row, col] = newLine[row];
                    }
                }
            }
            
            return moved;
        }

        private bool MoveDown()
        {
            bool moved = false;
            
            for (int col = 0; col < 4; col++)
            {
                int[] line = new int[4];
                for (int row = 0; row < 4; row++)
                {
                    line[row] = board[3 - row, col];
                }

                int[] newLine = MergeLine(line);
                
                for (int row = 0; row < 4; row++)
                {
                    if (board[3 - row, col] != newLine[row])
                    {
                        moved = true;
                        board[3 - row, col] = newLine[row];
                    }
                }
            }
            
            return moved;
        }

        private int[] MergeLine(int[] line)
        {
            int[] result = new int[4];
            int position = 0;

            // Move all non-zero values to the left
            for (int i = 0; i < 4; i++)
            {
                if (line[i] != 0)
                {
                    result[position++] = line[i];
                }
            }

            // Merge adjacent equal values
            for (int i = 0; i < 3; i++)
            {
                if (result[i] != 0 && result[i] == result[i + 1])
                {
                    result[i] *= 2;
                    score += result[i];
                    result[i + 1] = 0;
                }
            }

            // Move non-zero values to the left again
            int[] finalResult = new int[4];
            position = 0;
            for (int i = 0; i < 4; i++)
            {
                if (result[i] != 0)
                {
                    finalResult[position++] = result[i];
                }
            }

            return finalResult;
        }

        private bool CheckWin()
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (board[row, col] == 2048)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CheckGameOver()
        {
            // Check for empty cells
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (board[row, col] == 0)
                    {
                        return false;
                    }
                }
            }

            // Check for possible merges horizontally
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (board[row, col] == board[row, col + 1])
                    {
                        return false;
                    }
                }
            }

            // Check for possible merges vertically
            for (int col = 0; col < 4; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    if (board[row, col] == board[row + 1, col])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void OnNewGameClicked(object? sender, EventArgs e)
        {
            StartNewGame();
        }
    }
}
