# Plugin Server Context

This context defines the domain language for the personal plugin server PoC and its administration workflow.

## Plugin Catalog

**Plugin**:
A trusted, in-process .NET server extension implementing `IPluginEntry`, which can provide backend endpoints and optionally static frontend assets. Non-.NET and frontend-only plugin runtimes are future directions, not part of the PoC.
_Avoid_: Add-on, module (when referring to a catalogued plugin)

**Plugin Record**:
The admin-managed catalog entry that identifies a plugin and its name, URL subpath, currently active managed bundle, and enabled state.
_Avoid_: Plugin config (when referring specifically to the persisted record)

**Plugin Version**:
An opaque string exposed by `IPluginEntry`, displayed as supplied without a host-enforced versioning scheme.
_Avoid_: Semantic version (unless a plugin chooses to use that format)

**Launchable Application**:
A user-facing application that a plugin explicitly offers for opening from the dashboard; backend-only plugins do not have one.
_Avoid_: Plugin (when referring specifically to the user-facing application)

**Application Descriptor**:
The optional host-owned structural metadata subobject on a plugin entry for the one application it may declare in the PoC; null means there is no user-facing application. When present, it includes the application's user-facing name, SVG markup string, and launch mode, separate from how a dashboard presents it.
_Avoid_: Application UI (when referring to the metadata contract)

**Abstract UI Component**:
A host-defined semantic UI primitive, such as Button, Label, Text, Input, Form, Layout, Checkbox, Toggle, or Upload, with interaction logic owned by the app plugin and visual treatment supplied by the selected dashboard through a React context provider.
_Avoid_: Dashboard widget (when referring to a shared component contract)

**In-Page UI Module**:
A React module mounted inside the selected dashboard for a dashboard-window application, using one-way data flow with actions returned through callbacks or events.
_Avoid_: Standalone UI (when referring to an app opened in a new browser tab)

**Launch Mode**:
The plugin-declared way an application opens: in a dashboard window or in a new browser tab.
_Avoid_: Launch preference (the plugin declaration is authoritative in the PoC)

**Dashboard Window**:
A dashboard-managed window for a launchable application; different applications may be open concurrently, but each application has at most one such window.
_Avoid_: Browser tab (when referring specifically to an in-dashboard window)

**App Switcher**:
A dashboard control for moving among currently open applications, especially when only one app can be shown at a time on a small screen.
_Avoid_: Application launcher (the launcher lists apps to open; the switcher lists apps already open)

**Dashboard**:
The home-page experience used by the administrator to find and launch applications; the PoC uses one administrator-selected dashboard.
_Avoid_: Admin UI (when referring specifically to the application-launching home page)

**Application Selector**:
The dashboard interface for finding launchable applications, with a fuzzy-search field focused when opened.
_Avoid_: Desktop icon grid (when referring to how apps are discovered)

**Dashboard Taskbar**:
The persistent dashboard bar containing the application-selector button and shortcuts to currently open apps.
_Avoid_: Application Selector (when referring to the bar that also switches among open apps)

**Dock**:
Working name for the PoC's default dashboard plugin, which renders the launcher and application windows; not a settled public product brand.
_Avoid_: Harbor Deck (name of an existing self-hosted start-page product)

**Dashboard Plugin**:
A plugin that provides the graphical home-page and application-launching experience using the host's structural and abstract UI contracts.
_Avoid_: Dashboard (when referring to the provider rather than its user-facing experience)

**Dashboard Provider**:
The single enabled dashboard plugin selected by the administrator to render the home page.
_Avoid_: Dashboard plugin (when referring specifically to the currently selected provider)

**Plugin Management UI**:
The plugin-provided standalone administrator interface for routine catalog and managed-file operations, opened in a separate browser tab.
_Avoid_: Host recovery page (when referring to routine plugin administration), Dock plugin-management app (when referring to the standalone UI)

**Dock Plugin-Management App**:
A future Dock-integrated application for plugin-management workflows, distinct from the standalone Plugin Server management UI.
_Avoid_: Plugin Management UI (when referring to the future in-dashboard app)

**Host Recovery Page**:
The minimal host-provided administrator interface for restoring access when a plugin-provided UI fails.
_Avoid_: Plugin Management UI (when referring to host recovery)

**Route Subpath**:
The URL prefix assigned to one plugin for its backend endpoints and optional public assets.
_Avoid_: Public path, filesystem path

**Public Folder**:
The filesystem directory configured for a plugin's static frontend assets.
_Avoid_: Route subpath, web root

**Managed Plugin Directory**:
The server-controlled filesystem root where plugin DLLs and related files are stored and selected.
_Avoid_: Arbitrary plugin path

## Administration

**Administrator**:
The sole user considered in the PoC, responsible for managing the plugin catalog and server.
_Avoid_: End user (when referring to the PoC's administrator)

**Plugin Status**:
The server's runtime report for a plugin record, such as disabled, loaded, or failed, including relevant diagnostics.
_Avoid_: Plugin state (when referring to a server-reported runtime result)

**Pending Restart**:
A saved catalog change that has not yet been applied to the currently running plugin set.
_Avoid_: Runtime update