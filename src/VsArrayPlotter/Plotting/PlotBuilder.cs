using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using VsArrayPlotter.Models;

namespace VsArrayPlotter.Plotting
{
    /// <summary>根据内存数组构建 OxyPlot / WPF 3D 图形。</summary>
    public static class PlotBuilder
    {
        private const double DbFloorDb = -160.0; // dB 下限，避免 log10(0)

        /// <summary>线性值转 dB：20·log10(|v|)，0 值映射到下限 -160 dB。</summary>
        public static double ToDb(double value)
        {
            double m = Math.Abs(value);
            return m <= double.Epsilon ? DbFloorDb : 20.0 * Math.Log10(m);
        }

        // ---------------------------------------------------------------
        // 1D 折线
        // ---------------------------------------------------------------
        public static PlotModel BuildLineModel(ArrayData d, bool db, bool showReal, bool showImag, bool showMag, bool showPhase)
        {
            var model = new PlotModel
            {
                Title = "一维图" + (db ? "（dB）" : string.Empty)
            };
            // OxyPlot 2.x：图例设置挂在 Legends 集合上（PlotModel.LegendPosition 已移除）
            model.Legends.Add(new Legend
            {
                Position = LegendPosition.TopRight,
                Placement = LegendPlacement.Outside
            });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "索引", MinimumPadding = 0, MaximumPadding = 0 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = db ? "幅值 (dB)" : "数值" });

            bool any = false;
            if (d.IsComplex)
            {
                if (showReal)
                {
                    AddLine(model, "实部", OxyColors.Red, IndexSeries(d.Real));
                    any = true;
                }
                if (showImag)
                {
                    AddLine(model, "虚部", OxyColors.Blue, IndexSeries(d.Imag));
                    any = true;
                }
                if (showMag)
                {
                    var pts = new List<DataPoint>(d.UsedCount);
                    for (int i = 0; i < d.UsedCount; i++)
                    {
                        pts.Add(new DataPoint(i, db ? ToDb(d.MagnitudeAt(i)) : d.MagnitudeAt(i)));
                    }
                    AddLine(model, db ? "幅值 (dB)" : "幅值", OxyColors.Magenta, pts);
                    any = true;
                }
                if (showPhase)
                {
                    var pts = new List<DataPoint>(d.UsedCount);
                    for (int i = 0; i < d.UsedCount; i++)
                    {
                        pts.Add(new DataPoint(i, d.PhaseAt(i)));
                    }
                    AddLine(model, "相位 (°)", OxyColors.Green, pts);
                    any = true;
                }
            }
            else
            {
                var pts = new List<DataPoint>(d.UsedCount);
                for (int i = 0; i < d.UsedCount; i++)
                {
                    pts.Add(new DataPoint(i, db ? ToDb(Math.Abs(d.Real[i])) : d.Real[i]));
                }
                AddLine(model, db ? "数值 (dB)" : "数值", OxyColors.SteelBlue, pts);
                any = true;
            }

