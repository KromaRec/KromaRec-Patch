# Verification

## Windows metadata correction, 2026-11-18

The supplied Windows report showed two verifier failures before compilation. The only required line marked MISSING was the Unity scene-load event. Both Photon method signatures and signature types passed. The optional Unity log event was also absent. No native code or managed plugin was loaded by that check.

The corrected verifier accepts a synthetic proxy with a standalone scene add accessor and no event row. It reports independent BestHTTP and Photon failures together and rejects corrupt metadata. The authorized depot-record fixtures cover matching files, incorrect manifest ID, missing and duplicate entries, and a same-size binary mismatch. The readiness and managed regression suites run independently of Windows. A fresh run with the genuine references is still needed to determine the main compiler result. No authentication guard was removed.

This correction was tested with SDK 6.0.428. ProtocolChecks, MetadataChecks, the pinned BepInEx config parser, BuildReadiness, DepotIdentity and PowerShell syntax (8 scripts) passed. VerifyInterop and PhotonAuthProbe each built with zero warnings and zero errors. In a separate managed fixture the revised NativeDiagnostics compiled against the pinned Il2CppInterop runtime, dispatched a CLR event and handled an absent log event. IL2CPP delegate dispatch remains untested. Both 0.10.1 backend wire regressions passed (24 encrypted Authenticate and one AuthenticateOnce per backend). The main project again stopped at KRBUILD_INTEROP in the remote workspace; the revised native build on the Windows installation has not been run.

## Final build preparation against KromaRec 0.10.1

| Check executed on Linux | Result | Evidence |
| --- | --- | --- |
| Managed regression and pinned BepInEx configuration suites | PASS | SDK 6.0.428. The shipped config parses with the actual loader library. |
| Cecil verifier and standalone probe | PASS | Both Release tool builds completed with zero warnings and zero errors. Synthetic metadata acceptance and rejection cases passed. |
| PowerShell syntax, preflight and sanitized readiness | PASS | Six scripts parsed. Five negative preflight paths, valid/invalid backend settings, unrelated log sources and credential-like suffix rejection passed. |
| 0.10.1 binary hashes and HTTP smoke | PASS | Both backend binaries and bundled Luxon matched SHA256SUMS. HTTP smoke ran against musl and glibc. |
| 0.10.1 encrypted synthetic NameServer regression | PASS | Each build accepted 24 Authenticate and one AuthenticateOnce. Separate loopback Master/Game steps used plaintext fixtures. The suite exercised party/privacy, SignalR, eviction, observer continuity and fresh admission. |
| Main DLL compilation | BLOCKED | Attempted the main Release project with the staged loader. MSBuild stopped before C# compilation with KRBUILD_INTEROP, zero warnings and one prerequisite error. Genuine Assembly-CSharp and Photon interop were unavailable. |
| Caddy HTTPS/WSS integration rerun | BLOCKED | Caddy executable unavailable here. HTTP smoke and source inspection do not test the Windows client's TLS path. |
| Windows build/install, authentic Harmony dispatch and runtime probe | NOT TESTED | No authorized Windows depot available here. Successful install branch requires a successful genuine main build. Hosted Actions was not executed. |
| Native encrypted authentication and two-player gameplay | BLOCKED | Native field roles, settings and handoff semantics have no verified implementation. |

FAIL denotes an observed unmet assertion. No executed regression ended in FAIL. The main build prerequisite failure is recorded above. See COMPATIBILITY.md for the contract differences and NEXT_STEPS.md for the first Windows run.

## Historical 0.3.1 verification against 0.10.0

