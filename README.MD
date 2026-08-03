# Kingdom Enhanced

Kingdom Two Crowns 的 BepInEx 6 模组，支持 Mono 与 IL2CPP。

## 安装与语言

1. 下载与游戏后端、系统架构匹配的发布包，解压到游戏根目录。
2. 启动游戏后按 `F1`，在 `Settings` 中切换语言；选择会自动持久化。
3. 默认语言为英文，支持英文（`en-US`）和简体中文（`zh-CN`）。

手动安装时，保持以下目录结构：

```text
BepInEx/plugins/KingdomEnhanced/
  KingdomEnhanced.dll
  Localization/en-US.json
  Localization/zh-CN.json
```

若 `Localization` 目录缺失或语言文件损坏，模组仍会启动：优先回退英文；找不到对应文本时显示资源键。
