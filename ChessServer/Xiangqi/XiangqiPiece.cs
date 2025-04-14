namespace ChessClient.Xiangqi
{
    public enum Player { Red, Black }
    public enum PieceType { General, Advisor, Elephant, Chariot, Cannon, Horse, Soldier }

    public class XiangqiPiece
    {
        public PieceType Type { get; }
        public Player Owner { get; }

        public XiangqiPiece(PieceType type, Player owner)
        {
            Type = type;
            Owner = owner;
        }

        public char GetFenCharacter()
        {
            char c = Type switch
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
            return Owner == Player.Red ? char.ToUpper(c) : c;
        }
    }
}