| Check | Result | Scope |
| --- | --- | --- |
| .NET 6.0.428 managed regression | PASS | Configuration, discovery/Auth origin scoping, fixed route labels, numeric room IDs, ticket expiry and stale reconnect callbacks. |
| BepInEx configuration parser | PASS | Uses the staged pinned loader libraries. |
| Metadata checker build | PASS | Release, zero warnings and errors. |
| Standalone probe build | PASS | Release, zero warnings and errors. Native execution NOT TESTED. |
| Synthetic metadata fixtures | PASS | Complete managed shapes, exact auth methods, renamed peer discovery, wrong auth arguments, static auth method, static request accessor, corrupt DLL and recovery after rejection. |
| PowerShell syntax | PASS | Scripts parse locally. |
| Build preflight failures | PASS | Missing directory, missing loader, missing game files, missing interop and invalid metadata produced distinct KRBUILD messages. |
| KromaRec 0.10.0 encrypted NameServer regression | PASS | Musl and glibc runs each accepted 16 Authenticate responses and one AuthenticateOnce. Existing backend fixtures covered party invites, privacy, SignalR, Master/Game admission, events, disconnect and rejoin with synthetic peers. Downstream fixtures use loopback plaintext. |
| Main patch build | BLOCKED | Genuine 20230414 interop is absent. Direct build must stop at KRBUILD_INTEROP. Main compiler diagnostics and successful output classification cannot be exercised with that installation here. |
| Hosted Windows CI execution | NOT TESTED | Workflow includes the new scripts and synthetic tests. Syntax and selected branches were checked locally. |
| Native Windows gameplay | BLOCKED | Genuine metadata, member role evidence, plugin loading, TLS through Unity, encrypted native auth, handoffs, scenes and two-client movement require Windows acceptance. |

The baseline working tree already contained the 0.3.0 preview changes. Those changes are preserved and extended. The main plugin's settings override and ticket injection remain guarded until native evidence establishes the member roles, encrypted NameServer serialization and Master/Game handoff. The checker lists declared signatures and constants without treating them as proof of native behavior.

The 0.10.0 source retains the client ticket/discovery contract. Repeated AdditionalPlayerIds are passed through with the original game request. The client still conservatively invalidates cached credentials on observed logout, including the Orientation exception that the backend preserves. Native request ordering is an open acceptance item. The 0.9 format remains supported by the managed parser. See COMPATIBILITY.md for the contract audit and NATIVE-TESTING.md for every native milestone.

## 0.3.0 development preview

| Check | Result |
| --- | --- |
| .NET SDK 6.0.428 protocol suite | Passed. Resolves both reported signatures against managed fixtures and rejects missing types and wrong protocol type. Covers synthetic label detection, auth guard scope, discovery routing, startup diagnostics, numeric room IDs, ticket expiry, logout and stale reconnect callbacks. |
| BepInEx configuration test | Passed against the staged 6.0.0-pre.2 loader. |
| Standalone probe and Mono.Cecil checker | Built in Release with zero warnings and zero errors. The PowerShell `-ProbeOnly` path built the standalone DLL. |
| KromaRec 0.9.0 musl and glibc with patched Luxon 1.5 | Release hashes matched. Encrypted loopback NameServer checks passed on both builds. Each accepted eleven Authenticate requests and one AuthenticateOnce. Synthetic peers passed Master/Game admission, two-way event relay, leave/disconnect/rejoin, privacy and SignalR checks. |
| Hosted Windows workflow | Parsed YAML and PowerShell locally. Executed the missing-reference branch and verified its summary. The GitHub-hosted job and private reference provision were not run. |
| Main patch build | Stopped at the project check for missing locally generated Rec Room 20230414 interop. The two Windows compiler diagnostics, the full patch DLL and Harmony binding still require a local build. |
| Native account, rooms and multiplayer | Pending. Auth member roles, the encryption and handoff paths in the native game, room rendering, movement, invitations and reconnect await native testing. |

This preview is based on Phase 2 commit `f09e62d`. That snapshot has an older `NativeDiagnostics.cs` and predates the six local reference removals. Apply source changes selectively to preserve your working Windows project and merge any conflicts with the local diagnostics. See [PHOTON_DIAGNOSTICS.md](PHOTON_DIAGNOSTICS.md) for the evidence needed to complete the auth hook.

## Open native integration issues

