using System.Windows;
using System.Windows.Controls;

namespace ChessClient.window
{
  public class PromotionDialog : Window
  {
    public string SelectedPiece { get; private set; }

    public PromotionDialog()
    {
      Title = "Choose Promotion";
      Width = 300;
      Height = 150;
      WindowStartupLocation = WindowStartupLocation.CenterOwner;

      // Tạo StackPanel chính
      StackPanel panel = new StackPanel
      {
        Orientation = Orientation.Vertical,
        Margin = new Thickness(10)
      };

      // Thêm hướng dẫn
      TextBlock instructions = new TextBlock
      {
        Text = "Select piece for pawn promotion:",
        Margin = new Thickness(10),
        HorizontalAlignment = HorizontalAlignment.Center
      };
      panel.Children.Add(instructions);

      // Tạo StackPanel cho các nút
      StackPanel buttonsPanel = new StackPanel
      {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Center
      };

      // Tạo nút cho từng quân cờ
      string[] pieces = { "queen", "rook", "bishop", "knight" };
      foreach (var piece in pieces)
      {
        Button button = new Button
        {
          Content = piece.Substring(0, 1).ToUpper() + piece.Substring(1),
          Width = 60,
          Height = 30,
          Margin = new Thickness(5)
        };

        button.Click += (sender, e) =>
        {
          SelectedPiece = piece;
          DialogResult = true;
          Close();
        };

        buttonsPanel.Children.Add(button);
      }

      panel.Children.Add(buttonsPanel);
      Content = panel;
    }
  }
}