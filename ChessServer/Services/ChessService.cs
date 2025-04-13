using ChessDotNet;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessServer;
using File = ChessDotNet.File;

namespace ChessServer.Services
{
  public class ChessServiceImpl : ChessService.ChessServiceBase
  {
    private readonly Dictionary<string, string> players = new Dictionary<string, string>(); // color -> player_id
    private ChessGame game;

    private static void LogToConsole(string message, Exception ex = null)
    {
      string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
      string logMessage = $"[Server] [{timestamp}] {message}";
      if (ex != null)
      {
        logMessage += $"\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
      }
      Console.WriteLine(logMessage);
    }

    public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
    {
      try
      {
        LogToConsole($"Player {request.PlayerId} attempting to connect. Current players: {players.Count} ({string.Join(", ", players.Select(p => $"{p.Key}: {p.Value}"))})");

        // Kiểm tra PlayerId hợp lệ
        if (string.IsNullOrEmpty(request.PlayerId))
        {
          LogToConsole("Connection rejected: PlayerId is null or empty.");
          return Task.FromResult(new ConnectResponse { Message = "Invalid PlayerId" });
        }

        // Xóa PlayerId cũ nếu đã tồn tại (xử lý kết nối lại)
        var existingColor = players.FirstOrDefault(p => p.Value == request.PlayerId).Key;
        if (existingColor != null)
        {
          players.Remove(existingColor);
          LogToConsole($"Removed existing connection for PlayerId: {request.PlayerId} (was {existingColor})");
        }

        // Kiểm tra server đầy
        if (players.Count >= 2)
        {
          LogToConsole($"Connection rejected for {request.PlayerId}: server is full.");
          return Task.FromResult(new ConnectResponse { Message = "Server is full" });
        }

        // Gán màu mới
        string color = players.Count == 0 ? "white" : "black";
        players[color] = request.PlayerId;
        LogToConsole($"Player {request.PlayerId} connected as {color}. Total players: {players.Count} ({string.Join(", ", players.Select(p => $"{p.Key}: {p.Value}"))})");

        return Task.FromResult(new ConnectResponse
        {
          Color = color,
          Message = "Connected successfully"
        });
      }
      catch (Exception ex)
      {
        LogToConsole($"Error handling connect request for {request.PlayerId}.", ex);
        return Task.FromResult(new ConnectResponse { Message = "Error connecting." });
      }
    }
    public override Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
    {
      try
      {
        LogToConsole($"Player {request.PlayerId} requesting to start game. Current players: {players.Count} ({string.Join(", ", players.Select(p => $"{p.Key}: {p.Value}"))})");
        if (players.Count < 2)
        {
          LogToConsole($"Game start rejected: not enough players (need 2, have {players.Count}).");
          return Task.FromResult(new StartGameResponse { Message = "Not enough players" });
        }

        game = new ChessGame();
        LogToConsole("New game started.");
        return Task.FromResult(new StartGameResponse { Message = "Game started" });
      }
      catch (Exception ex)
      {
        LogToConsole("Error starting game.", ex);
        return Task.FromResult(new StartGameResponse { Message = "Error starting game." });
      }
    }

