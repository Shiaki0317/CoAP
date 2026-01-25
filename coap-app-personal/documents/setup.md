# セットアップ

.NET 8.0のセットアップの手順内容について説明する。

Linuxで.NET 8.0をセットアップするときの手順は以下となる。
これはシェルスクリプトにてまとめてある。

```sh
# Avalonia テンプレートのインストール
dotnet new install Avalonia.Templates

# プロジェクトの作成
mkdir CoapDesktopSender
cd CoapDesktopSender

# ソリューションの作成
dotnet new sln

# Coreライブラリ
dotnet new classlib -n CoapDesktopSender.Core -f net8.0

# Avalonia UI
dotnet new avalonia.app -n CoapDesktopSender.UI -f net8.0

dotnet sln add CoapDesktopSender.Core
dotnet sln add CoapDesktopSender.UI

dotnet add CoapDesktopSender.UI reference CoapDesktopSender.Core

# 必要なNuGetパッケージの追加
dotnet add CoapDesktopSender.Core package Waher.Networking.CoAP --version 3.1.2
dotnet add CoapDesktopSender.Core package PeterO.Cbor --version 4.5.5
dotnet add CoapDesktopSender.UI package CommunityToolkit.Mvvm --version 8.4.0
```

参考資料

- [.NET CLI の概要](https://learn.microsoft.com/ja-jp/dotnet/core/tools/)
- [.NET プロジェクト SDK](https://learn.microsoft.com/ja-jp/dotnet/core/project-sdk/overview)
- [dotnet new コマンド](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-new)
- [dotnet sln コマンド](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-sln)
- [dotnet new list コマンド](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-new-list)
- [dotnet new install コマンド](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-new-install)
- [dotnet new install Avalonia.Templates](https://www.nuget.org/packages/Avalonia.Templates/)
- [Avalonia.Templates](https://github.com/AvaloniaUI/avalonia-dotnet-templates)

## テンプレートのインストール

`dotnet new list`にて`dotnet new`コマンドより生成可能なリストの一覧を表示できる。
この中に利用したいテンプレートが存在しない場合には`dotnet new install <package>... [options]`にて新規でインストールすることができる。

デスクトップ開発用にWPF("Windows Presentation Foundation")という「Microsoftが提供するWindowsデスクトップアプリケーションのユーザーインターフェース（UI）を開発するためのフレームワーク」が提供されているが、開発環境がVisualStudioであり、OSもWindowsに限定されていることからここでは利用しないことにした。

そのため、WPFに比較的近いものであり、開発環境やOSが依存しない"[Avalonia Templates](https://github.com/AvaloniaUI/avalonia-dotnet-templates)"を利用することとした。テンプレートのインストール方法として`dotnet new install Avalonia.Templates`を利用することという記載がGitHubにて説明されていたため、こちらの方法を利用した。
また、.net 8.0もサポートされていたことから、こちらのものの利用を決定した。

## プロジェクト・ソリューションの生成

`dotnet new sln`にて新規のソリューションファイルを作成する。
ソリューションファイルとは、Visual Studio / dotnet が「ソリューションとして、どの構成・どのプラットフォームで、どんなプロジェクト群を扱うか」を管理するメタデータになる。
作成時のファイル名には、{フォルダ名}.slnになる。

以下がソリューションファイルの内容となり、ここではまだプロジェクトが登録されていない「空のソリューション」の状態になる。

```txt
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
EndGlobal
```

### ソリューションファイルの説明

前に示したソリューションファイルは以下の内容が含まれている。
ただし、設定するソリューションファイルとして下記の項目は基本的に必要となる項目になる。
このソリューションに新しくプロジェクトを追加するなどを行うと、"Project セクション"などの項目が新規で追加される。

1. ヘッダ部
2. Visual Studio バージョン情報
3. Global セクション
4. ビルド構成定義
5. ソリューション表示設定

より詳細な情報は以下のサイトなどを参照する。

- [ソリューション (.sln) ファイル](https://learn.microsoft.com/ja-jp/visualstudio/extensibility/internals/solution-dot-sln-file?view=visualstudio)
- [sln ファイルのヘッダーと Visual Studio のバージョン対応](https://qiita.com/kenjiuno/items/a3978fb40b2929692a24)
- [.slnファイルの完全解剖――「住所録」と「配線図」が織りなすビルドの裏側](https://note.com/tk_solution_arch/n/nf78f26c4da43)
- [.NETの新たなソリューションファイル形式(.slnx)](https://zenn.dev/nuskey/articles/e07f70b62105d5)

#### ヘッダ部

`Microsoft Visual Studio Solution File, Format Version 12.00`

これはソリューションファイルのフォーマット仕様バージョンを示しており、Visual Studio 2012以降で使われている現行フォーマットを表している。
Visual Studio 2022やdotnet CLIでもこのフォーマットを利用している。

#### Visual Studio バージョン情報

```txt
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
```

`# Visual Studio Version 17`はこのソリューションが作成されたVisual Studioのバージョンを示しており、"17"は"Visual Studio 2022"、"16"は"Visual Studio 2019"となる。

`VisualStudioVersion = 17.0.31903.59`は"最後に保存したVisual Studioの正確なビルド番号"を示しており、"メジャー.マイナー.ビルド.リビジョン"で構成されている。
これは設定の互換性チェックや機能差分の調整を目的として利用される。

`MinimumVisualStudioVersion = 10.0.40219.1`はこのソリューションを開くことが可能な最低の"Visual Studio"のバージョン情報を示しており、"メジャー.マイナー.ビルド.リビジョン"で構成されている。
ここで示すメジャーバージョンは"10"となっていることから、最低でも"Visual Studio 2010"以降でないと開くことができないことを示している。

#### Global セクション

```txt
Global
    ...
EndGlobal
```

ここはソリューション全体に適用される設定領域を表している。

#### ビルド構成定義

```txt
GlobalSection(SolutionConfigurationPlatforms) = preSolution
    Debug|Any CPU = Debug|Any CPU
    Release|Any CPU = Release|Any CPU
EndGlobalSection
```

これは"構成名|プラットフォーム = 構成名|プラットフォーム"で書かれたものとなり、詳細な設定内容は以下となる。

| 構成    | 意味                                                  |
| ------- | ----------------------------------------------------- |
| Debug   | デバッグ用ビルド                                      |
| Release | リリース用ビルド                                      |
| Any CPU | CPUアーキテクチャ非依存（x64 / x86 / ARM を自動適応） |

ここで書かれている"= preSolution"とは"プロジェクトが読み込まれる前に処理される"ことを表している。

#### ソリューション表示設定

```txt
GlobalSection(SolutionProperties) = preSolution
    HideSolutionNode = FALSE
EndGlobalSection
```

"HideSolutionNode"とは"Visual Studioのソリューションエクスプローラー表示制御"を行うのかを示す内容となり、TRUE・FALSE設定では以下の設定となる。

| 値    | 動作                                             |
| ----- | ------------------------------------------------ |
| FALSE | ソリューション名を表示                           |
| TRUE  | ソリューション名を非表示（プロジェクトだけ表示） |

## ライブラリの追加

`dotnet new classlib -n CoapDesktopSender.Core -f net8.0`

このコマンドは「CoapDesktopSender.Core という名前の .NET 8 向けクラスライブラリプロジェクトを、新しいフォルダとして作成する」ことを表している。
ちなみにここで実行しているコマンドを詳細にすると以下となる。

1. `dotnet new`

   .NET SDK にインストールされている テンプレートエンジンを起動し、指定されたテンプレートを元にプロジェクトを生成します。
   内部的には以下の内容を実施している。
   - ~/.dotnet/templates/ や SDK 内のテンプレートを検索
   - classlib テンプレートを展開
   - プレースホルダ（プロジェクト名、TFMなど）を置換
   - ファイルをカレントディレクトリにコピー

2. `classlib`

   これは**プロジェクト種別（テンプレート）**を指定しており、実行ファイルではなく、他のプロジェクトから参照されるDLL（ライブラリ）を作るためのプロジェクトを意味する。生成される成果物は"bin/Debug/net8.0/CoapDesktopSender.Core.dll"になる。

3. `-n CoapDesktopSender.Core`

   これはプロジェクト名兼フォルダ名を指定しており、これによって以下の内容が実行される。
   - フォルダが作られる
   - .csproj の名前が決まる
   - RootNamespace が設定される

   実際に起きることとして、以下のフォルダ・ファイルが生成される。

   ```txt
   ./CoapDesktopSender.Core/
   ├── CoapDesktopSender.Core.csproj
   └── Class1.cs
   ```

   .csproj として生成されるデータは以下の内容となる。

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
        </PropertyGroup>
   </Project>
   ```

   このcsprojで記載されている内容は以下となる。

   | 項目            | 説明                                                                                                                         | 設定内容                       |
   | --------------- | ---------------------------------------------------------------------------------------------------------------------------- | ------------------------------ |
   | TargetFramework | 対応するフレームワーク                                                                                                       | "net8.0" / "net8.0;net10.0" 等 |
   | ImplicitUsings  | 完全修飾名を指定したり、using ディレクティブを手動で追加したりしなくても、これらの名前空間で定義された型を使用できるかどうか | enable / disable               |
   | Nullable        | null許容参照型を許可するかどうか                                                                                             | enable / disable               |

4. `-f net8.0`

   ターゲットフレームワーク（TFM: Target Framework Moniker）をしており、このライブラリでは .NET 8 ランタイム / SDK 向けにビルドされることを表している。
   ビルド時の影響として使用可能なAPIが .NET 8 のものに限定され、出力パスが"bin/Debug/net8.0/"になる。
   また、NuGetパッケージの互換性が net8.0 と一致するものに制限される。

上記で説明したコマンドを実行して、実際に生成されたフォルダ・ファイル構成が以下になる。

```txt
CoapDesktopSender.Core
├── Class1.cs
├── CoapDesktopSender.Core.csproj
└── obj
    ├── CoapDesktopSender.Core.csproj.nuget.dgspec.json
    ├── CoapDesktopSender.Core.csproj.nuget.g.props
    ├── CoapDesktopSender.Core.csproj.nuget.g.targets
    ├── project.assets.json
    └── project.nuget.cache
```

参考にしたURLは以下となる

- [.NET6 usingディレクティブはどこ？](https://qiita.com/t_hane/items/3f866a27e9a28bfee784)
- [C#10のglobal usingを体験してみた。](https://oooomincrypto.hatenadiary.jp/entry/2022/04/24/202137)

## Avalonia UI

`dotnet new avalonia.app -n CoapDesktopSender.UI -f net8.0`

このコマンドは「Avalonia UI を使った .NET 8 対応のクロスプラットフォームGUIアプリの雛形をCoapDesktopSender.UI という名前で生成する」ことを表している。
ちなみにここで実行しているコマンドを詳細にすると以下となる。

1. `dotnet new`

   .NET SDK にインストールされている テンプレートエンジンを起動し、指定されたテンプレートを元にプロジェクトを生成します。
   内部的には以下の内容を実施している。
   - ~/.dotnet/templates/ や SDK 内のテンプレートを検索
   - avalonia.app テンプレートを展開
   - プレースホルダ（プロジェクト名、TFMなど）を置換
   - ファイルをカレントディレクトリにコピー

2. `avalonia.app`

   これは **プロジェクト種別（テンプレート）** を指定しており、Avalonia公式のデスクトップGUIアプリ用テンプレートを生成する。
   生成される成果物は実行ファイル(EXE / ELF / AppBundle)になる。

   ここで利用するテンプレートの特徴は以下となる。
   - クロスプラットフォーム
     - Linux
     - Windows
     - macOS
   - MVVM構成済み
   - XAML UI
   - .NET 8対応
   - OpenGL / Skia ベース描画

3. `-n CoapDesktopSender.UI`

   これはプロジェクト名兼フォルダ名を指定しており、これによって以下の内容が実行される。
   - フォルダ作成
   - プロジェクト名設定
   - ルート名前空間設定
   - アセンブリ名設定

   実際に起きることとして、以下のフォルダ・ファイルが生成される。

   ```txt
   CoapDesktopSender.UI/
   ├── App.axaml
   ├── App.axaml.cs
   ├── app.manifest
   ├── MainWindow.axaml
   ├── MainWindow.axaml.cs
   ├── Program.cs
   ├── obj/
   └── CoapDesktopSender.UI.csproj
   ```

   .csproj として生成されるデータは以下の内容となる。

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
       <PropertyGroup>
           <OutputType>WinExe</OutputType>
           <TargetFramework>net8.0</TargetFramework>
           <Nullable>enable</Nullable>
           <ApplicationManifest>app.manifest</ApplicationManifest>
           <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
       </PropertyGroup>
       <ItemGroup>
           <PackageReference Include="Avalonia" Version="11.3.11" />
           <PackageReference Include="Avalonia.Desktop" Version="11.3.11" />
           <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.11" />
           <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.11" />
           <!--Condition below is needed to remove Avalonia.Diagnostics package from build output in Release configuration.-->
           <PackageReference Include="Avalonia.Diagnostics" Version="11.3.11">
               <IncludeAssets Condition="'$(Configuration)' != 'Debug'">None</IncludeAssets>
               <PrivateAssets Condition="'$(Configuration)' != 'Debug'">All</PrivateAssets>
           </PackageReference>
       </ItemGroup>
   </Project>
   ```

   このcsprojで記載されている内容は以下となる。

   | 項目                                 | 説明                                                         | 設定内容                                  |
   | ------------------------------------ | ------------------------------------------------------------ | ----------------------------------------- |
   | Project                              | .NET SDK スタイルプロジェクトであることを示す                | Sdk="Microsoft.NET.Sdk"                   |
   | PropertyGroup                        | ビルドと動作の基本設定                                       | 以下の内容                                |
   | OutputType                           | アプリケーションの種類を指定                                 | "Library" / "Exe" / "WinExe" 等           |
   | TargetFramework                      | ビルド対象のフレームワークを指定                             | "net8.0" / "net8.0;net10.0" 等            |
   | Nullable                             | null許容参照型を有効にするか                                 | "enable" / "disable"                      |
   | ApplicationManifest                  | アプリケーションマニフェストファイルを指定                   | "app.manifest"                            |
   | AvaloniaUseCompiledBindingsByDefault | Avaloniaのコンパイル済みバインディングを有効にするか         | "true" / "false"                          |
   | ItemGroup                            | ユーザー定義 の Item 要素のセットを格納                      | 以下の内容                                |
   | PackageReference                     | NuGetパッケージの依存関係をプロジェクトファイル内に直接指定  | 参考資料を参照                            |
   | IncludeAssets                        | これらのアセットは使用されます                               | all / none / contentfiles;analyzers;build |
   | ExcludeAssets                        | これらのアセットは使用されません                             | all / none / contentfiles;analyzers;build |
   | PrivateAssets                        | これらのアセットは使用されますが、親プロジェクトに流れません | all / none / contentfiles;analyzers;build |

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
   <!-- This manifest is used on Windows only.
       Don't remove it as it might cause problems with window transparency and embedded controls.
       For more details visit https://learn.microsoft.com/en-us/windows/win32/sbscs/application-manifests -->
   <assemblyIdentity version="1.0.0.0" name="CoapDesktopSender.UI.Desktop"/>

   <compatibility xmlns="urn:schemas-microsoft-com:compatibility.v1">
       <application>
       <!-- A list of the Windows versions that this application has been tested on
           and is designed to work with. Uncomment the appropriate elements
           and Windows will automatically select the most compatible environment. -->

       <!-- Windows 10 -->
       <supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
       </application>
   </compatibility>
   </assembly>
   ```

   参考資料
   - [MSBuild リファレンス](https://learn.microsoft.com/ja-jp/visualstudio/msbuild/msbuild-reference?view=visualstudio)
   - [ItemGroup 要素](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-new-item-group)
   - [プロジェクトファイル内のPackageReference](https://learn.microsoft.com/ja-jp/nuget/consume-packages/package-references-in-project-files)
   - [コンパイラ出力を制御する C# コンパイラ オプション](https://learn.microsoft.com/ja-jp/dotnet/csharp/language-reference/compiler-options/output)
   - [アプリケーションマニフェスト](https://learn.microsoft.com/ja-jp/windows/win32/sbscs/application-manifests)

4. `-f net8.0`

   ターゲットフレームワーク（TFM: Target Framework Moniker）をしており、このライブラリでは .NET 8 ランタイム / SDK 向けにビルドされることを表している。
   ビルド時の影響として使用可能なAPIが .NET 8 のものに限定され、出力パスが"bin/Debug/net8.0/"になる。
   また、NuGetパッケージの互換性が net8.0 と一致するものに制限される。

````txt
CoapDesktopSender.UI
├── App.axaml
├── App.axaml.cs
├── CoapDesktopSender.UI.csproj
├── MainWindow.axaml
├── MainWindow.axaml.cs
├── Program.cs
├── app.manifest
└── obj
    ├── CoapDesktopSender.UI.csproj.nuget.dgspec.json
    ├── CoapDesktopSender.UI.csproj.nuget.g.props
    ├── CoapDesktopSender.UI.csproj.nuget.g.targets
    ├── project.assets.json
    └── project.nuget.cache```
````

## ソリューションファイルとの紐付け

```sh
dotnet sln add CoapDesktopSender.Core
dotnet sln add CoapDesktopSender.UI
```

ここでは生成したプロジェクトをソリューションファイルに紐付けを行う。
このときのコマンドの構成として、`dotnet sln add <path-to-csproj>`が本来正しい形であり、追加する"csproj"を指定する。
そのため、`dotnet sln add CoapDesktopSender.Core\CoapDesktopSender.Core.csproj`が適切な形となる。
ただし、今回は直下に"csproj"が存在しているプロジェクトフォルダであり、csprojのファイル名とプロジェクトフォルダ名が同じであったため、成功している。

```sln
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CoapDesktopSender.Core", "CoapDesktopSender.Core\CoapDesktopSender.Core.csproj", "{238D5747-7727-46B7-860C-7CA288E52A5D}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CoapDesktopSender.UI", "CoapDesktopSender.UI\CoapDesktopSender.UI.csproj", "{A91848B7-EDC9-46FF-8256-76C65527D1C9}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {238D5747-7727-46B7-860C-7CA288E52A5D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {238D5747-7727-46B7-860C-7CA288E52A5D}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {238D5747-7727-46B7-860C-7CA288E52A5D}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {238D5747-7727-46B7-860C-7CA288E52A5D}.Release|Any CPU.Build.0 = Release|Any CPU
        {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
EndGlobal
```

csprojに新規で追加された項目をまとめる。

まずはプロジェクトセクションについて整理する。
プロジェクトセクションは`Project("プロジェクトタイプGUID") = "表示名", "パス", "プロジェクトGUID"`の構成から成立する。
ここでの内容を以下に記載する。

```sln
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CoapDesktopSender.Core", "CoapDesktopSender.Core\CoapDesktopSender.Core.csproj", "{238D5747-7727-46B7-860C-7CA288E52A5D}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CoapDesktopSender.UI", "CoapDesktopSender.UI\CoapDesktopSender.UI.csproj", "{A91848B7-EDC9-46FF-8256-76C65527D1C9}"
EndProject
```

"プロジェクトタイプGUID"は"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"となっており、これはC#プロジェクトであることを示す固定GUIDになる。これ以外にも固定GUIDが存在し、一例として以下のものが存在する。Visual Studioはこれでエディタ・ビルドシステム・デバッガの種類を切り替える。

| 種類     | GUID                                 |
| -------- | ------------------------------------ |
| C#       | FAE04EC0-301F-11D3-BF4B-00C04F79EFBC |
| C++      | 8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942 |
| フォルダ | 66A26720-8FB5-11D2-AA7E-00C04F688DDE |

また、"プロジェクトGUID"は"ソリューション内でのプロジェクトの一意となるID"を表しており、このGUIDによって全設定が紐づくことになる。

```sln
GlobalSection(ProjectConfigurationPlatforms) = postSolution
    {238D5747-7727-46B7-860C-7CA288E52A5D}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
    {238D5747-7727-46B7-860C-7CA288E52A5D}.Debug|Any CPU.Build.0 = Debug|Any CPU
    {238D5747-7727-46B7-860C-7CA288E52A5D}.Release|Any CPU.ActiveCfg = Release|Any CPU
    {238D5747-7727-46B7-860C-7CA288E52A5D}.Release|Any CPU.Build.0 = Release|Any CPU
    {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
    {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Debug|Any CPU.Build.0 = Debug|Any CPU
    {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Release|Any CPU.ActiveCfg = Release|Any CPU
    {A91848B7-EDC9-46FF-8256-76C65527D1C9}.Release|Any CPU.Build.0 = Release|Any CPU
EndGlobalSection
```

上記の構造は`{プロジェクトGUID}.ソリューション構成|プラットフォーム.設定種別 = プロジェクト構成|プラットフォーム`で成立している。

| 種類      | キー         | 役割                     |
| --------- | ------------ | ------------------------ |
| ActiveCfg | `.ActiveCfg` | **使う構成の指定**       |
| Build.0   | `.Build.0`   | **ビルド対象に含めるか** |

ActiveCfgはソリューションのビルド構成を指定する際に利用され、Build.0はソリューションのビルド対象に含めるプロジェクトを指定する際に利用される。
例えば、Debug時にはとあるプロジェクトをビルドしない場合、`{プロジェクトGUID}.Debug|Any CPU.Build.0 = Debug|Any CPU`を削除することで対応できる。

参考資料

- [cscの作法 その236 C#](https://qiita.com/ohisama@github/items/dcfbed1fcb3168d7c522)

## プロジェクトの参照設定

```sh
dotnet add CoapDesktopSender.UI reference CoapDesktopSender.Core
```

上記のコマンドを実行することで、"CoapDesktopSender.UI.csproj"の最終行に以下の要素が追加される。
これにより別のプロジェクトへの参照を設定できる。

```txt
  <ItemGroup>
    <ProjectReference Include="..\CoapDesktopSender.Core\CoapDesktopSender.Core.csproj" />
  </ItemGroup>
```

参考資料

- [一般的な MSBuild プロジェクト項目](https://learn.microsoft.com/ja-jp/visualstudio/msbuild/common-msbuild-project-items?view=visualstudio)

## NuGetパッケージの追加

```sh
dotnet add CoapDesktopSender.Core package Waher.Networking.CoAP --version 3.1.2
dotnet add CoapDesktopSender.Core package PeterO.Cbor --version 4.5.5
dotnet add CoapDesktopSender.UI package CommunityToolkit.Mvvm --version 8.4.0
```

上記のコマンドを使って、Nugetよりパッケージをインストールすることができる。
これを実行することで、対応する"csproj"に"PackageReference"の要素が追加され、読み込み先のパッケージを指定する。
ここでインストール可能なパッケージは[NuGet Gallery](https://www.nuget.org/)で検索することができる。

"CoapDesktopSender.Core.csproj"の場合には、以下の要素が追加される。

```txt
<ItemGroup>
    <PackageReference Include="PeterO.Cbor" Version="4.5.5" />
    <PackageReference Include="Waher.Networking.CoAP" Version="3.1.2" />
</ItemGroup>
```

"CoapDesktopSender.UI.csproj"の場合には、以下の要素が追加される。

```txt
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
```

参考資料

- [NuGet Gallery](https://www.nuget.org/)
- [パッケージ参照をプロジェクトファイルに追加する](https://learn.microsoft.com/ja-jp/nuget/consume-packages/package-references-in-project-files)
- [dotnet add package](https://learn.microsoft.com/ja-jp/dotnet/core/tools/dotnet-package-add)

## ビルド・実行

```sh
dotnet run --project CoapDesktopSender.UI
```

ビルドする際には、対象とするプロジェクトを指定する。
ここでは、"CoapDesktopSender.UI"をビルド対象のプロジェクトとして指定する。
以前に`dotnet new avalonia.app -n CoapDesktopSender.UI -f net8.0`でアプリケーションとして生成していることからもわかる。
※ "CoapDesktopSender.UI.csproj"の"OutputType"にて"WinExe"となっていることからも確認できる

ここでビルドおよびアプリケーションの実行が行われ、画面上にアプリケーションが表示されることが確認できる。
