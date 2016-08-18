using System;

namespace Iridium.DB
{
    internal interface ISqliteAPI
    {
        void open_v2(string fileName, out IntPtr db, SqliteOpenFlags openFlags);
        SqliteReturnCode close(IntPtr db);
        SqliteReturnCode step(IntPtr stmt);
        string errmsg(IntPtr dbHandle);
        SqliteExtendedErrorCode extended_errcode(IntPtr dbHandle);
        int changes(IntPtr dbHandle);
        long last_insert_rowid(IntPtr dbHandle);
        void finalize(IntPtr stmt);
        SqliteReturnCode prepare_v2(IntPtr dbHandle, string sql, out IntPtr stmt);
        int bind_parameter_index(IntPtr stmt, string paramName);
        void bind_int64(IntPtr stmt, int paramNumber, long value);
        void bind_int(IntPtr stmt, int paramNumber, int value);
        void bind_null(IntPtr stmt, int paramNumber);
        void bind_double(IntPtr stmt, int paramNumber, double value);
        void bind_text(IntPtr stmt, int paramNumber, string value);
        void bind_blob(IntPtr stmt, int paramNumber, byte[] value);
        int column_count(IntPtr stmt);
        string column_name(IntPtr stmt, int i);
        long column_int64(IntPtr stmt, int i);
        double column_double(IntPtr stmt, int i);
        string column_text(IntPtr stmt, int i);
        byte[] column_blob(IntPtr stmt, int i);
        SqliteColumnType column_type(IntPtr stmt, int i);
    }
}