    public override Task<MoveResponse> MakeMove(MoveRequest request, ServerCallContext context)
    {
      try
      {
        LogToConsole($"Processing move from {request.From} to {request.To}...");
        if (game == null)
        {
          LogToConsole("Move rejected: game not started.");
          return Task.FromResult(new MoveResponse { Success = false, Message = "Game not started" });
        }

        if (!TryParsePosition(request.From, out Position from) || !TryParsePosition(request.To, out Position to))
        {
          LogToConsole("Move rejected: invalid position format.");
          return Task.FromResult(new MoveResponse { Success = false, Message = "Invalid position format" });
        }

        // Check if a piece exists at the 'from' position
        var piece = game.GetPieceAt(from);
        if (piece == null)
        {
          LogToConsole($"Move rejected: no piece at position {request.From}.");
          return Task.FromResult(new MoveResponse { Success = false, Message = "No piece at starting position" });
        }

        // Check if the piece belongs to the current player
        if (piece.Owner != game.WhoseTurn)
        {
          LogToConsole($"Move rejected: it's not {piece.Owner}'s turn.");
          return Task.FromResult(new MoveResponse { Success = false, Message = "It's not your turn" });
        }

        char? promotion = null;
        if (!string.IsNullOrEmpty(request.Promotion))
        {
          switch (request.Promotion.ToLower())
          {
            case "queen":
              promotion = 'Q';
              break;
            case "rook":
              promotion = 'R';
              break;
            case "bishop":
              promotion = 'B';
              break;
            case "knight":
              promotion = 'N';
              break;
            default:
              LogToConsole("Move rejected: invalid promotion type.");
              return Task.FromResult(new MoveResponse { Success = false, Message = "Invalid promotion type" });
          }
        }

        Move move = new Move(from, to, game.WhoseTurn, promotion);
        bool isValid = game.IsValidMove(move);

        if (isValid)
        {
          // Check if this is a pawn moving to the last rank without promotion specified
          bool needsPromotion = piece is ChessDotNet.Pieces.Pawn &&
                               ((game.WhoseTurn == Player.White && to.Rank == 8) ||
                                (game.WhoseTurn == Player.Black && to.Rank == 1));

          if (needsPromotion && promotion == null)
          {
            LogToConsole("Move rejected: pawn promotion required.");
            return Task.FromResult(new MoveResponse { Success = false, Message = "Pawn promotion required" });
          }

          // Check if there's a piece at the destination (capture)
          bool isCapture = game.GetPieceAt(to) != null;

          // Make the move
          game.MakeMove(move, true);

          string message = "Move accepted";

          // Check if the opponent is in check
          Player opponent = game.WhoseTurn; // After MakeMove, WhoseTurn is the opponent
          if (game.IsInCheck(opponent))
          {
            message = "Check!";
          }

          LogToConsole($"Move accepted: {request.From} to {request.To}.");

          if (game.IsCheckmated(opponent))
          {
            LogToConsole("Game over: checkmate.");
            return Task.FromResult(new MoveResponse
            {
              Success = true,
              Message = "Checkmate! Game over"
            });
          }
          else if (game.IsStalemated(opponent))
          {
            LogToConsole("Game over: stalemate.");
            return Task.FromResult(new MoveResponse
            {
              Success = true,
              Message = "Stalemate! Game over"
            });
          }

          return Task.FromResult(new MoveResponse { Success = true, Message = message });
        }

        LogToConsole("Move rejected: invalid move.");
        return Task.FromResult(new MoveResponse { Success = false, Message = "Invalid move" });
      }
      catch (Exception ex)
      {
        LogToConsole("Error processing move.", ex);
        return Task.FromResult(new MoveResponse { Success = false, Message = $"Error: {ex.Message}" });
      }
    }
    public override Task<ResignResponse> Resign(ResignRequest request, ServerCallContext context)
    {
      try
      {
        LogToConsole($"Player {request.PlayerId} attempting to resign...");

        // Kiểm tra PlayerId hợp lệ
        if (string.IsNullOrEmpty(request.PlayerId))
        {
          LogToConsole("Resign rejected: PlayerId is null or empty.");
          return Task.FromResult(new ResignResponse { Message = "Invalid PlayerId" });
        }

        // Kiểm tra game đã bắt đầu
        if (game == null)
        {
          LogToConsole("Resign rejected: game not started.");
          return Task.FromResult(new ResignResponse { Message = "Game not started" });
        }

        // Tìm người chơi
        var player = players.FirstOrDefault(x => x.Value == request.PlayerId);
        if (player.Key == null)
        {
          LogToConsole($"Resign rejected: PlayerId {request.PlayerId} not found. Current players: {string.Join(", ", players.Select(p => $"{p.Key}: {p.Value}"))}");
          return Task.FromResult(new ResignResponse { Message = "Player not found" });
        }

        string color = player.Key;
        game = null;
        players.Clear(); // Xóa danh sách người chơi để bắt đầu lại
        LogToConsole($"Player {color} resigned. Game reset.");
        return Task.FromResult(new ResignResponse { Message = $"{color} resigned. Game over" });
      }
      catch (Exception ex)
      {
        LogToConsole($"Error processing resign request for PlayerId: {request.PlayerId}.", ex);
        return Task.FromResult(new ResignResponse { Message = "Error resigning." });
      }
    }
    public override async Task GetGameState(GameStateRequest request, IServerStreamWriter<GameStateResponse> responseStream, ServerCallContext context)
    {
      try
      {
        LogToConsole($"Streaming game state for player {request.PlayerId}...");
        string playerId = request.PlayerId;

        // Kiểm tra PlayerId hợp lệ
        if (string.IsNullOrEmpty(playerId))
        {
          LogToConsole("Invalid request: PlayerId is null or empty.");
          await responseStream.WriteAsync(new GameStateResponse { Message = "Invalid PlayerId" });
          return;
        }

        // Kiểm tra người chơi có trong danh sách
        if (!players.ContainsValue(playerId))
        {
          LogToConsole($"Player {playerId} not found in connected players list. Current players: {string.Join(", ", players.Select(p => $"{p.Key}: {p.Value}"))}");
          await responseStream.WriteAsync(new GameStateResponse { Message = "Player not connected" });
          return;
        }

        // Vòng lặp để gửi cập nhật trạng thái game
        while (!context.CancellationToken.IsCancellationRequested)
        {
          if (game == null)
          {
            await responseStream.WriteAsync(new GameStateResponse { Message = "Game not started" });
            LogToConsole($"Game state stream for PlayerId {playerId}: game not started.");
          }
          else
          {
            var response = new GameStateResponse
            {
              Fen = game.GetFen(),
              CurrentTurn = game.WhoseTurn.ToString().ToLower(),
              IsCheck = game.IsInCheck(game.WhoseTurn),
              IsCheckmate = game.IsCheckmated(game.WhoseTurn),
              IsStalemate = game.IsStalemated(game.WhoseTurn)
            };

            await responseStream.WriteAsync(response);
            LogToConsole($"Game state sent to PlayerId {playerId}: FEN={response.Fen}, Turn={response.CurrentTurn}");
          }

          // Delay giữa các cập nhật
          await Task.Delay(3000);
        }
      }
      catch (Exception ex)
      {
        LogToConsole($"Error streaming game state for PlayerId: {request.PlayerId}.", ex);
      }
    }
    private bool TryParsePosition(string algebraic, out Position position)
    {
      try
      {
        position = default;
        if (string.IsNullOrEmpty(algebraic) || algebraic.Length != 2)
        {
          LogToConsole($"Invalid position format: {algebraic}");
          return false;
        }

        char file = algebraic[0];
        if (file < 'a' || file > 'h')
        {
          LogToConsole($"Invalid file in position: {file}");
          return false;
        }

        if (!int.TryParse(algebraic[1].ToString(), out int rank) || rank < 1 || rank > 8)
        {
          LogToConsole($"Invalid rank in position: {algebraic[1]}");
          return false;
        }

        position = new Position((File)(file - 'a'), rank); // File.A = 0, ..., File.H = 7
        LogToConsole($"Parsed position: {algebraic}, File={(int)position.File}, Rank={position.Rank}");
        return true;
      }
      catch (Exception ex)
      {
        LogToConsole($"Error parsing position: {algebraic}", ex);
        position = default;
        return false;
      }
    }
  }
}