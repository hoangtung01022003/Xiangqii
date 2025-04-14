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
        // Danh sách các luồng phát trực tiếp đang hoạt động
        private readonly List<IServerStreamWriter<GameStateResponse>> activeStreams = new List<IServerStreamWriter<GameStateResponse>>();

        private static void LogToConsole(string message, Exception ex = null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"[Server] [{timestamp}] {message}";
            if (ex != null)
                logMessage += $"\nError: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Console.WriteLine(logMessage);
        }

        // Phương thức để phát sóng trạng thái trò chơi đến tất cả người chơi
        private async Task BroadcastGameState()
        {
            try
            {
                if (game == null)
                {
                    foreach (var stream in activeStreams)
                    {
                        try
                        {
                            await stream.WriteAsync(new GameStateResponse { Message = "Game not started" });
                            LogToConsole($"Gửi trạng thái 'Game not started' tới luồng.");
                        }
                        catch
                        {
                            // Bỏ qua lỗi
                        }
                    }
                    return;
                }

                var response = new GameStateResponse
                {
                    Fen = game.GetFen(),
                    CurrentTurn = game.WhoseTurn.ToString().ToLower(),
                    IsCheck = game.IsInCheck(game.WhoseTurn),
                    IsCheckmate = game.IsCheckmate(game.WhoseTurn),
                    IsStalemate = game.IsStalemate(game.WhoseTurn)
                };

                if (game.IsCheckmate(game.WhoseTurn))
                {
                    response.Message = $"Checkmate! {(game.WhoseTurn == Player.Red ? "Black" : "Red")} wins!";
                }
                else if (game.IsStalemate(game.WhoseTurn))
                {
                    response.Message = "Stalemate! Game is a draw.";
                }
                else if (game.IsInCheck(game.WhoseTurn))
                {
                    response.Message = "Check!";
                }

                var failedStreams = new List<IServerStreamWriter<GameStateResponse>>();
                foreach (var stream in activeStreams)
                {
                    try
                    {
                        await stream.WriteAsync(response);
                        LogToConsole($"Gửi cập nhật trạng thái: FEN={response.Fen}, Turn={response.CurrentTurn}");
                    }
                    catch (Exception ex)
                    {
                        LogToConsole($"Không gửi được tới luồng: {ex.Message}");
                        failedStreams.Add(stream);
                    }
                }

                if (failedStreams.Count > 0)
                {
                    lock (activeStreams)
                    {
                        foreach (var stream in failedStreams)
                        {
                            activeStreams.Remove(stream);
                        }
                    }
                    LogToConsole($"Đã xóa {failedStreams.Count} luồng lỗi. Còn {activeStreams.Count} luồng.");
                }

                LogToConsole($"Đã phát trạng thái trò chơi tới {activeStreams.Count} người chơi. Turn: {response.CurrentTurn}, Check: {response.IsCheck}, Checkmate: {response.IsCheckmate}");
            }
            catch (Exception ex)
            {
                LogToConsole($"Lỗi khi phát trạng thái trò chơi: {ex.Message}", ex);
            }
        }
        public override Task<ConnectResponse> Connect(ConnectRequest request, ServerCallContext context)
        {
            try
            {
                var clientIp = context.GetHttpContext().Connection.RemoteIpAddress?.ToString() ?? "unknown";
                LogToConsole($"Người chơi {request.PlayerId} đang cố gắng kết nối từ {clientIp}");

                if (string.IsNullOrEmpty(request.PlayerId))
                    return Task.FromResult(new ConnectResponse { Message = "ID người chơi không hợp lệ" });

                var existingColor = players.FirstOrDefault(p => p.Value == request.PlayerId).Key;
                if (existingColor != null)
                {
                    // Người chơi đã kết nối trước đó - giữ nguyên màu sắc
                    LogToConsole($"Người chơi {request.PlayerId} được kết nối lại với màu {existingColor}");
                    return Task.FromResult(new ConnectResponse
                    {
                        Color = existingColor,
                        Message = "Kết nối lại thành công"
                    });
                }

                if (players.Count >= 2)
                    return Task.FromResult(new ConnectResponse { Message = "Máy chủ đã đủ người chơi" });

                string color = players.Count == 0 ? "red" : "black";
                players[color] = request.PlayerId;
                LogToConsole($"Người chơi {request.PlayerId} đã kết nối với màu {color} từ {clientIp}");

                return Task.FromResult(new ConnectResponse
                {
                    Color = color,
                    Message = "Kết nối thành công"
                });
            }
            catch (Exception ex)
            {
                LogToConsole($"Lỗi khi xử lý yêu cầu kết nối: {ex.Message}", ex);
                return Task.FromResult(new ConnectResponse { Message = "Lỗi kết nối." });
            }
        }

        public override async Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Người chơi {request.PlayerId} yêu cầu bắt đầu trò chơi.");

                if (players.Count < 2)
                    return new StartGameResponse { Message = "Không đủ người chơi" };

                game = new XiangqiGame();
                LogToConsole("Ván cờ tướng mới đã bắt đầu.");

                // Thông báo cho tất cả người chơi về trạng thái trò chơi mới
                await BroadcastGameState();

                return new StartGameResponse { Message = "Trò chơi đã bắt đầu" };
            }
            catch (Exception ex)
            {
                LogToConsole($"Lỗi khi bắt đầu trò chơi: {ex.Message}", ex);
                return new StartGameResponse { Message = "Lỗi khi bắt đầu trò chơi." };
            }
        }

        public override async Task<MoveResponse> MakeMove(MoveRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Processing move from {request.From} to {request.To}...");
                string clientIp = context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Kiểm tra xem trò chơi đã bắt đầu chưa
                if (game == null)
                {
                    LogToConsole($"Move rejected: Game not started");
                    return new MoveResponse { Success = false, Message = "Game not started" };
                }

                // Phân tích vị trí
                if (!TryParsePosition(request.From, out XiangqiPosition from) || !TryParsePosition(request.To, out XiangqiPosition to))
                {
                    LogToConsole($"Move rejected: Invalid position format");
                    return new MoveResponse { Success = false, Message = "Invalid position format" };
                }

                // Kiểm tra xem có quân cờ tại vị trí bắt đầu không
                var piece = game.GetPieceAt(from);
                if (piece == null)
                {
                    LogToConsole($"Move rejected: No piece at starting position {request.From}");
                    return new MoveResponse { Success = false, Message = "No piece at starting position" };
                }

                // Kiểm tra xem có đúng lượt không
                if (piece.Owner != game.WhoseTurn)
                {
                    LogToConsole($"Move rejected: It's not {piece.Owner}'s turn. Current turn: {game.WhoseTurn}");
                    return new MoveResponse { Success = false, Message = "It's not your turn" };
                }

                // Lấy PlayerId từ header nếu có
                string playerId = context.RequestHeaders.FirstOrDefault(h => h.Key == "PlayerId")?.Value ?? "";

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

                if (!string.IsNullOrEmpty(playerId) && playerColor == null)
                {
                    LogToConsole($"Move rejected: Unknown player ID {playerId}");
                    return new MoveResponse { Success = false, Message = "Player not connected" };
                }

                if (!string.IsNullOrEmpty(playerColor) &&
                    ((playerColor == "red" && piece.Owner != Player.Red) ||
                     (playerColor == "black" && piece.Owner != Player.Black)))
                {
                    LogToConsole($"Move rejected: Player {playerId} ({playerColor}) tried to move opponent's piece");
                    return new MoveResponse { Success = false, Message = "You can only move your own pieces" };
                }

                // Kiểm tra nước đi có hợp lệ không
                XiangqiMove move = new XiangqiMove(from, to, game.WhoseTurn);
                if (game.IsValidMove(move))
                {
                    // Thực hiện nước đi
                    game.MakeMove(move, true);

                    string message = "Move accepted";
                    bool gameOver = false;

                    // Kiểm tra các điều kiện đặc biệt
                    if (game.IsInCheck(game.WhoseTurn))
                        message = "Check!";

                    if (game.IsCheckmate(game.WhoseTurn))
                    {
                        message = "Checkmate! Game over";
                        gameOver = true;
                    }
                    else if (game.IsStalemate(game.WhoseTurn))
                    {
                        message = "Stalemate! Game over";
                        gameOver = true;
                    }

                    LogToConsole($"Move accepted: {request.From} to {request.To}. {message}");

                    // Cập nhật trạng thái trò chơi cho tất cả người chơi
                    await BroadcastGameState();

                    // Nếu trò chơi kết thúc, sẵn sàng cho ván mới
                    if (gameOver)
                    {
                        // Giữ nguyên trạng thái game để có thể xem lại 
                        // nhưng sau một khoảng thời gian, sẽ thiết lập lại
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(10000); // Đợi 10 giây
                            game = null;
                            LogToConsole("Game reset after game over");
                            await BroadcastGameState();
                        });
                    }

                    return new MoveResponse { Success = true, Message = message };
                }

                LogToConsole($"Move rejected: Invalid move from {request.From} to {request.To}");
                return new MoveResponse { Success = false, Message = "Invalid move" };
            }
            catch (Exception ex)
            {
                LogToConsole($"Error processing move: {ex.Message}", ex);
                return new MoveResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
        public override async Task<ResignResponse> Resign(ResignRequest request, ServerCallContext context)
        {
            try
            {
                LogToConsole($"Người chơi {request.PlayerId} đang cố gắng đầu hàng...");

                if (string.IsNullOrEmpty(request.PlayerId))
                    return new ResignResponse { Message = "ID người chơi không hợp lệ" };

                if (game == null)
                    return new ResignResponse { Message = "Trò chơi chưa bắt đầu" };

                var player = players.FirstOrDefault(x => x.Value == request.PlayerId);
                if (player.Key == null)
                    return new ResignResponse { Message = "Không tìm thấy người chơi" };

                string color = player.Key;
                game = null; // Đặt lại trò chơi

                string message = $"{color} đã đầu hàng. Trò chơi kết thúc";
                LogToConsole($"Người chơi {color} đã đầu hàng. Trò chơi đã đặt lại.");

                // Thông báo cho tất cả người chơi về việc đầu hàng
                foreach (var stream in activeStreams)
                {
                    try
                    {
                        await stream.WriteAsync(new GameStateResponse { Message = message });
                    }
                    catch
                    {
                        // Bỏ qua lỗi cho từng luồng
                    }
                }

                return new ResignResponse { Message = message };
            }
            catch (Exception ex)
            {
                LogToConsole($"Lỗi khi xử lý yêu cầu đầu hàng: {ex.Message}", ex);
                return new ResignResponse { Message = "Lỗi khi đầu hàng." };
            }
        }

        public override async Task GetGameState(GameStateRequest request, IServerStreamWriter<GameStateResponse> responseStream, ServerCallContext context)
        {
            try
            {
                var clientIp = context.GetHttpContext().Connection.RemoteIpAddress?.ToString() ?? "unknown";
                LogToConsole($"Bắt đầu phát trực tiếp trạng thái trò chơi cho người chơi {request.PlayerId} từ {clientIp}...");

                if (string.IsNullOrEmpty(request.PlayerId))
                {
                    await responseStream.WriteAsync(new GameStateResponse { Message = "ID người chơi không hợp lệ" });
                    LogToConsole($"Gửi lỗi: ID người chơi không hợp lệ tới {clientIp}");
                    return;
                }

                if (!players.ContainsValue(request.PlayerId))
                {
                    await responseStream.WriteAsync(new GameStateResponse { Message = "Người chơi chưa kết nối" });
                    LogToConsole($"Gửi lỗi: Người chơi chưa kết nối tới {clientIp}");
                    return;
                }

                lock (activeStreams)
                {
                    activeStreams.Add(responseStream);
                    LogToConsole($"Đã thêm luồng cho {request.PlayerId}. Tổng số luồng: {activeStreams.Count}");
                }

                try
                {
                    if (game == null)
                    {
                        await responseStream.WriteAsync(new GameStateResponse { Message = "Trò chơi chưa bắt đầu" });
                        LogToConsole($"Gửi trạng thái 'Trò chơi chưa bắt đầu' tới {request.PlayerId} ({clientIp})");
                    }
                    else
                    {
                        var response = new GameStateResponse
                        {
                            Fen = game.GetFen(),
                            CurrentTurn = game.WhoseTurn.ToString().ToLower(),
                            IsCheck = game.IsInCheck(game.WhoseTurn),
                            IsCheckmate = game.IsCheckmate(game.WhoseTurn),
                            IsStalemate = game.IsStalemate(game.WhoseTurn)
                        };
                        await responseStream.WriteAsync(response);
                        LogToConsole($"Gửi trạng thái ban đầu tới {request.PlayerId} ({clientIp}): FEN={response.Fen}, Turn={response.CurrentTurn}");
                    }

                    await Task.Delay(-1, context.CancellationToken);
                }
                catch (TaskCanceledException)
                {
                    LogToConsole($"Luồng trạng thái trò chơi cho người chơi {request.PlayerId} ({clientIp}) đã kết thúc.");
                }
                catch (Exception ex)
                {
                    LogToConsole($"Lỗi trong luồng trạng thái trò chơi cho {request.PlayerId} ({clientIp}): {ex.Message}", ex);
                }
                finally
                {
                    lock (activeStreams)
                    {
                        activeStreams.Remove(responseStream);
                        LogToConsole($"Đã xóa luồng cho {request.PlayerId}. Tổng số luồng còn lại: {activeStreams.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Lỗi khi phát trực tiếp trạng thái trò chơi cho {request.PlayerId}: {ex.Message}", ex);
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
                return true;
            }
            catch (Exception ex)
            {
                LogToConsole($"Lỗi phân tích vị trí: {notation}", ex);
                position = null;
                return false;
            }
        }
    }
}