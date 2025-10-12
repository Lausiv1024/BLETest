# BLETest.GattClientNative

.NET MAUIを使用したBLE Gattクライアントアプリケーションです。AndroidネイティブのBLE実装を使用しています。

## 機能

- **サービスフィルターによる自動接続**: 指定したサービスUUIDを持つデバイスを検索し、自動的に接続
- **BLEデバイススキャン**: 近くのBLEデバイスを検索（手動選択）
- **デバイス接続**: 選択したBLEデバイスに接続
- **Notifyメッセージ受信**: BLEサーバーからのNotify通知を受信して表示
- **テキスト送信**: UTF-8エンコードされたテキストをBLEサーバーに送信
- **接続状態表示**: 接続状態をリアルタイムで表示

## 実装の特徴

### AndroidネイティブBLE実装

- `BluetoothGatt` を使用したネイティブBLE通信
- `BluetoothGatt.writeCharacteristic` で直接送信（Android 13+対応）
- 古いAPIでは `setValue` メソッドを使用（非推奨メソッドの警告を抑制）

### アーキテクチャ

```
BLETest.GattClientNative/
├── Services/
│   └── IBleService.cs                    # BLEサービスのインターフェース
├── Platforms/
│   └── Android/
│       ├── BleGattClient.cs              # AndroidネイティブGattクライアント
│       ├── BleScanner.cs                 # BLEデバイススキャナー
│       └── BleService.cs                 # プラットフォーム固有のBLEサービス実装
├── MainPage.xaml                         # UI定義
├── MainPage.xaml.cs                      # UIロジック
└── MauiProgram.cs                        # DIコンテナ設定（BLESettings参照）
```

### 主要クラス

#### BleGattClient
AndroidネイティブのBLE Gattクライアント実装。以下の機能を提供：
- BLEデバイスへの接続/切断
- UTF-8テキストの送信（`BluetoothGatt.writeCharacteristic` 使用）
- Notifyメッセージの受信

#### BleScanner
BLEデバイスのスキャン機能を提供：
- デバイスの検索（全体またはサービスUUIDでフィルタリング）
- 検出デバイスのリスト管理
- デバイス発見イベント

#### BleService
プラットフォーム固有の実装を隠蔽するサービスレイヤー：
- 初期化
- デバイススキャン（全体スキャンまたはフィルタリング付き自動接続）
- 接続管理
- メッセージ送受信

**主要メソッド:**
- `ScanDevicesAsync()`: 全デバイスをスキャンしてリスト表示
- `ScanAndConnectAsync()`: サービスUUIDでフィルタリングして自動接続

## 設定

### BLE UUID設定

`BLETest.Settings` プロジェクトの `BLESettings.cs` でサービスUUIDとキャラクタリスティックUUIDを設定します：

```csharp
public class BLESettings
{
    public static readonly Guid ServiceId = Guid.Parse("0000ffe0-0000-1000-8000-00805f9b34fb");
    public static readonly Guid BleCommunicationCCharacteristic = Guid.Parse("0000ffe1-0000-1000-8000-00805f9b34fb");
}
```

このプロジェクトは `BLETest.Settings` プロジェクトを参照しており、`ServiceId` と `BleCommunicationCCharacteristic` の値を使用します。

### 必要な権限

`AndroidManifest.xml` で以下の権限が設定されています：

- `BLUETOOTH`
- `BLUETOOTH_ADMIN`
- `BLUETOOTH_SCAN`
- `BLUETOOTH_CONNECT`
- `ACCESS_FINE_LOCATION`
- `ACCESS_COARSE_LOCATION`

アプリ起動時に自動的に権限リクエストが実行されます。

## 使い方

### 自動接続モード（推奨）

1. **アプリ起動**: アプリを起動すると、Bluetoothと位置情報の権限リクエストが表示されます
2. **自動接続**: "Scan and Auto Connect" ボタンをタップ
3. **自動検索と接続**: アプリが指定したサービスUUIDを持つデバイスを検索し、見つかると自動的に接続します
4. **接続完了**: ステータスが "Ready" になると、送信ボタンが有効化されます
5. **メッセージ送信**: テキスト入力欄にメッセージを入力し、"Send" ボタンをタップ
6. **メッセージ受信**: Notifyメッセージは受信エリアに自動的に表示されます

### 手動選択モード

1. **アプリ起動**: アプリを起動すると、Bluetoothと位置情報の権限リクエストが表示されます
2. **デバイススキャン**: "Scan Devices (Manual)" ボタンをタップしてBLEデバイスをスキャン
3. **デバイス選択**: 検出されたデバイスのリストから接続したいデバイスをタップ
4. **接続完了**: 接続が完了すると、ステータスが "Ready" になり、送信ボタンが有効化されます
5. **メッセージ送信**: テキスト入力欄にメッセージを入力し、"Send" ボタンをタップ
6. **メッセージ受信**: Notifyメッセージは受信エリアに自動的に表示されます

## 通信仕様

- **エンコーディング**: UTF-8
- **送信方法**: BluetoothGatt.writeCharacteristic (Android 13+) または setValue + writeCharacteristic (古いバージョン)
- **受信方法**: Notifyコールバック経由

## ビルド

### Android

```bash
dotnet build -f net9.0-android
```

または Visual Studio / Visual Studio Code で Android プロジェクトをビルドしてください。

## トラブルシューティング

### デバイスが見つからない
- Bluetoothが有効になっているか確認
- 位置情報サービスが有効になっているか確認（Android）
- 必要な権限が付与されているか確認

### 接続できない
- BLE UUID（ServiceId、BleCommunicationCCharacteristic）が正しいか確認
- BLEサーバーが起動しているか確認
- デバイスのBluetooth接続範囲内にいるか確認

### メッセージが送信できない
- デバイスが "Ready" 状態になっているか確認
- BLEサーバーのCharacteristicが書き込み可能か確認

## 注意事項

- このプロジェクトはAndroidプラットフォームをターゲットとしています
- iOS/Windows/macOSでの実装は含まれていません（必要に応じて追加してください）
- BLE UUID は接続先のBLEサーバーに合わせて `BLETest.Settings/BLESettings.cs` で変更してください
- このプロジェクトは `BLETest.Settings` プロジェクトへの参照が必要です
