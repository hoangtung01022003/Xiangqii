using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChessDotNet;
using ChessServer;
using ChessDotNet.Pieces;

namespace ChessClient.window
{
  public partial class MainWindow : Window
  {
    private GrpcChannel channel;
    private ChessService.ChessServiceClient client;
    private string playerId;
    private string playerColor;
    private string currentFen;
    private Position selectedPosition = null;
    private bool isMyTurn = false;
    private Dictionary<string, BitmapImage> pieceImages = new Dictionary<string, BitmapImage>();

    public MainWindow()
    {
      try
      {
        LogToConsole("Starting MainWindow constructor...");
        InitializeComponent();
        InitializeChessBoard();
        LoadPieceImages();
        LogToConsole("MainWindow initialized successfully.");
      }
      catch (Exception ex)
      {
        LogToConsole("Error initializing MainWindow.", ex);
      }
    }

    private static void LogToConsole(string message, Exception ex = null)
    {
      string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      string logMessage = $"[Client] [{timestamp}] {message}";
      if (ex != null)
      {
        logMessage += $"\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
      }
      Console.WriteLine(logMessage);
    }

    private void InitializeChessBoard()
    {
      LogToConsole("Initializing chessboard...");
      ChessBoard.Children.Clear();
      for (int row = 0; row < 8; row++)
      {
        for (int col = 0; col < 8; col++)
        {
          Rectangle rect = new Rectangle
          {
            Fill = (row + col) % 2 == 0 ? Brushes.White : Brushes.Gray,
            IsHitTestVisible = false // Ngăn Rectangle chặn chuột
          };
          Grid.SetRow(rect, row);
          Grid.SetColumn(rect, col);
          ChessBoard.Children.Add(rect);
          LogToConsole($"Added rectangle at row {row}, col {col}");

          File file = (File)col;
          int rank = 8 - row;
          Image img = new Image
          {
            Tag = new Position(file, rank),
            Width = 60,  // Kích thước đủ lớn cho ô trống
            Height = 60,
            Stretch = Stretch.Fill
          };
          img.MouseDown += Image_MouseDown;
          Grid.SetRow(img, row);
          Grid.SetColumn(img, col);
          ChessBoard.Children.Add(img);
          LogToConsole($"Added image at row {row}, col {col}, position {(char)('a' + col)}{rank}");
        }
      }
      LogToConsole($"Chessboard initialized. Total children: {ChessBoard.Children.Count}");
    }
    private void LoadPieceImages()
    {
      try
      {
        LogToConsole("Loading piece images...");
        var pieceMappings = new Dictionary<string, string>
        {
            { "wp", "Chess_plt60.png" },
            { "wn", "Chess_nlt60.png" },
            { "wb", "Chess_blt60.png" },
            { "wr", "Chess_rlt60.png" },
            { "wq", "Chess_qlt60.png" },
            { "wk", "Chess_klt60.png" },
            { "bp", "Chess_pdt60.png" },
            { "bn", "Chess_ndt60.png" },
            { "bb", "Chess_bdt60.png" },
            { "br", "Chess_rdt60.png" },
            { "bq", "Chess_qdt60.png" },
            { "bk", "Chess_kdt60.png" }
        };

        string basePath = @"D:\congviec\ChessGame.NET\ChessClient\Images\";
        pieceImages.Clear(); // Xóa ánh xạ cũ
        foreach (var piece in pieceMappings)
        {
          string imagePath = System.IO.Path.Combine(basePath, piece.Value);
          try
          {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(imagePath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            pieceImages[piece.Key] = image;
            LogToConsole($"Loaded image for {piece.Key}: {imagePath}");
          }
          catch (Exception ex)
          {
            LogToConsole($"Failed to load image for {piece.Key}: {imagePath}", ex);
          }
        }
        LogToConsole($"Piece images loaded successfully. Total images: {pieceImages.Count}, Keys: {string.Join(", ", pieceImages.Keys)}");
      }
      catch (Exception ex)
      {
        LogToConsole("Error loading piece images.", ex);
      }
    }
    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // Tạo mới playerId cho mỗi lần kết nối
        playerId = Guid.NewGuid().ToString();
        string ipAddress = ServerIpTextBox.Text;
        LogToConsole($"Attempting to connect to server at {ipAddress} with PlayerId: {playerId}...");

        // Khởi tạo gRPC channel
        channel = GrpcChannel.ForAddress($"http://{ipAddress}");
        client = new ChessService.ChessServiceClient(channel);

        // Gửi yêu cầu kết nối
        var connectResponse = await client.ConnectAsync(new ConnectRequest { PlayerId = playerId });

        // Kiểm tra phản hồi từ server
        if (string.IsNullOrEmpty(connectResponse.Color))
        {
          StatusTextBlock.Text = "Connection failed: No color assigned.";
          LogToConsole($"Connection failed: Server did not assign a color for PlayerId: {playerId}.");
          return;
        }

        // Gán màu người chơi
        playerColor = connectResponse.Color.ToLower(); // Chuẩn hóa thành chữ thường
        StatusTextBlock.Text = $"Connected as {playerColor}";
        LogToConsole($"Connected successfully as {playerColor} with PlayerId: {playerId}.");

        // Bắt đầu luồng nhận trạng thái game
        _ = Task.Run(async () =>
        {
          try
          {
            var call = client.GetGameState(new GameStateRequest { PlayerId = playerId });
            LogToConsole($"Started receiving game state stream for PlayerId: {playerId}.");
            await foreach (var state in call.ResponseStream.ReadAllAsync())
            {
              Dispatcher.Invoke(() => UpdateGameState(state));
            }
          }
          catch (Exception ex)
          {
            Dispatcher.Invoke(() =>
            {
              StatusTextBlock.Text = "Disconnected from game state stream.";
              LogToConsole($"Error in game state stream for PlayerId: {playerId}.", ex);
            });
          }
        });

        // Cập nhật trạng thái giao diện
        ConnectButton.IsEnabled = false;
        NewGameButton.IsEnabled = true;
        ResignButton.IsEnabled = true;
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Connection failed.";
        LogToConsole($"Connection failed for PlayerId: {playerId}.", ex);
      }
    }
    /// <summary>
    /// Cải tiến trực quan cho bảng và các mảnh cờ
    /// </summary>
    /// <param name="state"></param>
    // Add these methods to the MainWindow class

