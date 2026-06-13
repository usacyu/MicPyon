# MicPyon 🎙

A lightweight Windows tray app for switching microphones and toggling mute — instantly, without digging through Sound Settings.

![normal](screenshots/normal.png) ![muted](screenshots/muted.png)

---

## Why I built this

Windows has no quick way to switch your default microphone. Every time I wanted to swap between my Bluetooth headset and another mic, I had to open Sound Settings, navigate to the input tab, and click through several menus. That was too slow. So I built MicPyon.

---

## Features

- **Tray-resident** — lives in your system tray, always one click away
- **Left-click to mute/unmute** — toggle mute instantly from the tray icon
- **Right-click to switch microphone** — set any mic as the default in one click
- **Floating window** — a clean dark-themed panel showing all your mics
- **Can't-miss mute overlay** — while muted, a pulsing red frame and a banner take over the window, so you notice at a glance even mid-stream (click anywhere on it to unmute)
- **Global hotkey** — mute toggle from anywhere (default: `Ctrl + Shift + M`, customizable)
- **Connection type display** — shows USB / Bluetooth / 3.5mm for each device
- **Busy device indicator** — if a mic is in use by another app (e.g. voice input), it stays visible but grayed out
- **Hide devices** — exclude mics you never use from the list
- **Prevent duplicate launch** — only one instance runs at a time

![switch](screenshots/switch.png)

---

## Requirements

- Windows 10 / 11
- .NET 10 Runtime ([download here](https://dotnet.microsoft.com/download/dotnet/10.0))

---

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Run `MicPyon.exe`
3. The app appears in your system tray

No installer needed.

---

## Usage

| Action | How |
|--------|-----|
| Mute / Unmute | Left-click the tray icon, or press `Ctrl + Shift + M` |
| Switch microphone | Right-click tray → select a mic, or click in the window |
| Open window | Right-click tray → "Open window" |
| Hide a device | Right-click tray → "Choose devices to show" |
| Change hotkey | Right-click tray → "Hotkey settings" |

---

## Tech

- C# / WPF / .NET 10
- [NAudio](https://github.com/naudio/NAudio) for audio device management
- `IPolicyConfig` (undocumented Windows COM API) for setting the default endpoint

---

## License

MIT
