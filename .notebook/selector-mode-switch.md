# Selector mode switch
> Compact and full choice windows reopen the same URL after a mode change

Entry: `FirawSelector.cs:Escolha.Monta()`

Flow:
- Compact footer **Modo completo** sets `Cfg.Compacto=false`, saves, sets `Reabrir`, and closes.
- Full action area **Modo compacto** performs the inverse with the same `Reabrir` flag.
- `Programa.Main()` loops while `Escolha.Reabrir`, preserving the current `Cfg`, URL, and remember behavior.

Updated: 2026-09-15
