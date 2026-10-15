# 全体人物基础素材与许可

本目录记录 12 位角色的人体、眼睛、皮肤、头发与服装来源，并保留轻量原始模型和拟合数据。逐文件来源、固定版本、SHA256、运行贴图位置见 [source-manifest.json](source-manifest.json)。人物身份与剧情名称以 `Assets/Scripts/CharacterRoster.cs` 为准；旁白复用文生的展示模型，不另算一个人物。

## 12 位角色对应表

所有角色的头部及眼睛基础均来自下述 MakeHuman 官方核心素材。服装身体、年龄对应皮肤和发型分别如下；角色脸形、身材、颜色及配饰是在这些来源上分别调整的。

| 游戏模型 | 显示身份 | Rocketbox 身体/服装基础 | 皮肤变体 | MakeHuman 发型基础 |
| --- | --- | --- | --- | --- |
| Wensheng | 陈文生 | Male_Adult_08 | young_male | short02 |
| Mother | 母亲 | Female_Adult_15 | middleage_female | ponytail01 |
| Aman | 阿满（妹妹） | Female_Child_02 | young_female | ponytail01 |
| Wife | 妻子 | Female_Adult_15 | young_female | braid01 |
| Worker | 帮工乡亲 | Male_Adult_08 | young_male | short03 |
| Uncle | 族亲 | Male_Adult_08 | middleage_male | short04 |
| Xusheng | 许生 | Male_Adult_08 | young_male | short01 |
| Guide | 接应同乡 | Male_Adult_08 | middleage_male | short04 |
| Foreman | 码头工头 | Male_Adult_08 | middleage_male | short03 |
| Shopkeeper | 陈掌柜 | Male_Adult_08 | middleage_male | short04 |
| MasterHe | 何师傅 | Male_Adult_08 | middleage_male | short04 |
| Clerk | 信局伙计 | Male_Adult_08 | young_male | short04 |

2.0.6 的三位家人分别调整脸型、眼型、眉形、鼻部、下颌与嘴部比例，并在既有 CC0 发片和贴图上制作独立发型：母亲将 `ponytail01` 改成向后梳理的鬓发与低发髻，加入细灰发；妻子使用 `braid01` 改成露出眉眼的轻侧分与长辫；阿满将 `ponytail01` 的后发束复制、重新拟合为两侧短发束，并加入布发绳。三者保留各自年龄和身份，所用原始发型文件仍按上表归档。

## MakeHuman 官方核心素材：CC0 1.0 Universal

