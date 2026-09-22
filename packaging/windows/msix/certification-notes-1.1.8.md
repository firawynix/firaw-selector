# FirawSelector 1.1.8: Microsoft Store submission notes

These notes describe the **MSIX edition** of FirawSelector. They do not claim
features that are available only through the classic installer.

## Package to submit

- Product ID: `9N8MV05X1NVP` (update the existing product, not a new listing).
- Package: `dist/FirawSelector-1.1.8.0-x86-store.msix`.
- Identity: `Firawynix.FirawSelector`.
- Publisher: `CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8`.
- Architecture: x86; Windows 10 build 19041 or later, including 64-bit Windows.
- The Store package is unsigned locally. Microsoft signs it after approval.
- Do not upload `FirawSelector-1.1.8.0-x86-sideload.msix`: it has a separate
  laboratory identity and a private test certificate.

On September 22, 2026, the laboratory MSIX signature verified successfully.
Windows installed `1.1.8.0` with architecture `X86` and package status `Ok`.
The package was then removed. This installation check does not cover
interactive behavior.

## What's new

The chooser now puts **Copy link [0]** above the browser list. Pressing `0`
or numpad `0` copies the current link without opening a browser. In Studio,
users can hide a configured browser from the visible chooser while retaining
its numeric shortcut. The package continues to support Windows 10 and 11 on
x86 and x64 devices. Packaged copies do not run the site's classic-installer
update checker; Store updates use the Microsoft Store channel.

## Notes for certification (paste into Partner Center)

FirawSelector is a user-controlled browser chooser. It registers Windows
HTTP, HTTPS, FTP, `microsoft-edge`, and HTML file associations through its
MSIX manifest. The user must choose FirawSelector as the default app in
Windows Settings; the app does not change that preference automatically.

The `runFullTrust` capability is needed because this is a classic .NET
Framework desktop application that opens installed browsers and uses the
Windows desktop clipboard. It does not request elevation. No account or
test credentials are required.

To test: launch FirawSelector Studio and configure at least one installed
browser. In Windows Settings, make FirawSelector the default for HTTP and
HTTPS. Open an HTTP or HTTPS link from another application. The chooser
appears; choose a browser, press `0` to copy the link, or press `Esc` to
cancel. In Studio, hide a browser from the visible chooser and verify its
assigned number still opens it when the chooser appears.

The packaged application uses Store-managed updates. It does not download or
launch the classic installer on startup.

Browser add-ons are separate, optional products being submitted to their
respective browser stores. They use local Native Messaging in the classic
installer edition. The **MSIX edition does not install or register the
Native Messaging host**, so add-on integration is not part of this Store
submission. Users who want that optional feature can use the classic
installer from the project website; the basic chooser works without it.

Rules and preferences are stored locally. The optional link log is off by
default. FirawSelector does not send clicked URLs or browsing history to
Firawynix. The privacy policy is available at
https://github.com/firawynix/firaw-selector/blob/main/privacy.md.

## Before submission

1. Test the chooser and Studio interactively in an isolated Windows
   installation, using the separately signed laboratory package rather than
   the Store upload file. The installation check above does not cover behavior.
2. Confirm the published package remains `1.1.5.0` before starting the
   update. This was the version shown in Partner Center on September 22, 2026.
3. Commit and publish the source changes used for this build so the public
   repository corresponds to the submitted binary.
4. Upload the Store package to the existing product's update submission and
   paste the certification notes above.
5. Do not advertise browser add-on integration in the MSIX Store listing
   unless that capability is implemented and tested in the packaged edition.
