using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Velox.DB.Sqlite.WinPhone
{
    public class sqlite3
    {
        [Flags]
        public enum OpenFlags
        {
            ReadOnly = 1, ReadWrite = 2, Create = 4,
            NoMutex = 0x8000, FullMutex = 0x10000,
            SharedCache = 0x20000, PrivateCache = 0x40000,
            ProtectionComplete = 0x00100000,
            ProtectionCompleteUnlessOpen = 0x00200000,
            ProtectionCompleteUntilFirstUserAuthentication = 0x00300000,
            ProtectionNone = 0x00400000
        }

        public enum ReturnCode : int
        {
            Ok = 0,
            Error = 1,
            Internal = 2,
            Perm = 3,
            Abort = 4,
            Busy = 5,
            Locked = 6,
            NoMem = 7,
            ReadOnly = 8,
            Interrupt = 9,
            IOError = 10,
            Corrupt = 11,
            NotFound = 12,
            Full = 13,
            CantOpen = 14,
            ProtocolLock = 15,
            Empty = 16,
            SchemaChanged = 17,
            TooBig = 18,
            Constraint = 19,
            Mismatch = 20,
            Misuse = 21,
            NotSupportedOnHost = 22,
            Authorization = 23,
            Format = 24,
            Range = 25,
            NoDatabasse = 26,
            Notice = 27,
            Warning = 28,
            Row = 100,
            Done = 101
        }
        
        public enum ConfigOption
        {
            SingleThread = 1,
            MultiThread = 2,
            Serialized = 3
        }

        public enum ColumnType
        {
            Integer = 1,
            Float = 2,
            Text = 3,
            Blob = 4,
            Null = 5
        }

        [DllImport("sqlite3", EntryPoint = "sqlite3_open", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode open([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode open_v2([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr db, int flags, IntPtr zvfs);

        [DllImport("sqlite3", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode close(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_initialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode initialize();

        [DllImport("sqlite3", EntryPoint = "sqlite3_shutdown", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode shutdown();

        [DllImport("sqlite3", EntryPoint = "sqlite3_config", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode config(ConfigOption option);

        [DllImport("sqlite3", EntryPoint = "sqlite3_win32_set_directory", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int win32_set_directory(uint directoryType, string directoryPath);

        [DllImport("sqlite3", EntryPoint = "sqlite3_busy_timeout", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode busy_timeout(IntPtr db, int milliseconds);

        [DllImport("sqlite3", EntryPoint = "sqlite3_changes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int changes(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode prepare_v2(IntPtr db, [MarshalAs(UnmanagedType.LPStr)] string sql, int numBytes, out IntPtr stmt, IntPtr pzTail);

        [DllImport("sqlite3", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode step(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_reset", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode reset(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        public static extern ReturnCode finalize(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_last_insert_rowid", CallingConvention = CallingConvention.Cdecl)]
        public static extern long last_insert_rowid(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_errmsg16", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr errmsg16(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_parameter_index", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bind_parameter_index(IntPtr stmt, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_null", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bind_null(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bind_int(IntPtr stmt, int index, int val);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bind_int64(IntPtr stmt, int index, long val);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bind_double(IntPtr stmt, int index, double val);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_text16", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int bind_text16(IntPtr stmt, int index, [MarshalAs(UnmanagedType.LPWStr)] string val, int n, IntPtr free);

        [DllImport("sqlite3", EntryPoint = "sqlite3_bind_blob", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bind_blob(IntPtr stmt, int index, byte[] val, int n, IntPtr free);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
        public static extern int column_count(IntPtr stmt);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_name", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr column_name_(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_name16", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr column_name_16(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern ColumnType column_type(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_int", CallingConvention = CallingConvention.Cdecl)]
        public static extern int column_int(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_int64", CallingConvention = CallingConvention.Cdecl)]
        public static extern long column_int64(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_double", CallingConvention = CallingConvention.Cdecl)]
        public static extern double column_double(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr column_text_(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_text16", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr column_text16(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_blob", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr column_blob_(IntPtr stmt, int index);

        [DllImport("sqlite3", EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
        public static extern int column_bytes(IntPtr stmt, int index);

        public static string column_name(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(column_name_16(stmt, index));
        }

        public static string column_text(IntPtr stmt, int index)
        {
            return Marshal.PtrToStringUni(column_text16(stmt, index));
        }

        public static byte[] column_blob(IntPtr stmt, int index)
        {
            int numBytes = column_bytes(stmt, index);
            var bytes = new byte[numBytes];

            if (numBytes > 0)
                Marshal.Copy(column_blob_(stmt, index), bytes, 0, numBytes);

            return bytes;
        }

        [DllImport("sqlite3", EntryPoint = "sqlite3_extended_errcode", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_extended_errcode(IntPtr db);

        [DllImport("sqlite3", EntryPoint = "sqlite3_libversion_number", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_libversion_number();
    }
}
