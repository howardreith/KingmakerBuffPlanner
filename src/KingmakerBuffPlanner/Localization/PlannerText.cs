using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner
{
    internal static class PlannerLanguage
    {
        internal const string Automatic = "automatic";
        internal const string English = "enGB";
        internal const string Chinese = "zhCN";
        internal static string Normalize(string value)
        {
            return value == English || value == Chinese ? value : Automatic;
        }
        internal static string Resolve(string preference, string gameLocale)
        {
            string selected = Normalize(preference);
            return selected == Automatic ? (gameLocale == Chinese ? Chinese : English) : selected;
        }
    }

    internal sealed class MessageCatalog
    {
        private static readonly Regex Slot = new Regex(@"\{(0|[1-9][0-9]*)\}");
        private static readonly Regex Tag = new Regex(@"</?(?:b|i|color|size)(?:=[^<>]+)?>");
        private readonly Dictionary<string, string> _english;
        private readonly Dictionary<string, string> _chinese = new Dictionary<string, string>(StringComparer.Ordinal);

        internal MessageCatalog(TextReader english, TextReader chinese, Action<string> warn)
        {
            _english = Read(english);
            foreach (var entry in _english)
                if (!Valid(entry.Value, entry.Value)) throw new FormatException(entry.Key);
            try
            {
                var translated = Read(chinese);
                foreach (var entry in _english)
                {
                    string value;
                    if (translated.TryGetValue(entry.Key, out value) && Valid(entry.Value, value))
                        _chinese.Add(entry.Key, value);
                    else warn("Invalid or missing zhCN entry " + entry.Key);
                }
                foreach (string extra in translated.Keys.Except(_english.Keys)) warn("Unknown zhCN entry " + extra);
            }
            catch (Exception error) { warn("Cannot read zhCN catalog: " + error.Message); }
        }

        private static Dictionary<string, string> Read(TextReader input)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var reader = new JsonTextReader(input))
            {
                if (!reader.Read() || reader.TokenType != JsonToken.StartObject) throw new FormatException("Expected catalog object.");
                while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
                {
                    string key = (string)reader.Value;
                    if (!reader.Read() || reader.TokenType != JsonToken.String) throw new FormatException(key);
                    values.Add(key, (string)reader.Value);
                }
                if (reader.TokenType != JsonToken.EndObject || reader.Read()) throw new FormatException("Incomplete catalog.");
            }
            return values;
        }

        private static bool Valid(string english, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || Slot.Replace(value, "").IndexOfAny(new[] { '{', '}' }) >= 0)
                return false;
            // Unrecognized or unterminated markup is never passed to TMP.
            if (Tag.Replace(value, "").Contains("<") || Tag.Replace(english, "").Contains("<")) return false;
            var nesting = new Stack<string>();
            foreach (string tag in Tokens(Tag, value))
            {
                string name = tag.Trim('<', '>', '/').Split('=')[0];
                if (tag.StartsWith("</", StringComparison.Ordinal))
                {
                    if (nesting.Count == 0 || nesting.Pop() != name) return false;
                }
                else nesting.Push(name);
            }
            return nesting.Count == 0 && Tokens(Slot, english).OrderBy(x => x).SequenceEqual(Tokens(Slot, value).OrderBy(x => x))
                && Tokens(Tag, english).SequenceEqual(Tokens(Tag, value));
        }

        private static IEnumerable<string> Tokens(Regex pattern, string value)
        {
            return pattern.Matches(value).Cast<Match>().Select(m => m.Value);
        }

        internal string Format(string language, string key, params object[] args)
        {
            string value;
            if (language != PlannerLanguage.Chinese || !_chinese.TryGetValue(key, out value))
                if (!_english.TryGetValue(key, out value)) return key;
            return args.Length == 0 ? value : string.Format(CultureInfo.InvariantCulture, value, args);
        }
    }

    internal static class PlannerText
    {
        private static MessageCatalog _catalog;
        internal static string Language { get; set; } = PlannerLanguage.English;
        internal static bool NativeChinese { get; set; }
        internal static bool Chinese { get { return Language == PlannerLanguage.Chinese; } }

        internal static void Initialize(Action<string> warn)
        {
            using (var english = Open("enGB"))
            using (var chinese = Open("zhCN"))
                _catalog = new MessageCatalog(english, chinese, warn);
        }

        private static TextReader Open(string language)
        {
            Stream stream = typeof(PlannerText).Assembly.GetManifestResourceStream("KBP.Text." + language + ".json");
            return stream == null ? (TextReader)new StringReader("") : new StreamReader(stream);
        }

        internal static string Routine(string id)
        {
            switch (id)
            {
                case "long": case "important": case "short": return Get("routine." + id);
                default: return id ?? string.Empty;
            }
        }

        internal static string Get(string key, params object[] args)
        {
            if (_catalog == null) Initialize(message => System.Diagnostics.Trace.WriteLine(message));
            return _catalog.Format(Language, key, args);
        }
    }
}
