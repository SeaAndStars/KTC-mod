# Kingdom Enhanced i18n 设计

## 目标

为 Kingdom Enhanced 增加可运行时切换的中文/英文 i18n，实现：

- F1 菜单的 Settings 页面切换语言。
- 切换立即刷新当前菜单、HUD、通知、TTS 和调试显示。
- 语言选择通过 BepInEx 配置持久化。
- Mono 与 IL2CPP 使用同一套加载服务和资源文件。
- 用户可见翻译不直接写入 C# 逻辑代码。

## 已确认决策

- 资源格式：外部 UTF-8 JSON。
- 语言范围：简体中文 `zh-CN`、英文 `en-US`。
- 默认语言：简体中文。
- 缺失键回退顺序：当前语言 → 英文 → 资源键。
- 切换入口：F1 → Settings。
- 资源目录：与 `KingdomEnhanced.dll` 同级的 `Localization` 目录。
- 不增加第三方 JSON 依赖，使用 Unity `JsonUtility` 解析对象和条目数组，以同时兼容 `net6.0` IL2CPP 与 `netstandard2.1` Mono。

## 覆盖范围

纳入资源化的运行时用户可见内容：

- F1 菜单标题、标签页、Settings、功能名称、分类、描述、锁定原因和状态。
- Kingdom Monitor、HUD、通知、反馈消息和调试区域标签。
- TTS 播报文本、可支付对象的简化名称和环境提示。
- IL2CPP、Mono 共用的功能元数据文本。

不纳入运行时语言切换的内容：

- C# 类型名、方法名、配置键名和文件名。
- 面向开发者的日志文本。
- 上游开发文档和许可证原文；README 只补充中文安装与语言切换说明。

## 资源格式

每种语言一个文件：

- `KingdomEnhanced/Localization/en-US.json`
- `KingdomEnhanced/Localization/zh-CN.json`

JSON 使用固定文档结构，包含语言标识、显示名称、回退语言和条目数组。每个条目以稳定资源键映射一个字符串值。资源键只描述用途，不承载具体语言文本；值支持 Unity GUI 使用的富文本标记和换行转义。

## 运行时架构

新增 `KingdomEnhanced/Core/LocalizationService.cs`，负责：

1. 从插件程序集所在目录的 `Localization` 子目录发现 JSON 文件。
2. 使用 Unity `JsonUtility` 读取资源文档，并建立语言到键值字典的索引。
3. 校验语言文件、跳过空键和重复键，并记录加载错误而不阻止插件启动。
4. 提供按键取值、当前语言切换、可用语言列表和切换事件。
5. 对未知语言、缺失文件和缺失键执行确定性回退。

`Settings` 新增字符串配置项 `Language`，默认值为 `zh-CN`。插件初始化顺序调整为先加载语言资源，再初始化配置和 UI；读取到非法值时使用中文并写回有效语言标识。切换语言时更新配置值并保存配置文件，UI 下一帧通过资源键重新取值。

## UI 接入方式

- `FeatureMeta` 保存 `LabelKey`、`SectionKey`、`DescriptionKey` 和 `LockReasonKey`，不保存最终语言文本。
- `ModMenuFeatures` 的注册方法接收资源键。
- `ModMenu`、`GuiHelper`、`KingdomMonitor`、`WorldManager`、`AccessibilityFeature` 和相关功能类在绘制或播报时调用统一取值服务。
- 标签页数组、Settings 文本、状态文本和通知模板全部改为资源键。
- 切换语言不重建功能逻辑、不改变存档和作弊状态，只改变显示与播报文本。

## 构建与发布

- `.csproj` 将 `Localization/*.json` 作为内容文件复制到输出目录。
- Windows 构建脚本和发布脚本把 `Localization` 目录放入 IL2CPP、Mono 两种发布包的插件目录。
- 不把资源文件复制到系统目录，也不依赖当前工作目录。

## 验证标准

- `BIE6_IL2CPP` 构建成功。
- `BIE6_Mono` 构建成功。
- 两种构建输出都包含两个 JSON 文件。
- JSON 键集合一致，中文和英文没有重复键或空值。
- 静态检查确认运行时用户可见文本均通过资源键获取。
- 游戏内验证：默认中文、切换英文、切回中文、重启后语言保持。
- 资源文件缺失或 JSON 损坏时插件仍能启动并回退到资源键/英文。

