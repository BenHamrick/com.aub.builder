# AUB Builder

Unity editor package for [Automatic Unity Builds](https://automaticunitybuilds.com). This package is required for structured builds when using AUB's self-hosted runners.

## What it does

When an AUB runner executes a Unity build, it calls `-executeMethod AUB.Builder.Build` on your project. This package provides that entry point and handles:

- **Build execution** - Compiles your project for the target platform (Windows, Linux, macOS, WebGL, Android, iOS, consoles)
- **Version stamping** - Injects build ID and git commit hash into your build
- **Build result reporting** - Writes a structured JSON result (`aub-build-result.json`) with output size, duration, warnings, and errors for the runner to consume
- **Platform switching** - Automatically switches Unity's active build target
- **Scripting define injection** - Adds custom `#define` symbols for CI builds
- **macOS code signing** - Optional code signing and notarization for macOS builds
- **Bee cache cleanup** - Utility to clean Unity's incremental build cache

## Installation

### Via Unity Package Manager (recommended)

1. Open your Unity project
2. Go to **Window > Package Manager**
3. Click **+** > **Add package from git URL...**
4. Enter:
   ```
   https://github.com/BenHamrick/com.aub.builder.git
   ```
5. Click **Add**

### Via manifest.json

Add this line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.aub.builder": "https://github.com/BenHamrick/com.aub.builder.git",
    ...
  }
}
```

## Requirements

- Unity 2020.3 or newer

## How it works

The AUB runner detects this package by checking your project's `Packages/manifest.json` for `com.aub.builder`. If found, Unity is launched with:

```
Unity -batchmode -quit -nographics -executeMethod AUB.Builder.Build
```

Build configuration is passed via environment variables set by the runner:

| Variable | Description |
|----------|-------------|
| `AUB_BUILD_TARGET` | Target platform (e.g. `windows`, `linux`, `webgl`) |
| `AUB_OUTPUT_DIR` | Where to write build output |
| `AUB_BUILD_ID` | Unique build identifier |
| `AUB_COMMIT_HASH` | Git commit being built |

## Without this package

If this package is not installed, the runner falls back to Unity's raw `-buildTarget` flag. This works for basic builds but you lose version stamping, structured result reporting, and scripting define injection.

## License

MIT
