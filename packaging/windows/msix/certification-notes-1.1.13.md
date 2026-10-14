# FirawSelector 1.1.13: Microsoft Store submission

Upload `dist/FirawSelector-1.1.13.0-x86-store.msix` to the existing product
`9N8MV05X1NVP`. The laboratory package has a different identity; don't upload
it to the Store. Store updates continue through Microsoft Store.

## What's new

Optional browser extension integration now works with the Microsoft Store
edition through the registered `firawselector:` protocol. The extension can
remember your opening method for each source site. The desktop application
respects saved browser and profile rules for the destination site. The
compact picker now also offers the option to remember a destination domain.

## Certification notes

FirawSelector is a user-controlled browser and profile chooser. Its MSIX
manifest registers HTTP, HTTPS, FTP, microsoft-edge, HTML file associations,
and the firawselector protocol. The user chooses default associations in
Windows Settings. The application doesn't change them automatically.

The new protocol receives only a URL-encoded HTTP or HTTPS destination:
`firawselector://open/?url=https%3A%2F%2Fexample.com%2F`. Other destination
schemes are rejected. Receiving a link applies the user's local rules or
opens the picker. It doesn't execute commands supplied in the link.

The optional browser extension first uses Native Messaging for the classic
installer edition. If that host is absent, it offers a user-clicked link
to the Store edition through Windows protocol activation. The extension
doesn't install a native host or modify browser/Windows security settings.
The browser controls its external-application confirmation. Extensions are
distributed separately in the browser stores; the desktop picker also
works without them.

To test the desktop package, open FirawSelector Studio and configure an
installed browser/profile. Open the example protocol above from Windows.
The picker appears unless a saved rule selects a browser. Select “Always use
this browser for example.com” and a browser. The next example.com link uses
that choice. Review rules and remembered sites in Studio. No account,
credentials, elevation, or server access is needed for this flow.

With extension 1.1.2, click a link in an ordinary HTTP(S) page. If the native
host is absent, select the Store option in the extension's prompt. Select
“Remember for this site” to reuse that method for future clicks from the
same source origin. The extension settings allow changing or forgetting
that preference. Browser site-access restrictions remain in force.

The existing runFullTrust capability supports the classic .NET Framework
desktop application, browser launching, and clipboard access. No additional
restricted capability was added. The packaged application doesn't launch
the classic installer updater. Rules and preferences are local; the
optional link log remains off by default. Clicked URLs aren't sent to
Firawynix. See the
[privacy policy](https://github.com/firawynix/firaw-selector/blob/main/privacy.md).

## Verification record

- Store package built successfully with MakeAppx.
- Lab 1.1.13.1 installed with status `Ok` on October 14, 2026.
- User confirmed extension-to-MSIX picker activation.
- Automated extension tests cover native success, protocol fallback,
  remembered source origins, another origin, Shift, and forgetting a choice.
- Compiled engine checks cover remembered browser/profile selection,
  unrelated domains, Shift, and rejection of non-HTTP(S) protocol payloads.
- User's final repeat-click test is pending; certification isn't submitted.