1. The exact Windows 20230414 interop must be inspected and used to build the main DLL. The offline verifier and the Windows-only standalone probe collect the evidence without transferring game binaries.
2. The native roles of the auth properties and custom-auth enum remain unconfirmed. Correlate the proxy fields with native writes for Photon keys 216, 217, 221 and 225, then verify the real encrypted NameServer send and server-issued Master/Game token path. The preview keeps the guard and does not override Photon settings while that evidence is missing.
3. Capture native HTTPS discovery, sign-in, device-ID behavior with game checks active, Orientation/dorm startup and Game admission separately. Capture notification and scene loading failures at the first affected stage.
4. Run the real two-account acceptance including rendered avatars, movement, rotation, gestures, interaction, invitations and leave/rejoin. Use [NATIVE-TESTING.md](NATIVE-TESTING.md).
5. Test 0.10.0 with the same contract and two-account checks when available.

## Historical Phase 2 test results

| Check | Result |
| --- | --- |
| .NET 6.0.428 protocol harness | Passed. Phase 1 credential lifetime, stale callbacks, canonical subject, and parameter 216 still pass. Added exact HTTPS discovery/MatchUrl validation, fixed route labels, numeric-only results, and scene failure classification. The follow-up also checks reconnect diagnostic labels and result-field selection. |
| BepInEx configuration parser | Passed with the genuine pinned 6.0.0-pre.2 BepInEx.Core and Phase 2 example. |
| Mono.Cecil verifier | Compiled against the pin without warnings. Actual signature inspection awaits the generated game interop. |
| 0.8.0 trusted HTTPS preflight | Passed with the real musl backend behind a temporary TLS proxy. Verified discovery, version, initial config, game configs, emoji config, and CDN tips. Untrusted CA and wrong MatchUrl each failed as expected. This exercises the Python verifier. BestHTTP and the Windows trust store remain untested. |
| 0.8.0 musl and glibc smoke and rooms suites | Passed both, including local accounts, persistence, presence and SignalR reconnect, room allocation, private admission, two-way event relay and rejoin. The musl run also loaded a database produced using the 0.7.0 baseline. |
| Encrypted Photon NameServer probe | Passed against both 0.8.0 builds with the unchanged patched Luxon. Each run accepted seven encrypted Authenticate (230) responses and one encrypted AuthenticateOnce (231), rejected invalid credentials, and exercised native token handoffs and two-peer room/event/rejoin behavior. The probe encrypts NameServer auth; downstream operations are the release's existing loopback probes. |
| Full native build | Attempted. Stopped at the project's explicit missing Rec Room 20230414 interop check. No candidate DLL produced. |
| Windows native multiplayer | Awaiting the genuine depot, generated interop, and two independent Windows client sessions. No plugin load, rendered room, avatar replication, or native reconnect was observed. |

The SHA-256 of the Luxon executable tested is `9a1d13bd5c26141d46cb2a589536c0748be1c6d49004cc61d33a52e795a08fe7`.

See [NATIVE_ACCEPTANCE.md](NATIVE_ACCEPTANCE.md) for reproduction steps and evidence for each milestone.

The follow-up reran the C# protocol and configuration checks, the Mono.Cecil build, and both encrypted wire probes. The native build reached the same missing-interop dependency check.

## Windows build preparation

The .NET SDK 6.0.428 protocol checks and pinned BepInEx configuration tests passed again. The Mono.Cecil verifier compiled against the pinned loader. PowerShell parsed both scripts. The build preflight ran on Linux using synthetic managed metadata: `-VerifyOnly` passed with expected signatures, rejected a renamed Photon factory, and stopped with a specific message for a missing `PhotonRealtime.dll`. These fixtures test the verifier and error paths. They contain no game code or generated game assemblies.

The full plugin build still stopped before compilation because the genuine 20230414 interop was missing. No candidate DLL was produced. The next step is to run the verifier and build against Steam depot 471711, manifest 6426603215211043630, after generating interop with BepInEx 6.0.0-pre.2. Windows runtime acceptance remains pending.
