# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a multi-project .NET solution for Bluetooth Low Energy (BLE) communication between devices. The solution contains a WPF desktop server application and multiple .NET MAUI cross-platform client/server applications targeting Android, iOS, Windows, and macOS.

### Current Active Development
**Currently, development is focused on BLE communication between these two projects:**
- **BLETest** (desktop WPF server)
- **BLETest.GattClientNative** (MAUI mobile server/client)

**IMPORTANT**: Unless explicitly instructed otherwise, development should only be performed on:
1. BLETest project
2. BLETest.GattClientNative project (note: the actual project is named "GattClientNative" but referred to as BLETest.GattClientNative)
3. BLETest.Settings (shared settings library, originally referred to as BLETest.Core)

Other projects (GattClient, GattServerNative, etc.) should generally not be modified.

## Build Commands

### Build entire solution
```bash
dotnet build BLETest.sln
```

### Build specific projects
```bash
# Desktop WPF application (Windows only)
dotnet build BLETest/BLETest.csproj

# MAUI Client (cross-platform)
dotnet build GattClient/GattClient.csproj

# MAUI Server (cross-platform)
dotnet build GattServerNative/GattServerNative.csproj

# MAUI Server (legacy, uses net8.0)
dotnet build GattClientNative/GattServerNative.csproj

# Shared Settings library
dotnet build BLETest.Settings/BLETest.Settings.csproj
```

### Platform-specific builds
```bash
# Android
dotnet build GattClient/GattClient.csproj -f net9.0-android

# iOS
dotnet build GattClient/GattClient.csproj -f net9.0-ios

# Windows
dotnet build GattClient/GattClient.csproj -f net9.0-windows10.0.19041.0
```

### Run/Deploy
```bash
# Android deployment
dotnet build GattClient/GattClient.csproj -f net9.0-android -t:Run

# Windows desktop
dotnet run --project BLETest/BLETest.csproj
```

## Project Architecture

### Project Structure
- **BLETest** (BLETest/): WPF desktop application (.NET Framework 4.7.2) that acts as a BLE GATT server on Windows. Uses Windows UWP APIs for BLE functionality.

- **GattClient**: .NET MAUI cross-platform BLE client using Plugin.BLE library. Targets net9.0-android, net9.0-ios, net9.0-maccatalyst, net9.0-windows.

- **GattClientNative**: Older MAUI-based BLE server implementation targeting net8.0. Contains Android platform-specific BLE server code in `Platforms/Android/BLEServer/`.

- **GattServerNative**: Newer .NET MAUI BLE server implementation (net9.0). Has partial class structure for platform-specific implementations.

- **BLETest.Settings**: Shared netstandard2.0 library containing common settings, constants, and utilities (e.g., service UUIDs, characteristic UUIDs).

### Key Architectural Patterns

#### BLE Communication Pattern
The codebase implements a client-server BLE architecture:

1. **Server Side** (BLETest desktop or GattServerNative/GattClientNative mobile apps):
   - Uses `BLECommunicationServer` class (in BLETest project) or native Android `BluetoothGattServer` (in MAUI projects)
   - Creates GATT services with Write and Notify characteristics
   - Handles incoming data via `OnDataReceived` event handlers
   - Can trigger keyboard input simulation on Windows (via `KeyControl` class using Win32 interop)

2. **Client Side** (GattClient mobile app):
   - Uses Plugin.BLE library for cross-platform BLE scanning and connection
   - Platform-specific implementations in `Platforms/Android/` for Android
   - Connects to server, discovers services, and communicates via characteristics

#### Characteristic Usage
- **Write Characteristic**: Client writes data to server
- **Notify Characteristic**: Server sends notifications to client
- Projects support both separate characteristics (WriteCharacteristic + NotifyCharacteristic) and legacy single characteristic mode

#### Platform-Specific Code
MAUI projects use partial classes and conditional compilation:
- Platform implementations in `Platforms/Android/`, `Platforms/iOS/`, `Platforms/Windows/`, etc.
- Android BLE server code uses native Android Bluetooth APIs (`BluetoothManager`, `BluetoothGattServer`, `BluetoothGattServerCallback`)
- Windows desktop uses UWP `Windows.Devices.Bluetooth.GenericAttributeProfile` APIs

#### Device Authentication
The BLETest desktop app has a `BLEAuthenticationServer` class for managing new device registration (currently stub implementation).

### Important Constants
Service IDs, characteristic UUIDs, and other BLE constants are defined in `BLETest.Settings` project (shared across all projects). The `BLESettings` class contains static service and characteristic GUIDs.

### Key Dependencies
- **Plugin.BLE**: Used by GattClient for cross-platform BLE client functionality
- **Microsoft.Windows.SDK.Contracts**: Used by WPF desktop app for Windows BLE APIs
- **QRCoder**: Used in desktop app (likely for device pairing/authentication)
- **R3**: Reactive extensions library used in GattClientNative
- **Microsoft.Maui.Controls**: UI framework for cross-platform mobile apps

## Development Notes

### Framework Versions
- Desktop app uses .NET Framework 4.7.2 (older WPF project format)
- MAUI apps use .NET 8.0 or .NET 9.0
- Shared library uses netstandard2.0 for maximum compatibility

### Git Branch Structure
- Main branch: `master`
- Current development branch: `feature/auth` (device authentication features)

### Key Implementation Details
- Desktop server handles received BLE data by simulating keyboard input (KeyDown/KeyUp for key '1')
- Data format uses `0xFF` as a header byte followed by command bytes (e.g., `0x80` = key down, `0x81` = key up)
- Android BLE server uses custom callbacks via `BLEServerCallback` class
