# 数组绘图器（VsArrayPlotter）

一个 **Visual Studio 2022 扩展（VSIX）**：**读取进程内存中的数组**（支持 12 种元素类型：8/16/32/64 位整数、float32 / float64、complex64 / complex128 复数），并绘制 **一维折线、二维热图、三维曲面**，Y 轴 / 幅值支持 **线性与 dB** 两种刻度。

适用场景：调试 C/C++/C# 程序时，快速查看内存中某个数组（频谱、滤波器系数、时域波形、图像矩阵等）的形状与数值分布。

## 功能一览

| 功能 | 说明 |
| --- | --- |
| 读内存 | 指定 PID + 十六进制起始地址 + 元素类型 + 元素个数，只读 `ReadProcessMemory` |
| 元素类型 | int8/uint8/int16/uint16/int32/uint32/int64/uint64/float32/float64/complex64/complex128 |
| 1D 折线 | 复数组可选 实部 / 虚部 / 幅值 / 相位；实数组绘制数值曲线 |
| 2D 热图 | 按「列数」重排为 行×列 矩阵，绘制幅值热图（Jet 色标） |
| 3D 曲面 | 行×列 曲面，按幅值高度着色（WPF 原生 3D，无额外依赖） |
| 线性 / dB | dB = 20·log10(幅值)，0 值映射到 -160 dB，1D/2D/3D 均适用 |
| 调试联动 | 调试中一键填入当前调试进程 PID |

## 项目结构

```
VsArrayPlotter/
├── VsArrayPlotter.sln                    # 解决方案
├── build.bat                             # 一键构建脚本（自动选择 MSBuild / dotnet）
├── README.md
└── src/VsArrayPlotter/
    ├── VsArrayPlotter.csproj             # 工程文件（net48 + VS SDK 17.x）
    ├── source.extension.vsixmanifest     # VSIX 清单
    ├── VsArrayPlotterPackage.vsct        # 菜单命令定义（工具 → 数组绘图器）
    ├── Package.cs                        # 扩展包入口（AsyncPackage）
    ├── PlotterToolWindow.cs              # 工具窗口
    ├── PlotterToolWindowControl.cs       # 主界面（纯代码 WPF，无 XAML）
    ├── Models/
    │   ├── ElementType.cs                # 元素类型 / 绘图模式 / 刻度枚举
    │   └── ArrayData.cs                  # 解码后的数组数据
    ├── Memory/
    │   └── MemoryReader.cs               # ReadProcessMemory + 类型解码
    ├── Plotting/
    │   └── PlotBuilder.cs                # 1D/2D/3D 图形构建（OxyPlot + WPF 3D）
    └── Resources/Icon.png                # 扩展图标
```

## 构建（两种方式，任选其一）

### 方式 A：用 VS2022 构建（推荐，可 F5 调试）
1. 打开 **Visual Studio Installer** → 修改 → 勾选 **「使用 C# 的桌面开发」** 与 **「Visual Studio 扩展开发」** 工作负载，安装；
2. 双击打开 `VsArrayPlotter.sln`；
3. **F5** 启动调试（会自动进入实验实例）；
4. 菜单 **工具 → 数组绘图器** 打开窗口。

### 方式 B：命令行构建（不装 C# 工作负载）
1. 安装免费的 **.NET SDK**：<https://dotnet.microsoft.com/download>；
2. 在项目根目录运行 `build.bat`（自动优先找 VS 的 MSBuild，找不到则用 `dotnet build`）；
3. 产物：`src\VsArrayPlotter\bin\Release\VsArrayPlotter.vsix`，双击即可安装到 VS2022。

> 所有依赖（Microsoft.VisualStudio.SDK、Microsoft.VSSDK.BuildTools、OxyPlot.Wpf、EnvDTE）均由 NuGet 自动还原。

### 方式 C：GitHub Actions 云端构建（零本地安装）
1. 注册/登录 GitHub（免费）；
2. 新建**私有仓库**（Private），上传本目录全部文件（含 `.github/workflows/build-vsix.yml`）；
3. Actions 会自动开始构建（也可在 Actions 页手动 `Run workflow`）；
4. 构建完成后，在 Actions 页下载 **VsArrayPlotter-vsix** 工件，解压得到 `VsArrayPlotter.vsix`，双击安装。

> 云端构建机会自动在**它自己的服务器**上安装 .NET SDK，您本机不需要安装任何工具。

## 使用步骤

1. 运行目标程序（调试或非调试均可）；若正在调试，点 **取调试进程 PID** 自动填入；
2. 填 **地址**（十六进制，如 `0x7FF6A1B2C3D0`）、**元素类型**、**元素个数**；
3. 2D/3D 时填 **列数**（每行元素数），一维填 `1`；
4. 选择 **图类型** 与 **Y轴刻度**，点 **读取并绘图**。

**示例（频谱查看）**：地址 `0x…`、类型 `complex128`、个数 `4096`、列数 `1`、图类型 `1D 折线`、勾选 `幅值`、Y轴 `dB` → 得到 dB 单位的频谱幅度图。

## 注意事项 / 已知限制

- 内存读取为**只读**操作，不会修改目标进程；地址无效 / 越界 / 页面不可读时会报错；
- 读取权限需与目标进程同级或更高（管理员运行 VS 可读绝大多数进程）；
- 64 位地址按完整十六进制填写；UInt64 超过 2^53 时以 double 展示会有精度损失（仅影响绘图显示）；
- 3D 曲面暂未加鼠标旋转交互（可后续扩展）；2D/3D 以幅值绘制，dB 时取 20·log10；
- 单次读取上限 **256 MB**，防止误填地址导致内存占用过大；
- 安装 .vsix 后需重启 VS（或使用实验实例）。

## 验证状态

当前交付为**完整源码工程（未编译）**——本机缺少 C# 编译环境（VS 未装 C# 工作负载、无 .NET SDK），按您的要求先交付代码。XML 清单与工程文件已做结构校验；首次构建时如 NuGet 版本提示可用新版，可自行升级 `Microsoft.VisualStudio.SDK` 与 `Microsoft.VSSDK.BuildTools` 版本号（两处需同时升级）。
