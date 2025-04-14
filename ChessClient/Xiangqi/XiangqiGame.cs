using System;
using System.Text;
using ChessClient.window;

namespace ChessClient.Xiangqi
{
    public class XiangqiGame
    {
        private XiangqiPiece[,] board = new XiangqiPiece[9, 10];
        public Player WhoseTurn { get; private set; }
        private bool isInCheck;
        private bool isCheckmate;
        private bool isStalemate;
        private int moveCount; // Đếm nước đi để kiểm tra hòa

        // Constructor mặc định
        public XiangqiGame()
        {
            InitializeBoard();
            WhoseTurn = Player.Red;
            moveCount = 0;
        }

        // Constructor mới để khởi tạo từ FEN
        public XiangqiGame(string fen)
        {
            if (string.IsNullOrEmpty(fen))
                throw new ArgumentException("FEN string cannot be empty.");
            board = ParseFen(fen);
            WhoseTurn = fen.EndsWith(" r") ? Player.Red : Player.Black;
            moveCount = 0;
        }

        private void InitializeBoard()
        {
            // Quân Đỏ
            board[4, 0] = new XiangqiPiece(PieceType.General, Player.Red);    // Tướng (5,0)
            board[3, 0] = new XiangqiPiece(PieceType.Advisor, Player.Red);    // Sĩ (4,0)
            board[5, 0] = new XiangqiPiece(PieceType.Advisor, Player.Red);    // Sĩ (6,0)
            board[2, 0] = new XiangqiPiece(PieceType.Elephant, Player.Red);   // Tượng (3,0)
            board[6, 0] = new XiangqiPiece(PieceType.Elephant, Player.Red);   // Tượng (7,0)
            board[0, 0] = new XiangqiPiece(PieceType.Chariot, Player.Red);    // Xe (1,0)
            board[8, 0] = new XiangqiPiece(PieceType.Chariot, Player.Red);    // Xe (9,0)
            board[1, 0] = new XiangqiPiece(PieceType.Horse, Player.Red);      // Mã (2,0)
            board[7, 0] = new XiangqiPiece(PieceType.Horse, Player.Red);      // Mã (8,0)
            board[1, 2] = new XiangqiPiece(PieceType.Cannon, Player.Red);     // Pháo (2,2)
            board[7, 2] = new XiangqiPiece(PieceType.Cannon, Player.Red);     // Pháo (8,2)
            board[0, 3] = new XiangqiPiece(PieceType.Soldier, Player.Red);    // Tốt (1,3)
            board[2, 3] = new XiangqiPiece(PieceType.Soldier, Player.Red);    // Tốt (3,3)
            board[4, 3] = new XiangqiPiece(PieceType.Soldier, Player.Red);    // Tốt (5,3)
            board[6, 3] = new XiangqiPiece(PieceType.Soldier, Player.Red);    // Tốt (7,3)
            board[8, 3] = new XiangqiPiece(PieceType.Soldier, Player.Red);    // Tốt (9,3)

            // Quân Đen
            board[4, 9] = new XiangqiPiece(PieceType.General, Player.Black);  // Tướng (5,9)
            board[3, 9] = new XiangqiPiece(PieceType.Advisor, Player.Black);  // Sĩ (4,9)
            board[5, 9] = new XiangqiPiece(PieceType.Advisor, Player.Black);  // Sĩ (6,9)
            board[2, 9] = new XiangqiPiece(PieceType.Elephant, Player.Black); // Tượng (3,9)
            board[6, 9] = new XiangqiPiece(PieceType.Elephant, Player.Black); // Tượng (7,9)
            board[0, 9] = new XiangqiPiece(PieceType.Chariot, Player.Black);  // Xe (1,9)
            board[8, 9] = new XiangqiPiece(PieceType.Chariot, Player.Black);  // Xe (9,9)
            board[1, 9] = new XiangqiPiece(PieceType.Horse, Player.Black);    // Mã (2,9)
            board[7, 9] = new XiangqiPiece(PieceType.Horse, Player.Black);    // Mã (8,9)
            board[1, 7] = new XiangqiPiece(PieceType.Cannon, Player.Black);   // Pháo (2,7)
            board[7, 7] = new XiangqiPiece(PieceType.Cannon, Player.Black);   // Pháo (8,7)
            board[0, 6] = new XiangqiPiece(PieceType.Soldier, Player.Black);  // Tốt (1,6)
            board[2, 6] = new XiangqiPiece(PieceType.Soldier, Player.Black);  // Tốt (3,6)
            board[4, 6] = new XiangqiPiece(PieceType.Soldier, Player.Black);  // Tốt (5,6)
            board[6, 6] = new XiangqiPiece(PieceType.Soldier, Player.Black);  // Tốt (7,6)
            board[8, 6] = new XiangqiPiece(PieceType.Soldier, Player.Black);  // Tốt (9,6)
        }

