# Text editing shortcuts
> Every Firaw window handles the same standard editing keys

Entry: `Janela.cs:JanelaFiraw.ProcessCmdKey()`

Flow:
- `CampoFocado()` walks nested controls to find the focused `TextBoxBase`.
- `Ctrl+A/C/X/V/Z` call the matching WinForms editing operation.
- `Ctrl+Y` sends `EM_REDO`, which is not exposed as a `TextBoxBase` method.
- Read-only fields still allow selection and copy but reject mutation commands.

Coverage: selector, link editor, Studio, installer, and log viewer inherit
`JanelaFiraw`.

Updated: 2026-09-15
