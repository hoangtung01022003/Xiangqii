using System;

namespace ChessClient.Xiangqi
{
    public class XiangqiPosition
    {
        public int File { get; } // Cột: 1-9
        public int Rank { get; } // Hàng: 0-9

        public XiangqiPosition(int file, int rank)
        {
            if (file < 1 || file > 9 || rank < 0 || rank > 9)
                throw new ArgumentException("Invalid position");
            File = file;
            Rank = rank;
        }

        public override bool Equals(object obj) => obj is XiangqiPosition pos && File == pos.File && Rank == pos.Rank;
        public override int GetHashCode() => HashCode.Combine(File, Rank);
        public string ToNotation() => $"{File}{Rank}";
    }
}