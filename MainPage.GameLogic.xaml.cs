namespace Demo2_RocketLaunch
{
  using com.mtaulty.OxfordVerify;
  using System;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.ComponentModel;
  using System.Linq;
  using System.Threading.Tasks;
  using Windows.UI.Popups;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;
  using Windows.UI.Xaml.Data;
  using Windows.UI.Xaml.Media.Imaging;

  public partial class MainPage : Page, INotifyPropertyChanged
  {
    enum MoveDirection
    {
      Left,
      Right,
      Up
    }
    public event PropertyChangedEventHandler PropertyChanged;

    // We'll fall over if you set rows and columns too low here.
    static readonly int GRID_ROWS = 12;
    static readonly int GRID_COLUMNS = 6;
    static readonly int OBSTACLES = 10;
    static readonly int IMAGE_DANGER_LOWER_BOUND = 1;
    static readonly int IMAGE_DANGER_UPPER_BOUND = 3;
    static readonly int TIMER_INTERVAL_SEC = 2;

    int rocketRow;
    int rocketColumn;
    int moonColumn;
    bool isSecure;
    string userToAdd;
    string launchUser;
    ObservableCollection<string> launchUsers;
    List<Tuple<int,int>> obstaclePositions;
    OxfordVerificationClient oxfordClient;

    public ObservableCollection<string> LaunchUsers
    {
      get
      {
        return (this.launchUsers);
      }
      set
      {
        this.launchUsers = value;
        this.FirePropertyChanged("LaunchUsers");
      }
    }

    public string LaunchUser
    {
      get
      {
        return (this.launchUser);
      }
      set
      {
        this.launchUser = value;
        this.FirePropertyChanged("LaunchUser");
      }
    }

    public string UserToAdd
    {
      get
      {
        return (this.userToAdd);
      }
      set
      {
        this.userToAdd = value;
        this.FirePropertyChanged("UserToAdd");
      }
    }
    async Task ShowMessageDialogAsync(string message, string title)
    {
      var dialog = new MessageDialog(message, title);
      await dialog.ShowAsync();
    }

    public int RocketRow
    {
      get
      {
        return rocketRow;
      }
      set
      {
        rocketRow = value;
        this.FirePropertyChanged("RocketRow");
      }
    }
    public int RocketColumn
    {
      get
      {
        return(this.rocketColumn);
      }
      set
      {
        this.rocketColumn = value;
        this.FirePropertyChanged("RocketColumn");
      }
    }
    public bool IsSecure
    {
      get
      {
        return(this.isSecure);
      }
      set
      {
        this.isSecure = value;
        this.FirePropertyChanged("IsSecure");
      }
    }
    void FirePropertyChanged(string propertyName)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    void InitialiseGame()
    {
      this.obstaclePositions = new List<Tuple<int, int>>();

      this.gridGame.Children.Clear();

      if (this.gridGame.ColumnDefinitions.Count == 0)
      {
        for (int i = 0; i < GRID_COLUMNS; i++)
        {
          this.gridGame.ColumnDefinitions.Add(
            new ColumnDefinition());
        }
        for (int i = 0; i < GRID_ROWS; i++)
        {
          this.gridGame.RowDefinitions.Add(
            new RowDefinition());
        }
      }
      Random random = new Random();

      // TODO: these hard-coded literals to images aren't very nice.
      for (int i = 0; i < OBSTACLES; i++)
      {
        int randomRow = random.Next(1, GRID_ROWS - 1);
        int randomCol = random.Next(0, GRID_COLUMNS);
        int imageCount = random.Next(
          IMAGE_DANGER_LOWER_BOUND,
          IMAGE_DANGER_UPPER_BOUND + 1);

        // TODO: We should replace these with vector items that we can then colour
        // appropriately rather than use 3 images.
        this.AddImageAtGridRowColumn(
          $"ms-appx:///Assets/GameItems/danger{imageCount}.png",
          randomRow,
          randomCol);

        this.obstaclePositions.Add(Tuple.Create(randomRow, randomCol));
      }
      this.moonColumn = random.Next(0, GRID_COLUMNS);

      // TODO:
      this.AddImageAtGridRowColumn(
        "ms-appx:///Assets/GameItems/goal.png", 0, this.moonColumn);

      this.RocketColumn = random.Next(0, GRID_COLUMNS);
      this.RocketRow = GRID_ROWS - 1;

      // TODO:
      var rocketImage = this.AddImageAtGridRowColumn(
        "ms-appx:///Assets/GameItems/rocket.png",
        this.RocketRow,
        this.RocketColumn);

      rocketImage.SetBinding(Grid.RowProperty,
        new Binding()
        {
          Path = new PropertyPath("RocketRow")
        }
      );
      rocketImage.SetBinding(Grid.ColumnProperty,
        new Binding()
        {
          Path = new PropertyPath("RocketColumn")
        }
      );
    }
    Image AddImageAtGridRowColumn(string imagePath, int row, int col)
    {
      BitmapImage source = new BitmapImage(
        new Uri(imagePath));

      Image image = new Image();
      image.Source = source;
      Grid.SetRow(image, row);
      Grid.SetColumn(image, col);
      this.gridGame.Children.Add(image);

      return (image);
    }
    async void OnGameLoopTimerTick(object sender, object e)
    {
      string endGameMessage = string.Empty;

      if (this.TryMoveUp())
      {
        if (this.HasHitObstacle)
        {
          // Game over
          endGameMessage = "Woops. Would you like to try again?";
        }
      }
      else
      {
        // Game over
        endGameMessage = this.HasLandedOnMoon ?
          "Well done! Would you like to try again?" :
          "Woops. Would you like to try again?";
      }
      if (!string.IsNullOrEmpty(endGameMessage))
      {
        this.timer.Stop();

        var dialog = new MessageDialog(
          endGameMessage, "Game Over");

        await dialog.ShowAsync();

        this.InitialiseGame();
      }
    }

    bool TryMoveUp()
    {
      bool moved = this.CanMove(MoveDirection.Up);

      if (moved)
      {
        this.RocketRow--;
      }
      return (moved);
    }
    void MoveLeft()
    {
      if (this.CanMove(MoveDirection.Left))
      {
        this.RocketColumn--;
      }
    }
    void MoveRight()
    {
      if (this.CanMove(MoveDirection.Right))
      {
        this.RocketColumn++;
      }
    }
    bool CanMove(MoveDirection direction)
    {
      bool canMove = true;

      switch (direction)
      {
        case MoveDirection.Left:
          canMove = this.RocketColumn > 0;
          break;
        case MoveDirection.Right:
          canMove = this.RocketColumn < (GRID_COLUMNS - 1);
          break;
        case MoveDirection.Up:
          canMove = this.RocketRow > 0;
          break;
        default:
          break;
      }
      return (canMove);
    }
    bool HasHitObstacle
    {
      get
      {
        bool hit = this.obstaclePositions.Any(
          entry => ((this.RocketRow == entry.Item1) && (this.RocketColumn == entry.Item2)));

        return (hit);
      }
    }
    bool HasLandedOnMoon
    {
      get
      {
        return (this.RocketColumn == this.moonColumn);
      }
    }

    async void BuildLaunchUsersAsync()
    {
      this.launchUsers.Clear();

      var list = await this.oxfordClient.GetUsersAsync();

      foreach (var user in list)
      {
        this.launchUsers.Add(user);
      }
    }
  }
}