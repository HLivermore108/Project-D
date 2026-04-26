using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public sealed class SQLiteGameDatabase : IDisposable
{
    private const string DatabaseFileName = "game_data.sqlite";
    private const int SqliteOk = 0;
    private const int SqliteRow = 100;
    private const int SqliteDone = 101;
    private const int SqliteOpenReadWrite = 0x00000002;
    private const int SqliteOpenCreate = 0x00000004;
    private const int SqliteOpenNoMutex = 0x00008000;

    private static readonly IntPtr SqliteTransient = new IntPtr(-1);
    private IntPtr connection = IntPtr.Zero;

    public string DatabasePath { get; }

    public SQLiteGameDatabase()
    {
        DatabasePath = Path.Combine(Application.persistentDataPath, DatabaseFileName);
    }

    public void Initialize()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        int result = sqlite3_open_v2(ToUtf8(DatabasePath), out connection, SqliteOpenReadWrite | SqliteOpenCreate | SqliteOpenNoMutex, IntPtr.Zero);
        if (result != SqliteOk)
        {
            Debug.LogWarning($"Could not open SQLite database at {DatabasePath}: {GetLastError()}");
            return;
        }

        ExecuteNonQuery(
            "CREATE TABLE IF NOT EXISTS game_saves (" +
            "id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "high_score INTEGER NOT NULL, " +
            "last_score INTEGER NOT NULL, " +
            "last_health INTEGER NOT NULL, " +
            "saved_at TEXT NOT NULL" +
            ");");
    }

    public void WriteSave(GameSaveData data)
    {
        if (connection == IntPtr.Zero || data == null)
            return;

        IntPtr statement = Prepare(
            "INSERT INTO game_saves (high_score, last_score, last_health, saved_at) " +
            "VALUES (?, ?, ?, ?);");
        if (statement == IntPtr.Zero)
            return;

        try
        {
            sqlite3_bind_int(statement, 1, data.highScore);
            sqlite3_bind_int(statement, 2, data.lastScore);
            sqlite3_bind_int(statement, 3, data.lastHealth);
            BindText(statement, 4, DateTime.UtcNow.ToString("O"));

            int result = sqlite3_step(statement);
            if (result != SqliteDone)
            {
                Debug.LogWarning($"Could not write SQLite save data: {GetLastError()}");
            }
        }
        finally
        {
            sqlite3_finalize(statement);
        }
    }

    public GameSaveData ReadLatestSave()
    {
        if (connection == IntPtr.Zero)
            return null;

        IntPtr statement = Prepare(
            "SELECT high_score, last_score, last_health, saved_at " +
            "FROM game_saves ORDER BY id DESC LIMIT 1;");
        if (statement == IntPtr.Zero)
            return null;

        try
        {
            int result = sqlite3_step(statement);
            if (result != SqliteRow)
                return null;

            return new GameSaveData
            {
                highScore = sqlite3_column_int(statement, 0),
                lastScore = sqlite3_column_int(statement, 1),
                lastHealth = sqlite3_column_int(statement, 2),
                lastSavedUtc = Marshal.PtrToStringAnsi(sqlite3_column_text(statement, 3))
            };
        }
        finally
        {
            sqlite3_finalize(statement);
        }
    }

    public void Dispose()
    {
        if (connection == IntPtr.Zero)
            return;

        sqlite3_close(connection);
        connection = IntPtr.Zero;
    }

    private void ExecuteNonQuery(string sql)
    {
        IntPtr errorMessage;
        int result = sqlite3_exec(connection, ToUtf8(sql), IntPtr.Zero, IntPtr.Zero, out errorMessage);
        if (result == SqliteOk)
            return;

        string message = errorMessage != IntPtr.Zero ? Marshal.PtrToStringAnsi(errorMessage) : GetLastError();
        sqlite3_free(errorMessage);
        Debug.LogWarning($"SQLite command failed: {message}");
    }

    private IntPtr Prepare(string sql)
    {
        IntPtr statement;
        int result = sqlite3_prepare_v2(connection, ToUtf8(sql), -1, out statement, IntPtr.Zero);
        if (result == SqliteOk)
            return statement;

        Debug.LogWarning($"Could not prepare SQLite statement: {GetLastError()}");
        return IntPtr.Zero;
    }

    private static void BindText(IntPtr statement, int index, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
        sqlite3_bind_text(statement, index, bytes, bytes.Length, SqliteTransient);
    }

    private string GetLastError()
    {
        return connection != IntPtr.Zero ? Marshal.PtrToStringAnsi(sqlite3_errmsg(connection)) : "No connection";
    }

    private static byte[] ToUtf8(string value)
    {
        return Encoding.UTF8.GetBytes((value ?? string.Empty) + "\0");
    }

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr zvfs);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_close(IntPtr db);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_exec(IntPtr db, byte[] sql, IntPtr callback, IntPtr firstArg, out IntPtr errorMessage);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern void sqlite3_free(IntPtr value);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int byteCount, out IntPtr statement, IntPtr tail);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_step(IntPtr statement);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_finalize(IntPtr statement);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_int(IntPtr statement, int index, int value);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_text(IntPtr statement, int index, byte[] value, int byteCount, IntPtr destructor);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_column_int(IntPtr statement, int columnIndex);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_column_text(IntPtr statement, int columnIndex);

    [DllImport("winsqlite3", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_errmsg(IntPtr db);
}
