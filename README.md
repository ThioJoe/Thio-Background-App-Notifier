<h1 align = 'center'>
    <img 
        src = '.github/assets/Icon.png' 
        height = '100' 
        width = '100' 
        alt = 'Thio Background App Notifier Icon' 
    >
    <br>
    Thio's Background App Notifier
    <br>
</h1>

### A lightweight Windows tool that tells you when an app quietly adds a new startup scheduled task or background service.

It does **not** run continuously. You can set it to quietly run once at each Windows startup, and it will show a message box if any new startup items are found. If not, it just closes.

# What It Does (And Why?)

Many programs set themselves to launch at startup without ever showing up in the normal Startup apps list. They register a **Windows Service** or a **Scheduled Task** instead, which run silently in the background and are easy to miss.

The first time you run it, it records a baseline of auto-starting services and scheduled tasks. After that, whenever you open it (or let it check at logon), it tells you **what's new since you last looked** and highlights it.

It is **fully portable** (just one exe), but there is an _optional_ `.msi` installer, so you can keep it up to date via `winget`.

## What It Does _NOT_ Track (On Purpose)

It does **not** track "regular" startup apps — the ones that add themselves to your **Startup folder** or the **Run** registry keys. That's not an oversight:

* **Windows already notifies you about those** via its built-in *"Startup app notifications"* setting (Settings ▸ System ▸ Notifications ▸ Startup app notifications).
* This app focuses on the categories Windows _doesn't_ warn you about, so the two complement each other.
* It even shows you whether that Windows setting is currently On, and can turn it on for you — so you're covered for **both** kinds of startup items.

## It's Not a Background Process

**It only scans when you open it,** and exits when you close it.

If you want automatic checks, there's an optional *"Re-check on each Windows Startup"* toggle.
It scans silently and only pops up a notification **if** something new actually appeared, otherwise you never see it.

--------

# How to Download

1. Go to the [Releases](https://github.com/ThioJoe/New-Startup-App-Notifier/releases) page.
2. For the latest release, look under `Assets` and grab either:
    - **Installer**: The `.msi` — installs per-user or for all users, and can add the optional Start Menu / quiet-startup shortcuts.
    - **Portable**: The standalone `Thio-Background-App-Notifier.exe` -- just run it, no install needed.

> **Requirements:** Windows with **.NET Framework 4.8** (already included on Windows 10/11). No admin rights are needed for normal use.

--------

# How to Use

1. **Run it once to set your baseline.** The first launch just records everything currently set to auto-start — nothing is flagged as new yet.
2. **Open it again whenever you want to check.** It rescans and shows any startup Services or Scheduled Tasks that appeared since last time, highlighted in the list.
3. **Double-click any row** for full details (executable path, service start type, task triggers, when it was first detected, etc.).
4. Use **"All Startup Services"** or **"All Startup Tasks"** to browse *everything* currently set to run, not just the new stuff.

--------


# Frequently Asked Questions

### **Q:** Does it run constantly in the background?
**A:** No. It only scans when you open it. The optional "Run at startup" mode does a single silent check at logon and then exits if there's nothing new.

### **Q:** Why doesn't it list my normal startup programs?
**A:** Because Windows already warns you about "normal" startup apps via its built-in "Startup app notifications." This tool covers the categories Windows *doesn't* (background Services and Scheduled Tasks). There may be other categories added in the future, but these are the big ones.

### **Q:** It flagged a bunch of items the first time — is something wrong?
**A:** The very first run just records a baseline of what's already there, so nothing should be flagged as new. Anything highlighted afterward genuinely appeared *after* that baseline.

### **Q:** Does it need administrator rights?
**A:** Not for normal scanning or the detection log. Some items you lack permission to read are simply skipped.

### **Q:** Does it change or remove any of my startup items?
**A:** No. It's read-only — it only *reports* what it finds. The only thing it writes is its own detection log and (optionally) its own startup shortcut.

### **Q:** Will this detect malware?
**A:** Possibly, but it's mainly intended for legitimate software with nuissance practices. There are lots of other ways malware can hide that this app wouldn't catch.

--------

# Building From Source

### Requirements:
* Visual Studio (with the .NET desktop workload)
* **.NET Framework 4.8** and **4.8.1** developer pack

### Instructions:
1. Open `src/New-Startup-App-Notifier.slnx` in Visual Studio.
2. Select your build configuration (`Debug` or `Release`).
3. Build and run.

The installer is built with [WiX](https://wixtoolset.org/); the installer definition lives in the `Build/` folder.

