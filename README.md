![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black)

[README - 日本語版](README_jp.md)

> [!IMPORTANT]
> DISCLAIMER: Quick AdMob Integrator is an open-source tool and is not an official service by Unity Technologies Inc. It uses the Open UPM registry but is not affiliated with nor provided by Open UPM.

# Quick AdMob Integrator
Simplify the integration of Google AdMob into Unity and seamlessly set up mobile ads.

![add package](Docs/header.png)

## Getting Started

### Installation via Git URL

Go to `Unity Editor: Window > Package Manager > Add package from git URL...`.

URL:  
`https://github.com/IShix-g/QuickAdMobIntegrator.git?path=Packages/QuickAdMobIntegrator`

![add package](Docs/add_package.png)

### Open Quick AdMob Integrator

Access it via  
`Unity Editor: Window > Quick AdMob Integrator`

![open](Docs/open_qai.png)

### Setting Up Registry

Click the `Set up required registries...` button.

<img src="Docs/qai_set_registries.jpg" width="550"/>

### Open Settings

Click the settings icon (gear icon) in the toolbar.

<img src="Docs/qai_open_setting.jpg" width="550"/>

### Choose Mediation Platforms

Deselect any mediation platforms that you do not need.

<img src="Docs/qai_setting_mediation.jpg" width="550"/>

### Complete Setup

Click the back button to complete the initial setup.

<img src="Docs/qai_end_setting.jpg" width="550"/>

### Install SDKs and Mediation Packages

Click the `Install All` button to install all displayed SDKs and mediation packages.

<img src="Docs/qai_install_all.jpg" width="550"/>

## Explanation of Buttons

<img src="Docs/qai_buttons.jpg" width="550"/>

1. Open Settings
2. Reload Packages
3. Open Unity Package Manager
4. Install or update all SDK and mediation packages

## Displaying Current Package Status

<img src="Docs/qai_package_state.jpg" width="550"/>

1. Installed and up-to-date
2. Installed but with updates available  
   Example: v3.13.1 (current) → v3.14.0 (new)
3. Not installed

## How the Plugin Works

This plugin operates by using `Open UPM` as a Scoped Registry.

You can check and manage it here:  
`Unity Editor: Project Settings > Package Manager > Scoped Registries`

![scoped registries](Docs/upm_scoped_registries.jpg)

Installation and uninstallation are handled through Unity Package Manager's built-in functionality. If you decide to remove this plugin, it will not affect the packages that were installed via the plugin.

You can view and manage installed packages under:  
`Unity Editor: Window > Package Manager > My Registries`

<img src="Docs/upm.jpg" width="800"/>