using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChessServer;
using ChessClient.Xiangqi;

namespace ChessServer.Services
{
    public class ChessServiceImpl : ChessService.ChessServiceBase
    {
        private readonly Dictionary<string, string> players = new Dictionary<string, string>(); // color -> player_id
        private XiangqiGame game;

        private static void LogToConsole(string message, Exception ex = null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[Server] [{timestamp}] {message}";
            if (ex != null)
                logMessage += $"\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Console.WriteLine(logMessage);
        }

        public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Player {request.PlayerId} attempting to connect.");
                if (string.IsNullOrEmpty(request.PlayerId))
                    return Task.FromResult(new ConnectResponse { Message = "Invalid PlayerId" });

                var existingColor = players.FirstOrDefault(p => p.Value == request.PlayerId).Key;
                if (existingColor != null)
                {
                    players.Remove(existingColor);
                    LogToConsole($"Removed existing connection for PlayerId: {request.PlayerId}");
                }

                if (players.Count >= 2)
                    return Task.FromResult(new ConnectResponse { Message = "Server is full" });

                string color = players.Count == 0 ? "red" : "black";
                players[color] = request.PlayerId;
                LogToConsole($"Player {request.PlayerId} connected as {color}");

                return Task.FromResult(new ConnectResponse
                {
                    Color = color,
                    Message = "Connected successfully"
                });
            }
            catch (Exception ex)
            {
                LogToConsole($"Error handling connect request: {ex.Message}", ex);
                return Task.FromResult(new ConnectResponse { Message = "Error connecting." });
            }
        }

        public override Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Player {request.PlayerId} requesting to start game.");
                if (players.Count < 2)
                    return Task.FromResult(new StartGameResponse { Message = "Not enough players" });

                game = new XiangqiGame();
                LogToConsole("New Xiangqi game started.");
                return Task.FromResult(new StartGameResponse { Message = "Game started" });
            }
            catch (Exception ex)
            {
                LogToConsole($"Error starting game: {ex.Message}", ex);
                return Task.FromResult(new StartGameResponse { Message = "Error starting game." });
            }
        }

        public override Task<MoveResponse> MakeMove(MoveRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Processing move from {request.From} to {request.To}...");

                // Lấy PlayerId từ metadata
                string playerId = context.RequestHeaders.FirstOrDefault(h => h.Key == "PlayerId")?.Value;
                if (string.IsNullOrEmpty(playerId))
                {
                    LogToConsole("Missing PlayerId in request headers.");
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Missing PlayerId in request"
                    });
                }

                if (game == null)
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Game not started"
                    });

                if (!TryParsePosition(request.From, out XiangqiPosition from) || !TryParsePosition(request.To, out XiangqiPosition to))
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Invalid position format"
                    });

                var piece = game.GetPieceAt(from);
                if (piece == null)
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "No piece at starting position"
                    });

                // Kiểm tra xem có phải lượt của người chơi hiện tại không
                if (piece.Owner != game.WhoseTurn)
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "It's not your turn"
                    });

                // Kiểm tra xem người chơi có đang di chuyển quân cờ của mình không
                string playerColor = null;
                foreach (var entry in players)
                {
                    if (entry.Value == playerId)
                    {
                        playerColor = entry.Key;
                        break;
                    }
                }

                if (playerColor == null)
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Player not connected"
                    });

                if ((playerColor == "red" && piece.Owner != Player.Red) ||
                    (playerColor == "black" && piece.Owner != Player.Black))
                {
                    return Task.FromResult(new MoveResponse
                    {
                        Success = false,
                        Message = "You can only move your own pieces"
                    });
                }

                XiangqiMove move = new XiangqiMove(from, to, game.WhoseTurn);
                if (game.IsValidMove(move))
                {
                    game.MakeMove(move, true);
                    string message = "Move accepted";

                    if (game.IsInCheck(game.WhoseTurn))
                        message = "Check!";
                    if (game.IsCheckmate(game.WhoseTurn))
                        return Task.FromResult(new MoveResponse
                        {
                            Success = true,
                            Message = "Checkmate! Game over"
                        });
                    if (game.IsStalemate(game.WhoseTurn))
                        return Task.FromResult(new MoveResponse
                        {
                            Success = true,
                            Message = "Stalemate! Game over"
                        });

                    LogToConsole($"Move accepted: {request.From} to {request.To}.");
                    return Task.FromResult(new MoveResponse
                    {
                        Success = true,
                        Message = message
                    });
                }

                LogToConsole("Move rejected: invalid move.");
                return Task.FromResult(new MoveResponse
                {
                    Success = false,
                    Message = "Invalid move"
                });
            }
            catch (Exception ex)
            {
                LogToConsole($"Error processing move: {ex.Message}", ex);
                return Task.FromResult(new MoveResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
        public override Task<ResignResponse> Resign(ResignRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Player {request.PlayerId} attempting to resign...");
                if (string.IsNullOrEmpty(request.PlayerId))
                    return Task.FromResult(new ResignResponse { Message = "Invalid PlayerId" });

                if (game == null)
                    return Task.FromResult(new ResignResponse { Message = "Game not started" });

                var player = players.FirstOrDefault(x => x.Value == request.PlayerId);
                if (player.Key == null)
                    return Task.FromResult(new ResignResponse { Message = "Player not found" });

                string color = player.Key;
                game = null;
                players.Clear();
                LogToConsole($"Player {color} resigned. Game reset.");
                return Task.FromResult(new ResignResponse { Message = $"{color} resigned. Game over" });
            }
            catch (Exception ex)
            {
                LogToConsole($"Error processing resign request: {ex.Message}", ex);
                return Task.FromResult(new ResignResponse { Message = "Error resigning." });
            }
        }

        public override async Task GetGameState(GameStateRequest request, IServerStreamWriter<GameStateResponse> responseStream, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Streaming game state for player {request.PlayerId}...");
                if (string.IsNullOrEmpty(request.PlayerId))
                {
                    await responseStream.WriteAsync(new GameStateResponse { Message = "Invalid PlayerId" });
                    return;
                }

                if (!players.ContainsValue(request.PlayerId))
                {
                    await responseStream.WriteAsync(new GameStateResponse { Message = "Player not connected" });
                    return;
                }

                while (!context.CancellationToken.IsCancellationRequested)
                {
                    if (game == null)
                    {
                        await responseStream.WriteAsync(new GameStateResponse { Message = "Game not started" });
                    }
                    else
                    {
                        await responseStream.WriteAsync(new GameStateResponse
                        {
                            Fen = game.GetFen(),
                            CurrentTurn = game.WhoseTurn.ToString().ToLower(),
                            IsCheck = game.IsInCheck(game.WhoseTurn),
                            IsCheckmate = game.IsCheckmate(game.WhoseTurn),
                            IsStalemate = game.IsStalemate(game.WhoseTurn)
                        });
                    }
                    await Task.Delay(3000);
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error streaming game state: {ex.Message}", ex);
            }
        }

        private bool TryParsePosition(string notation, out XiangqiPosition position)
        {
            try
            {
                position = null;
                if (string.IsNullOrEmpty(notation) || notation.Length != 2)
                    return false;

                if (!int.TryParse(notation[0].ToString(), out int file) || file < 1 || file > 9)
                    return false;

                if (!int.TryParse(notation[1].ToString(), out int rank) || rank < 0 || rank > 9)
                    return false;

                position = new XiangqiPosition(file, rank);
                LogToConsole($"Parsed position: {notation}");
                return true;
            }
            catch (Exception ex)
            {
                LogToConsole($"Error parsing position: {notation}", ex);
                position = null;
                return false;
            }
        }
    }
}