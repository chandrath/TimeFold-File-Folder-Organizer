<p align="center">
  <img src="src/Assets/app.png" alt="TimeFold Logo" width="128" />
</p>

<h1 align="center">TimeFold: File & Folder Organizer</h1>

<p align="center">
  <strong>Fast, non-destructive file and folder organizer for Windows that sorts messy directories into clean date-based timelines or smart file-type categories.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows&logoColor=white" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 10" />
  <img src="https://img.shields.io/badge/Architecture-x64-blue" alt="Architecture" />
  <img src="https://img.shields.io/badge/License-GPLv3-green" alt="License" />
  <img src="https://img.shields.io/badge/Release-Standalone%20Single%20File-success" alt="Single File" />
</p>

<p align="center">
  <img src="docs/assets/timefold-banner.jpg" alt="TimeFold: File and Folder Organizer Showcase" width="100%" />
</p>

---

## 😫 The Problem TimeFold Solves

Do you have folders like **Downloads**, **Screenshots**, **Photos**, or your **Desktop** packed with thousands of loose files?

* **Windows Explorer Slows Down:** Opening folders with thousands of files takes forever to load, scroll, or search.
* **Wasted Time:** Digging through an ocean of unorganized documents and images causes frustration.
* **Digital Clutter:** Years of mixed downloads and receipts accumulate into an unmanageable mess.

**TimeFold fixes this instantly.** It inspects each item's true timestamp and organizes everything into tidy, chronological folders without altering or deleting any data.

---

## ✨ Key Features

* **⚡ 1-Click Smart Date Sorting:** Automatically groups files and folders by **Month** (`2026-08`), **Day** (`2026-08-15`), **Quarter** (`2026-Q3`), or **Year** (`2026`).
* **🗂️ Smart Categories & Hybrid Modes:** Organize files into intuitive categories (*Images, Documents, Video, Audio, 3D Files, Code, Archives*), by raw file extension, or combine both with 2-level hybrid nesting (`Category / Date` and `Date / Category`).
* **🛡️ 100% Non-Destructive:** TimeFold **never deletes**, alters, or compresses your original files. It only relocates them into neat date folders.
* **🔍 Full Interactive Preview:** Review every file and its exact destination in a live grid *before* moving a single byte.
* **📝 Automatic CSV Audit Logs:** Every organization run generates an exact timestamped audit trail so you always know where files went.
* **🚀 Heavy-Duty Performance:** Seamlessly scans and paginates through **10,000 to 100,000+ files** without freezing your PC.
* **⚠️ Smart Timestamp Detection:** Automatically detects and alerts you if files share identical timestamps (common with unzipped archives or chat downloads).
* **📁 Top-Level Folder Support:** Optionally organize loose subfolders alongside files with a single toggle.
* **🎨 Modern UI with Dark Mode:** Clean desktop interface with full Light / Dark theme support and Windows 11 accent integration.
* **🖱️ Drag & Drop Ready:** Simply drag and drop any folder into TimeFold to start organizing right away.

---

## 🛠️ Tech Stack

* **Runtime & Framework:** .NET 10 (Windows Desktop SDK)
* **Language:** C# 13
* **UI Framework:** Windows Forms (High-DPI aware, custom theme engine)
* **Dependencies:** Zero external NuGet packages (pure .NET standard libraries for maximum speed, security, and portability)

---

## 🚀 Getting Started

### Option 1: Download Pre-built Release (Recommended)

1. Go to the [Releases](https://github.com/chandrath/TimeFold-File-Folder-Organizer/releases) page.
2. Choose the download that fits your needs:
   * **⭐ ReadyToRun (`TimeFold-win-x64-ReadyToRun.zip`):** Recommended for everyone. Just extract and double-click `TimeFold.exe` — completely standalone with zero prerequisites.
   * **💻 Lightweight (`TimeFold-win-x64-RequiresDotNet10.zip`):** Ultra-compact 1.5 MB download for developers who already have the .NET 10 Desktop Runtime installed.

---

### Option 2: Build from Source

#### Prerequisites
* [Windows 10 / 11](https://www.microsoft.com/windows) (64-bit)
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

#### Clone & Run
```bash
# Clone the repository
git clone https://github.com/chandrath/TimeFold-File-Folder-Organizer.git
cd TimeFold-File-Folder-Organizer/src

# Run in Development Mode
dotnet run
```

#### Build Standalone Single-File Executable
To produce a standalone `.exe` that bundles all runtimes:
```bash
dotnet publish TimeFold.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o ../publish
```
The compiled single-file `TimeFold.exe` will be generated inside the `publish/` directory.

---

## 📖 How It Works

1. **Select Source:** Choose or drag-and-drop the messy folder you want to organize.
2. **Review Preview:** TimeFold scans the directory and populates the live preview grid.
3. **Configure Options:** Pick your preferred date format (Month, Day, Quarter, Year), custom prefixes, or 24-hour timestamps in Preferences.
4. **Click Start Organizing:** Watch progress in real-time as files move into organized folders.
5. **Open & Enjoy:** Click "Open Output Folder" or review the generated CSV audit logs.

---

## 📄 License

This project is open source and licensed under the [GNU General Public License v3.0 (GPLv3)](LICENSE).
