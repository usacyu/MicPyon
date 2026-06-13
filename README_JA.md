<div align="center">

# 🎀 まいぴょん

**マイク切り替えもミュートも、ワンクリック。サウンド設定はもう開かない。**

Windowsのタスクトレイに住む、マイク切り替え＆ミュートツールだよ。

<img src="https://img.shields.io/badge/C%23-.NET%2010-512BD4?logo=dotnet&logoColor=white" alt="C# .NET 10">
<img src="https://img.shields.io/badge/GUI-WPF-9b6bd6" alt="WPF">
<img src="https://img.shields.io/badge/OS-Windows-0078D6?logo=windows&logoColor=white" alt="Windows">
<img src="https://img.shields.io/badge/Audio-NAudio-67b26f" alt="NAudio">
<img src="https://img.shields.io/badge/License-MIT-lightgrey" alt="License MIT">
<img src="https://img.shields.io/badge/made%20with-%F0%9F%8E%80-e85d9a" alt="made with ribbon">

**日本語** ｜ [English](README.md)

<img src="screenshots/normal.png" width="270" alt="normal"> <img src="screenshots/muted.png" width="270" alt="muted">

</div>

---

## 作った理由

Windowsってマイクをさっと切り替える方法がないんだよね。  
Bluetoothヘッドセットと別のマイクを行き来するたびに、サウンド設定 → 入力タブ → デバイス選択…ってクリックしまくらないといけなくて、めんどくさすぎる。  
だから作った。

---

## できること

- **タスクトレイ常駐** — 常にトレイにいて、いつでもワンクリック
- **左クリックでミュートトグル** — トレイアイコンをクリックするだけ
- **右クリックでマイク切り替え** — ワンクリックでデフォルトマイクを変更
- **フローティングウィンドウ** — ダークテーマのパネルでマイク一覧を表示
- **目立つミュート表示** — ミュート中は赤い点滅枠と斜めバナーがウィンドウ全体に出るから、配信中でも一目でミュートに気づける（どこをクリックしても即解除）
- **グローバルホットキー** — どこにいてもミュート切り替え（デフォルト: `Ctrl + Shift + M`、変更もできるよ）
- **接続タイプ表示** — USB / Bluetooth / 3.5mm を表示
- **使用中デバイスのグレーアウト** — 別のアプリが使ってるマイクは薄く表示して「切り替え不可」ってわかるようにしてる
- **デバイス非表示** — 使わないマイクをリストから消せる
- **二重起動防止** — 1個だけ起動

![switch](screenshots/switch.png)

---

## 動作環境

- Windows 10 / 11
- .NET 10 ランタイム（[ここからダウンロード](https://dotnet.microsoft.com/download/dotnet/10.0)）

---

## インストール

1. [Releases](../../releases) ページから最新版をダウンロード
2. `まいぴょん.exe` を実行
3. タスクトレイにアイコンが出たら完了！

インストーラーとかないよ。exeをダブルクリックするだけ。

---

## 使い方

| 操作 | やり方 |
|------|------|
| ミュート / 解除 | トレイアイコンを左クリック、または `Ctrl + Shift + M` |
| マイク切り替え | トレイ右クリック → マイク選択、またはウィンドウ内をクリック |
| ウィンドウを開く | トレイ右クリック → "ウィンドウを開く" |
| デバイスを非表示 | トレイ右クリック → "表示するデバイスを選ぶ" |
| ホットキー変更 | トレイ右クリック → "ホットキー設定" |

---

## 技術的なやつ

- C# / WPF / .NET 10
- [NAudio](https://github.com/naudio/NAudio) — オーディオデバイス操作
- `IPolicyConfig`（Windowsの非公式COM API） — デフォルトマイクの切り替えに使ってる

---

## ライセンス

MIT — 自由に使ってね！
