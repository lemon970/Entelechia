# Entelechia / 隐德来希

《杀戮尖塔 2》自定义角色 Mod，以鲜血、萃血和心烛为主要机制。

## 安装

1. 安装 `BaseLib 3.3.5` 或更高兼容版本。
2. 从 Releases 下载发布包。
3. 将压缩包中的 `Entelechia` 文件夹放入游戏目录的 `mods` 文件夹。

最终目录应为：

```text
Slay the Spire 2/
└─ mods/
   └─ Entelechia/
      ├─ Entelechia.dll
      ├─ Entelechia.json
      └─ Entelechia.pck
```

最低游戏版本：`0.107.0`。

## 构建

项目使用 .NET 9、Godot 4.5.1 和 BaseLib 3.3.5。

```powershell
dotnet build Entelechia.csproj -c Release
```

发布 PCK 前，将 `GODOT_PATH` 设置为 Godot 4.5.1 控制台程序路径，或向 MSBuild 传入 `GodotPath`：

```powershell
dotnet publish Entelechia.csproj -c Release /p:GodotPath="<path-to-godot-console>"
```

`Sts2PathDiscovery.props` 会尝试从 Steam 安装位置查找游戏；也可以传入 `/p:Sts2Path="<game-directory>"`。

## 说明

这是非官方同人 Mod，与 Mega Crit 或《明日方舟》官方无关。
