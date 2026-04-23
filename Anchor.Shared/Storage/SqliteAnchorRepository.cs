using System.Text.Json;
using Anchor.Shared.Models;
using Microsoft.Data.Sqlite;

namespace Anchor.Shared.Storage;

public sealed class SqliteAnchorRepository : IAnchorRepository, IAsyncDisposable
{
    private readonly SqliteConnection _conn;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SqliteAnchorRepository(string dbPath)
    {
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Init();
    }

    private void Init()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS intentions (
                session_id TEXT PRIMARY KEY,
                primary_goal TEXT NOT NULL,
                started_at TEXT NOT NULL,
                expected_end_at TEXT NOT NULL,
                if_cond TEXT NOT NULL,
                then_action TEXT NOT NULL,
                checkpoints_json TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS interoception (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL,
                answered_at TEXT NOT NULL,
                water INTEGER NOT NULL,
                food INTEGER NOT NULL,
                bathroom INTEGER NOT NULL,
                stood_up INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS anchors (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                room TEXT NOT NULL,
                feature_prints_json TEXT NOT NULL,
                expected_classes_json TEXT NOT NULL,
                registered_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS events (
                id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                kind INTEGER NOT NULL,
                at TEXT NOT NULL,
                details_json TEXT
            );
            CREATE TABLE IF NOT EXISTS kv (
                key TEXT PRIMARY KEY,
                value BLOB
            );
        """;
        cmd.ExecuteNonQuery();
    }

    public async Task SaveIntentionAsync(IntentionRecord r, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO intentions
            (session_id, primary_goal, started_at, expected_end_at, if_cond, then_action, checkpoints_json)
            VALUES ($id, $goal, $start, $end, $if, $then, $cps);
        """;
        cmd.Parameters.AddWithValue("$id", r.SessionId.ToString());
        cmd.Parameters.AddWithValue("$goal", r.PrimaryGoal);
        cmd.Parameters.AddWithValue("$start", r.StartedAt.ToString("o"));
        cmd.Parameters.AddWithValue("$end", r.ExpectedEndAt.ToString("o"));
        cmd.Parameters.AddWithValue("$if", r.IfCondition);
        cmd.Parameters.AddWithValue("$then", r.ThenAction);
        cmd.Parameters.AddWithValue("$cps", JsonSerializer.Serialize(r.ExplicitCheckpoints, JsonOpts));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IntentionRecord?> GetIntentionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT primary_goal, started_at, expected_end_at, if_cond, then_action, checkpoints_json FROM intentions WHERE session_id = $id";
        cmd.Parameters.AddWithValue("$id", sessionId.ToString());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        var cps = JsonSerializer.Deserialize<List<DateTimeOffset>>(r.GetString(5), JsonOpts) ?? new();
        return new IntentionRecord(
            sessionId,
            r.GetString(0),
            DateTimeOffset.Parse(r.GetString(1)),
            DateTimeOffset.Parse(r.GetString(2)),
            r.GetString(3),
            r.GetString(4),
            cps);
    }

    public async Task AddInteroceptionAsync(InteroceptionAnswer a, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO interoception (session_id, answered_at, water, food, bathroom, stood_up)
            VALUES ($sid, $at, $w, $f, $b, $s);
        """;
        cmd.Parameters.AddWithValue("$sid", a.SessionId.ToString());
        cmd.Parameters.AddWithValue("$at", a.AnsweredAt.ToString("o"));
        cmd.Parameters.AddWithValue("$w", (int)a.LastWater);
        cmd.Parameters.AddWithValue("$f", (int)a.LastFood);
        cmd.Parameters.AddWithValue("$b", (int)a.LastBathroom);
        cmd.Parameters.AddWithValue("$s", (int)a.LastStoodUp);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task UpsertAnchorObjectAsync(AnchorObject obj, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO anchors (id, name, room, feature_prints_json, expected_classes_json, registered_at)
            VALUES ($id, $name, $room, $fp, $cls, $at);
        """;
        cmd.Parameters.AddWithValue("$id", obj.Id.ToString());
        cmd.Parameters.AddWithValue("$name", obj.Name);
        cmd.Parameters.AddWithValue("$room", obj.Room);
        cmd.Parameters.AddWithValue("$fp", JsonSerializer.Serialize(obj.ReferenceFeaturePrints, JsonOpts));
        cmd.Parameters.AddWithValue("$cls", JsonSerializer.Serialize(obj.ExpectedVisionClasses, JsonOpts));
        cmd.Parameters.AddWithValue("$at", obj.RegisteredAt.ToString("o"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AnchorObject>> ListAnchorsAsync(CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, room, feature_prints_json, expected_classes_json, registered_at FROM anchors";
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<AnchorObject>();
        while (await r.ReadAsync(ct))
        {
            list.Add(ReadAnchor(r));
        }
        return list;
    }

    public async Task<AnchorObject?> GetAnchorAsync(Guid id, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, room, feature_prints_json, expected_classes_json, registered_at FROM anchors WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadAnchor(r) : null;
    }

    private static AnchorObject ReadAnchor(SqliteDataReader r)
    {
        var fps = JsonSerializer.Deserialize<List<float[]>>(r.GetString(3), JsonOpts) ?? new();
        var cls = JsonSerializer.Deserialize<List<string>>(r.GetString(4), JsonOpts) ?? new();
        return new AnchorObject(
            Guid.Parse(r.GetString(0)),
            r.GetString(1),
            r.GetString(2),
            fps,
            cls,
            DateTimeOffset.Parse(r.GetString(5)));
    }

    public async Task LogEventAsync(SessionEvent ev, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO events (id, session_id, kind, at, details_json) VALUES ($id, $sid, $k, $at, $d)";
        cmd.Parameters.AddWithValue("$id", ev.Id.ToString());
        cmd.Parameters.AddWithValue("$sid", ev.SessionId.ToString());
        cmd.Parameters.AddWithValue("$k", (int)ev.Kind);
        cmd.Parameters.AddWithValue("$at", ev.At.ToString("o"));
        cmd.Parameters.AddWithValue("$d", (object?)ev.DetailsJson ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SessionEvent>> EventsForSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, kind, at, details_json FROM events WHERE session_id = $sid ORDER BY at";
        cmd.Parameters.AddWithValue("$sid", sessionId.ToString());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<SessionEvent>();
        while (await r.ReadAsync(ct))
        {
            list.Add(new SessionEvent(
                Guid.Parse(r.GetString(0)),
                sessionId,
                (SessionEventKind)r.GetInt32(1),
                DateTimeOffset.Parse(r.GetString(2)),
                r.IsDBNull(3) ? null : r.GetString(3)));
        }
        return list;
    }

    public async Task<byte[]?> GetPairingSecretAsync(CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM kv WHERE key = 'pairing_secret'";
        var result = await cmd.ExecuteScalarAsync(ct);
        return result as byte[];
    }

    public async Task SetPairingSecretAsync(byte[] secret, CancellationToken ct = default)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO kv (key, value) VALUES ('pairing_secret', $v)";
        cmd.Parameters.AddWithValue("$v", secret);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        _conn.Dispose();
        return ValueTask.CompletedTask;
    }
}
