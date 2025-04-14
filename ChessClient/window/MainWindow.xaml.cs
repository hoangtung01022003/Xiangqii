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
using ChessServer;
using ChessClient.Xiangqi;
using System.IO;
using Path = System.IO.Path;
using System.Threading;
using System.Net.Http;

namespace ChessClient.window
{
  public partial class MainWindow : Window
  {
    private GrpcChannel channel;
    private ChessService.ChessServiceClient client;
    private string playerId;
    private string playerColor; // red hoặc black
    private string currentFen;
    private XiangqiPosition selectedPosition = null;
    private bool isMyTurn = false;
    private Dictionary<string, BitmapImage> pieceImages = new Dictionary<string, BitmapImage>();

    public MainWindow()
    {
      try
      {
        InitializeComponent();
        InitializeChessBoard();
        LoadPieceImages();
        // Khởi tạo bàn cờ mặc định
        currentFen = new XiangqiGame().GetFen();
        UpdateGameState(new GameStateResponse { Fen = currentFen, CurrentTurn = "red" });
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
        logMessage += $"\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
      Console.WriteLine(logMessage);
      File.AppendAllText("debug.log", logMessage + "\n");
    }

    private void InitializeChessBoard()
    {
      try
      {
        LogToConsole("Initializing Xiangqi board...");
        ChessBoard.Children.Clear();

        double squareWidth = 450.0 / 9; // 50px mỗi cột
        double squareHeight = 500.0 / 10; // 50px mỗi hàng

        // Đặt Image cho quân cờ
        for (int row = 0; row < 10; row++)
        {
          for (int col = 0; col < 9; col++)
          {
            Image img = new Image
            {
              Tag = new XiangqiPosition(col + 1, row),
              Width = 50,  // Giảm kích thước để lộ lưới
              Height = 50, // Giảm kích thước để lộ lưới
              Stretch = Stretch.Uniform,
              IsHitTestVisible = true
            };
            // Căn giữa ảnh trong ô
            Canvas.SetLeft(img, col * squareWidth + (squareWidth - 50) / 2); // Căn giữa theo chiều ngang
            Canvas.SetTop(img, row * squareHeight + (squareHeight - 50) / 2); // Căn giữa theo chiều dọc
            Canvas.SetZIndex(img, 100); // Giữ z-index cao để tương tác
            ChessBoard.Children.Add(img);
          }
        }

        ChessBoard.MouseDown += ChessBoard_MouseDown;
        LogToConsole("Xiangqi board initialized.");
      }
      catch (Exception ex)
      {
        LogToConsole("Error initializing Xiangqi board.", ex);
      }
    }
    private void LoadPieceImages()
    {
      try
      {
        LogToConsole("Loading Xiangqi piece images...");
        var pieceMappings = new Dictionary<string, string>
                {
                    { "rk", "rk.png" }, // Tướng đỏ
                    { "ra", "ra.png" }, // Sĩ đỏ
                    { "rn", "rn.png" }, // Tượng đỏ
                    { "rr", "rr.png" }, // Xe đỏ
                    { "rb", "rb.png" }, // Pháo đỏ
                    { "rc", "rc.png" }, // Mã đỏ
                    { "rp", "rp.png" }, // Tốt đỏ
                    { "bk", "bk.png" }, // Tướng đen
                    { "ba", "ba.png" }, // Sĩ đen
                    { "bn", "bn.png" }, // Tượng đen
                    { "br", "br.png" }, // Xe đen
                    { "bb", "bb.png" }, // Pháo đen
                    { "bc", "bc.png" }, // Mã đen
                    { "bp", "bp.png" }  // Tốt đen
                };

        string basePath = @"D:\congviec\ChessGame.NET\ChessClient\Images\";
        pieceImages.Clear();
        foreach (var piece in pieceMappings)
        {
          string imagePath = Path.Combine(basePath, piece.Value);
          if (!File.Exists(imagePath))
          {
            LogToConsole($"Image not found: {imagePath}. Skipping...");
            continue;
          }
          var image = new BitmapImage();
          image.BeginInit();
          image.UriSource = new Uri(imagePath, UriKind.Absolute);
          image.CacheOption = BitmapCacheOption.OnLoad;
          image.EndInit();
          pieceImages[piece.Key] = image;
          LogToConsole($"Loaded image for {piece.Key}: {imagePath}");
        }
        LogToConsole($"Piece images loaded: {pieceImages.Count}");
      }
      catch (Exception ex)
      {
        LogToConsole("Error loading piece images.", ex);
      }
    }
    private void ChessBoard_MouseDown(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var point = e.GetPosition(ChessBoard);
        double squareWidth = 450.0 / 9; // 50px
        double squareHeight = 500.0 / 10; // 50px
        int col = (int)(point.X / squareWidth) + 1;
        int row = (int)(point.Y / squareHeight);
        if (col >= 1 && col <= 9 && row >= 0 && row <= 9)
        {
          string pos = $"{col}{row}";
          LogToConsole($"ChessBoard_MouseDown at {pos}");
          var img = ChessBoard.Children
              .OfType<Image>()
              .FirstOrDefault(i => Math.Abs(Canvas.GetLeft(i) + 22.5 - (col - 1) * squareWidth) < squareWidth / 2 &&
                                   Math.Abs(Canvas.GetTop(i) + 22.5 - row * squareHeight) < squareHeight / 2);
          if (img != null)
            Image_MouseDown(img, e);
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error in ChessBoard_MouseDown: {ex.Message}", ex);
      }
    }
    private async void Image_MouseDown(object sender, MouseButtonEventArgs e)
    {
      try
      {
        Image img = sender as Image;
        if (img == null) return;
        XiangqiPosition pos = img.Tag as XiangqiPosition;
        if (pos == null)
        {
          LogToConsole("Invalid image tag.");
          return;
        }
        string posStr = pos.ToNotation();
        LogToConsole($"MouseDown at {posStr}, playerColor={playerColor}, isMyTurn={isMyTurn}");

        if (string.IsNullOrEmpty(currentFen))
        {
          StatusTextBlock.Text = "Trò chơi chưa bắt đầu. Vui lòng nhấn 'Ván mới'.";
          LogToConsole("Game not started: currentFen is empty.");
          return;
        }

        var game = new XiangqiGame(currentFen);
        var targetPiece = game.GetPieceAt(pos);

        // Nếu đã chọn một quân cờ trước đó, đây là ô đích
        if (selectedPosition != null)
        {
          var from = selectedPosition;
          var to = pos;
          var move = new XiangqiMove(from, to, playerColor == "red" ? Player.Red : Player.Black);

          if (game.IsValidMove(move))
          {
            StatusTextBlock.Text = "Đang gửi nước đi...";

            try
            {
              // Gửi yêu cầu di chuyển đến server với PlayerId trong header
              var headers = new Metadata
          {
            { "PlayerId", playerId }
          };

              var request = new MoveRequest { From = from.ToNotation(), To = to.ToNotation() };
              var response = await client.MakeMoveAsync(request, headers);

              LogToConsole($"Moved from {from.ToNotation()} to {to.ToNotation()}: {response.Success} - {response.Message}");

              if (response.Success)
              {
                StatusTextBlock.Text = "Nước đi thành công. Đang chờ đối thủ...";
              }
              else
              {
                StatusTextBlock.Text = $"Nước đi thất bại: {response.Message}";
              }
            }
            catch (Exception ex)
            {
              StatusTextBlock.Text = "Lỗi khi gửi nước đi";
              LogToConsole($"Error sending move: {ex.Message}", ex);
            }
          }
          else
          {
            StatusTextBlock.Text = "Nước đi không hợp lệ.";
            LogToConsole($"Invalid move from {from.ToNotation()} to {to.ToNotation()}");
          }

          // Xóa lựa chọn sau khi di chuyển (thành công hoặc thất bại)
          selectedPosition = null;
          ClearAllHighlights();
          return;
        }

        // Nếu chưa chọn quân cờ, xử lý việc chọn quân cờ
        if (targetPiece != null && targetPiece.Owner.ToString().ToLower() == playerColor && isMyTurn)
        {
          selectedPosition = pos;
          ClearAllHighlights();
          HighlightSquare(pos, Brushes.LightGray, 3); // Tô sáng ô đang chọn màu xám nhạt
          HighlightLegalMoves(pos);
          StatusTextBlock.Text = $"Đã chọn {posStr}. Hãy chọn ô đích.";
        }
        else
        {
          StatusTextBlock.Text = targetPiece == null ? $"Không có quân cờ tại {posStr}." :
                                isMyTurn ? "Hãy chọn quân cờ của bạn." : "Chưa đến lượt của bạn.";
          LogToConsole($"Cannot select piece at {posStr}: pieceOwner={targetPiece?.Owner}, playerColor={playerColor}, isMyTurn={isMyTurn}");
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error in Image_MouseDown: {ex.Message}", ex);
      }
    }
    private void HighlightSquare(XiangqiPosition pos, Brush color, double thickness)
    {
      try
      {
        double squareWidth = 450.0 / 9; // 50px
        double squareHeight = 500.0 / 10; // 50px
        int col = pos.File - 1;
        int row = pos.Rank;

        var ellipse = ChessBoard.Children
            .OfType<Ellipse>()
            .FirstOrDefault(e => Math.Abs(Canvas.GetLeft(e) + 3 - col * squareWidth) < 0.01 &&
                                 Math.Abs(Canvas.GetTop(e) + 3 - row * squareHeight) < 0.01);

        if (ellipse != null)
        {
          ellipse.Stroke = color;
          ellipse.StrokeThickness = thickness;
          Canvas.SetZIndex(ellipse, 0);
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error highlighting square: {ex.Message}", ex);
      }
    }
    private void ClearAllHighlights()
    {
      foreach (var ellipse in ChessBoard.Children.OfType<Ellipse>())
      {
        ellipse.Stroke = null;
        ellipse.StrokeThickness = 0;
      }
    }

    private void HighlightLegalMoves(XiangqiPosition from)
    {
      try
      {
        LogToConsole($"Highlighting legal moves from {from.ToNotation()}...");
        var game = new XiangqiGame(currentFen);
        var piece = game.GetPieceAt(from);
        if (piece == null) return;

        for (int rank = 0; rank < 10; rank++)
        {
          for (int file = 1; file <= 9; file++)
          {
            XiangqiPosition to = new XiangqiPosition(file, rank);
            XiangqiMove move = new XiangqiMove(from, to, piece.Owner);
            if (game.IsValidMove(move))
            {
              HighlightSquare(to, Brushes.White, 3); // Tô sáng màu trắng
              LogToConsole($"Highlighted legal move to {to.ToNotation()}");
            }
          }
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error highlighting legal moves: {ex.Message}", ex);
      }
    }
    private void UpdateGameState(GameStateResponse state)
    {
      try
      {
        if (state != null && state.Message.Contains("Game not started"))
        {
          StatusTextBlock.Text = "Waiting for game to start...";
          currentFen = null;
          ClearAllHighlights();
          ChessBoard.Children.OfType<Image>().ToList().ForEach(img => img.Source = null);
          return;
        }

        currentFen = state?.Fen ?? currentFen ?? new XiangqiGame().GetFen();
        LogToConsole($"Updating game state with FEN: {currentFen}");

        var game = new XiangqiGame(currentFen); // Tạo instance của XiangqiGame từ FEN

        for (int row = 0; row < 10; row++)
        {
          for (int col = 0; col < 9; col++)
          {
            XiangqiPosition pos = new XiangqiPosition(col + 1, row);
            XiangqiPiece piece = game.GetPieceAt(pos); // Sử dụng GetPieceAt thay vì GetBoard
            var img = ChessBoard.Children
                .OfType<Image>()
                .FirstOrDefault(i => i.Tag is XiangqiPosition p &&
                                     p.File == col + 1 && p.Rank == row);
            if (img != null)
            {
              img.Source = GetPieceImage(piece);
              img.Width = 50;  // Đảm bảo kích thước
              img.Height = 50; // Đảm bảo kích thước
              Canvas.SetZIndex(img, 100);
            }
          }
        }

        if (state != null)
        {
          isMyTurn = state.CurrentTurn.ToLower() == playerColor && !state.IsCheckmate && !state.IsStalemate;
          StatusTextBlock.Text = $"Current turn: {state.CurrentTurn}";
          if (state.IsCheck) StatusTextBlock.Text += " - Check!";
          if (state.IsCheckmate) StatusTextBlock.Text = "Checkmate! Game over.";
          else if (state.IsStalemate) StatusTextBlock.Text = "Stalemate! Game over.";
          else if (state.Message.Contains("Draw")) StatusTextBlock.Text = "Draw! Game over.";
        }
        else
        {
          StatusTextBlock.Text = "Waiting for server connection...";
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error updating game state: {ex.Message}", ex);
      }
    }
    private XiangqiPiece[,] ParseFen(string fen)
    {
      try
      {
        XiangqiPiece[,] board = new XiangqiPiece[9, 10];
        string[] parts = fen.Split(' ');
        string position = parts[0];
        string[] rows = position.Split('/');

        for (int row = 0; row < 10; row++)
        {
          string fenRow = rows[9 - row];
          int col = 0;
          foreach (char c in fenRow)
          {
            if (char.IsDigit(c))
            {
              int emptySquares = int.Parse(c.ToString());
              for (int i = 0; i < emptySquares; i++)
                if (col < 9) col++;
            }
            else
            {
              if (col < 9)
              {
                board[col, row] = CreatePiece(c);
                col++;
              }
            }
          }
        }
        LogToConsole("FEN parsed successfully.");
        return board;
      }
      catch (Exception ex)
      {
        LogToConsole($"Error parsing FEN: {fen}", ex);
        return new XiangqiPiece[9, 10];
      }
    }

    private XiangqiPiece CreatePiece(char fenChar)
    {
      try
      {
        Player owner = char.IsUpper(fenChar) ? Player.Red : Player.Black;
        char pieceType = char.ToLower(fenChar);
        LogToConsole($"Creating piece for FEN char: {fenChar}");
        return pieceType switch
        {
          'k' => new XiangqiPiece(PieceType.General, owner),
          'a' => new XiangqiPiece(PieceType.Advisor, owner),
          'n' => new XiangqiPiece(PieceType.Elephant, owner),
          'r' => new XiangqiPiece(PieceType.Chariot, owner),
          'b' => new XiangqiPiece(PieceType.Cannon, owner),
          'c' => new XiangqiPiece(PieceType.Horse, owner),
          'p' => new XiangqiPiece(PieceType.Soldier, owner),
          _ => null
        };
      }
      catch (Exception ex)
      {
        LogToConsole($"Error creating piece for char: {fenChar}", ex);
        return null;
      }
    }

    private BitmapImage GetPieceImage(XiangqiPiece piece)
    {
      try
      {
        if (piece == null) return null;
        char fenChar = piece.GetFenCharacter();
        string color = char.IsUpper(fenChar) ? "r" : "b";
        string type = fenChar.ToString().ToLower();
        string key = color + type;
        LogToConsole($"Attempting to get image for key: {key}");
        if (pieceImages.TryGetValue(key, out var image))
        {
          LogToConsole($"Image found for {key}");
          return image;
        }
        LogToConsole($"No image found for {key}");
        return null;
      }
      catch (Exception ex)
      {
        LogToConsole($"Error getting piece image: {ex.Message}", ex);
        return null;
      }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        playerId = Guid.NewGuid().ToString();
        string ipAddress = ServerIpTextBox.Text;

        // Lấy cổng từ TextBox và kiểm tra tính hợp lệ
        if (!int.TryParse(ServerPortTextBox.Text, out int port))
        {
          MessageBox.Show("Cổng không hợp lệ. Vui lòng nhập một số nguyên.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        // Hiển thị thông báo đang kết nối
        StatusTextBlock.Text = $"Đang kết nối đến {ipAddress}:{port}...";
        ConnectButton.IsEnabled = false;

        // Cấu hình cho HTTP/2 không bảo mật (lưu ý - chỉ dùng cho mạng nội bộ tin cậy)
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        // Cấu hình HttpClient với thời gian chờ dài hơn
        var httpClientHandler = new HttpClientHandler();
        var httpClient = new HttpClient(httpClientHandler)
        {
          Timeout = TimeSpan.FromSeconds(30)
        };

        // Tạo kênh gRPC
        var channelOptions = new GrpcChannelOptions
        {
          HttpClient = httpClient,
          MaxReceiveMessageSize = 16 * 1024 * 1024, // 16 MB
          MaxSendMessageSize = 16 * 1024 * 1024     // 16 MB
        };

        LogToConsole($"Đang kết nối tới http://{ipAddress}:{port}");
        channel = GrpcChannel.ForAddress($"http://{ipAddress}:{port}", channelOptions);
        client = new ChessService.ChessServiceClient(channel);

        // Thiết lập thời gian chờ cho cuộc gọi gRPC
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var headers = new Metadata
    {
      { "PlayerId", playerId }
    };

        var connectResponse = await client.ConnectAsync(
          new ConnectRequest { PlayerId = playerId },
          headers,
          deadline: deadline
        );

        if (string.IsNullOrEmpty(connectResponse.Color))
        {
          StatusTextBlock.Text = $"Kết nối thất bại: {connectResponse.Message}";
          ConnectButton.IsEnabled = true;
          return;
        }

        playerColor = connectResponse.Color.ToLower();
        StatusTextBlock.Text = $"Đã kết nối với tư cách {playerColor}";
        ConnectButton.IsEnabled = false;
        NewGameButton.IsEnabled = true;
        ResignButton.IsEnabled = true;

        // Bắt đầu luồng nhận cập nhật trạng thái trò chơi
        cancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
          try
          {
            var headers = new Metadata
            {
          { "PlayerId", playerId }
            };

            var call = client.GetGameState(
              new GameStateRequest { PlayerId = playerId },
              headers
            );

            await foreach (var state in call.ResponseStream.ReadAllAsync(CancellationTokenSource.Token))
            {
              Dispatcher.Invoke(() => UpdateGameState(state));
            }
          }
          catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
          {
            // Bình thường khi hủy
            Dispatcher.Invoke(() => LogToConsole("Luồng cập nhật trạng thái bị hủy."));
          }
          catch (Exception ex)
          {
            Dispatcher.Invoke(() =>
            {
              StatusTextBlock.Text = "Mất kết nối với máy chủ.";
              LogToConsole($"Lỗi trong luồng trạng thái trò chơi: {ex.Message}", ex);

              // Kích hoạt kết nối lại
              ConnectButton.IsEnabled = true;
              NewGameButton.IsEnabled = false;
              ResignButton.IsEnabled = false;
            });
          }
        });
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Kết nối thất bại.";
        LogToConsole($"Kết nối thất bại: {ex.Message}", ex);
        ConnectButton.IsEnabled = true;
      }
    }
    private async void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        NewGameButton.IsEnabled = false;
        StatusTextBlock.Text = "Đang bắt đầu trò chơi mới...";

        var headers = new Metadata
    {
      { "PlayerId", playerId }
    };

        var response = await client.StartGameAsync(
          new StartGameRequest { PlayerId = playerId },
          headers
        );

        StatusTextBlock.Text = response.Message;
        NewGameButton.IsEnabled = true;
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Lỗi khi bắt đầu trò chơi.";
        LogToConsole($"Lỗi bắt đầu trò chơi: {ex.Message}", ex);
        NewGameButton.IsEnabled = true;
      }
    }

    private async void ResignButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        ResignButton.IsEnabled = false;
        StatusTextBlock.Text = "Đang đầu hàng...";

        var headers = new Metadata
    {
      { "PlayerId", playerId }
    };

        var response = await client.ResignAsync(
          new ResignRequest { PlayerId = playerId },
          headers
        );

        StatusTextBlock.Text = response.Message;
        ResignButton.IsEnabled = true;
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Lỗi khi đầu hàng.";
        LogToConsole($"Lỗi đầu hàng: {ex.Message}", ex);
        ResignButton.IsEnabled = true;
      }
    }
    private async void ResignButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var response = await client.ResignAsync(new ResignRequest { PlayerId = playerId });
        StatusTextBlock.Text = response.Message;
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Error resigning game.";
        LogToConsole($"Error resigning game: {ex.Message}", ex);
      }
    }
  }
}