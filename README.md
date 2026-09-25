# Unity Mobile Build Doctor

A small, read-only Unity Editor extension that checks a few common Android and iOS release settings and exports a Markdown report.

## What it checks

- Application identifier and bundle version
- Orientation configuration
- Android scripting backend, SDK ordering, keystore selection, and CPU architectures
- iOS bundle identifier, with a reminder to verify signing and capabilities in Xcode

Findings are advisory. The tool does not edit project settings, inspect source code or assets, access the network, read keystore passwords, build an app, or verify store policy compliance.

## Requirements

- Unity 2021.3 LTS or later (the project is distributed as source; no package manager dependency)
- A Unity project with Android or iOS modules installed for the checks you want to run

## Install

1. Copy the `Editor` folder into your Unity project's `Assets/UnityMobileBuildDoctor/` folder.
2. In Unity, open **Tools → Build Doctor → Mobile Build Doctor**.
3. Select **Run checks**. Select **Export Markdown report** to save the results.

You can also import this repository as a local Unity package by adding a `package.json` later; for now, copying the editor folder is the simplest supported installation method.

## Compatibility notes

The source uses Unity `PlayerSettings` APIs available in recent Unity LTS versions. Unity API signatures can vary between releases. If you use an older editor or see a compile error, open an issue with your Unity version and the compiler message. The checks are designed for the Editor and are not included in player builds.

## Development

The checks live in `Editor/UnityMobileBuildDoctor.cs`. They are read-only and emit findings with a severity, stable code, title, and detail. To add a check, add it to `BuildDoctorChecks.Run()` or one of its target-specific methods and document its limits here.

Manual smoke test:

1. Open a disposable Unity project with Android Build Support installed.
2. Copy the `Editor` folder as described above and let Unity compile.
3. Open the window and run checks.
4. Export a report and confirm it contains the selected project settings without secrets.
5. Change a disposable setting, rerun checks, and confirm the matching finding changes.

## Security and privacy

The extension runs locally in the Unity Editor. It has no network code and writes a report only when the user chooses an export path. Reports include project settings and Unity version; review the report before sharing it. Never commit signing keys, passwords, provisioning profiles, or private project data.

## Contributing

Bug reports and focused pull requests are welcome. Include your Unity version, operating system, active build target, and exact compiler or runtime message. Do not attach keystores, credentials, or proprietary project files.

## License

MIT. See [LICENSE](LICENSE).
