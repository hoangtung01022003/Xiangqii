using System;
using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;

namespace ChessClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
