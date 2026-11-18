# Native Windows acceptance for 0.3.1

Follow NEXT_STEPS.md for the initial build and installation. **NOT TESTED on Windows** — while account A and an independent account B are in a room, change A's password through the backend, then separately repeat with completed logout. Observe A's Game disconnect and SignalR close, B's continued session, fresh credentials, fresh reservation and rejoin. Test bearer expiry on an open SignalR socket and verify subsequent invitation delivery after reconnect. Record any failed stage without copying tokens or raw network payloads.

Use two Windows x64 Rec Room PC 20230414 installations, BepInEx 6.0.0-pre.2 IL2CPP and two independent KromaRec accounts. Target KromaRec 0.10.1 with patched Luxon 1.5.

Copy this table into the run report. Set each result to PASS, FAIL, BLOCKED or NOT TESTED and add the first numeric status or sanitized observation. PASS requires observing that milestone.

| Milestone | Current result | Pass evidence |
| --- | --- | --- |
| 1. Generate genuine interop | NOT TESTED | Original 20230414 depot starts with pinned loader and generates BepInEx/interop. Record the depot manifest and PhotonRealtime SHA-256. |
| 2. Verify metadata | BLOCKED | Offline report matches required managed signatures on that installation. Run the synthetic auth probe and resolve native member roles separately. |
| 3. Build main DLL | BLOCKED | Release build produces KromaRec.Patch.dll with zero warnings and errors. |
| 4. Load main plugin | BLOCKED | BepInEx loads 0.3.1 and the required Harmony installation message appears without exceptions. |
| 5. HTTPS discovery | NOT TESTED | Discovery and bootstrap succeed through the normal trusted CA, with matching MatchUrl and supported version. |
| 6. Local login and ticket | NOT TESTED | Account login, session initialization and fresh connection-info succeed against KromaRec 0.10.1. |
| 7. Encrypted Photon auth | BLOCKED | Verified ticket binding authenticates at NameServer, then native Master and Game handoffs succeed with encryption observed. |
| 8. Orientation or dorm | NOT TESTED | Correct scene renders after admission. Record scene failures separately from transport admission. |
| 9. Same room, two clients | BLOCKED | Both independent accounts receive the same instance and display each other's avatar. |
| 10. Movement and interactions | BLOCKED | Position, rotation, gestures and interactions appear in both directions, including after leave/rejoin and reconnect. |

## Windows actions

1. Confirm Steam app 471710, depot 471711, manifest 6426603215211043630. Generate interop using the pinned loader.
2. Run the commands in PHOTON_DIAGNOSTICS.md and copy its sanitized report. Close the probe before reinstalling the main patch.
3. Run the metadata check and main build from BUILDING.md. The current auth guard remains active until native bindings, encryption and handoff semantics are established.
4. Install the built DLL using INSTALLATION.md. Set General.Debug=true for the capture, then restart. Verify both hook installation and actual execution.
5. Configure reachable HTTPS service names, trusted certificates and Luxon UDP 5058/5055/5056. Keep the shared gate on loopback. Run the 0.10.1 backend bundle's HTTPS preflight on both Windows clients.
6. Sign in with independent accounts. Record HTTP stage/status, fresh ticket availability, real Photon stages and return codes. Keep credentials and packet payloads local.
7. After implementing the verified auth binding, complete Orientation/dorm, same-room admission, visible replication, party invitations, private room denial and leave/rejoin.
8. Test a transport disconnect, fresh connection-info, normal logout and account switch. In 0.10.1, completed normal logout evicts active Game actors and SignalR sessions and revokes future ticket admission, while Orientation logout retains the backend's startup exception. Record the native request ordering.

Debug output uses fixed labels and numeric codes. Numeric room and instance IDs support local comparison. Raw Photon room names, tokens, bearer values and arbitrary URLs stay out of shared logs. If a game security check blocks startup, record its stage and server response for review.

Original EAC, device identity and CheatManager checks remain active. Native failures involving those checks require documented compatibility work.
