using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KingdomEnhanced.Core
{
    /// <summary>
    /// 表示可由 Unity JsonUtility 反序列化的语言资源文档。
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

            if (string.IsNullOrWhiteSpace(localizationDirectory))
            {
                LogWarning("Localization 目录路径为空，已回退到默认语言配置。");
                ApplyConfiguredLanguage(configuredLanguage);
                return;
            }

            if (!Directory.Exists(localizationDirectory))
            {
                LogWarning($"Localization 目录不存在：{localizationDirectory}");
                ApplyConfiguredLanguage(configuredLanguage);
                return;
            }

            string[] resourceFiles = Directory
                .GetFiles(localizationDirectory, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string resourceFile in resourceFiles)
            {
                LoadLanguageFile(resourceFile);
            }

            ApplyConfiguredLanguage(configuredLanguage);
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
                LogWarning($"本地化格式化失败：key={key}，language={_currentLanguageCode}，reason={exception.Message}");
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
                LogWarning($"忽略未加载的语言切换请求：{languageCode}");
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
                LocalizationDocument document = JsonUtility.FromJson<LocalizationDocument>(jsonText);
                if (document == null)
                {
                    LogWarning($"本地化文件解析结果为空：{Path.GetFileName(filePath)}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(document.language))
                {
                    LogWarning($"本地化文件缺少有效 language：{Path.GetFileName(filePath)}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(document.displayName))
                {
                    LogWarning($"本地化文件缺少有效 displayName：{Path.GetFileName(filePath)}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(document.fallback))
                {
                    LogWarning($"本地化文件缺少有效 fallback：{Path.GetFileName(filePath)}");
                    return;
                }

                if (document.entries == null)
                {
                    LogWarning($"本地化文件缺少 entries：{Path.GetFileName(filePath)}");
                    return;
                }

                string languageCode = document.language.Trim();
                var resourceMap = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (LocalizationEntry entry in document.entries)
                {
                    if (entry == null)
                    {
                        LogWarning($"本地化文件包含空条目：{Path.GetFileName(filePath)}");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(entry.key))
                    {
                        LogWarning($"本地化文件包含空资源键：{Path.GetFileName(filePath)}");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(entry.value))
                    {
                        LogWarning($"本地化文件包含空资源值：{Path.GetFileName(filePath)} -> {entry.key}");
                        return;
                    }

                    string resourceKey = entry.key.Trim();
                    if (resourceMap.ContainsKey(resourceKey))
                    {
                        LogWarning($"本地化文件包含重复资源键：{Path.GetFileName(filePath)} -> {resourceKey}");
                        return;
                    }

                    resourceMap.Add(resourceKey, entry.value);
                }

                LoadedCatalogs[languageCode] = resourceMap;
                if (!AvailableLanguageCodes.Any(code => string.Equals(code, languageCode, StringComparison.OrdinalIgnoreCase)))
                {
                    AvailableLanguageCodes.Add(languageCode);
                }
            }
            catch (Exception exception)
            {
                LogWarning($"本地化文件加载失败：{Path.GetFileName(filePath)}，reason={exception.Message}");
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
                LogWarning($"默认英文资源缺失，已回退到首个可用语言：{_currentLanguageCode}");
                return;
            }

            _currentLanguageCode = DefaultLanguageCode;
            PersistLanguageSetting(DefaultLanguageCode);
            LogWarning("未加载到任何本地化资源，将直接回退到资源键显示。");
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
                LogWarning($"语言配置保存失败：{languageCode}，reason={exception.Message}");
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
