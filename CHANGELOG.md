# Changelog

## 0.3.0 development preview

- Resolve both reported 20230414 Photon auth signatures by exact types and parameter lists. Require both before enabling the RecNet integration.
- Gate KromaRec Photon authentication and defer the settings factory override until the native auth value roles are verified.
- Add a standalone BepInEx probe, offline interop reports, and regression coverage for method discovery, diagnostic labels, config, ticket lifetime and reconnect callbacks.
- Remove the inherited EAC readiness and challenge hooks and the CheatManager and DUID overrides.
- Keep RecNet HTTPS discovery, fixed-route diagnostics, and native Unity scene diagnostics available for startup testing. Photon response and stage diagnostics on this branch await the Windows-specific bindings.
