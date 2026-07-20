## STS2

这是 [Slay The Spire 2](https://store.steampowered.com/app/2868840/Slay_the_Spire_2/) 的反编译仓库，仅供 mod 开发和学习使用，侵删。

## 仓库运作方式

- `main` 是仓库的默认入口，指向当前最新的已归档版本。
- 每个游戏版本使用独立的同名分支保存，例如 `0.109.0` 和 `0.108.0`。

## 常见入口
- `/src/Core/Models/` 大部分游戏内容，比如卡牌，药水，敌人等
- `/localization/zhs/` 中文本地化文本，适合从名称<->键名双向查找，比如查找`虚空形态`找到键名`VoidForm`，从`MayhemPower`找到`乱战`

## 收录范围

- 源码由 [GDRE Tools](https://github.com/GDRETools/gdsdecomp) 解包获得。
- 主要收录代码、本地化、配置、Godot 场景/资源元数据以及其他文本类资源。
- 不包含二进制美术、音频、字体、模型、构建产物。

## 已归档版本

- `0.109.0`
- `0.108.0`
- `0.107.1`