        // Phương thức ParseFen được di chuyển từ MainWindow.xaml.cs
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
                return board;
            }
            catch (Exception ex)
            {
                return new XiangqiPiece[9, 10];
            }
        }

        // Phương thức CreatePiece được di chuyển từ MainWindow.xaml.cs
        private XiangqiPiece CreatePiece(char fenChar)
        {
            try
            {
                Player owner = char.IsUpper(fenChar) ? Player.Red : Player.Black;
                char pieceType = char.ToLower(fenChar);
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
                return null;
            }
        }

        // Các phương thức khác của XiangqiGame giữ nguyên
        public XiangqiPiece GetPieceAt(XiangqiPosition pos)
        {
            if (pos.File < 1 || pos.File > 9 || pos.Rank < 0 || pos.Rank > 9) return null;
            return board[pos.File - 1, pos.Rank];
        }

        public bool IsValidMove(XiangqiMove move)
        {
            var piece = GetPieceAt(move.From);
            if (piece == null || piece.Owner != move.Player || move.Player != WhoseTurn)
                return false;

            bool isValid = piece.Type switch
            {
                PieceType.General => IsValidGeneralMove(move),
                PieceType.Advisor => IsValidAdvisorMove(move),
                PieceType.Elephant => IsValidElephantMove(move),
                PieceType.Chariot => IsValidChariotMove(move),
                PieceType.Cannon => IsValidCannonMove(move),
                PieceType.Horse => IsValidHorseMove(move),
                PieceType.Soldier => IsValidSoldierMove(move),
                _ => false
            };

            if (!isValid) return false;

            // Kiểm tra luật Tướng đối diện
            var tempBoard = new XiangqiPiece[9, 10];
            Array.Copy(board, tempBoard, board.Length);
            tempBoard[move.From.File - 1, move.From.Rank] = null;
            tempBoard[move.To.File - 1, move.To.Rank] = piece;
            if (IsKingsFacing(tempBoard)) return false;

            // Kiểm tra nước đi có dẫn đến tự chiếu không
            tempBoard[move.From.File - 1, move.From.Rank] = null;
            tempBoard[move.To.File - 1, move.To.Rank] = piece;
            bool wouldBeInCheck = IsInCheck(move.Player, tempBoard);
            return !wouldBeInCheck;
        }

        private bool IsValidGeneralMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            // Tướng chỉ di chuyển trong Cửu cung
            if (move.Player == Player.Red)
            {
                if (toFile < 4 || toFile > 6 || toRank < 0 || toRank > 2) return false;
            }
            else
            {
                if (toFile < 4 || toFile > 6 || toRank < 7 || toRank > 9) return false;
            }

            int fileDiff = Math.Abs(toFile - fromFile);
            int rankDiff = Math.Abs(toRank - fromRank);
            return (fileDiff == 1 && rankDiff == 0) || (fileDiff == 0 && rankDiff == 1) &&
                   (piece == null || piece.Owner != move.Player);
        }

        private bool IsValidAdvisorMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            // Sĩ chỉ di chuyển trong Cửu cung
            if (move.Player == Player.Red)
            {
                if (toFile < 4 || toFile > 6 || toRank < 0 || toRank > 2) return false;
            }
            else
            {
                if (toFile < 4 || toFile > 6 || toRank < 7 || toRank > 9) return false;
            }

            return Math.Abs(toFile - fromFile) == 1 && Math.Abs(toRank - fromRank) == 1 &&
                   (piece == null || piece.Owner != move.Player);
        }

        private bool IsValidElephantMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            // Tượng không vượt sông
            if (move.Player == Player.Red && toRank > 4) return false;
            if (move.Player == Player.Black && toRank < 5) return false;

            int fileDiff = Math.Abs(toFile - fromFile);
            int rankDiff = Math.Abs(toRank - fromRank);
            if (fileDiff != 2 || rankDiff != 2) return false;

            // Kiểm tra ô giữa (cản Tượng)
            int midFile = (fromFile + toFile) / 2;
            int midRank = (fromRank + toRank) / 2;
            return GetPieceAt(new XiangqiPosition(midFile, midRank)) == null &&
                   (piece == null || piece.Owner != move.Player);
        }

        private bool IsValidChariotMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            if (fromFile != toFile && fromRank != toRank) return false;

            // Kiểm tra đường đi không bị cản
            if (fromFile == toFile)
            {
                int minRank = Math.Min(fromRank, toRank);
                int maxRank = Math.Max(fromRank, toRank);
                for (int r = minRank + 1; r < maxRank; r++)
                    if (GetPieceAt(new XiangqiPosition(fromFile, r)) != null) return false;
            }
            else
            {
                int minFile = Math.Min(fromFile, toFile);
                int maxFile = Math.Max(fromFile, toFile);
                for (int f = minFile + 1; f < maxFile; f++)
                    if (GetPieceAt(new XiangqiPosition(f, fromRank)) != null) return false;
            }

            return piece == null || piece.Owner != move.Player;
        }

        private bool IsValidCannonMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            if (fromFile != toFile && fromRank != toRank) return false;

            int count = 0;
            if (fromFile == toFile)
            {
                int minRank = Math.Min(fromRank, toRank);
                int maxRank = Math.Max(fromRank, toRank);
                for (int r = minRank + 1; r < maxRank; r++)
                    if (GetPieceAt(new XiangqiPosition(fromFile, r)) != null) count++;
            }
            else
            {
                int minFile = Math.Min(fromFile, toFile);
                int maxFile = Math.Max(fromFile, toFile);
                for (int f = minFile + 1; f < maxFile; f++)
                    if (GetPieceAt(new XiangqiPosition(f, fromRank)) != null) count++;
            }

            // Pháo cần 0 quân giữa để di chuyển, 1 quân giữa để ăn
            return (piece == null && count == 0) || (piece != null && piece.Owner != move.Player && count == 1);
        }

        private bool IsValidHorseMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            int fileDiff = Math.Abs(toFile - fromFile);
            int rankDiff = Math.Abs(toRank - fromRank);

            if (!((fileDiff == 2 && rankDiff == 1) || (fileDiff == 1 && rankDiff == 2))) return false;

            // Kiểm tra cản Mã
            if (fileDiff == 2)
            {
                int midFile = fromFile + (toFile > fromFile ? 1 : -1);
                if (GetPieceAt(new XiangqiPosition(midFile, fromRank)) != null) return false;
            }
            else
            {
                int midRank = fromRank + (toRank > fromRank ? 1 : -1);
                if (GetPieceAt(new XiangqiPosition(fromFile, midRank)) != null) return false;
            }

            return piece == null || piece.Owner != move.Player;
        }

        private bool IsValidSoldierMove(XiangqiMove move)
        {
            int fromFile = move.From.File, fromRank = move.From.Rank;
            int toFile = move.To.File, toRank = move.To.Rank;
            var piece = GetPieceAt(move.To);

            int fileDiff = Math.Abs(toFile - fromFile);
            int rankDiff = toRank - fromRank;

            if (move.Player == Player.Red)
            {
                if (fromRank < 5) // Chưa qua sông
                    return fileDiff == 0 && rankDiff == 1 && (piece == null || piece.Owner != move.Player);
                else // Qua sông
                    return (fileDiff == 0 && rankDiff == 1) || (fileDiff == 1 && rankDiff == 0) &&
                           (piece == null || piece.Owner != move.Player);
            }
            else
            {
                if (fromRank > 4) // Chưa qua sông
                    return fileDiff == 0 && rankDiff == -1 && (piece == null || piece.Owner != move.Player);
                else // Qua sông
                    return (fileDiff == 0 && rankDiff == -1) || (fileDiff == 1 && rankDiff == 0) &&
                           (piece == null || piece.Owner != move.Player);
            }
        }

        private bool IsKingsFacing(XiangqiPiece[,] tempBoard)
        {
            int? redKingFile = null, blackKingFile = null;
            int redKingRank = 0, blackKingRank = 0;

            for (int rank = 0; rank < 10; rank++)
            {
                for (int file = 1; file <= 9; file++)
                {
                    var piece = tempBoard[file - 1, rank];
                    if (piece != null && piece.Type == PieceType.General)
                    {
                        if (piece.Owner == Player.Red)
                        {
                            redKingFile = file;
                            redKingRank = rank;
                        }
                        else
                        {
                            blackKingFile = file;
                            blackKingRank = rank;
                        }
                    }
                }
            }

            if (redKingFile == null || blackKingFile == null || redKingFile != blackKingFile)
                return false;

            for (int rank = redKingRank + 1; rank < blackKingRank; rank++)
            {
                if (tempBoard[redKingFile.Value - 1, rank] != null)
                    return false;
            }

            return true;
        }

        private bool IsInCheck(Player player, XiangqiPiece[,] tempBoard)
        {
            int? kingFile = null, kingRank = null;

            // Tìm vị trí của Tướng
            for (int rank = 0; rank < 10; rank++)
            {
                for (int file = 1; file <= 9; file++)
                {
                    var piece = tempBoard[file - 1, rank];
                    if (piece != null && piece.Type == PieceType.General && piece.Owner == player)
                    {
                        kingFile = file;
                        kingRank = rank;
                        break;
                    }
                }
                if (kingFile != null) break;
            }

            if (kingFile == null || kingRank == null) return false;

            // Kiểm tra xem có quân nào của đối thủ có thể tấn công Tướng không
            for (int rank = 0; rank < 10; rank++)
            {
                for (int file = 1; file <= 9; file++)
                {
                    var piece = tempBoard[file - 1, rank];
                    if (piece != null && piece.Owner != player)
                    {
                        XiangqiMove move = new XiangqiMove(new XiangqiPosition(file, rank),
                            new XiangqiPosition(kingFile.Value, kingRank.Value), piece.Owner);
                        if (piece.Type switch
                        {
                            PieceType.General => IsValidGeneralMove(move),
                            PieceType.Advisor => IsValidAdvisorMove(move),
                            PieceType.Elephant => IsValidElephantMove(move),
                            PieceType.Chariot => IsValidChariotMove(move),
                            PieceType.Cannon => IsValidCannonMove(move),
                            PieceType.Horse => IsValidHorseMove(move),
                            PieceType.Soldier => IsValidSoldierMove(move),
                            _ => false
                        })
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void MakeMove(XiangqiMove move)
        {
            var piece = board[move.From.File - 1, move.From.Rank];
            board[move.From.File - 1, move.From.Rank] = null;
            board[move.To.File - 1, move.To.Rank] = piece;
            moveCount++;
        }

        public string GetFen()
        {
            var sb = new StringBuilder();
            for (int rank = 9; rank >= 0; rank--)
            {
                int emptyCount = 0;
                for (int file = 1; file <= 9; file++)
                {
                    var piece = GetPieceAt(new XiangqiPosition(file, rank));
                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            sb.Append(emptyCount);
                            emptyCount = 0;
                        }
                        char pieceChar = piece.Type switch
                        {
                            PieceType.General => 'k',
                            PieceType.Advisor => 'a',
                            PieceType.Elephant => 'n',
                            PieceType.Chariot => 'r',
                            PieceType.Cannon => 'b',
                            PieceType.Horse => 'c',
                            PieceType.Soldier => 'p',
                            _ => ' '
                        };
                        sb.Append(piece.Owner == Player.Red ? char.ToUpper(pieceChar) : pieceChar);
                    }
                }
                if (emptyCount > 0) sb.Append(emptyCount);
                if (rank > 0) sb.Append('/');
            }
            sb.Append(WhoseTurn == Player.Red ? " r" : " b");
            return sb.ToString();
        }
    }
}