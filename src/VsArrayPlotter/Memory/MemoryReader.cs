using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using VsArrayPlotter.Models;

namespace VsArrayPlotter.Memory
{
    /// <summary>
    /// 只读读取指定进程的虚拟内存（ReadProcessMemory），并按元素类型解码为数组。
    /// 仅读取、不写入，权限要求与目标进程同级或更高（管理员权限可读绝大多数进程）。
    /// </summary>
    public static class MemoryReader
    {
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const long MaxBytes = 256L * 1024 * 1024; // 单次读取上限 256MB，防止误填地址

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// 从 pid 进程的 address 起连续读取 count 个元素并解码。
        /// </summary>
        public static ArrayData Read(int pid, IntPtr address, ElementType type, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentException("元素个数必须大于 0");
            }

            int elementSize = ElementTypeInfo.GetSize(type);
            long total = (long)count * elementSize;
            if (total > MaxBytes)
            {
                throw new ArgumentException(
                    string.Format("请求的字节数（{0} MB）超过上限 256 MB，请减小元素个数。", total / 1024 / 1024));
            }

            IntPtr hProcess = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);
            if (hProcess == IntPtr.Zero)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    string.Format("无法打开进程 PID={0}（可能需要以管理员身份运行 VS）。", pid));
            }

            try
            {
                byte[] buffer = new byte[(int)total];
                IntPtr bytesRead;
                if (!ReadProcessMemory(hProcess, address, buffer, buffer.Length, out bytesRead))
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error(),
                        "读取内存失败：地址无效、地址越界或该内存区域不可读。");
                }
                if (bytesRead.ToInt64() < total)
                {
                    throw new InvalidOperationException(
                        string.Format("只读取到 {0} 字节，不足要求的 {1} 字节。", bytesRead.ToInt64(), total));
                }

                return Decode(buffer, type, count);
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        private static ArrayData Decode(byte[] buf, ElementType type, int count)
        {
            double[] real = new double[count];
            double[] imag = (type == ElementType.Complex64 || type == ElementType.Complex128)
                ? new double[count]
                : null;

            int off = 0;
            for (int i = 0; i < count; i++)
            {
                switch (type)
                {
                    case ElementType.Int8:
                        real[i] = (sbyte)buf[off++];
                        break;
                    case ElementType.UInt8:
                        real[i] = buf[off++];
                        break;
                    case ElementType.Int16:
                        real[i] = BitConverter.ToInt16(buf, off);
                        off += 2;
                        break;
                    case ElementType.UInt16:
                        real[i] = BitConverter.ToUInt16(buf, off);
                        off += 2;
                        break;
                    case ElementType.Int32:
                        real[i] = BitConverter.ToInt32(buf, off);
                        off += 4;
                        break;
                    case ElementType.UInt32:
                        real[i] = BitConverter.ToUInt32(buf, off);
                        off += 4;
                        break;
                    case ElementType.Int64:
                        real[i] = BitConverter.ToInt64(buf, off);
                        off += 8;
                        break;
                    case ElementType.UInt64:
                        real[i] = BitConverter.ToUInt64(buf, off);
                        off += 8;
                        break;
                    case ElementType.Single:
                        real[i] = BitConverter.ToSingle(buf, off);
                        off += 4;
                        break;
                    case ElementType.Double:
                        real[i] = BitConverter.ToDouble(buf, off);
                        off += 8;
                        break;
                    case ElementType.Complex64:
                        real[i] = BitConverter.ToSingle(buf, off);
                        imag[i] = BitConverter.ToSingle(buf, off + 4);
                        off += 8;
                        break;
                    case ElementType.Complex128:
                        real[i] = BitConverter.ToDouble(buf, off);
                        imag[i] = BitConverter.ToDouble(buf, off + 8);
                        off += 16;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type));
                }
            }

            return new ArrayData
            {
                Real = real,
                Imag = imag,
                UsedCount = count
            };
        }
    }
}
