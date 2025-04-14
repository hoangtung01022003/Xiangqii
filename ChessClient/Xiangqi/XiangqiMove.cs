namespace ChessClient.Xiangqi
{
    public class XiangqiMove
    {
        public XiangqiPosition From { get; }
        public XiangqiPosition To { get; }
        public Player Player { get; }

        public XiangqiMove(XiangqiPosition from, XiangqiPosition to, Player player)
        {
            From = from;
            To = to;
            Player = player;
        }
    }
}