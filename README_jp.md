![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)

> [!IMPORTANT]
> 免責事項：Quick AdMob Integratorはオープンソースのサービスであり、Unity Technologies Inc.が提供する公式のサービスではありません。また、レジストリにOpen UPMを利用しますが、Open UPMが提供するサービスでもありません。

# Quick AdMob Integrator
UnityのGoogle AdMob統合を簡素化し、モバイル広告のシームレスなセットアップを提供します。

![add package](Docs/header.png)

- Admobとメディエーションのパッケージを簡単にインストール
- 更新情報を一目で確認
- 内部ではPackage Manager(UPM)を利用しているので安心

## Getting Started

### Git Urlからインストール

"Unity Editor : Window > Package Manager > Add package from git URL...".

URL: `https://github.com/IShix-g/QuickAdMobIntegrator.git?path=Packages/QuickAdMobIntegrator`

![add package](Docs/add_package.png)

### Quick AdMob Integratorを開く

`Unity Editor : Window > Quick AdMob Integrator`

![open](Docs/open_qai.png)

### レジストリーの設定

`Set up required registries...`ボタンをクリック

<img src="Docs/qai_set_registries.jpg" width="550"/>

### 設定を開く

歯車アイコンの設定をクリック

<img src="Docs/qai_open_setting.jpg" width="550"/>

### メディエーションの選択

必要無いメディエーションの選択を外してください

<img src="Docs/qai_setting_mediation.jpg" width="550"/>

### 設定の完了

戻るボタンをクリックして設定を完了します。

<img src="Docs/qai_end_setting.jpg" width="550"/>

### Admobをインストール済みの方

下記を削除してください。

- Assets/ExternalDependencyManager
- Assets/GoogleMobileAds (Resources以外)
- Assets/Plugins/Android/googlemobileads-unity.aar
- Assets/Plugins/Android/GoogleMobileAdsPlugin
- Assets/Plugins/iOS/GADUAdNetworkExtras
- Assets/Plugins/iOS/unity-plugin-library.a

### SDKとメディエーションのインストール

`Install All`ボタンをクリックする事で表示されているすべてのSDKとメディエーションをインストールします。

<img src="Docs/qai_install_all.jpg" width="550"/>

## 各ボタンの説明

<img src="Docs/qai_buttons.jpg" width="550"/>

1. 設定
2. パッケージのリロード
3. Unity Package Managerを開く
4. すべてのSDKとメディエーションパッケージのインストールまたはアップデート
5. スタートガイド/ヘルプを表示
6. パッケージのインストール

## 現在のパッケージの状態表示

<img src="Docs/qai_package_state.jpg" width="550"/>

1. 最新バージョンのインストール済み
2. インストール済み、且つ更新可能な新バージョンあり v3.13.1 (現在) -> v3.14.0 (新)
3. 未インストール

## プラグインの仕組み

このプラグインは、`Open UPM`をScoped Registriesに設定して利用しています。

`Unity Editor: Project Settings > Package Manager > Scoped Registries`

![scoped registries](Docs/upm_scoped_registries.jpg)

インストールやアンインストールはUnity Package Managerの機能を利用しているので、もしこのプラグインが必要無くなって削除しても、インストールしたものに影響を与えません。

`Unity Editor: Window > Package Manager > My Registries`

<img src="Docs/upm.jpg" width="800"/>