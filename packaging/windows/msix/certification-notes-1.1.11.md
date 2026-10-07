# FirawSelector 1.1.11: Microsoft Store submission notes

## Package

- Update the existing product `9N8MV05X1NVP`.
- Upload `dist/FirawSelector-1.1.11.0-x86-store.msix`.
- The package uses identity `Firawynix.FirawSelector` and publisher
  `CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8`.
- The package is unsigned locally; Microsoft signs it after certification.
  Do not upload the separate laboratory sideload package.

## What's new

After you choose a browser, FirawSelector attempts to bring that browser's
window to the foreground, including when the browser is already open. The
URL still opens if Windows restricts the focus change. The chooser and Studio
also include the same custom cursors as the existing classic release.

## Certification test instructions

FirawSelector is a user-controlled browser chooser. It registers HTTP, HTTPS,
FTP, `microsoft-edge`, and HTML associations through the MSIX manifest. The
user must choose it as the default app in Windows Settings; it does not
change that preference automatically.

To test, launch FirawSelector Studio and configure at least one installed
browser. Open that browser, switch to another desktop application, then open
an HTTP or HTTPS link through FirawSelector. Choose the open browser in the
chooser. The browser receives the link, and FirawSelector attempts to bring
its window to the foreground. Windows may restrict foreground activation.
Press `Esc` to cancel a separate test link.

The `runFullTrust` capability supports the classic .NET Framework desktop
application, installed-browser launching, and Windows clipboard access. The
application does not require elevation, an account, or test credentials.

The Store package uses Store-managed updates. It does not install or register
the optional Native Messaging host used by browser extensions in the classic
installer edition. It does not send clicked URLs or browsing history to
Firawynix. The optional local link log is off by default.

Read the [privacy policy](https://github.com/firawynix/firaw-selector/blob/main/privacy.md).