    private void HighlightSquare(Position pos, Brush color, double thickness = 2)
    {
      try
      {
        int row = 8 - pos.Rank;
        int col = (int)pos.File; // File.A = 0, ..., File.H = 7

        if (row < 0 || row >= 8 || col < 0 || col >= 8)
        {
          LogToConsole($"Invalid highlight position: row={row}, col={col}");
          return;
        }

        var rect = ChessBoard.Children
            .OfType<Rectangle>()
            .FirstOrDefault(r => Grid.GetRow(r) == row && Grid.GetColumn(r) == col);

        if (rect != null)
        {
          rect.Stroke = color;
          rect.StrokeThickness = thickness;
          LogToConsole($"Highlighted square at row {row}, col {col}");
        }
        else
        {
          LogToConsole($"No rectangle found at row {row}, col {col}");
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error highlighting square: {ex.Message}", ex);
      }
    }
    private void ClearAllHighlights()
    {
      foreach (var rect in ChessBoard.Children.OfType<Rectangle>())
      {
        rect.Stroke = null;
        rect.StrokeThickness = 0;
      }
    }

    private void HighlightLegalMoves(Position from)
    {
      if (string.IsNullOrEmpty(currentFen))
        return;

      try
      {
        var game = new ChessGame(currentFen);
        var piece = game.GetPieceAt(from);
        if (piece == null)
        {
          LogToConsole($"No piece at {from} to highlight moves.");
          return;
        }

        LogToConsole($"Highlighting legal moves for {piece.GetType().Name} at {from}");

        // Duyệt qua tất cả các ô trên bàn cờ
        for (int rank = 1; rank <= 8; rank++)
        {
          for (File file = File.A; file <= File.H; file++)
          {
            Position to = new Position(file, rank);
            Move move = new Move(from, to, game.WhoseTurn);

            if (game.IsValidMove(move))
            {
              LogToConsole($"Legal move found: from {from} to {to}");
              HighlightSquare(to, Brushes.LightGreen, 2); // Tô sáng ô hợp lệ
            }
          }
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error highlighting legal moves for {from}: {ex.Message}", ex);
      }
    }
    private void UpdateGameState(GameStateResponse state)
    {
      try
      {
        LogToConsole($"Updating game state with Message: {state.Message}...");
        if (!string.IsNullOrEmpty(state.Message) && state.Message.Contains("Game not started"))
        {
          StatusTextBlock.Text = "Waiting for game to start...";
          LogToConsole("Game not started.");
          currentFen = null; // Xóa FEN để ngăn tương tác
          ClearAllHighlights();
          ChessBoard.Children.OfType<Image>().ToList().ForEach(img => img.Source = null);
          return;
        }

        currentFen = state.Fen;
        if (string.IsNullOrEmpty(currentFen))
        {
          LogToConsole("FEN is empty or null, using default FEN.");
          currentFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        }
        LogToConsole($"Processing FEN: {currentFen}");

        var board = ParseFen(currentFen);

        for (int row = 0; row < 8; row++)
        {
          for (int col = 0; col < 8; col++)
          {
            int rank = 8 - row;
            string fileLetter = ((char)('a' + col)).ToString();
            Piece piece = board[col, 7 - row];

            LogToConsole($"Piece at {fileLetter}{rank}: {(piece != null ? piece.GetFenCharacter() : "null")}");

            var img = ChessBoard.Children
                .OfType<Image>()
                .FirstOrDefault(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col);
            if (img != null)
            {
              img.Source = GetPieceImage(piece);
              LogToConsole($"Set image at position {fileLetter}{rank}: {(piece != null ? piece.GetFenCharacter() : "null")}");
            }
            else
            {
              LogToConsole($"No image control found at row {row}, col {col}");
            }
          }
        }

        LogToConsole($"Checking turn: CurrentTurn={state.CurrentTurn}, PlayerColor={playerColor}, IsCheckmate={state.IsCheckmate}, IsStalemate={state.IsStalemate}");
        isMyTurn = state.CurrentTurn.ToLower() == playerColor &&
                   !state.IsCheckmate &&
                   !state.IsStalemate;
        LogToConsole($"Set IsMyTurn={isMyTurn} for PlayerId: {playerId}");

        StatusTextBlock.Text = $"Current turn: {state.CurrentTurn}";
        if (state.IsCheck) StatusTextBlock.Text += " - Check!";
        if (state.IsCheckmate) StatusTextBlock.Text = "Checkmate! Game over.";
        else if (state.IsStalemate) StatusTextBlock.Text = "Stalemate! Game over.";

        LogToConsole($"Game state updated. Current turn: {state.CurrentTurn}, IsMyTurn: {isMyTurn}, PlayerColor: {playerColor}");
      }
      catch (Exception ex)
      {
        LogToConsole("Error updating game state.", ex);
      }
    }
    private static Piece[,] ParseFen(string fen)
    {
      try
      {
        Piece[,] board = new Piece[8, 8];
        string[] parts = fen.Split(' ');
        string position = parts[0];
        string[] rows = position.Split('/');

        for (int row = 0; row < 8; row++)
        {
          string fenRow = rows[7 - row]; // Hàng 8 -> row=0, hàng 1 -> row=7
          int col = 0;
          foreach (char c in fenRow)
          {
            if (char.IsDigit(c))
            {
              int emptySquares = int.Parse(c.ToString());
              for (int i = 0; i < emptySquares; i++)
              {
                if (col < 8)
                {
                  board[col, row] = null;
                  col++;
                }
              }
            }
            else
            {
              if (col < 8)
              {
                Piece piece = CreatePiece(c);
                board[col, row] = piece;
                col++;
              }
            }
          }
        }
        return board;
      }
      catch (Exception ex)
      {
        LogToConsole($"Error parsing FEN: {fen}", ex);
        return new Piece[8, 8];
      }
    }
    private static Piece CreatePiece(char fenChar)
    {
      Player owner = char.IsUpper(fenChar) ? Player.White : Player.Black;
      char pieceType = char.ToLower(fenChar);
      switch (pieceType)
      {
        case 'p': return new Pawn(owner);
        case 'r': return new Rook(owner);
        case 'n': return new Knight(owner);
        case 'b': return new Bishop(owner);
        case 'q': return new Queen(owner);
        case 'k': return new King(owner);
        default: return null;
      }
    }
    private BitmapImage GetPieceImage(Piece piece)
    {
      try
      {
        if (piece == null)
        {
          LogToConsole("Piece is null, returning null image.");
          return null;
        }
        char fenChar = piece.GetFenCharacter();
        string color = char.IsUpper(fenChar) ? "w" : "b";
        string type = fenChar.ToString().ToLower() switch
        {
          "p" => "p",
          "n" => "n",
          "b" => "b",
          "r" => "r",
          "q" => "q",
          "k" => "k",
          _ => ""
        };
        if (string.IsNullOrEmpty(type))
        {
          LogToConsole($"Invalid FEN character: {fenChar}");
          return null;
        }
        string key = color + type;
        if (pieceImages.TryGetValue(key, out var image))
        {
          LogToConsole($"Returning image for {key}: {image.UriSource}");
          return image;
        }
        LogToConsole($"No image found for {key}.");
        return null;
      }
      catch (Exception ex)
      {
        LogToConsole("Error getting piece image.", ex);
        return null;
      }
    }
    private void Image_MouseEnter(object sender, MouseEventArgs e)
    {
      try
      {
        Image img = (Image)sender;
        Position pos = (Position)img.Tag;
        if ((int)pos.File < 0 || (int)pos.File > 7 || pos.Rank < 1 || pos.Rank > 8)
        {
          LogToConsole($"Invalid position on hover: {pos.File}, {pos.Rank}");
          return;
        }

        LogToConsole($"Hover at position: {PositionToString(pos)}");
        if (!string.IsNullOrEmpty(currentFen))
        {
          var game = new ChessGame(currentFen);
          var piece = game.GetPieceAt(pos);
          if (piece != null)
          {
            LogToConsole($"Piece at {PositionToString(pos)}: {piece.GetFenCharacter()}");
            HighlightSquare(pos, Brushes.Yellow, 2);
          }
          else
          {
            LogToConsole($"No piece at {PositionToString(pos)}");
          }
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error in Image_MouseEnter: {ex.Message}", ex);
      }
    }

    private void Image_MouseLeave(object sender, MouseEventArgs e)
    {
      try
      {
        Image img = (Image)sender;
        Position pos = (Position)img.Tag;
        if ((int)pos.File < 0 || (int)pos.File > 7 || pos.Rank < 1 || pos.Rank > 8)
        {
          LogToConsole($"Invalid position on leave: {pos.File}, {pos.Rank}");
          return;
        }
        HighlightSquare(pos, null, 0);
      }
      catch (Exception ex)
      {
        LogToConsole($"Error in Image_MouseLeave: {ex.Message}", ex);
      }
    }

    private async void Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
      try
      {
        LogToConsole("Image_MouseDown triggered.");
        Image img = sender as Image;
        if (img == null)
        {
          LogToConsole("Sender is not an Image.");
          return;
        }

        Position pos = img.Tag as Position;
        if (pos == null)
        {
          LogToConsole("Invalid position.");
          return;
        }

        LogToConsole($"Mouse down at position {PositionToString(pos)}");

        if (string.IsNullOrEmpty(currentFen))
        {
          LogToConsole("No game state available.");
          return;
        }

        var game = new ChessGame(currentFen);
        var targetPiece = game.GetPieceAt(pos);

        if (selectedPosition == null)
        {
          // Click lần 1: Chọn quân cờ
          if (targetPiece != null && targetPiece.Owner.ToString().ToLower() == playerColor && isMyTurn)
          {
            selectedPosition = pos;
            LogToConsole($"Selected piece at {PositionToString(pos)}");
            ClearAllHighlights();
            HighlightSquare(pos, Brushes.Blue, 3);
            HighlightLegalMoves(pos);
          }
          else
          {
            LogToConsole("Cannot select: Not your piece or not your turn.");
          }
        }
        else
        {
          // Click lần 2: Di chuyển, hủy chọn, hoặc chọn quân mới
          string from = PositionToString(selectedPosition);
          string to = PositionToString(pos);
          LogToConsole($"Attempting move from {from} to {to}");

          if (from == to)
          {
            selectedPosition = null;
            ClearAllHighlights();
            LogToConsole($"Deselected piece at {from}");
            return;
          }

          Move move = new Move(selectedPosition, pos, playerColor == "white" ? Player.White : Player.Black);
          if (game.IsValidMove(move))
          {
            try
            {
              var moveRequest = new MoveRequest { From = from, To = to };
              LogToConsole($"Sending MakeMove request: From={from}, To={to}");
              var moveResponse = await client.MakeMoveAsync(moveRequest);

              if (moveResponse.Success)
              {
                LogToConsole($"Move succeeded: {from} to {to}");
              }
              else
              {
                LogToConsole($"Move failed: {moveResponse.Message}");
              }
            }
            catch (Exception ex)
            {
              LogToConsole($"Error sending move: {ex.Message}");
            }
          }
          else
          {
            // Nếu nhấp vào quân cờ khác của mình, chọn quân mới
            if (targetPiece != null && targetPiece.Owner.ToString().ToLower() == playerColor && isMyTurn)
            {
              selectedPosition = pos;
              LogToConsole($"Selected new piece at {PositionToString(pos)}");
              ClearAllHighlights();
              HighlightSquare(pos, Brushes.Blue, 3);
              HighlightLegalMoves(pos);
            }
            else
            {
              LogToConsole($"Invalid move from {from} to {to}");
            }
          }
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error in Image_MouseDown: {ex.Message}");
        selectedPosition = null;
      }
    }
    private string PositionToString(Position pos)
    {
      try
      {
        char fileChar = (char)('a' + (int)pos.File); // File.A = 0 -> 'a', File.H = 7 -> 'h'
        string result = $"{fileChar}{pos.Rank}";
        LogToConsole($"Converted position to string: {result} for PlayerId: {playerId}.");
        return result;
      }
      catch (Exception ex)
      {
        LogToConsole($"Error converting position to string for PlayerId: {playerId}.", ex);
        return "";
      }
    }
    private async void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        LogToConsole($"Starting new game for PlayerId: {playerId}...");
        var response = await client.StartGameAsync(new StartGameRequest { PlayerId = playerId });
        StatusTextBlock.Text = response.Message;
        LogToConsole($"New game response: {response.Message} for PlayerId: {playerId}.");
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Error starting game.";
        LogToConsole($"Error starting game for PlayerId: {playerId}.", ex);
      }
    }

    private async void ResignButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        LogToConsole($"Resigning game for PlayerId: {playerId}...");
        var response = await client.ResignAsync(new ResignRequest { PlayerId = playerId });
        StatusTextBlock.Text = response.Message;
        LogToConsole($"Game resigned: {response.Message} for PlayerId: {playerId}.");
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Error resigning game.";
        LogToConsole($"Error resigning game for PlayerId: {playerId}.", ex);
      }
    }
  }
}