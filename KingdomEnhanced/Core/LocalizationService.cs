using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KingdomEnhanced.Core
{
    /// <summary>
    /// 表示固定 schema 的语言资源文档。
    /// </summary>
    [Serializable]
    public sealed class LocalizationDocument
    {
        /// <summary>
        /// 语言代码，例如 en-US 或 zh-CN。
        /// </summary>
        public string language;

        /// <summary>
        /// 设置界面展示用语言名称。
        /// </summary>
        public string displayName;

        /// <summary>
        /// 该语言声明的回退语言代码。
        /// </summary>
        public string fallback;

        /// <summary>
        /// 当前语言文档包含的全部资源条目。
        /// </summary>
        public LocalizationEntry[] entries;
    }

    /// <summary>
    /// 表示单个资源键和值的 JSON 条目。
    /// </summary>
    [Serializable]
    public sealed class LocalizationEntry
    {
        /// <summary>
        /// 稳定资源键。
        /// </summary>
        public string key;

        /// <summary>
        /// 资源键对应的翻译文本。
        /// </summary>
        public string value;
    }

    /// <summary>
    /// 提供本地化资源加载、语言切换和文本回退能力。
    /// </summary>
    public static class LocalizationService
    {
        /// <summary>
        /// 默认语言代码。
        /// </summary>
        private const string DefaultLanguageCode = "en-US";

        /// <summary>
        /// 已加载的语言资源字典，键为语言代码，值为资源键到文本的映射。
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> LoadedCatalogs =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 已加载语言代码列表，用于保持稳定的展示顺序。
        /// </summary>
        private static readonly List<string> AvailableLanguageCodes = new List<string>();

        /// <summary>
        /// 当前生效的语言代码。
        /// </summary>
        private static string _currentLanguageCode = DefaultLanguageCode;

        /// <summary>
        /// 获取当前生效的语言代码。
        /// </summary>
        public static string CurrentLanguageCode => _currentLanguageCode;

        /// <summary>
        /// 初始化本地化资源，并根据配置选择当前语言。
        /// </summary>
        /// <param name="localizationDirectory">插件 DLL 同级的 Localization 目录。</param>
        /// <param name="configuredLanguage">配置文件中保存的语言代码。</param>
        public static void Initialize(string localizationDirectory, string configuredLanguage)
        {
            LoadedCatalogs.Clear();
            AvailableLanguageCodes.Clear();
            _currentLanguageCode = DefaultLanguageCode;

            bool loadedAnyExternal = false;

            if (!string.IsNullOrWhiteSpace(localizationDirectory) && Directory.Exists(localizationDirectory))
            {
                string[] resourceFiles = Directory
                    .GetFiles(localizationDirectory, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (string resourceFile in resourceFiles)
                {
                    LoadLanguageFile(resourceFile);
                }

                loadedAnyExternal = LoadedCatalogs.Count > 0;
                if (loadedAnyExternal)
                {
                    LogWarning("External localization catalog(s) loaded from: " + localizationDirectory);
                }
            }

            if (!loadedAnyExternal)
            {
                // 外部目录缺失或为空时，回退到 DLL 内嵌目录，保证菜单与播报永远有可用文本。
                LoadEmbeddedCatalogs();
            }

            ApplyConfiguredLanguage(configuredLanguage);
        }

        /// <summary>
        /// 从当前程序集内嵌资源加载全部本地化目录（DLL 内置兜底）。
        /// </summary>
        private static void LoadEmbeddedCatalogs()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            int loadedCount = 0;

            try
            {
                string[] resourceNames = assembly.GetManifestResourceNames();
                foreach (string resourceName in resourceNames)
                {
                    if (!resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!resourceName.Contains(".Localization.")) continue;

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) continue;

                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string jsonText = reader.ReadToEnd();
                            if (LoadLanguageText(jsonText, resourceName))
                            {
                                loadedCount++;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                LogWarning($"Failed to load embedded localization catalogs: {exception.Message}");
            }

            if (loadedCount > 0)
            {
                LogWarning($"Embedded localization catalog(s) loaded ({loadedCount}) - external Localization folder was missing or empty.");
            }
            else if (LoadedCatalogs.Count == 0)
            {
                LogWarning("No localization catalogs loaded (external or embedded); falling back to raw resource keys.");
            }
        }

        /// <summary>
        /// 根据资源键获取当前语言文本，并按当前语言、英文、资源键的顺序回退。
        /// </summary>
        /// <param name="key">稳定资源键。</param>
        /// <returns>本地化文本；若资源缺失则返回资源键本身。</returns>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (TryGetValue(_currentLanguageCode, key, out string currentValue))
            {
                return currentValue;
            }

            if (!string.Equals(_currentLanguageCode, DefaultLanguageCode, StringComparison.OrdinalIgnoreCase) &&
                TryGetValue(DefaultLanguageCode, key, out string englishValue))
            {
                return englishValue;
            }

            return key;
        }

        /// <summary>
        /// 根据资源键获取格式化文本，并沿用与 <see cref="Get"/> 相同的回退策略。
        /// </summary>
        /// <param name="key">稳定资源键。</param>
        /// <param name="args">格式化参数。</param>
        /// <returns>格式化后的本地化文本；格式化失败时返回原模板。</returns>
        public static string Format(string key, params object[] args)
        {
            string template = Get(key);
            if (args == null || args.Length == 0)
            {
                return template;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException exception)
            {
                LogWarning($"Localization format failed: key={key}, language={_currentLanguageCode}, reason={exception.Message}");
                return template;
            }
        }

        /// <summary>
        /// 立即切换当前语言，并持久化到 BepInEx 配置。
        /// </summary>
        /// <param name="languageCode">目标语言代码。</param>
        public static void SetLanguage(string languageCode)
        {
            if (!TryResolveLanguageCode(languageCode, out string resolvedLanguageCode))
            {
                LogWarning($"Ignoring language switch request for unloaded language: {languageCode}");
                return;
            }

            _currentLanguageCode = resolvedLanguageCode;
            PersistLanguageSetting(resolvedLanguageCode);
        }

        /// <summary>
        /// 获取已加载的可用语言代码列表。
        /// </summary>
        /// <returns>当前已加载的语言代码快照。</returns>
        public static IReadOnlyList<string> GetAvailableLanguages()
        {
            return AvailableLanguageCodes.ToArray();
        }

        /// <summary>
        /// 从单个 JSON 文件读取并校验一种语言资源。
        /// </summary>
        /// <param name="filePath">资源文件路径。</param>
        private static void LoadLanguageFile(string filePath)
        {
            try
            {
                string jsonText = File.ReadAllText(filePath, Encoding.UTF8);
                LoadLanguageText(jsonText, Path.GetFileName(filePath));
            }
            catch (Exception exception)
            {
                LogWarning($"Failed to load localization file: {Path.GetFileName(filePath)}, reason={exception.Message}");
            }
        }

        /// <summary>
        /// 解析并注册一份本地化目录文本（外部文件与内嵌资源共用）。
        /// </summary>
        /// <param name="jsonText">原始本地化 JSON 文本。</param>
        /// <param name="sourceName">资源来源名称，仅用于日志。</param>
        /// <returns>成功注册返回 true，否则返回 false。</returns>
        private static bool LoadLanguageText(string jsonText, string sourceName)
        {
            try
            {
                LocalizationDocument document = DeserializeLocalizationDocument(jsonText);
                if (document == null)
                {
                    LogWarning($"Localization parse returned null: {sourceName}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(document.language))
                {
                    LogWarning($"Localization file missing valid language: {sourceName}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(document.displayName))
                {
                    LogWarning($"Localization file missing valid displayName: {sourceName}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(document.fallback))
                {
                    LogWarning($"Localization file missing valid fallback: {sourceName}");
                    return false;
                }

                if (document.entries == null)
                {
                    LogWarning($"Localization file missing entries: {sourceName}");
                    return false;
                }

                string languageCode = document.language.Trim();
                var resourceMap = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (LocalizationEntry entry in document.entries)
                {
                    if (entry == null)
                    {
                        LogWarning($"Localization file contains a null entry: {sourceName}");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(entry.key))
                    {
                        LogWarning($"Localization file contains an empty resource key: {sourceName}");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(entry.value))
                    {
                        LogWarning($"Localization file contains an empty value: {sourceName} -> {entry.key}");
                        return false;
                    }

                    string resourceKey = entry.key.Trim();
                    if (resourceMap.ContainsKey(resourceKey))
                    {
                        LogWarning($"Localization file contains a duplicate key: {sourceName} -> {resourceKey}");
                        return false;
                    }

                    resourceMap.Add(resourceKey, entry.value);
                }

                LoadedCatalogs[languageCode] = resourceMap;
                if (!AvailableLanguageCodes.Any(code => string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase)))
                {
                    AvailableLanguageCodes.Add(languageCode);
                }

                return true;
            }
            catch (Exception exception)
            {
                LogWarning($"Failed to load localization catalog: {sourceName}, reason={exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用固定 schema JSON 解析器将本地化文本转换为文档对象。
        /// </summary>
        /// <param name="jsonText">原始本地化 JSON 文本。</param>
        /// <returns>成功时返回托管本地化文档；失败时返回空。</returns>
        private static LocalizationDocument DeserializeLocalizationDocument(string jsonText)
        {
            return new LocalizationJsonParser(jsonText).ParseDocument();
        }

        /// <summary>
        /// 提供固定 schema 的本地化 JSON 解析能力。
        /// </summary>
        private sealed class LocalizationJsonParser
        {
            /// <summary>
            /// 原始 JSON 文本。
            /// </summary>
            private readonly string _jsonText;

            /// <summary>
            /// 当前读取位置。
            /// </summary>
            private int _position;

            /// <summary>
            /// 初始化固定 schema 本地化 JSON 解析器。
            /// </summary>
            /// <param name="jsonText">原始 JSON 文本。</param>
            public LocalizationJsonParser(string jsonText)
            {
                _jsonText = jsonText ?? throw new ArgumentNullException(nameof(jsonText));
                _position = 0;
            }

            /// <summary>
            /// 解析完整的本地化文档。
            /// </summary>
            /// <returns>解析成功后的本地化文档。</returns>
            public LocalizationDocument ParseDocument()
            {
                SkipWhitespace();
                ExpectCharacter('{');

                LocalizationDocument document = new LocalizationDocument();
                bool languageSeen = false;
                bool displayNameSeen = false;
                bool fallbackSeen = false;
                bool entriesSeen = false;

                SkipWhitespace();
                if (TryConsumeCharacter('}'))
                {
                    EnsureDocumentEnd();
                    return document;
                }

                while (true)
                {
                    string propertyName = ParseString();
                    SkipWhitespace();
                    ExpectCharacter(':');
                    SkipWhitespace();

                    switch (propertyName)
                    {
                        case "language":
                            EnsureUniqueProperty(languageSeen, propertyName);
                            document.language = ParseString();
                            languageSeen = true;
                            break;
                        case "displayName":
                            EnsureUniqueProperty(displayNameSeen, propertyName);
                            document.displayName = ParseString();
                            displayNameSeen = true;
                            break;
                        case "fallback":
                            EnsureUniqueProperty(fallbackSeen, propertyName);
                            document.fallback = ParseString();
                            fallbackSeen = true;
                            break;
                        case "entries":
                            EnsureUniqueProperty(entriesSeen, propertyName);
                            document.entries = ParseEntriesArray();
                            entriesSeen = true;
                            break;
                        default:
                            throw CreateFormatException($"unsupported top-level field: {propertyName}");
                    }

                    SkipWhitespace();
                    if (TryConsumeCharacter('}'))
                    {
                        break;
                    }

                    ExpectCharacter(',');
                    SkipWhitespace();
                }

                EnsureDocumentEnd();
                return document;
            }

            /// <summary>
            /// 解析本地化条目数组。
            /// </summary>
            /// <returns>解析成功后的本地化条目数组。</returns>
            private LocalizationEntry[] ParseEntriesArray()
            {
                ExpectCharacter('[');
                SkipWhitespace();

                List<LocalizationEntry> entries = new List<LocalizationEntry>();
                if (TryConsumeCharacter(']'))
                {
                    return entries.ToArray();
                }

                while (true)
                {
                    entries.Add(ParseEntryObject());
                    SkipWhitespace();

                    if (TryConsumeCharacter(']'))
                    {
                        return entries.ToArray();
                    }

                    ExpectCharacter(',');
                    SkipWhitespace();
                }
            }

            /// <summary>
            /// 解析单个本地化条目对象。
            /// </summary>
            /// <returns>解析成功后的本地化条目。</returns>
            private LocalizationEntry ParseEntryObject()
            {
                ExpectCharacter('{');
                SkipWhitespace();

                string key = null;
                string value = null;
                bool keySeen = false;
                bool valueSeen = false;

                if (TryConsumeCharacter('}'))
                {
                    throw CreateFormatException("localization entry cannot be an empty object.");
                }

                while (true)
                {
                    string propertyName = ParseString();
                    SkipWhitespace();
                    ExpectCharacter(':');
                    SkipWhitespace();

                    switch (propertyName)
                    {
                        case "key":
                            EnsureUniqueProperty(keySeen, propertyName);
                            key = ParseString();
                            keySeen = true;
                            if (string.IsNullOrWhiteSpace(key))
                            {
                                throw CreateFormatException("localization entry key cannot be empty.");
                            }

                            break;
                        case "value":
                            EnsureUniqueProperty(valueSeen, propertyName);
                            value = ParseString();
                            valueSeen = true;
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw CreateFormatException($"localization entry value cannot be empty: {key ?? "<unknown>"}");
                            }

                            break;
                        default:
                            throw CreateFormatException($"unsupported entry field: {propertyName}");
                    }

                    SkipWhitespace();
                    if (TryConsumeCharacter('}'))
                    {
                        break;
                    }

                    ExpectCharacter(',');
                    SkipWhitespace();
                }

                if (!keySeen)
                {
                    throw CreateFormatException("localization entry is missing key.");
                }

                if (!valueSeen)
                {
                    throw CreateFormatException($"localization entry is missing value: {key}");
                }

                return new LocalizationEntry
                {
                    key = key,
                    value = value
                };
            }

            /// <summary>
            /// 解析 JSON 字符串并处理所有支持的转义序列。
            /// </summary>
            /// <returns>解析成功后的字符串值。</returns>
            private string ParseString()
            {
                ExpectCharacter('"');
                StringBuilder builder = new StringBuilder();

                while (true)
                {
                    if (IsAtEnd())
                    {
                        throw CreateFormatException("string is missing closing quote.");
                    }

                    char currentCharacter = ReadCharacter();
                    if (currentCharacter == '"')
                    {
                        return builder.ToString();
                    }

                    if (currentCharacter == '\\')
                    {
                        builder.Append(ParseEscapeSequence());
                        continue;
                    }

                    if (currentCharacter < 0x20)
                    {
                        throw CreateFormatException("string contains unescaped control character.");
                    }

                    builder.Append(currentCharacter);
                }
            }

            /// <summary>
            /// 解析 JSON 转义序列。
            /// </summary>
            /// <returns>转义序列对应的字符。</returns>
            private char ParseEscapeSequence()
            {
                if (IsAtEnd())
                {
                    throw CreateFormatException("escape sequence is missing following characters.");
                }

                char escapeCharacter = ReadCharacter();
                switch (escapeCharacter)
                {
                    case '"':
                        return '"';
                    case '\\':
                        return '\\';
                    case '/':
                        return '/';
                    case 'b':
                        return '\b';
                    case 'f':
                        return '\f';
                    case 'n':
                        return '\n';
                    case 'r':
                        return '\r';
                    case 't':
                        return '\t';
                    case 'u':
                        return ParseUnicodeEscapeSequence();
                    default:
                        throw CreateFormatException($"unsupported escape sequence: \\{escapeCharacter}");
                }
            }

            /// <summary>
            /// 解析 JSON Unicode 转义序列。
            /// </summary>
            /// <returns>Unicode 转义对应的字符。</returns>
            private char ParseUnicodeEscapeSequence()
            {
                if (_position + 4 > _jsonText.Length)
                {
                    throw CreateFormatException("unicode escape sequence is shorter than 4 digits.");
                }

                int codePoint = 0;
                for (int index = 0; index < 4; index++)
                {
                    codePoint = (codePoint << 4) + ParseHexValue(ReadCharacter());
                }

                return (char)codePoint;
            }

            /// <summary>
            /// 将单个十六进制字符转换为数值。
            /// </summary>
            /// <param name="hexCharacter">十六进制字符。</param>
            /// <returns>对应的十进制数值。</returns>
            private int ParseHexValue(char hexCharacter)
            {
                if (hexCharacter >= '0' && hexCharacter <= '9')
                {
                    return hexCharacter - '0';
                }

                if (hexCharacter >= 'a' && hexCharacter <= 'f')
                {
                    return hexCharacter - 'a' + 10;
                }

                if (hexCharacter >= 'A' && hexCharacter <= 'F')
                {
                    return hexCharacter - 'A' + 10;
                }

                throw CreateFormatException($"invalid hex character: {hexCharacter}");
            }

            /// <summary>
            /// 跳过当前位置开始的所有 JSON 空白字符。
            /// </summary>
            private void SkipWhitespace()
            {
                while (!IsAtEnd())
                {
                    char currentCharacter = _jsonText[_position];
                    if (currentCharacter != ' ' &&
                        currentCharacter != '\t' &&
                        currentCharacter != '\r' &&
                        currentCharacter != '\n')
                    {
                        break;
                    }

                    _position++;
                }
            }

            /// <summary>
            /// 读取并返回一个字符。
            /// </summary>
            /// <returns>当前位置的字符。</returns>
            private char ReadCharacter()
            {
                if (IsAtEnd())
                {
                    throw CreateFormatException("unexpected end of JSON.");
                }

                return _jsonText[_position++];
            }

            /// <summary>
            /// 断言当前位置必须为指定字符。
            /// </summary>
            /// <param name="expectedCharacter">期望字符。</param>
            private void ExpectCharacter(char expectedCharacter)
            {
                if (IsAtEnd())
                {
                    throw CreateFormatException($"missing expected character: {expectedCharacter}");
                }

                char actualCharacter = ReadCharacter();
                if (actualCharacter != expectedCharacter)
                {
                    throw CreateFormatException($"expected character {expectedCharacter} but got {actualCharacter}");
                }
            }

            /// <summary>
            /// 尝试消费指定字符。
            /// </summary>
            /// <param name="expectedCharacter">待匹配字符。</param>
            /// <returns>匹配成功返回 true，否则返回 false。</returns>
            private bool TryConsumeCharacter(char expectedCharacter)
            {
                if (IsAtEnd() || _jsonText[_position] != expectedCharacter)
                {
                    return false;
                }

                _position++;
                return true;
            }

            /// <summary>
            /// 断言属性名称在当前对象范围内未重复出现。
            /// </summary>
            /// <param name="alreadySeen">该属性是否已出现。</param>
            /// <param name="propertyName">属性名称。</param>
            private void EnsureUniqueProperty(bool alreadySeen, string propertyName)
            {
                if (alreadySeen)
                {
                    throw CreateFormatException($"duplicate field: {propertyName}");
                }
            }

            /// <summary>
            /// 确认解析结束后不存在额外的非空白字符。
            /// </summary>
            private void EnsureDocumentEnd()
            {
                SkipWhitespace();
                if (!IsAtEnd())
                {
                    throw CreateFormatException("unexpected trailing content after JSON.");
                }
            }

            /// <summary>
            /// 判断是否已经到达 JSON 文本末尾。
            /// </summary>
            /// <returns>到达末尾时返回 true。</returns>
            private bool IsAtEnd()
            {
                return _position >= _jsonText.Length;
            }

            /// <summary>
            /// 创建带位置上下文的格式异常。
            /// </summary>
            /// <param name="message">异常消息。</param>
            /// <returns>带位置信息的格式异常。</returns>
            private FormatException CreateFormatException(string message)
            {
                return new FormatException($"{message} position={_position}");
            }
        }

        /// <summary>
        /// 根据配置值、默认语言和已加载语言列表确定最终语言，并在需要时写回配置。
        /// </summary>
        /// <param name="configuredLanguage">配置文件中的语言代码。</param>
        private static void ApplyConfiguredLanguage(string configuredLanguage)
        {
            if (TryResolveLanguageCode(configuredLanguage, out string configuredResolvedLanguage))
            {
                _currentLanguageCode = configuredResolvedLanguage;
                if (!string.Equals(configuredLanguage, configuredResolvedLanguage, StringComparison.Ordinal))
                {
                    PersistLanguageSetting(configuredResolvedLanguage);
                }

                return;
            }

            if (TryResolveLanguageCode(DefaultLanguageCode, out string defaultResolvedLanguage))
            {
                _currentLanguageCode = defaultResolvedLanguage;
                PersistLanguageSetting(defaultResolvedLanguage);
                return;
            }

            if (AvailableLanguageCodes.Count > 0)
            {
                _currentLanguageCode = AvailableLanguageCodes[0];
                PersistLanguageSetting(_currentLanguageCode);
                LogWarning($"Default English catalog missing; fell back to first available language: {_currentLanguageCode}");
                return;
            }

            _currentLanguageCode = DefaultLanguageCode;
            PersistLanguageSetting(DefaultLanguageCode);
            LogWarning("No localization catalogs loaded; falling back to raw resource keys.");
        }

        /// <summary>
        /// 尝试按语言代码解析出已加载语言的规范写法。
        /// </summary>
        /// <param name="languageCode">待解析的语言代码。</param>
        /// <param name="resolvedLanguageCode">解析成功后的规范语言代码。</param>
        /// <returns>若语言已加载则返回 true。</returns>
        private static bool TryResolveLanguageCode(string languageCode, out string resolvedLanguageCode)
        {
            resolvedLanguageCode = string.Empty;
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            string matchedLanguageCode = AvailableLanguageCodes.FirstOrDefault(
                code => string.Equals(code, languageCode.Trim(), StringComparison.OrdinalIgnoreCase));

            if (matchedLanguageCode == null)
            {
                return false;
            }

            resolvedLanguageCode = matchedLanguageCode;
            return true;
        }

        /// <summary>
        /// 尝试从指定语言资源中读取某个键的文本。
        /// </summary>
        /// <param name="languageCode">语言代码。</param>
        /// <param name="key">资源键。</param>
        /// <param name="value">读取到的文本。</param>
        /// <returns>读取成功返回 true，否则返回 false。</returns>
        private static bool TryGetValue(string languageCode, string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!LoadedCatalogs.TryGetValue(languageCode, out Dictionary<string, string> resourceMap))
            {
                return false;
            }

            return resourceMap.TryGetValue(key, out value);
        }

        /// <summary>
        /// 将语言设置写回 BepInEx 配置文件。
        /// </summary>
        /// <param name="languageCode">需要持久化的语言代码。</param>
        private static void PersistLanguageSetting(string languageCode)
        {
            if (Settings.Language == null)
            {
                return;
            }

            try
            {
                Settings.Language.Value = languageCode;
                Settings.Config?.Save();
            }
            catch (Exception exception)
            {
                LogWarning($"Failed to save language setting: {languageCode}, reason={exception.Message}");
            }
        }

        /// <summary>
        /// 记录安全的本地化警告日志。
        /// </summary>
        /// <param name="message">日志消息。</param>
        private static void LogWarning(string message)
        {
            if (Plugin.Instance != null)
            {
                Plugin.Instance.LogSource.LogWarning(message);
                return;
            }

            Debug.LogWarning($"[KingdomEnhanced] {message}");
        }
    }
}
