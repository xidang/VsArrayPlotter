using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VsArrayPlotter
{
    /// <summary>
    /// VS2022 扩展入口包：注册菜单命令与工具窗口。
    /// </summary>
    [InstalledProductRegistration("数组绘图器", "读取进程内存数组并绘制 1D/2D/3D 图（线性/dB）", "VsArrayPlotter")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [ProvideToolWindow(typeof(PlotterToolWindow), MultiInstances = false)]
    public sealed class VsArrayPlotterPackage : AsyncPackage
    {
        public const string PackageGuidString = "3d8f6a2c-9e5b-4f1a-8c7d-2b6e4a1f9c3d";
        public const string CmdSetGuidString = "b2c4d6e8-1a3f-4e5b-9d7c-6f8a2b4e1c3d";
        public const int cmdidPlotterWindow = 0x0100;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AddCommandAsync();
        }

        private async Task AddCommandAsync()
        {
            var commandService = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
            {
                return;
            }
            var menuCommand = new MenuCommand(ShowToolWindow, new CommandID(new Guid(CmdSetGuidString), cmdidPlotterWindow));
            commandService.AddCommand(menuCommand);
        }

        private void ShowToolWindow(object sender, EventArgs e)
        {
            ToolWindowPane window = this.FindToolWindow(typeof(PlotterToolWindow), 0, true);
            if (window == null || window.Frame == null)
            {
                return;
            }
            IVsWindowFrame frame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(frame.Show());
        }
    }
}
