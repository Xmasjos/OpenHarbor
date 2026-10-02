# Plugin Server Context

This context defines the domain language for the personal plugin server PoC and its administration workflow.

## Plugin Catalog

**Plugin**:
A trusted, in-process server extension that can provide backend endpoints and optionally static frontend assets.
_Avoid_: Add-on, module (when referring to a catalogued plugin)

**Plugin Record**:
The admin-managed catalog entry that identifies a plugin and its name, URL subpath, DLL, optional public folder, and enabled state.
_Avoid_: Plugin config (when referring specifically to the persisted record)

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

**Plugin Status**:
The server's runtime report for a plugin record, such as disabled, loaded, or failed, including relevant diagnostics.
_Avoid_: Plugin state (when referring to a server-reported runtime result)

**Pending Restart**:
A saved catalog change that has not yet been applied to the currently running plugin set.
_Avoid_: Runtime update