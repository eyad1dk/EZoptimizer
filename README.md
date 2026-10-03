# EZoptimizer

Set up your Windows PC for what you're about to do.

EZoptimizer brings a live system overview, practical Windows controls and reversible tuning profiles into one desktop app. Review the proposed changes before applying, with original settings saved locally.

![EZoptimizer overview](docs/overview.png)

## What's inside

- **Gaming:** prefers an installed High performance plan and reduces desktop animations.
- **Coding:** uses Balanced power and reduced animations, with an optional keep-awake session for long builds.
- **Everyday:** Balanced power with Windows animations enabled.
- **Quiet:** prefers an installed Power saver plan with reduced animations.
- **Live overview:** CPU usage, physical memory usage, system-drive free space and pressure hints.
- **Inspection:** the twelve largest processes by working set, plus startup entries from Run keys and startup folders.
- **Windows tools:** Game Mode, captures, graphics preferences, storage, startup, updates and notifications.
- **History and undo:** durable change records, read-back verification and recovery after an interrupted apply.
- **Local reports:** export system information without usernames, paths or process names.

Profiles are presets for two animation preferences and an existing power plan. They do not change game files or compiler settings. If the preferred plan is missing, the current plan stays selected. You can change the selection before applying.

## Download and run

Get **EZoptimizer.exe** from [Releases](https://github.com/eyad1dk/EZoptimizer/releases). This is a portable, self-contained Windows x64 app; no installer or separate .NET runtime is needed. Launch it, choose **Profiles**, review your options, click **Preview changes**, then **Apply reviewed changes**.

Use **History & undo → Undo session** to restore the saved settings before trying another profile.

Version 0.1.0 is a preview. The executable is unsigned. The initial build has been checked on Windows 10 x64; Windows 11 and a wider range of hardware still need hands-on testing. Some PCs expose only a Balanced power plan, and managed PCs may restrict changes.

## What changes

| Control | Mechanism | Tradeoff |
| --- | --- | --- |
| App animations | Windows SystemParametersInfoW | Less motion; apps may ignore the preference |
| Menu animations | Windows SystemParametersInfoW | Menus appear without their usual animation |
| Power plan | Windows powercfg /setactive | Higher performance plans can use more power and increase heat |
| Keep awake | Windows SetThreadExecutionState | Stops idle sleep while this app is open; the screen can still turn off |

Animation preferences change how the desktop feels. They do not promise higher FPS. Power-plan benefits depend on hardware, cooling and workload. Measure an actual game or build before and after if you're comparing performance.

EZoptimizer does not disable antivirus, Windows Update, services or security features. It does not delete files, purge memory, alter process priorities, apply network tweaks or install drivers. Storage cleanup opens Windows so you can review what will be deleted.

## Recovery and privacy

Change history is stored in %LOCALAPPDATA%\EZoptimizer\history. The engine writes intent to disk before each Windows change, then reads the value back. On an interrupted apply, open the app again and use Undo.

If another app changes a setting after the session, Undo leaves that setting alone and reports the conflict. Restoring other settings still proceeds. Only one session can be active at a time, and only one app instance can run per user session.

There is no telemetry, account system, background service or automatic updater. Reports stay wherever you save them. Windows tools open locally. Startup inspection is partial; it does not include every scheduled task or packaged startup app.

## Build from source

Install the .NET 8 SDK on Windows, then run:

~~~powershell
dotnet build src/ForgePC/ForgePC.csproj -c Release
dotnet run --project tests/ForgePC.Tests/ForgePC.Tests.csproj -c Release
dotnet publish src/ForgePC/ForgePC.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts
~~~

The result is artifacts/EZoptimizer.exe. The source folder and namespace use the original working name, ForgePC.

Run the packaged app's read-only compatibility check:

~~~powershell
$result = Start-Process artifacts/EZoptimizer.exe -ArgumentList '--smoke-test' -Wait -PassThru
$result.ExitCode
~~~

Exit code 0 means the UI could be constructed and native memory, process, startup, power-plan and animation reads succeeded. It does not prove tuning works on every device. The regression suite uses a simulated backend to exercise apply failures and recovery without changing the test machine.

## Releases

The included GitHub Actions workflow tests and builds the app on pushes and pull requests. Pushing a version tag also attaches the executable and its SHA-256 checksum to a preview release.

The repository owner can publish the next version after updating the version and release notes:

~~~powershell
git tag v0.1.1
git push origin v0.1.1
~~~

Allow Actions to finish before sharing the release link. Never commit local bin or obj folders.

## Contribute

Bug reports with reproduction steps and Windows version are useful. Suggestions should explain the workload, the measurable benefit and how the change can be undone. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

MIT licensed. Technical references: [Windows power configuration](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options), [system UI parameters](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfow), and [session sleep requests](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-setthreadexecutionstate).
