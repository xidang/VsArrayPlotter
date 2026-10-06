using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace VsArrayPlotter
{
    /// <summary>数组绘图器工具窗口。</summary>
    [Guid("7e9c1b5a-4f2d-4a8e-9c3b-1d6f8a2e4c5d")]
    public class PlotterToolWindow : ToolWindowPane
    {
        public PlotterToolWindow()
        {
            Caption = "数组绘图器";
            Content = new PlotterToolWindowControl();
        }
    }
}
