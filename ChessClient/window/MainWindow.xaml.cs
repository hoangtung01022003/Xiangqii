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
        LogToConsole("MainWindow khởi tạo thành công.");
      }
      catch (Exception ex)
      {
        LogToConsole("Lỗi khi khởi tạo MainWindow.", ex);
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
        LogToConsole("Khởi tạo bàn cờ Xiangqi...");
        ChessBoard.Children.Clear();

        double squareWidth = 450.0 / 9; // 50px mỗi cột
        double squareHeight = 500.0 / 10; // 50px mỗi hàng

        // Tạo các ô hình ảnh cho quân cờ
        for (int row = 0; row < 10; row++)
        {
          for (int col = 0; col < 9; col++)
          {
            Image img = new Image
            {
              Tag = new XiangqiPosition(col + 1, row),
              Width = 50,
              Height = 50,
              Stretch = Stretch.Uniform,
              IsHitTestVisible = true
            };
            Canvas.SetLeft(img, col * squareWidth + (squareWidth - 50) / 2);
            Canvas.SetTop(img, row * squareHeight + (squareHeight - 50) / 2);
            Canvas.SetZIndex(img, 100);
            ChessBoard.Children.Add(img);
          }
        }

        ChessBoard.MouseDown += ChessBoard_MouseDown;
        LogToConsole("Bàn cờ Xiangqi đã được khởi tạo.");
      }
      catch (Exception ex)
      {
        LogToConsole("Lỗi khi khởi tạo bàn cờ Xiangqi.", ex);
      }
    }
    private void LoadPieceImages()
    {
      try
      {
        LogToConsole("Đang tải hình ảnh quân cờ Xiangqi...");
        var pieceMappings = new Dictionary<string, string>
        {
            { "rk", "Images/rk.png" }, // Tướng đỏ
            { "ra", "Images/ra.png" }, // Sĩ đỏ
            { "rn", "Images/rn.png" }, // Tượng đỏ
            { "rr", "Images/rr.png" }, // Xe đỏ
            { "rb", "Images/rb.png" }, // Pháo đỏ
            { "rc", "Images/rc.png" }, // Mã đỏ
            { "rp", "Images/rp.png" }, // Tốt đỏ
            { "bk", "Images/bk.png" }, // Tướng đen
            { "ba", "Images/ba.png" }, // Sĩ đen
            { "bn", "Images/bn.png" }, // Tượng đen
            { "br", "Images/br.png" }, // Xe đen
            { "bb", "Images/bb.png" }, // Pháo đen
            { "bc", "Images/bc.png" }, // Mã đen
            { "bp", "Images/bp.png" }  // Tốt đen
        };

        pieceImages.Clear();
        foreach (var piece in pieceMappings)
        {
          try
          {
            var uri = new Uri($"pack://application:,,,/{piece.Value}", UriKind.Absolute);
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = uri;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            pieceImages[piece.Key] = image;
            LogToConsole($"Đã tải hình ảnh cho {piece.Key}: {piece.Value}");
          }
          catch (Exception ex)
          {
            LogToConsole($"Không tải được hình ảnh: {piece.Value}. Bỏ qua...", ex);
            continue;
          }
        }

        LogToConsole($"Đã tải {pieceImages.Count} hình ảnh quân cờ.");
        if (pieceImages.Count < pieceMappings.Count)
        {
          StatusTextBlock.Text = $"Cảnh báo: Chỉ tải được {pieceImages.Count}/{pieceMappings.Count} hình ảnh quân cờ.";
          LogToConsole($"Lỗi: Không tải đủ hình ảnh, chỉ có {pieceImages.Count} hình ảnh được tải.");
        }
      }
      catch (Exception ex)
      {
        LogToConsole("Lỗi nghiêm trọng khi tải hình ảnh quân cờ.", ex);
        StatusTextBlock.Text = "Lỗi: Không tải được hình ảnh quân cờ.";
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
          StatusTextBlock.Text = "Game not started. Please click 'New Game'.";
          LogToConsole("Game not started: currentFen is empty.");
          return;
        }

        var targetPiece = new XiangqiGame(currentFen).GetPieceAt(pos);

        // Nếu đã chọn một quân cờ trước đó, đây là ô đích
        if (selectedPosition != null)
        {
          var from = selectedPosition;
          var to = pos;
          var game = new XiangqiGame(currentFen);
          var move = new XiangqiMove(from, to, playerColor == "red" ? Player.Red : Player.Black);

          if (game.IsValidMove(move))
          {
            // Gửi yêu cầu di chuyển đến server
            var request = new MoveRequest { From = from.ToNotation(), To = to.ToNotation() };
            var response = await client.MakeMoveAsync(request);
            LogToConsole($"Moved from {from.ToNotation()} to {to.ToNotation()}: {response.Success} - {response.Message}");

            if (response.Success)
            {
              StatusTextBlock.Text = "Move successful. Waiting for opponent's turn...";
            }
            else
            {
              StatusTextBlock.Text = $"Move failed: {response.Message}";
            }
          }
          else
          {
            StatusTextBlock.Text = "Invalid move.";
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
          StatusTextBlock.Text = $"Selected {posStr}. Choose destination.";
        }
        else
        {
          StatusTextBlock.Text = targetPiece == null ? $"No piece at {posStr}." :
                                 isMyTurn ? "Select your piece." : "Not your turn.";
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
        if (state == null || string.IsNullOrEmpty(state.Fen))
        {
          StatusTextBlock.Text = "Trò chơi chưa bắt đầu. Nhấn 'New Game' để bắt đầu.";
          currentFen = new XiangqiGame().GetFen(); // Sử dụng FEN mặc định
          LogToConsole("Trạng thái trò chơi rỗng hoặc FEN không hợp lệ. Sử dụng FEN mặc định.");
        }
        else
        {
          currentFen = state.Fen;
          LogToConsole($"Cập nhật trạng thái trò chơi với FEN: {currentFen}");
        }

        var game = new XiangqiGame(currentFen);

        ChessBoard.Children.OfType<Image>().ToList().ForEach(img => img.Source = null);

        int piecesRendered = 0;
        for (int row = 0; row < 10; row++)
        {
          for (int col = 0; col < 9; col++)
          {
            XiangqiPosition pos = new XiangqiPosition(col + 1, row);
            XiangqiPiece piece = game.GetPieceAt(pos);
            var img = ChessBoard.Children
                .OfType<Image>()
                .FirstOrDefault(i => i.Tag is XiangqiPosition p &&
                                     p.File == col + 1 && p.Rank == row);
            if (img != null)
            {
              img.Source = GetPieceImage(piece);
              if (img.Source != null)
              {
                piecesRendered++;
                // LogToConsole($"Đã gán hình ảnh tại {pos.ToNotation()}: {piece?.GetFenCharacter() ?? "trống"}");
              }
              else if (piece != null)
              {
                LogToConsole($"Không tìm thấy hình ảnh cho quân cờ tại {pos.ToNotation()}: {piece.GetFenCharacter()}");
              }
              img.Width = 50;
              img.Height = 50;
              Canvas.SetZIndex(img, 100);
            }
          }
        }

        LogToConsole($"Đã hiển thị {piecesRendered} quân cờ.");
        if (piecesRendered == 0)
        {
          StatusTextBlock.Text = "Lỗi: Không hiển thị được quân cờ. Kiểm tra hình ảnh.";
        }

        if (state != null)
        {
          isMyTurn = state.CurrentTurn.ToLower() == playerColor && !state.IsCheckmate && !state.IsStalemate;
          StatusTextBlock.Text = $"Lượt hiện tại: {(state.CurrentTurn ?? "chưa xác định")}";
          if (state.IsCheck) StatusTextBlock.Text += " - Chiếu!";
          if (state.IsCheckmate) StatusTextBlock.Text = "Chiếu hết! Kết thúc trò chơi.";
          else if (state.IsStalemate) StatusTextBlock.Text = "Hòa cờ! Kết thúc trò chơi.";
          else if (state.Message.Contains("Draw")) StatusTextBlock.Text = "Hòa! Kết thúc trò chơi.";
        }

        LogToConsole($"Trạng thái cập nhật: isMyTurn={isMyTurn}, CurrentTurn={(state?.CurrentTurn ?? "null")}");
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Lỗi khi cập nhật trạng thái trò chơi.";
        LogToConsole($"Lỗi cập nhật trạng thái trò chơi: {ex.Message}", ex);
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
        string ipAddress = ServerIpTextBox.Text;
        int port = 5038;

        playerId = Guid.NewGuid().ToString();
        LogToConsole($"Đang kết nối tới http://{ipAddress}:{port}");

        StatusTextBlock.Text = $"Đang kết nối đến {ipAddress}:{port}...";
        ConnectButton.IsEnabled = false;

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var options = new GrpcChannelOptions
        {
          MaxReceiveMessageSize = 16 * 1024 * 1024,
          MaxSendMessageSize = 16 * 1024 * 1024
        };

        channel = GrpcChannel.ForAddress($"http://{ipAddress}:{port}", options);
        client = new ChessService.ChessServiceClient(channel);

        var connectResponse = await client.ConnectAsync(new ConnectRequest { PlayerId = playerId });

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

        // Fetch initial game state using the streaming RPC
        try
        {
          using var call = client.GetGameState(new GameStateRequest { PlayerId = playerId });
          var responseStream = call.ResponseStream;
          if (await responseStream.MoveNext()) // Wait for the first state
          {
            var initialState = responseStream.Current;
            Dispatcher.Invoke(() => UpdateGameState(initialState));
          }
          else
          {
            LogToConsole("Không nhận được trạng thái ban đầu từ server.");
            StatusTextBlock.Text = "Đã kết nối, nhưng không nhận được trạng thái trò chơi.";
          }
        }
        catch (Exception ex)
        {
          LogToConsole($"Lỗi khi lấy trạng thái ban đầu: {ex.Message}", ex);
          StatusTextBlock.Text = "Đã kết nối, nhưng không thể lấy trạng thái trò chơi.";
        }

        // Start streaming game state updates in the background
        _ = Task.Run(async () =>
        {
          try
          {
            using var call = client.GetGameState(new GameStateRequest { PlayerId = playerId });
            await foreach (var state in call.ResponseStream.ReadAllAsync())
            {
              Dispatcher.Invoke(() => UpdateGameState(state));
            }
          }
          catch (Exception ex)
          {
            Dispatcher.Invoke(() =>
            {
              StatusTextBlock.Text = "Mất kết nối với máy chủ.";
              LogToConsole($"Lỗi trong luồng trạng thái trò chơi: {ex.Message}", ex);
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
        var response = await client.StartGameAsync(new StartGameRequest { PlayerId = playerId });
        StatusTextBlock.Text = response.Message;
      }
      catch (Exception ex)
      {
        StatusTextBlock.Text = "Error starting game.";
        LogToConsole($"Error starting game: {ex.Message}", ex);
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