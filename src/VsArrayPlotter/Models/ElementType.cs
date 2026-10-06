using System;
using System.ComponentModel;

namespace VsArrayPlotter.Models
{
    /// <summary>内存数组的元素类型。</summary>
    public enum ElementType
    {
        Int8 = 0,
        UInt8 = 1,
        Int16 = 2,
        UInt16 = 3,
        Int32 = 4,
        UInt32 = 5,
        Int64 = 6,
        UInt64 = 7,
        Single = 8,
        Double = 9,
        Complex64 = 10,
        Complex128 = 11
    }

    public static class ElementTypeInfo
    {
        public static ElementType[] All { get; } = (ElementType[])Enum.GetValues(typeof(ElementType));

        /// <summary>单个元素占用的字节数（复数为一对实/虚部）。</summary>
        public static int GetSize(ElementType type)
        {
            switch (type)
            {
                case ElementType.Int8:
                case ElementType.UInt8:
                    return 1;
                case ElementType.Int16:
                case ElementType.UInt16:
                    return 2;
                case ElementType.Int32:
                case ElementType.UInt32:
                case ElementType.Single:
                    return 4;
                case ElementType.Int64:
                case ElementType.UInt64:
                case ElementType.Double:
                    return 8;
                case ElementType.Complex64:
                    return 8;   // 两个 float
                case ElementType.Complex128:
                    return 16;  // 两个 double
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public static string DisplayName(ElementType type)
        {
            switch (type)
            {
                case ElementType.Int8: return "int8";
                case ElementType.UInt8: return "uint8";
                case ElementType.Int16: return "int16";
                case ElementType.UInt16: return "uint16";
                case ElementType.Int32: return "int32";
                case ElementType.UInt32: return "uint32";
                case ElementType.Int64: return "int64";
                case ElementType.UInt64: return "uint64";
                case ElementType.Single: return "float32";
                case ElementType.Double: return "float64";
                case ElementType.Complex64: return "complex64 (float,float)";
                case ElementType.Complex128: return "complex128 (double,double)";
                default: return type.ToString();
            }
        }
    }

    /// <summary>绘图模式：一维折线 / 二维热图 / 三维曲面。</summary>
    public enum PlotMode
    {
        [Description("1D 折线")]
        Line1D,
        [Description("2D 热图")]
        Heat2D,
        [Description("3D 曲面")]
        Surface3D
    }

    /// <summary>Y 轴/幅值刻度：线性 / dB。</summary>
    public enum ScaleType
    {
        [Description("线性")]
        Linear,
        [Description("dB")]
        Db
    }

    /// <summary>枚举显示名辅助（优先取 Description 特性）。</summary>
    public static class EnumDisplay
    {
        public static string Of(Enum value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            var field = value.GetType().GetField(value.ToString());
            var attr = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attr != null ? attr.Description : value.ToString();
        }
    }
}
