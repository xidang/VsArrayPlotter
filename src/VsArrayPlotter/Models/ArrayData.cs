using System;

namespace VsArrayPlotter.Models
{
    /// <summary>从内存读取并解码后的数组数据。</summary>
    public class ArrayData
    {
        /// <summary>实部（实数类型即数值本身）。</summary>
        public double[] Real { get; set; }

        /// <summary>虚部（仅复数类型非空）。</summary>
        public double[] Imag { get; set; }

        /// <summary>实际使用的元素个数。</summary>
        public int UsedCount { get; set; }

        /// <summary>二维/三维时的行数。</summary>
        public int Rows { get; set; } = 1;

        /// <summary>二维/三维时的列数。</summary>
        public int Cols { get; set; } = 1;

        public bool IsComplex => Imag != null;

        /// <summary>第 i 个元素的幅值（实数取绝对值，复数取模）。</summary>
        public double MagnitudeAt(int i)
        {
            if (IsComplex)
            {
                double re = Real[i];
                double im = Imag[i];
                return Math.Sqrt(re * re + im * im);
            }
            return Math.Abs(Real[i]);
        }

        /// <summary>第 i 个元素的相位（度，仅复数有意义）。</summary>
        public double PhaseAt(int i)
        {
            return Math.Atan2(Imag[i], Real[i]) * 180.0 / Math.PI;
        }
    }
}
