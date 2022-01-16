using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FantomasVs
{
    public static class Theme
    {
        public static string Background => (string) VsBrushes.ToolWindowBackgroundKey ?? "White";
        public static string Foreground => (string) VsBrushes.ToolWindowTextKey ?? "Black";        
        public static string ButtonFace => (string) VsBrushes.ButtonFaceKey ?? "White";
        public static string ButtonText => (string) VsBrushes.ButtonTextKey ?? "Black";        

    }
}
