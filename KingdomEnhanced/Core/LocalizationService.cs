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
    /// Represents a fixed-schema localization resource document.
    /// </summary>
    [Serializable]
    public sealed class LocalizationDocument
    {
        /// <summary>
        /// Language code, e.g. en-US or zh-CN.
        /// </summary>
        public string language;

        /// <summary>
        /// Display name shown in the settings UI.
        /// </summary>
        public string displayName;

        /// <summary>
        /// Fallback language code declared by this document.
        /// </summary>
        public string fallback;

        /// <summary>
        /// All resource entries contained in this document.
        /// </summary>
        public LocalizationEntry[] entries;
    }

    /// <summary>
    /// Represents a single key-value JSON resource entry.
    /// </summary>
    [Serializable]
    public sealed class LocalizationEntry
    {
        /// <summary>
        /// Stable resource key.
        /// </summary>
        public string key;

        /// <summary>
        /// Translated text for the resource key.
        /// </summary>
        public string value;
    }

    /// <summary>
    /// Provides localization catalog loading, language switching and text fallback.
    /// </summary>
    public static class LocalizationService
    {
        /// <summary>
        /// Default language code.
        /// </summary>
        private const string DefaultLanguageCode = "en-US";

        /// <summary>
        /// Loaded language catalogs: language code -> (resource key -> text) map.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> LoadedCatalogs =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loaded language codes, kept in a stable display order.
        /// </summary>
        private static readonly List<string> AvailableLanguageCodes = new List<string>();

        /// <summary>
        /// Currently active language code.
        /// </summary>
        private static string _currentLanguageCode = DefaultLanguageCode;

        /// <summary>
        /// Gets the currently active language code.
        /// </summary>
        public static string CurrentLanguageCode => _currentLanguageCode;

        /// <summary>
        /// Initializes localization resources and selects the language from config.
        /// </summary>
        /// <param name="localizationDirectory">Localization directory next to the plugin DLL.</param>
        /// <param name="configuredLanguage">Language code stored in the config file.</param>
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
                // When the external directory is missing or empty, fall back to the embedded catalogs so the menu and narration always have usable text.
                LoadEmbeddedCatalogs();
            }

            ApplyConfiguredLanguage(configuredLanguage);
        }

        /// <summary>
        /// Loads all localization catalogs from the assembly embedded resources (built-in DLL fallback).
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
        /// Gets the text for a resource key, falling back through current language, English, then the raw key.
        /// </summary>
        /// <param name="key">Stable resource key.</param>
        /// <returns>Localized text; returns the raw key itself when missing.</returns>
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
        /// Gets formatted text for a resource key, using the same fallback strategy as <see cref="Get"/>.
        /// </summary>
        /// <param name="key">Stable resource key.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>Formatted localized text; returns the raw template when formatting fails.</returns>
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
        /// Immediately switches the current language and persists it to the BepInEx config.
        /// </summary>
        /// <param name="languageCode">Target language code.</param>
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
        /// Gets the list of loaded, available language codes.
        /// </summary>
        /// <returns>Snapshot of currently loaded language codes.</returns>
        public static IReadOnlyList<string> GetAvailableLanguages()
        {
            return AvailableLanguageCodes.ToArray();
        }

        /// <summary>
        /// Reads and validates one language catalog from a single JSON file.
        /// </summary>
        /// <param name="filePath">Path to the resource file.</param>
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
        /// Parses and registers one localization catalog (shared by external files and embedded resources).
        /// </summary>
        /// <param name="jsonText">Raw localization JSON text.</param>
        /// <param name="sourceName">Resource source name, used for logging only.</param>
        /// <returns>True when registered successfully, otherwise false.</returns>
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
        /// Converts localization text to a document object using the fixed-schema JSON parser.
        /// </summary>
        /// <param name="jsonText">Raw localization JSON text.</param>
        /// <returns>The parsed localization document on success; null on failure.</returns>
        private static LocalizationDocument DeserializeLocalizationDocument(string jsonText)
        {
            return new LocalizationJsonParser(jsonText).ParseDocument();
        }

        /// <summary>
        /// Provides fixed-schema JSON parsing for localization documents.
        /// </summary>
        private sealed class LocalizationJsonParser
        {
            /// <summary>
            /// Raw JSON text.
            /// </summary>
            private readonly string _jsonText;

            /// <summary>
            /// Current read position.
            /// </summary>
            private int _position;

            /// <summary>
            /// Initializes the fixed-schema localization JSON parser.
            /// </summary>
        /// <param name="jsonText">Raw JSON text.</param>
            public LocalizationJsonParser(string jsonText)
            {
                _jsonText = jsonText ?? throw new ArgumentNullException(nameof(jsonText));
                _position = 0;
            }

            /// <summary>
            /// Parses a complete localization document.
            /// </summary>
            /// <returns>The parsed localization document.</returns>
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
            /// Parses the localization entries array.
            /// </summary>
            /// <returns>The parsed localization entries array.</returns>
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
            /// Parses a single localization entry object.
            /// </summary>
            /// <returns>The parsed localization entry.</returns>
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
            /// Parses a JSON string and handles all supported escape sequences.
            /// </summary>
            /// <returns>The parsed string value.</returns>
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
            /// Parses a JSON escape sequence.
            /// </summary>
            /// <returns>The character represented by the escape sequence.</returns>
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
            /// Parses a JSON unicode escape sequence.
            /// </summary>
            /// <returns>The character represented by the unicode escape.</returns>
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
            /// Converts a single hex character to its numeric value.
            /// </summary>
            /// <param name="hexCharacter">Hex character.</param>
            /// <returns>The corresponding decimal value.</returns>
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
            /// Skips all JSON whitespace starting at the current position.
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
            /// Reads and returns a single character.
            /// </summary>
            /// <returns>The character at the current position.</returns>
            private char ReadCharacter()
            {
                if (IsAtEnd())
                {
                    throw CreateFormatException("unexpected end of JSON.");
                }

                return _jsonText[_position++];
            }

            /// <summary>
            /// Asserts the current position is the expected character.
            /// </summary>
            /// <param name="expectedCharacter">Expected character.</param>
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
            /// Attempts to consume the expected character.
            /// </summary>
            /// <param name="expectedCharacter">Character to match.</param>
            /// <returns>True when matched, otherwise false.</returns>
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
            /// Asserts the property name has not already appeared in the current object scope.
            /// </summary>
            /// <param name="alreadySeen">Whether the property has already appeared.</param>
            /// <param name="propertyName">Property name.</param>
            private void EnsureUniqueProperty(bool alreadySeen, string propertyName)
            {
                if (alreadySeen)
                {
                    throw CreateFormatException($"duplicate field: {propertyName}");
                }
            }

            /// <summary>
            /// Ensures no trailing non-whitespace content remains after parsing.
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
            /// Determines whether the end of the JSON text has been reached.
            /// </summary>
            /// <returns>True when the end has been reached.</returns>
            private bool IsAtEnd()
            {
                return _position >= _jsonText.Length;
            }

            /// <summary>
            /// Creates a format exception with position context.
            /// </summary>
            /// <param name="message">Exception message.</param>
            /// <returns>A format exception carrying position information.</returns>
            private FormatException CreateFormatException(string message)
            {
                return new FormatException($"{message} position={_position}");
            }
        }

        /// <summary>
        /// Determines the final language from the configured value, default language and loaded catalogs, persisting it back when needed.
        /// </summary>
        /// <param name="configuredLanguage">Language code from the config file.</param>
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
        /// Attempts to resolve a language code to the canonical form of a loaded language.
        /// </summary>
        /// <param name="languageCode">Language code to resolve.</param>
        /// <param name="resolvedLanguageCode">Resolved canonical language code.</param>
        /// <returns>True when the language is loaded.</returns>
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
        /// Attempts to read the text for a key from the specified language catalog.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <param name="key">Resource key.</param>
        /// <param name="value">Read text.</param>
        /// <returns>True when the value was read successfully, otherwise false.</returns>
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
        /// Persists the language setting to the BepInEx config file.
        /// </summary>
        /// <param name="languageCode">Language code to persist.</param>
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
        /// Logs a safe localization warning message.
        /// </summary>
        /// <param name="message">Log message.</param>
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
