# Modern DigitalRune Builds

## Official Source in Your Fork

Shared source is in C:/source/Indie-Games-Core/DigitalRune. origin is mortlockpaul-arch/DigitalRune; upstream is DigitalRune/DigitalRune. The official source is BSD 3-Clause licensed. The old decompiled projects are retained for comparison but removed from the active solution. No changes have been pushed.

## FNA and MonoGame Builds

Build/Modern contains six .NET 9 Windows runtime projects: base, Mathematics, Geometry, Animation, Physics and Physics.Specialized. GraphicsBackend selects FNA (default) or MonoGame. MonoGame uses DesktopGL 3.8.4.1 by default; MonoGameVersion can override it. FnaProject can override the sibling FNA source project path. Each backend has separate bin and obj folders. All framework-dependent libraries in a game must use the same backend.

## Build-Time Game References

Shared/DigitalRune.targets in Indie-Games-Core substitutes direct DigitalRune DLL references with the official source projects. UseSharedDigitalRune=false restores the original binary-reference selection. The existing Zombie game uses FNA. A MonoGame DigitalRune build alone does not migrate that entire game to MonoGame.

## Running the Built-In Samples

Open DigitalRune.SampleTester from the solution or the supplied executable. Select FNA or MonoGame, then BasicMathSample or BoxStackSample. Run builds and starts that example. Headless Check runs the physics sample without a graphics window; the math sample writes results to the log. Escape closes the graphical sample. The sample classes are linked unchanged from the repository; a small host provides their display and logging instead of building the full legacy sample-browser content pipeline.

## Verified Scope

All six libraries compile for both backends. The original math sample and 600-step BoxStack check pass on both, and both graphical hosts completed smoke checks. Legacy Xbox LIVE Avatar animation/ragdoll helpers are excluded from the desktop builds. The Zombie game still has other legacy animation-library build errors and is not claimed playable.

## Stable SDK Selection

DigitalRune/global.json pins installed stable SDK 10.0.403 and excludes preview SDKs. Libraries still target .NET 9 Windows. NETSDK1057 is an informational preview-SDK notice; an ILSpy checkout may intentionally request a preview SDK in its own global.json. The sample tester clears inherited SDK overrides before invoking child builds.

## Commands

Run from the DigitalRune fork. The expected layout has the FNA repository beside it in Indie-Games-Core.

```powershell
dotnet build Build/Modern/DigitalRune.Physics.Specialized/DigitalRune.Physics.Specialized.csproj -p:GraphicsBackend=FNA -p:Platform=x64
dotnet build Build/Modern/DigitalRune.Physics.Specialized/DigitalRune.Physics.Specialized.csproj -p:GraphicsBackend=MonoGame -p:Platform=x64
dotnet run --project Tools/SampleTester/SampleTester.csproj
```

The new projects use the original upstream compile lists. Source library/sample files are unchanged; migration files live under Build/Modern and Tools/SampleTester. Retain LICENSE.TXT when redistributing. The six runtime builds do not include DigitalRune.Graphics, the full sample browser, content builders or the legacy MonoGame submodule.
