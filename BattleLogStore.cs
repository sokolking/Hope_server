using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BattleServer;

public sealed class BattleLogStore
{
    private const int MaxEntries = 2000;
    /// <summary>Cap DB persist queue: unbounded Task.Run per line exhausted Npgsql pool and thread pool under log bursts (whole process appeared hung).</summary>
    private const int PersistQueueCapacity = 16384;
    private readonly object _lock = new();
    private readonly List<BattleLogEntry> _entries = new();
    private readonly ConcurrentDictionary<Guid, Channel<BattleLogEntry>> _subscribers = new();
    private readonly Action<BattleLogEntry>? _persistEntry;
    private readonly Channel<BattleLogEntry>? _persistChannel;
    private long _nextId;

    public BattleLogStore(Action<BattleLogEntry>? persistEntry = null)
    {
        _persistEntry = persistEntry;
        if (persistEntry == null)
            return;

        _persistChannel = Channel.CreateBounded<BattleLogEntry>(new BoundedChannelOptions(PersistQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
        _ = Task.Run(() => RunPersistWorkerAsync(_persistChannel.Reader, persistEntry));
    }

    private static async Task RunPersistWorkerAsync(ChannelReader<BattleLogEntry> reader, Action<BattleLogEntry> persist)
    {
        try
        {
            await foreach (var entry in reader.ReadAllAsync())
            {
                try
                {
                    persist(entry);
                }
                catch
                {
                    // Never break persist loop due to one bad row.
                }
            }
        }
        catch
        {
            // Reader completed or host shutting down.
        }
    }

    public BattleLogEntry Append(string rawMessage, bool isError = false)
    {
        string normalized = Normalize(rawMessage);
        var entry = CreateEntry(normalized, isError);

        List<Channel<BattleLogEntry>> subscribers;
        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
            subscribers = _subscribers.Values.ToList();
        }

        foreach (var channel in subscribers)
            channel.Writer.TryWrite(entry);

        // One sequential worker + bounded queue: never spawn unbounded Task.Run → OpenConnection storms.
        if (_persistChannel != null)
            _persistChannel.Writer.TryWrite(entry);

        return entry;
    }

    public IReadOnlyList<BattleLogEntry> GetRecent(int take)
    {
        lock (_lock)
        {
            int safeTake = Math.Clamp(take, 1, MaxEntries);
            int skip = Math.Max(0, _entries.Count - safeTake);
            return _entries.Skip(skip).ToArray();
        }
    }

    public (Guid subscriptionId, ChannelReader<BattleLogEntry> reader) Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<BattleLogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        _subscribers[id] = channel;
        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (_subscribers.TryRemove(subscriptionId, out var channel))
            channel.Writer.TryComplete();
    }

    private BattleLogEntry CreateEntry(string raw, bool isError)
    {
        string tag = DetectTag(raw);
        string level = DetectLevel(raw, isError);
        string message = StripLeadingTag(raw);
        return new BattleLogEntry
        {
            Id = Interlocked.Increment(ref _nextId),
            Utc = DateTimeOffset.UtcNow,
            Level = level,
            Tag = tag,
            Message = message,
            Raw = raw
        };
    }

    private static string Normalize(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            return string.Empty;
        return rawMessage.Replace("\r\n", "\n").Trim();
    }

    private static string DetectTag(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "system";

        int open = raw.IndexOf('[');
        int close = raw.IndexOf(']');
        if (open >= 0 && close > open)
        {
            string candidate = raw[(open + 1)..close].Trim();
            if (!string.IsNullOrEmpty(candidate) && candidate.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_'))
                return candidate;
        }

        if (raw.Contains(" GET ", StringComparison.Ordinal) ||
            raw.Contains(" POST ", StringComparison.Ordinal) ||
            raw.Contains(" PUT ", StringComparison.Ordinal) ||
            raw.Contains(" DELETE ", StringComparison.Ordinal))
            return "http";

        return "system";
    }

    private static string DetectLevel(string raw, bool isError)
    {
        if (isError)
            return "error";
        if (raw.Contains(" error", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains(" failed", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("exception", StringComparison.OrdinalIgnoreCase))
            return "error";
        if (raw.Contains("warn", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains(" reject", StringComparison.OrdinalIgnoreCase))
            return "warn";
        return "info";
    }

    private static string StripLeadingTag(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        int close = raw.IndexOf(']');
        if (raw.StartsWith("[", StringComparison.Ordinal) && close >= 0 && close + 1 < raw.Length)
            return raw[(close + 1)..].TrimStart();

        return raw;
    }
}

public sealed class BattleLogEntry
{
    public long Id { get; init; }
    public DateTimeOffset Utc { get; init; }
    public string Level { get; init; } = "info";
    public string Tag { get; init; } = "system";
    public string Message { get; init; } = "";
    public string Raw { get; init; } = "";
}

public sealed class BattleLogConsoleWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly BattleLogStore _store;
    private readonly bool _isError;
    private readonly StringBuilder _buffer = new();
    private readonly object _sync = new();

    public BattleLogConsoleWriter(TextWriter inner, BattleLogStore store, bool isError)
    {
        _inner = inner;
        _store = store;
        _isError = isError;
    }

    public override Encoding Encoding => _inner.Encoding;

    public override void Write(char value)
    {
        string? toPersist = null;
        lock (_sync)
        {
            _inner.Write(value);
            if (value == '\n')
            {
                toPersist = TakeBufferForPersist();
            }
            else if (value != '\r')
                _buffer.Append(value);
        }

        if (toPersist != null)
            _store.Append(toPersist, _isError);
    }

    public override void Write(string? value)
    {
        if (value == null)
            return;

        List<string>? pending = null;
        lock (_sync)
        {
            _inner.Write(value);
            foreach (char ch in value)
            {
                if (ch == '\n')
                {
                    var line = TakeBufferForPersist();
                    if (line != null)
                    {
                        pending ??= new List<string>();
                        pending.Add(line);
                    }
                }
                else if (ch != '\r')
                    _buffer.Append(ch);
            }
        }

        if (pending != null)
        {
            foreach (var line in pending)
                _store.Append(line, _isError);
        }
    }

    public override void WriteLine(string? value)
    {
        string? toPersist;
        lock (_sync)
        {
            _inner.WriteLine(value);
            if (!string.IsNullOrEmpty(value))
                _buffer.Append(value);
            toPersist = TakeBufferForPersist();
        }

        if (toPersist != null)
            _store.Append(toPersist, _isError);
    }

    public override void Flush()
    {
        string? toPersist = null;
        lock (_sync)
        {
            _inner.Flush();
            if (_buffer.Length > 0)
                toPersist = TakeBufferForPersist();
        }

        if (toPersist != null)
            _store.Append(toPersist, _isError);
    }

    /// <summary>Must run under <see cref="_sync"/>. Returns one logical line for persist, or null if empty.</summary>
    private string? TakeBufferForPersist()
    {
        string text = _buffer.ToString();
        _buffer.Clear();
        if (string.IsNullOrWhiteSpace(text))
            return null;
        return text;
    }
}
