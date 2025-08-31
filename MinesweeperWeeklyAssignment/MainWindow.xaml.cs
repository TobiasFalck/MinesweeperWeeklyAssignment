using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MinesweeperWeeklyAssignment
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool[,] mineField;
        private ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();
            DataContext = viewModel;
            InitializeGameBoard(20, 20);
            InitializeMineField(20, 20, viewModel.TotalMines);
        }

        private void InitializeGameBoard(int rows, int cols)
        {
            GameBoard.Rows = rows;
            GameBoard.Columns = cols;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Button cellButton = new Button
                    {
                        Name = $"Cell{row}{col}",
                        Tag = new Point(row, col),
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.DarkGray,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        Content = "",
                    };

                    cellButton.Click += Cell_Click;
                    cellButton.MouseRightButtonDown += Cell_RightClick;
                    GameBoard.Children.Add(cellButton);
                }
            }
        }

        private void InitializeMineField(int rows, int cols, int mineCount)
        {
            mineField = new bool[rows, cols];
            Random random = new();

            for (int i = 0; i < mineCount; i++)
            {
                int row = random.Next(rows);
                int col = random.Next(cols);
                mineField[row, col] = true;
            }
        }

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            viewModel.StartTimer();

            Button clickedButton = (Button)sender;
            Point position = (Point)clickedButton.Tag;

            if (clickedButton.Content.ToString() == "🚩" || !clickedButton.Background.Equals(Brushes.LightGray))
            {
                return;
            }

            bool isMine = CheckIfMine(position);

            if (isMine)
            {
                clickedButton.Background = Brushes.Red;
                viewModel.StopTimer();
                RevealAllMines();
                MessageBox.Show("Game Over!");
            }
            else
            {
                RevealCell(clickedButton, position);
            }
        }

        private void Cell_RightClick(object sender, MouseButtonEventArgs e)
        {
            Button clickedButton = (Button)sender;
            Point position = (Point)clickedButton.Tag;

            if (clickedButton.Background.Equals(Brushes.LightGray))
            {
                if (clickedButton.Content.ToString() == "🚩")
                {
                    clickedButton.Content = "";
                    viewModel.FlagsPlaced--;
                }
                else
                {
                    clickedButton.Content = "🚩";
                    viewModel.FlagsPlaced++;
                }
            }
            e.Handled = true;
        }

        private bool CheckIfMine(Point position)
        {
            return mineField[(int)position.X, (int)position.Y];
        }

        private int CountAdjacentMines(Point position)
        {
            int count = 0;
            int row = (int)position.X;
            int col = (int)position.Y;

            for (int r = Math.Max(0, row - 1); r <= Math.Min(row + 1, mineField.GetLength(0) - 1); r++)
            {
                for (int c = Math.Max(0, col - 1); c <= Math.Min(col + 1, mineField.GetLength(1) - 1); c++)
                {
                    if (mineField[r, c])
                        count++;
                }
            }
            return count;
        }

        private void RevealCell(Button cellButton, Point position)
        {
            if (!cellButton.Background.Equals(Brushes.LightGray))
                return;

            int adjacentMines = CountAdjacentMines(position);

            if (adjacentMines > 0)
            {
                cellButton.Content = adjacentMines.ToString();
                cellButton.Background = Brushes.White;
            }
            else
            {
                cellButton.Background = Brushes.White;
                RevealAdjacentCells(position);
            }
        }

        private void RevealAdjacentCells(Point position)
        {
            int row = (int)position.X;
            int col = (int)position.Y;

            for (int r = Math.Max(0, row - 1); r <= Math.Min(row + 1, GameBoard.Rows - 1); r++)
            {
                for (int c = Math.Max(0, col - 1); c <= Math.Min(col + 1, GameBoard.Columns - 1); c++)
                {
                    if (r == row && c == col)
                        continue;

                    Point adjacentPosition = new(r, c);
                    Button adjacentButton = FindButtonByPosition(adjacentPosition);

                    if (adjacentButton != null && adjacentButton.Background.Equals(Brushes.LightGray))
                    {
                        Cell_Click(adjacentButton, new RoutedEventArgs());
                    }
                }
            }
        }

        private Button FindButtonByPosition(Point position)
        {
            foreach (Button button in GameBoard.Children)
            {
                Point buttonPosition = (Point)button.Tag;
                if (buttonPosition.X == position.X && buttonPosition.Y == position.Y)
                {
                    return button;
                }
            }
            return null;
        }

        private void RevealAllMines()
        {
            foreach (Button button in GameBoard.Children)
            {
                Point position = (Point)button.Tag;
                bool isMine = CheckIfMine(position);

                if (isMine)
                {
                    if (button.Content.ToString() == "🚩")
                    {
                        button.Background = Brushes.Green;
                    }
                    else
                    {
                        button.Background = Brushes.Red;
                        button.Content = "💣";
                    }
                }
                else if (button.Content.ToString() == "🚩")
                {
                    button.Background = Brushes.Yellow;
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.ResetTimer();
            GameBoard.Children.Clear();
            InitializeGameBoard(20, 20);
            InitializeMineField(20, 20, viewModel.TotalMines);
            viewModel.FlagsPlaced = 0;
            StatusLabel.Text = "";
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private int _flagsPlaced;
        private int _totalMines = 70;
        private int _timeElapsed;
        private DispatcherTimer _gameTimer;
        private bool _isFirstClick = true;

        public int FlagsPlaced
        {
            get => _flagsPlaced;
            set
            {
                _flagsPlaced = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MinesRemaining));
            }
        }

        public int TotalMines
        {
            get => _totalMines;
            set
            {
                _totalMines = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MinesRemaining));
            }
        }

        public int MinesRemaining => TotalMines - FlagsPlaced;

        public int TimeElapsed
        {
            get => _timeElapsed;
            set
            {
                _timeElapsed = value;
                OnPropertyChanged();
            }
        }

        public ViewModel()
        {
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromSeconds(1);
            _gameTimer.Tick += (s, e) => TimeElapsed++;
        }

        public void StartTimer()
        {
            if (_isFirstClick)
            {
                _gameTimer.Start();
                _isFirstClick = false;
            }
        }

        public void StopTimer()
        {
            _gameTimer.Stop();
        }

        public void ResetTimer()
        {
            _gameTimer.Stop();
            TimeElapsed = 0;
            _isFirstClick = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}