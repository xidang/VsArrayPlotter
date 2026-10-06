using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VsArrayPlotter.Memory;
using VsArrayPlotter.Models;
using VsArrayPlotter.Plotting;

namespace VsArrayPlotter
{
    /// <summary>
    /// 数组绘图器主界面（纯代码构建 WPF，无需 XAML）。
    /// 功能：读取指定进程内存中的数组（多种类型 / 实数 / 复数），
    /// 绘制 1D 折线、2D 热图、3D 曲面，支持线性与 dB 刻度。
    /// </summary>
    public class PlotterToolWindowControl : UserControl
    {
        // ---- 输入控件 ----
        private readonly TextBox _pidBox = new TextBox();
        private readonly TextBox _addressBox = new TextBox { Text = "0x" };
        private readonly ComboBox _typeCombo = new ComboBox();
        private readonly TextBox _countBox = new TextBox { Text = "1024" };
        private readonly TextBox _colsBox = new TextBox { Text = "1" };
        private readonly ComboBox _modeCombo = new ComboBox();
        private readonly ComboBox _scaleCombo = new ComboBox();
        private readonly CheckBox _chkReal = new CheckBox { Content = "实部", IsChecked = true, Margin = new Thickness(0, 0, 10, 0) };
        private readonly CheckBox _chkImag = new CheckBox { Content = "虚部", IsChecked = true, Margin = new Thickness(0, 0, 10, 0) };
        private readonly CheckBox _chkMag = new CheckBox { Content = "幅值", IsChecked = true, Margin = new Thickness(0, 0, 10, 0) };
        private readonly CheckBox _chkPhase = new CheckBox { Content = "相位(°)" };
        private readonly TextBlock _status = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 6, 0, 0)
        };
        private readonly ContentControl _plotHost = new ContentControl();
        private readonly OxyPlot.Wpf.PlotView _plotView = new OxyPlot.Wpf.PlotView();

        private readonly ElementType[] _types = ElementTypeInfo.All;
        private readonly PlotMode[] _modes = (PlotMode[])Enum.GetValues(typeof(PlotMode));
        private readonly ScaleType[] _scales = (ScaleType[])Enum.GetValues(typeof(ScaleType));

        public PlotterToolWindowControl()
        {
            BuildLayout();
        }

        // ---------------------------------------------------------------
        // 界面构建
        // ---------------------------------------------------------------
        private void BuildLayout()
        {
            var root = new DockPanel { Margin = new Thickness(8) };

            var left = new StackPanel { Width = 330, Margin = new Thickness(0, 0, 10, 0) };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(84) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int row = 0;
            AddInputRow(grid, row++, "PID", _pidBox, "目标进程 PID（调试时可用右侧按钮自动填入）");
            AddInputRow(grid, row++, "地址(hex)", _addressBox, "要读取的内存起始地址，十六进制，如 0x7FF6A1B2C3D0");
            AddInputRow(grid, row++, "元素类型", _typeCombo, "数组元素类型：整数 / 浮点 / 复数（complex64、complex128）");
            AddInputRow(grid, row++, "元素个数", _countBox, "要读取的元素个数（复数时一对实/虚部算 1 个元素）");
            AddInputRow(grid, row++, "列数(2D/3D)", _colsBox, "二维/三维时每行元素个数（一维时填 1）");
            left.Children.Add(grid);

            // 按钮行
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
            var readBtn = new Button
            {
                Content = "读取并绘图",
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0)
            };
            readBtn.Click += OnReadClick;
            var pidBtn = new Button { Content = "取调试进程 PID", Padding = new Thickness(8, 4, 8, 4) };
            pidBtn.Click += OnPickDebugProcess;
            btnRow.Children.Add(readBtn);
            btnRow.Children.Add(pidBtn);
            left.Children.Add(btnRow);

            // 图类型 / 刻度
            var modeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            _modeCombo.ItemsSource = Array.ConvertAll(_modes, m => EnumDisplay.Of(m));
            _modeCombo.SelectedIndex = 0;
            _modeCombo.Width = 130;
            _modeCombo.Margin = new Thickness(0, 0, 12, 0);
            _modeCombo.SelectionChanged += OnModeChanged;
            _scaleCombo.ItemsSource = Array.ConvertAll(_scales, s => EnumDisplay.Of(s));
            _scaleCombo.SelectedIndex = 0;
            _scaleCombo.Width = 130;
            modeRow.Children.Add(new TextBlock { Text = "图类型", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            modeRow.Children.Add(_modeCombo);
            modeRow.Children.Add(new TextBlock { Text = "Y轴", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            modeRow.Children.Add(_scaleCombo);
            left.Children.Add(modeRow);

            // 1D 序列复选框
            var chkPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            chkPanel.Children.Add(_chkReal);
            chkPanel.Children.Add(_chkImag);
            chkPanel.Children.Add(_chkMag);
            chkPanel.Children.Add(_chkPhase);
            left.Children.Add(chkPanel);

            left.Children.Add(_status);

            DockPanel.SetDock(left, Dock.Left);
            root.Children.Add(left);
            root.Children.Add(_plotHost);

            _plotHost.Content = _plotView;
            Content = root;
            UpdateSeriesEnabled();
        }

        private static void AddInputRow(Grid grid, int row, string label, FrameworkElement editor, string tooltip)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 6, 2)
            };
            Grid.SetRow(labelBlock, row);
            Grid.SetColumn(labelBlock, 0);
            editor.Margin = new Thickness(0, 2, 0, 2);
            editor.ToolTip = tooltip;
            Grid.SetRow(editor, row);
            Grid.SetColumn(editor, 1);
            grid.Children.Add(labelBlock);
            grid.Children.Add(editor);
        }

        // ---------------------------------------------------------------
        // 状态与取值
        // ---------------------------------------------------------------
        private PlotMode CurrentMode => _modes[_modeCombo.SelectedIndex];
        private ScaleType CurrentScale => _scales[_scaleCombo.SelectedIndex];
        private ElementType CurrentType => _types[_typeCombo.SelectedIndex];

        private void OnModeChanged(object sender, SelectionChangedEventArgs e) => UpdateSeriesEnabled();

        private void UpdateSeriesEnabled()
        {
            bool oneD = CurrentMode == PlotMode.Line1D;
            _chkReal.IsEnabled = oneD;
            _chkImag.IsEnabled = oneD;
            _chkMag.IsEnabled = oneD;
            _chkPhase.IsEnabled = oneD;
        }

        // ---------------------------------------------------------------
        // 读取内存并绘图
        // ---------------------------------------------------------------
        private async void OnReadClick(object sender, RoutedEventArgs e)
        {
            int pid;
            if (!int.TryParse(_pidBox.Text, out pid) || pid <= 0)
            {
                _status.Text = "PID 无效：请输入正整数（如任务管理器中的进程 ID）。";
                return;
            }

            string hex = (_addressBox.Text ?? string.Empty).Trim();
            hex = hex.Replace("0x", "").Replace("0X", "");
            ulong address;
            if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address))
            {
                _status.Text = "地址无效：请输入十六进制地址，例如 0x7FF6A1B2C3D0。";
                return;
            }

            int count;
            if (!int.TryParse(_countBox.Text, out count) || count <= 0)
            {
                _status.Text = "元素个数无效：请输入正整数。";
                return;
            }

            int cols;
            if (!int.TryParse(_colsBox.Text, out cols) || cols <= 0)
            {
                cols = 1;
            }
            int rows = count / cols;
            if (rows == 0)
            {
                _status.Text = "元素个数小于列数，无法构成数组，请调整。";
                return;
            }
            int used = rows * cols;

            _status.Text = string.Format(
                "正在读取进程 {0} 地址 0x{1:X}（{2} 个元素，类型 {3}）…",
                pid, address, used, ElementTypeInfo.DisplayName(CurrentType));

            try
            {
                ArrayData data = await Task.Run(
                    () => MemoryReader.Read(pid, new IntPtr((long)address), CurrentType, used));

                data.Rows = rows;
                data.Cols = cols;

                ShowPlot(data);

                string dim = CurrentMode == PlotMode.Line1D ? "一维折线"
                    : (CurrentMode == PlotMode.Heat2D ? "二维热图" : "三维曲面");
                _status.Text = string.Format(
                    "读取成功：{0} 个元素（{1} 行 × {2} 列），已绘制{3}（刻度：{4}）。{5}",
                    used, rows, cols, dim,
                    CurrentScale == ScaleType.Db ? "dB" : "线性",
                    used < count ? "多余元素已截断。" : string.Empty);
            }
            catch (Exception ex)
            {
                _status.Text = "读取失败：" + ex.Message;
            }
        }

        private void ShowPlot(ArrayData data)
        {
            bool db = CurrentScale == ScaleType.Db;
            switch (CurrentMode)
            {
                case PlotMode.Line1D:
                    _plotView.Model = PlotBuilder.BuildLineModel(
                        data, db, _chkReal.IsChecked == true, _chkImag.IsChecked == true,
                        _chkMag.IsChecked == true, _chkPhase.IsChecked == true);
                    _plotHost.Content = _plotView;
                    break;
                case PlotMode.Heat2D:
                    _plotView.Model = PlotBuilder.BuildHeatModel(data, db);
                    _plotHost.Content = _plotView;
                    break;
                case PlotMode.Surface3D:
                    _plotHost.Content = PlotBuilder.BuildSurface(data, db);
                    break;
            }
        }

        private void OnPickDebugProcess(object sender, RoutedEventArgs e)
        {
            try
            {
                var dte = Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                if (dte == null)
                {
                    _status.Text = "无法获取 DTE 服务。";
                    return;
                }
                EnvDTE.Program program = dte.Debugger.CurrentProgram;
                if (program != null && program.Process != null)
                {
                    _pidBox.Text = program.Process.ProcessID.ToString(CultureInfo.InvariantCulture);
                    _status.Text = "已填入当前调试进程 PID：" + _pidBox.Text;
                }
                else
                {
                    _status.Text = "当前没有正在调试的进程（请先 F5 启动调试）。";
                }
            }
            catch (Exception ex)
            {
                _status.Text = "获取调试进程失败：" + ex.Message;
            }
        }
    }
}
