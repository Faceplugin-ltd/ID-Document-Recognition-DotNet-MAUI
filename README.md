<div align="center">
<img alt="FacePlugin" src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/brand/logo.png" width="400"/>
</div>

#### 🌐 Company Site - [Here](https://faceplugin.com)

#### 🤗 Hugging Face - [Here](https://huggingface.co/FacePlugin-Ltd)

#### 🛟 Help Center - [Here](https://doc.faceplugin.com)

#### 🐳 Docker Hub - [Here](https://hub.docker.com/u/faceplugin)

# FacePlugin ID Document Recognition SDK — .NET MAUI (Fully On-Premise)

> Android, iOS, and Windows each call the Document Reader SDK inside this app.
> Jump: [Quick Start](#quick-start) · [Get the runtimes](#get-the-runtimes) · [Run the demo](#run-the-demo) · [License](#sdk-license) · [Integrate](#setup-on-your-own-app)



## Quick Start

- [ ] Clone [ID-Document-Recognition-DotNet-MAUI](https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI)
- [ ] Install the **.NET 8** SDK (side by side with any newer SDK) and the `maui` workload. `global.json` in this folder pins the build to 8.0 so .NET 9/10 do not reject `net8.0-ios` / `net8.0-android`
- [ ] Download Android / iOS / Windows runtimes — [Get the runtimes](#get-the-runtimes)
- [ ] Keep the demo ids: Android `com.faceplugin.documentreader` / iOS `com.faceplugin.documentreader.app` (until **12 Aug 2027**)
- [ ] Run on a connected phone, or on Windows — [Run the demo](#run-the-demo). Home shows **Camera**, **Gallery**, and **About**. A scan opens the same Result / Liveness / Images / Raw JSON screen as the Flutter and React Native demos



## Introduction

FacePlugin **ID Document Recognition SDK for .NET MAUI** is an on-premise sample for Android, iOS, and Windows. Scan ID cards, passports, and driver licenses with OCR, MRZ, and barcode extraction. Each platform runs the SDK inside this process.

Demo UI assets (`fp_logo.png`) ship in `Resources/Images/`; home tile glyphs are drawn in code. The launcher uses a single **FP** master at `Resources/AppIcon/appicon.png` (do not add a second FP `ForegroundFile` — that double-composites the logo). Splash is `Resources/Splash/splash.png`. Native SDK binaries are **not** in git; download them from Google Drive (links below).

### Main Functionalities


| Feature                                           | Supported |
| ------------------------------------------------- | --------- |
| ID Card, Passport, and Driver License recognition | ✓         |
| MRZ, Barcode, and OCR data extraction             | ✓         |
| Gallery (front required, back optional)           | ✓         |
| Result JSON                                       | ✓         |
| Authenticity / document liveness (`normal`)       | ✓         |
| Windows on-device via `DocumentReaderSDK.dll`     | ✓         |




### Product List


| Platform                       | Repository                                                                                                           |
| ------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| Android                        | [ID-Document-Recognition-Android](https://github.com/Faceplugin-ltd/ID-Document-Recognition-Android)                 |
| iOS                            | [ID-Document-Recognition-iOS](https://github.com/Faceplugin-ltd/ID-Document-Recognition-iOS)                         |
| Windows                        | [ID-Document-Recognition-Windows](https://github.com/Faceplugin-ltd/ID-Document-Recognition-Windows)                 |
| Linux / Docker                 | [ID-Document-Recognition-Docker](https://github.com/Faceplugin-ltd/ID-Document-Recognition-Docker)                   |
| React Native                   | [ID-Document-Recognition-React-Native](https://github.com/Faceplugin-ltd/ID-Document-Recognition-React-Native)       |
| Flutter                        | [ID-Document-Recognition-Flutter](https://github.com/Faceplugin-ltd/ID-Document-Recognition-Flutter)                 |
| Ionic Capacitor                | [ID-Document-Recognition-Ionic-Capacitor](https://github.com/Faceplugin-ltd/ID-Document-Recognition-Ionic-Capacitor) |
| Ionic Cordova                  | [ID-Document-Recognition-Ionic-Cordova](https://github.com/Faceplugin-ltd/ID-Document-Recognition-Ionic-Cordova)     |
| **.NET MAUI**                  | **[ID-Document-Recognition-DotNet-MAUI](https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI)** (**this repo**) |
| Linux / Docker (Liveness-Only) | [ID-Document-Liveness-Detection-Docker](https://github.com/Faceplugin-ltd/ID-Document-Liveness-Detection-Docker)     |


---



## Before you start


| Step | What you need                                                                                                                                                                                                                                                               |
| ---- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | **.NET 8 SDK** and `dotnet workload install maui`, run from this folder so `global.json` selects SDK 8. iOS builds need **macOS + Xcode 15+**                                                                                                                               |
| 2    | Android `documentreadersdk.aar`, iOS `docsdk.framework`, and the Windows `lib/cpu` runtime — [Get the runtimes](#get-the-runtimes)                                                                                                                                          |
| 3    | Demo licenses are in `src/DocumentReader.Net.Maui/LicenseKeys.cs` (Android `com.faceplugin.documentreader`, iOS `com.faceplugin.documentreader.app`, valid until **12 August 2027**). Request a new key only if you change the application id — [SDK License](#sdk-license) |




### System requirements


| Item    | Android                                         | iOS                                 | Windows                                         |
| ------- | ----------------------------------------------- | ----------------------------------- | ----------------------------------------------- |
| Host    | Windows / macOS                                 | **macOS only**                      | Windows                                         |
| SDK     | .NET 8 (pinned by `global.json`), MAUI workload | Same, plus Xcode **15+**            | .NET 8 (pinned by `global.json`), MAUI workload |
| OS      | `minSdk` **24**                                 | **15.0**                            | Windows 10 17763+                               |
| Device  | Physical phone                                  | Physical iPhone                     | This PC                                         |
| Demo id | `com.faceplugin.documentreader`                 | `com.faceplugin.documentreader.app` | Windows FP1 key, or `license.txt`               |


---



## Get the runtimes



### Android — `documentreadersdk.aar`

**Download:** [DocumentReader Android runtime (Google Drive)](https://drive.google.com/drive/folders/1nDSfvj0WtC1lZgzwFd7471ECVtk-nuYH)

```text
src/DocumentReader.Net.Maui/Platforms/Android/libs/documentreadersdk.aar
```

The project references the AAR only when that file is present.

### iOS — `docsdk.framework`

**Download:** [DocumentReader iOS runtime (Google Drive)](https://drive.google.com/drive/folders/1do6Ws_BlXGkR_K9jI_ULd1zHjqLGSP4q)

```text
src/DocumentReader.Net.Maui/Platforms/iOS/Frameworks/docsdk.framework
```



### Windows — `lib/cpu`

**Download:** [DocumentReader Windows runtime (Google Drive)](https://drive.google.com/drive/folders/1YfHUwnXO0E2NSvS_81nTNO2z3mKVO85g)

```text
src/DocumentReader.Net.Maui/Platforms/Windows/native/lib/cpu/DocumentReaderSDK.dll
```

Put every file from that Drive folder directly into `lib/cpu`, including `dcr-eng.dll` and `dcr.fpk`. The project copies that tree next to the Windows executable.

---



## Run the demo

Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). `global.json` in this folder selects SDK 8.0 when you run the commands below.

```bash
git clone https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI.git
cd ID-Document-Recognition-DotNet-MAUI
dotnet workload install maui
```

Place the Android AAR, the iOS framework, and the Windows `lib/cpu` runtime first — [Get the runtimes](#get-the-runtimes). Keep the demo application ids so the included license activates. iOS runs on a physical iPhone (`docsdk.framework` does not link for the simulator).

```bash
# Android — phone connected, USB debugging on
dotnet build src/DocumentReader.Net.Maui/DocumentReader.Net.Maui.csproj -f net8.0-android -t:Run

# iOS — macOS + Xcode 15+, iPhone connected
dotnet build src/DocumentReader.Net.Maui/DocumentReader.Net.Maui.csproj -f net8.0-ios -r ios-arm64 -t:Run

# Windows
dotnet build src/DocumentReader.Net.Maui/DocumentReader.Net.Maui.csproj -f net8.0-windows10.0.19041.0 -t:Run
```

On the first iOS install, trust the developer under **Settings → General → VPN & Device Management**.


| Platform               | Identifier                          |
| ---------------------- | ----------------------------------- |
| Android application id | `com.faceplugin.documentreader`     |
| iOS bundle id          | `com.faceplugin.documentreader.app` |
| Windows runtime        | `lib/cpu` beside the executable     |

### Screenshots

<p align="center">
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/home.png" width="240" alt="FacePlugin Document Reader — Home with Camera, Gallery, About and Recognition + Liveness"/>
&nbsp;
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/camera.png" width="240" alt="FacePlugin Document Reader — live camera overlay and Capture for ID scanning"/>
&nbsp;
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/gallery.png" width="240" alt="FacePlugin Document Reader — Gallery front and optional back, then Recognize"/>
</p>

<p align="center">
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/result.png" width="240" alt="FacePlugin Document Reader — Result tab with OCR, MRZ, and barcode fields"/>
&nbsp;
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/security.png" width="240" alt="FacePlugin Document Reader — Liveness tab with authenticity and document liveness"/>
&nbsp;
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/images.png" width="240" alt="FacePlugin Document Reader — Images tab with portrait, signature, and document crops"/>
</p>

<p align="center">
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/raw.png" width="240" alt="FacePlugin Document Reader — Raw JSON recognize response for integration"/>
&nbsp;
<img src="https://raw.githubusercontent.com/Faceplugin-ltd/faceplugin-assets/main/screenshots/document-reader/mobile/about.png" width="240" alt="FacePlugin Document Reader — About with on-device Recognition + Liveness license"/>
</p>

---

## SDK License

Licenses are **offline** and bound to your `applicationId` / bundle identifier.

The sample app already includes a valid key for `com.faceplugin.documentreader` (Android) / `com.faceplugin.documentreader.app` (iOS) (until **12 August 2027**). You only need a new key if you use a different `applicationId` or bundle identifier.

Windows takes an FP1 key in `LicenseKeys.Windows`, or one line in `src/DocumentReader.Net.Maui/Platforms/Windows/native/license.txt`. That key is bound to the machine code shown on the home screen. The Android and iOS demo keys do not activate the Windows engine.

### How to get a license

The code below shows how to use the license:

[https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI/blob/c15925b7d0bc852fd8b6c737b34258efa54b0bb0/src/DocumentReader.Net.Maui/LicenseKeys.cs#L10-L29](https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI/blob/c15925b7d0bc852fd8b6c737b34258efa54b0bb0/src/DocumentReader.Net.Maui/LicenseKeys.cs#L10-L29)

[https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI/blob/c15925b7d0bc852fd8b6c737b34258efa54b0bb0/src/DocumentReader.Net.Maui/DemoUi.cs#L33-L48](https://github.com/Faceplugin-ltd/ID-Document-Recognition-DotNet-MAUI/blob/c15925b7d0bc852fd8b6c737b34258efa54b0bb0/src/DocumentReader.Net.Maui/DemoUi.cs#L33-L48)

Please [contact us](#contact) to get a license for **your own app**.

### License capabilities (Recognition + Liveness)

After activation, `GetLicenseStatusAsync` reports what the key unlocks. Home shows the same summary on the status bar (for example **Ready · Recognition + Liveness**). About shows **License: …**.

| Capability | Meaning |
| ---------- | ------- |
| **Recognition** | OCR, MRZ, barcode/QR, and document type classification |
| **Liveness** (authenticity) | Document authenticity: physical document, security patterns, photo origin, barcode format |

Typical labels:

- **Recognition + Liveness** — full identity verification (Result + Liveness tabs)
- **Recognition** — OCR, MRZ, and barcode only; Security stays empty / not checked
- **Liveness** — authenticity / document liveness only; OCR/MRZ/barcode stays empty / not checked
- **Not licensed** — until you activate

Call order, off the UI thread: `GetMachineCodeAsync` → `SetActivationAsync` → `InitAsync` → `RecognizeAsync`. `0` means success.

| Code | Meaning |
| ---- | ------- |
| 0 | Success |
| 1 | Invalid license |
| 2 | Expired license |
| 3 | Not activated |
| 4 | Init failed |

---



## Setup on your own app

Reference `DocumentReaderSdk` from this project, or copy `DocumentReaderSdk.cs` and the platform file for the target you ship.

1. Android: add `documentreadersdk.aar` as an `AndroidLibrary`.
2. iOS: add `docsdk.framework` as a `NativeReference`.
3. Windows: copy `lib/cpu` beside the executable and load `DocumentReaderSDK.dll`.
4. Keep calls off the UI thread.
5. `RecognizeAsync(frontJpeg, backJpeg, "normal")` runs OCR, MRZ, barcode, and authenticity. Pass `"none"` to skip authenticity.

You do not need `MainPage` in your own app.

---



## Contact

![faceplugin.com](https://img.shields.io/badge/email-info@faceplugin.com-blue.svg?logo=gmail)  ![Telegram @FacePluginSupport](https://img.shields.io/badge/telegram-@FacePluginSupport-blue.svg?logo=telegram)  ![faceplugin.com](https://img.shields.io/badge/whatsapp-faceplugin-blue.svg?logo=whatsapp)