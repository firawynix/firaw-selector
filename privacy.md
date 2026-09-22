# Privacy policy

FirawSelector doesn't send analytics, account information, browsing history,
or telemetry to Firawynix. Rules and preferences stay on the user's computer.
The optional link log is off by default. If you enable it in the app, the app
stores clicked URLs, browser choices, and decision reasons locally on your
computer. You can disable or clear the log in FirawSelector Studio.

The classic installer edition checks a Firawynix update manifest when you
launch the app and may download a newer installer from the Firawynix server.
That request does not contain clicked URLs, rules, or browser history. The
Microsoft Store MSIX edition does not use this updater; the Store manages its
package updates. FirawSelector does not otherwise transfer your data unless
you explicitly ask it to open a link in a browser or use an optional feature
that requires a network request.

When you open a link, FirawSelector passes that link to the browser and profile
selected by your local rules. The chosen browser, not FirawSelector, then
contacts the destination. Optional browser extensions send the clicked URL to
the local FirawSelector application through the browser's native messaging
interface. They do not send it to Firawynix servers.

Build tools may access package repositories while maintainers create a release.
That build-time activity isn't part of the installed application's behavior.

Report privacy concerns through the
[public issue tracker](https://github.com/firawynix/firaw-selector/issues).