            if (!any)
            {
                model.Series.Add(new LineSeries { Title = "（未勾选任何序列）" });
            }
            return model;
        }

        private static IEnumerable<DataPoint> IndexSeries(double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                yield return new DataPoint(i, values[i]);
            }
        }

        private static void AddLine(PlotModel model, string title, OxyColor color, IEnumerable<DataPoint> points)
        {
            var series = new LineSeries { Title = title, Color = color };
            series.Points.AddRange(points);
            model.Series.Add(series);
        }

        // ---------------------------------------------------------------
        // 2D 热图
        // ---------------------------------------------------------------
        public static PlotModel BuildHeatModel(ArrayData d, bool db)
        {
            int rows = d.Rows;
            int cols = d.Cols;
            var data = new double[rows, cols];
            double min = double.MaxValue;
            double max = double.MinValue;

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double v = db ? ToDb(d.MagnitudeAt(idx)) : d.MagnitudeAt(idx);
                    data[r, c] = v;
                    if (v < min) min = v;
                    if (v > max) max = v;
                    idx++;
                }
            }

            var model = new PlotModel { Title = "二维热图" + (db ? "（dB）" : string.Empty) };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "列", MinimumPadding = 0, MaximumPadding = 0 });
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "行", MinimumPadding = 0, MaximumPadding = 0 });
            model.Series.Add(new HeatMapSeries
            {
                X0 = 0,
                X1 = cols - 1,
                Y0 = 0,
                Y1 = rows - 1,
                Data = data,
                Interpolate = false
            });
            model.Axes.Add(new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Title = db ? "幅值 (dB)" : "幅值",
                Minimum = min,
                Maximum = max,
                Palette = OxyPalettes.Jet(256)
            });
            return model;
        }

        // ---------------------------------------------------------------
        // 3D 曲面（WPF 原生 Viewport3D，无额外依赖）
        // ---------------------------------------------------------------
        public static Viewport3D BuildSurface(ArrayData d, bool db)
        {
            int rows = d.Rows;
            int cols = d.Cols;

            var z = new double[rows, cols];
            double zmin = double.MaxValue;
            double zmax = double.MinValue;
            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double v = db ? ToDb(d.MagnitudeAt(idx)) : d.MagnitudeAt(idx);
                    z[r, c] = v;
                    if (v < zmin) zmin = v;
                    if (v > zmax) zmax = v;
                    idx++;
                }
            }
            if (zmax <= zmin)
            {
                zmax = zmin + 1.0;
            }

            var positions = new Point3DCollection();
            var texture = new PointCollection();
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    positions.Add(new Point3D(c, z[r, c], r));
                    double u = cols > 1 ? (double)c / (cols - 1) : 0.5;
                    double v = (z[r, c] - zmin) / (zmax - zmin);
                    texture.Add(new Point(u, v));
                }
            }

            var indices = new Int32Collection();
            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    int i0 = r * cols + c;
                    int i1 = i0 + 1;
                    int i2 = i0 + cols;
                    int i3 = i2 + 1;
                    indices.Add(i0); indices.Add(i2); indices.Add(i1);
                    indices.Add(i1); indices.Add(i2); indices.Add(i3);
                }
            }

            var mesh = new MeshGeometry3D
            {
                Positions = positions,
                TriangleIndices = indices,
                TextureCoordinates = texture
            };

            var material = new DiffuseMaterial(new ImageBrush
            {
                ImageSource = BuildPaletteBitmap(),
                Stretch = Stretch.Fill
            });
            var geometryModel = new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            };

            double zmid = (zmin + zmax) / 2.0;
            double span = Math.Max(zmax - zmin, 1.0);
            var camera = new PerspectiveCamera
            {
                Position = new Point3D(cols / 2.0, zmax + span * 2.5, rows * 1.8),
                LookDirection = new Vector3D(0, -(zmax + span * 2.5 - zmid), -rows * 0.8),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 45
            };

            var viewport = new Viewport3D();
            viewport.Children.Add(new ModelVisual3D { Content = new AmbientLight(Color.FromRgb(70, 70, 70)) });
            viewport.Children.Add(new ModelVisual3D { Content = new DirectionalLight(Colors.White, new Vector3D(-1, -1, -1)) });
            viewport.Children.Add(new ModelVisual3D { Content = geometryModel });
            viewport.Camera = camera;
            return viewport;
        }

        /// <summary>生成 1×256 的 Jet 调色板位图，用于 3D 曲面高度着色。</summary>
        private static WriteableBitmap BuildPaletteBitmap()
        {
            var colors = OxyPalettes.Jet(256).Colors;
            var pixels = new byte[1 * 256 * 4];
            for (int y = 0; y < 256; y++)
            {
                OxyColor c = colors[y];
                int baseIndex = (255 - y) * 4; // 顶部为最大值
                pixels[baseIndex + 0] = (byte)(c.B * 255.0 + 0.5);
                pixels[baseIndex + 1] = (byte)(c.G * 255.0 + 0.5);
                pixels[baseIndex + 2] = (byte)(c.R * 255.0 + 0.5);
                pixels[baseIndex + 3] = 255;
            }
            var bitmap = new WriteableBitmap(1, 256, 96, 96, PixelFormats.Bgra32, null);
            bitmap.WritePixels(new Int32Rect(0, 0, 1, 256), pixels, 4, 0);
            return bitmap;
        }
    }
}
