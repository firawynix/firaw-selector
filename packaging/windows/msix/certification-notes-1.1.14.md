# FirawSelector 1.1.14: Microsoft Store submission

Upload `dist/FirawSelector-1.1.14.0-x86-store.msix` to product `9N8MV05X1NVP`.
Don't upload the separate laboratory package. Store manages MSIX updates;
the package doesn't run the classic EXE updater.

## Changes and restricted capability

FirawSelector is a user-controlled browser and profile chooser. This update
adds an optional Native Messaging bridge for the separately distributed
browser extensions. The host executable is inside the MSIX and uses a stable
console App Execution Alias, `FirawSelectorNativeHost.exe`.

The new `unvirtualizedResources` capability is needed for Windows 11's
fine-grained virtualization exclusions. External browsers must read the host
manifest and its registration. Only the following resources are excluded:

- `HKCU\Software\Google\Chrome\NativeMessagingHosts\com.firawynix.firaw_selector`
- `HKCU\Software\Microsoft\Edge\NativeMessagingHosts\com.firawynix.firaw_selector`
- `HKCU\Software\Mozilla\NativeMessagingHosts\com.firawynix.firaw_selector`
- `%LOCALAPPDATA%\FirawSelectorStore`, for host JSON manifests, the integration
  enabled flag, and a backup of previous host-registration paths.

The package doesn't globally disable registry or filesystem virtualization.
The user explicitly selects **Conectar extensões** in Studio's options and
confirms the per-user registration. No elevation, machine-wide registration,
certificate installation, or browser-security change is involved. The host
manifest accepts only the known extension IDs. **Desconectar extensões**
restores previous registration paths if the current paths still belong to
this integration. A later classic installation isn't silently overridden.
Disconnect before uninstalling if previous paths need to be restored.

Native Messaging integration is enabled only on Windows 11. Windows 10 keeps
the desktop chooser, default-association configuration, and manual protocol
fallback. The existing `runFullTrust` capability supports the desktop app,
local browser launch, clipboard operations, and the packaged native host.

The `firawselector:` protocol accepts only an encoded HTTP(S) destination.
Neither it nor Native Messaging executes commands supplied in a clicked URL.
The user chooses Windows default associations in Settings; the app doesn't
change them automatically. Saved browser/profile rules apply to destination
sites. Optional extension preferences apply to the source origin and can be
changed or forgotten in the extension's settings.

## Test instructions

1. On Windows 11, open Studio, configure an installed browser/profile, and
   enable **Conectar extensões** under **Opções**. Confirm the explanation.
2. Install extension 1.1.4. For an unpacked Chromium test, use
   `dist/extensions/chrome`, which retains the known local extension ID.
   Don't use `chrome-store` unpacked; its ID is assigned by the web store.
3. Allow extension access on a test source site. Click several HTTP(S) links
   in sequence, including in Opera's WhatsApp sidebar. Each click must reach
   the app without requiring a browser-protocol prompt or a focus change.
4. Select a browser/profile and remember the destination domain. Another link
   for that domain must follow the saved rule. Other domains remain separate.
5. Verify foreground activation with multiple Opera profiles open: the chosen
   profile, not a different Opera window, must come to the foreground.
6. Disconnect in Studio and verify that prior host paths are restored. The
   desktop chooser remains usable without the extension.

No account or server access is required for these tests. Rules and settings
stay local. The optional URL log is off by default. See the
[privacy policy](https://github.com/firawynix/firaw-selector/blob/main/privacy.md).

## Verification record

- MakeAppx built Store 1.1.14.0 and Lab 1.1.14.1 successfully.
- Lab installation reports `Ok`; the packaged execution alias answered a
  framed Native Messaging ping with version 1.1.14.
- Automated extension/background tests pass for repeated native requests,
  origin preferences, Shift, reset, and rejected non-HTTP(S) destinations.
- User enabled the integration; external checks verified the real host JSON
  manifests and HKCU pointers. User confirmed that multiple consecutive links
  in Opera's WhatsApp sidebar open the picker without a focus change.
- The diagnostic button's internal message channel closed without a response.
  The button now pings the native host directly without changing link routing.
  Its automated test passes. On October 14, 2026, the user confirmed that the
  keyed extension reports host version 1.1.14 and that the last clicked link
  was accepted by that host. The native bridge and repeated sidebar openings
  are ready for submission; Microsoft certification is not guaranteed.
- Previous protocol-only attempts 1.1.1–1.1.3 failed the user's sidebar test;
  don't submit those extension packages.
