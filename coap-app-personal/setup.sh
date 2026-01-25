#!/bin/bash

# root権限の確認
if [ "$(id -u)" -ne 0 ]; then
    echo "root権限が必要です。次のように実行してください:" >&2
    echo "  sudo chmod +x $0 $*" >&2
    echo "  sudo $0 $*" >&2
    exit 1
fi

# 環境のセットアップ

# .NET 8 SDK
sudo apt update
sudo apt install -y curl apt-transport-https

if command -v code > /dev/null 2>&1; then
    echo "VS Codeはインストールされています"
else
    # VS Codeのリポジトリ登録
    curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
    sudo install -o root -g root -m 644 microsoft.gpg /etc/apt/trusted.gpg.d/
    sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/vscode stable main" > /etc/apt/sources.list.d/vscode.list'

    # VS Code インストール
    sudo apt update
    sudo apt install -y code

    echo "VS Codeはインストールされました"
fi

if command -v dotnet > /dev/null 2>&1; then
    echo ".NET SDKはインストールされています"
else
    echo ".NET SDKをインストールします"
    sudo apt update
    sudo apt install -y dotnet-sdk-8.0
    echo ".NET SDKはインストールされました"
fi

# C# 拡張
code --install-extension ms-dotnettools.csharp
echo "C# 拡張はインストールされました"