- [官方核心仓库固定版本](https://github.com/makehumancommunity/makehuman/tree/a8bc2d54ff0ac92e78ff71431b1023eda42bf482)：`a8bc2d54ff0ac92e78ff71431b1023eda42bf482`。
- [官方许可说明](https://static.makehumancommunity.org/about/license.html)；完整许可保留于 [LICENSE.ASSETS.md](LICENSE.ASSETS.md)。
- `base.obj` 对应上游 `makehuman/data/3dobjs/base.obj`；`makehuman/data/targets/` 保存亚洲男女性青年、儿童、长辈和脸形/眼部等形变数据，供差异调整与复现编辑使用。
- `makehuman/data/eyes/high-poly/` 保存眼球 OBJ 与 MHCLO 拟合数据；棕色虹膜原图位于 [`Assets/Resources/CharacterArt/brown_eye.png`](../../Assets/Resources/CharacterArt/brown_eye.png)。原目录已有的虹膜副本继续保留。
- 2.0.6 补存 [`nose-scale-vert-decr.target`](makehuman/data/targets/nose/nose-scale-vert-decr.target)，用于阿满鼻部纵向比例调整；该文件保留上游 CC0 声明及原始顶点编号，固定提交与 SHA256 已列入清单。
- 本目录只收录素材数据；MakeHuman 程序代码采用另一套许可，本目录不包含其上游 Python 程序。

## MakeHuman 官方系统皮肤与发型：CC0 1.0 Universal

来源为 [官方系统素材包及逐项许可表](https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html)，下载地址为 [makehuman_system_assets_cc0.zip](https://files2.makehumancommunity.org/asset_packs/makehuman_system_assets/makehuman_system_assets_cc0.zip)。该压缩包地址不带固定提交号，因此清单以选用文件的 SHA256 标识本次素材版本。

选用 4 份皮肤：`young_asian_male`、`middleage_asian_male`、`young_asian_female`、`middleage_asian_female`；6 款发型：`short01`、`short02`、`short03`、`short04`、`ponytail01`、`braid01`。上游材质/拟合文件的 CC0 声明和署名原样保留，其中列有 Data Collection AB、Joel Palmius、Jonas Hauquier。

`makehuman-system/skins/` 保留原始 `.mhmat`；`makehuman-system/hair/` 保留原始 `.obj`、`.mhclo`、`.mhmat`。皮肤和发型 PNG 已存在运行资源 [`Assets/Resources/CharacterArt/`](../../Assets/Resources/CharacterArt/)，本目录不再复制一套图片。各素材的原图文件名与 `QP2_SkinTexture_*` / `QP2_HairAsset_*` 路径见清单。编辑原始 `.mhmat` 时，需将其相对贴图路径指向这些运行 PNG，或按清单还原原目录。

## Microsoft Rocketbox 身体与服装：MIT

来源为 [Microsoft Rocketbox 固定版本](https://github.com/microsoft/Microsoft-Rocketbox/tree/0943055db6ec570bcef9f2c8b41c9e5467c808f9)，提交 `0943055db6ec570bcef9f2c8b41c9e5467c808f9`。版权为 Copyright (c) 2020 Microsoft；完整 MIT 许可及版权声明见 [rocketbox/LICENSE.md](rocketbox/LICENSE.md)。重新分发这些素材或其重要衍生部分时，应一并保留该声明。

本目录保存三个原始 FBX：

- [`Male_Adult_08`](rocketbox/Male_Adult_08/Male_Adult_08.fbx)：9 位男性的身体、手、鞋与服装基础，贴图标识 `m014`。
- [`Female_Adult_15`](rocketbox/Female_Adult_15/Female_Adult_15.fbx)：母亲和妻子的身体、手、鞋与服装基础，贴图标识 `f018`。
- [`Female_Child_02`](rocketbox/Female_Child_02/Female_Child_02.fbx)：阿满的身体、手、鞋与服装基础，贴图标识 `cf011`。

本项目为这些基础身体替换头部、适配比例与服装样式，调整领口/配色/配饰，并转换为适合游戏四肢动作的连续蒙皮。游戏人物没有使用 Rocketbox 原人物头部。三个原始 FBX 和相关原贴图的 Git blob 已与上述固定提交核对；女性素材的原采集记录另保留为 [women-source-manifest.json](rocketbox/women-source-manifest.json)。

原始身体颜色、法线、高光共有 9 张 TGA，原文件合计约 113 MB。工程已有无损 PNG：`QP2_AuthoredCottonAndTwill_{color,normal,specular}.png`、`QP2_AuthoredWomen_f018_{color,normal,specular}.png`、`QP2_AuthoredWomen_cf011_{color,normal,specular}.png`。已逐像素核对与原 TGA 一致，故不重复打包 TGA；清单保留每张原 TGA 的固定来源 URL、Git blob、SHA256 及对应 PNG 的 SHA256。PNG 原像素与材质运行时调色分开记录。

## 素材保留与核对

清单共记录 141 项来源，其中 20 张运行图片已核对像素一致（4 皮肤、6 发型、1 虹膜、9 服装）。清单中的 `repository_path` 是仓库内保留文件，`runtime_path` 是不重复复制的图片位置。`.target`、`.mhclo` 和 `.obj` 的原始编号用于形变和拟合，编辑时应保持它们之间的对应关系。

人物、信件与经历属于本项目虚构叙事；来源人物资源不代表真实侨民肖像或历史当事人。
