# Icon build consistency
> Every Windows executable uses the same freshly generated selector icon

Entry: `build.cmd` icon stage

- `tools/mkico.cs` regenerates `firawselector.ico` on every build.
- `/win32icon` is applied to motor, native host, Studio, and setup.
- `dist/` receives all four compiled executables after icon generation.

Gotcha: the native host previously omitted `/win32icon`, so Explorer showed a generic executable icon while the other three matched.

Updated: 2026-09-15
