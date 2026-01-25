#!/bin/sh
# .NET 8 SDK と Avalonia テンプレートを使用してソリューションとプロジェクトを作成するスクリプト

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
dotnet add CoapDesktopSender.UI package BinToss.GroupBox.Avalonia --version 1.0.0
