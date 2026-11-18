# Windows 20230414 acceptance

First complete [PHOTON_DIAGNOSTICS.md](PHOTON_DIAGNOSTICS.md). The preview supports startup checks. Resume the Photon and two-client stages with a build containing the verified native auth binding.

Use two Windows x64 systems or isolated Windows installations. Prepare a writable copy of Rec Room Steam app `471710`, depot `471711`, manifest `6426603215211043630` on each. Use two distinct KromaRec accounts. Prepare one additional local account for the private-room denial test if both of the first two accounts already have access.

1. Set up KromaRec 0.9.0 and its bundled patched Luxon 1.5. Preserve Host when forwarding HTTPS to Rust and pass WebSocket upgrades through the notify origin. Make the HTTPS names resolve on both clients and trust the proxy certificate chain in Windows. Set client-reachable Luxon addresses for UDP 5058/5055/5056. Keep gate address/key shared by Rust and Luxon on loopback.
2. From each Windows machine run the 0.9 release preflight with Python 3. Replace both HTTPS origins with your deployment. For a private CA, append `--ca C:\KR\trusted-root.pem` to this check and install the CA in Windows separately for the game.
   ```powershell
   py -3 .\tools\https_preflight.py https://ns.rec.test --match-url https://match.rec.test
   ```
3. Generate interop with BepInEx 6.0.0-pre.2 Windows x64 IL2CPP, then build and install as in [BUILDING.md](BUILDING.md) and [INSTALLATION.md](INSTALLATION.md). Set `General.Debug = true` before starting the game. Confirm required hook initialization and Unity diagnostics in the BepInEx log. Once the authenticated build has verified Photon diagnostic bindings, record its numeric stage results. Record a missing Photon diagnostic hook if a stage cannot yet be observed.
4. Provision local accounts over the auth service with `grant_type=create_account`, `ver=20230414`, and a unique password per account with password signup enabled. Use the access token to look up each username at `accounts GET /account/me`; disable signup after provisioning. Use manual password login in the game. Avoid including command lines, passwords, tokens, user IDs, or unfiltered native logs in the report.
5. Use the table below in order. Stop at the first failed stage and record its safe diagnostics and visible behavior. Keep the two clients' raw logs private.

| Stage | Pass evidence |
| --- | --- |
| Startup | The candidate DLL loads in both copies. Discovery returns 200 and the advertised Matchmaking origin matches MatchUrl. Version returns 0; record 200 for each native bootstrap route that is actually requested. |
| Login, Orientation and dorm | Separate accounts sign in; new account renders Orientation and can enter its respective dorm. A later login loads the proper profile and dorm again. |
| Encrypted Photon auth and handoff | The game requests and accepts a fresh connection-info ticket. Record which NameServer auth opcode was observed, its zero result, then the Master and Game results and the Unity scene load. A successful Master room response is routing evidence. A Game response plus a rendered scene is admission evidence. |
| Public instance | A creates and publishes a built-in room and joins. B discovers it and joins the same instance. Confirm instance equality locally without pasting IDs and verify the two avatars on both screens. |
| Replication | Move A while B watches, then move B while A watches. Each triggers an observable gesture or interaction and the other sees it. Record each direction independently. |
| Leave and rejoin | B leaves while A remains and rejoins. Both avatars become visible and both directions of movement and gestures still work. |
| Private reservation | A creates a private instance. A separate account without an invite is denied through matchmaking. A invites B; B joins and both clients render the room. The direct Photon reservation-denial path is also covered by the wire suite. |
| Network reconnect | Briefly interrupt B's connection, restore it, then use the game's normal reconnect or rejoin UI. Confirm A is still in the room and both directions of replication resume. Compare the HTTP status of session-verify-lock and the status and numeric result of session-heartbeat before and after reconnect. If a ticket is stale, note whether the game requests fresh connection-info or requires a new sign-in. |

For the report, keep fixed `RecNet`, `Photon`, and `Unity` categories, numeric result/status codes, the client A/B designation, and observations such as “avatar visible” or “scene failed to render.” The Unity scene callback is a load milestone, and the generic scene-error diagnostic only recognizes common Unity error text. Game-specific bundle or scene errors may require local inspection before redacting a report.
