# 素材与第三方说明

- **代码、剧情与本项目制作的资产**：包括故事、程序场景、服装与发型的项目适配、角色配饰、道具模型、原创音乐/音效、示意侨批和界面。人物网格、皮肤和发型的第三方素材来源单列如下，不将完整人物网格统称为全部原创。协作使用范围见 `协作使用说明.md`。虚构信件和人物不代表真实档案、真实肖像或真实当事人经历。
- **新文生（Meshy 试装）**：本项目原创人物造型图经用户同意上传到 Meshy，由用户升级后的 Studio 账号生成 `Quiet Resolve`（Meshy 7.1，Private），用户自行下载后在本地完成接缝合并、15 骨骼绑定和 Unity 材质适配。该新模型不来自下列 MakeHuman / Rocketbox 人物素材；旧文生文件仍保留作回退。源文件哈希与具体限制见 [模型说明](Assets/Resources/Models/WenshengMeshy/README.txt)。
- **新妻子与母亲（2026-10-13 人物更新）**：分别由本项目原创虚构人物造型图在 Meshy 7.1 生成。经用户授权，使用 Google Chrome 工作区提交并自动下载，每个模型消耗 35 积分。两人的原 GLB 分别保留；本地完成独立 15 骨身体绑定、裙装与袖口动作适配，保留原脸型、UV 和 2K 贴图。没有眨眼、口型、手指动作或布料模拟；旧人物文件仍保留为回退。来源与限制见 [妻子模型说明](Assets/Resources/Models/WifeMeshy/README.txt) 和 [母亲模型说明](Assets/Resources/Models/MotherMeshy/README.txt)。
- **保留的旧文生及其他 11 位人物的头部与眼睛基础**：文生、母亲、阿满、妻子、族亲、许生、接应同乡、码头工头、陈掌柜、何师傅、信局伙计、帮工乡亲使用 [MakeHuman 官方核心素材](https://github.com/makehumancommunity/makehuman/tree/a8bc2d54ff0ac92e78ff71431b1023eda42bf482)。固定提交为 `a8bc2d54ff0ac92e78ff71431b1023eda42bf482`；基础网格、形变目标、眼球网格与棕色虹膜采用 **CC0 1.0 Universal**。见 [官方许可说明](https://static.makehumancommunity.org/about/license.html) 和完整 [CC0 许可](美术源文件/人物基础/LICENSE.ASSETS.md)。MakeHuman 程序代码采用不同许可，本项目保留的是素材数据。
- **上述保留人物的皮肤与发型**：选用 [MakeHuman 官方系统素材包](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html) 的 `young_asian_male`、`middleage_asian_male`、`young_asian_female`、`middleage_asian_female` 皮肤，以及 `short01`、`short02`、`short03`、`short04`、`ponytail01`、`braid01` 发型，均为 **CC0**。上游文件中的原始许可、署名与拟合数据保留；角色拟合、轮廓调整与运行时材质由本项目完成。原始图片像素保存在 `Assets/Resources/CharacterArt/`，来源目录不再重复复制。
- **上述保留人物的身体、手部、鞋与服装基础**：采用 [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox/tree/0943055db6ec570bcef9f2c8b41c9e5467c808f9) 固定提交 `0943055db6ec570bcef9f2c8b41c9e5467c808f9`。9 位男性使用 `Male_Adult_08`；母亲与妻子使用 `Female_Adult_15`；阿满使用 `Female_Child_02`。Copyright (c) 2020 Microsoft，**MIT License**；完整许可见 [Rocketbox LICENSE](美术源文件/人物基础/rocketbox/LICENSE.md)，应随相关素材及其重要衍生部分保留。本项目替换原头部、调整衣饰与配色并适配连续蒙皮动作。身体颜色/法线/高光共 9 张原始 TGA 已转为像素一致的无损 PNG，清单保留原来源及哈希。
- **人物逐项清单**：角色与皮肤/发型/服装对应关系、三个轻量原始 FBX、原发型 OBJ/MHCLO/MHMAT 及许可证见 [人物基础说明](美术源文件/人物基础/README.md)；每项固定版本、来源 URL、SHA256 和运行贴图位置见 [source-manifest.json](美术源文件/人物基础/source-manifest.json)。
- **字体**：Noto Sans CJK SC 与 Noto Serif CJK SC，采用 SIL Open Font License 1.1。完整许可与版权见 `Assets/Resources/Fonts/LICENSE.txt`；字体内嵌版权与名称保持不变。
- **图标及插画**：部分图像通过生成式图像工具制作，图标提示词与创作说明见 `图标创作说明.md`，不标作实拍史料。
- **原创配乐**：程序合成，无外部歌曲或采样；以南音器乐音色与侨乡情感为创作意象，不是传统南音实录。可编辑素材见 `音乐源文件/`。
- **Unity 运行时**：由 Unity 构建工具随游戏生成，不属于本项目原创资产；编辑器与运行时受 Unity 自身许可约束。

## 为什么公开版没有原系统配音

原本机试作使用 macOS 系统中文语音。Apple macOS Tahoe 软件许可第2F节限制 System Voices 输出在公开分享等场景中的使用与分发，因此本仓库不包含这批录音。

官方许可：https://www.apple.com/legal/sla/docs/macOSTahoe.pdf

字幕、说话人、对话推进和配音播放接口都保留。没有配音资源时以字幕运行；若加入配音资源，构建检查要求完整覆盖台词，避免半套素材被误当作完成。Unity菜单“侨批 → 配音 → 导出完整离线配音清单”可生成 `tools/voice/manifest.json`，供录制或替换为具有相应分发授权的声音。

加入新的录音前，应注明声音来源、授权人和许可范围；不要直接提交原系统语音文件。原本机合成脚本未包含在公开工程。
