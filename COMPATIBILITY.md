# Compatibility

## KromaRec 0.10.1 and its bundled Luxon 1.5

| Contract | Source comparison and remaining differences |
| --- | --- |
| Discovery and OAuth | Backend `compat/service-hosts.json` and `src/bootstrap.rs` supply Auth and Matchmaking. The patch rewrites the stock discovery hostname and recognizes OAuth only at the discovered HTTPS Auth origin, path `/connect/token`. A missing or mismatched MatchUrl prevents service-map acceptance. A discovery network or non-200 failure leaves the previous successful trace map in memory. Routing of other game transports and actual certificate trust remain native checks. |
| Connection-info | `src/sessions.rs` emits the same app, region and signed ticket with a 900-second lifetime. PhotonTicketCache checks app, region, purpose, subject and expiry with a five-second margin. Signature and live generation validation belong to the backend gate. The cache has no native consumer in this revision. |
| NameServer | The wire fixture supplies string parameter 216 as `token=<ticket>` and 217 as Custom. The patch has the matching managed representation but does not set these fields, redirect Photon settings or initiate authentication. Its guard identifies the configured app ID or exact NameServer address. No evidence establishes that the native game uses either identifier on the current startup path. |
| Master and Game | Luxon returns the handoff token in 221. The wire harness waits for ENet disconnect acknowledgement and forwards the server token. Game admission requires a current reservation. Ticket expiry limits new admissions and handoffs. A continuously admitted actor uses its live reservation lease. The patch has no native handoff binding or proof of downstream encryption. |
| Reconnect, logout and password changes | The client invalidates the cache when it observes OAuth, connection-info and match session changes. Orientation or stale-lock logout can leave the backend session active while the patch clears its ticket. Failed requests can also discard a usable local ticket. Another successful connection-info is needed. Password changes on another client cannot synchronously clear local memory. Server generation checks, eviction and fresh login/admission remain authoritative. |
| Rooms and invitations | StartupTrace reads numeric roomId and roomInstanceId on successful match responses. SendRequestPatch retains the original body and repeated AdditionalPlayerIds. The backend validates party members and private-room admission. The patch does not construct invitations, process their delivery or implement replication. |
| SignalR | The patch labels BestHTTP negotiation and hub requests. Backend `src/notify.rs` closes active sockets on bearer expiry or session invalidation. Normal logout closes existing sockets but an otherwise valid HTTP bearer can negotiate a new one. Photon-only bans do not revoke SignalR. There is no client hook for WebSocket close, token renewal or resubscription. Native recovery and the order of obtaining new credentials remain unverified. |

The bundled binary hashes were checked against the 0.10.1 SHA256SUMS. The backend wire tests used this exact Luxon. Results are in TEST_RESULTS.md. Sections below describe earlier revisions.

## 0.3.1 contract audit against KromaRec 0.10.0

The 0.10.0 source retains the HTTPS discovery map, Auth origin, connection-info fields, Realtime audience and region. Tickets last 900 seconds and carry access and Photon generation claims. The client reads the existing lifetime and identity fields while the backend verifies signatures and live admission.

The request hook preserves the original body and query, including repeated AdditionalPlayerIds. The backend accepts up to 16 distinct valid party invitees for room matchmaking, emits game-invite notifications and still requires invitees to join through RecNet. Private instance policy and reservations remain server decisions. Fixed HTTP labels observe matchmaking, invite and SignalR negotiation requests where they traverse BestHTTP.

Native SignalR WebSocket dispatch, request encoding and room synchronization remain unverified. The synthetic backend suite exercises its own implementation of these operations. The client conservatively invalidates its cached ticket on all observed login/logout operations, including Orientation logout. The 0.10.0 backend retains its Orientation logout exception. Native ordering must show whether the game obtains a new connection-info before attempting another auth. The 0.9 parser contract remains supported, while party semantics require the 0.10 backend.

## 0.3.0 development preview

The target remains Rec Room PC 20230414 with KromaRec 0.9.0 and patched Luxon 1.5. The 0.9 discovery, connection-info and matchmaking DTOs retain the fields used by the preview. The parser tolerates additive fields. Run the same contract and native tests against 0.10.0 when that implementation is available.

The authentication guards recognize both methods reported by the Windows interop dump, and the settings factory override is deferred. See [PHOTON_DIAGNOSTICS.md](PHOTON_DIAGNOSTICS.md). The preview removes EAC challenge and readiness replacement, CheatManager deactivation, and DUID mismatch suppression. Login and room entry with game security active remain Windows acceptance checks. If a route is missing, record the sanitized request method, route, HTTP status and expected reply in the backend handoff.

## Historical Phase 1 audit and Phase 2 compatibility

| Source | Observed history and build wiring | Phase 1 treatment |
| --- | --- | --- |
| [RecFlare patch 68f42eb](https://github.com/recflare/patch/tree/68f42eb6614e4fa788603bc0dd8124527c382e30) | `main` points at the audited commit. `unstable` is separate. `8ddda00` adds 20230414 support. The original project used .NET 6, floating BepInEx 6 bleeding-edge packages, generated Unity/Rec Room interop, Harmony, and a post-build copy into the game. | Keeps the source structure and interop references, uses the same pinned runtime as the installer, and produces a standalone plugin artifact. |
| [RecFlare client beff81c](https://github.com/recflare/client/tree/beff81ced64ece1134f3ab84bb45649b8a14fa06) | The default branch is `2026`; branches `2025` and `master` exist. `0ee5735` switched to 20230414 and removed replacement IL2CPP metadata. `6fead24` and `4c7a243` temporarily used a different manifest in the batch file; `beff81c` restored 6426603215211043630. It ships Inno Setup and batch paths with Steam QR/username, an external game mirror, Steam app-ID override, BepInEx pre.2 and a RecFlare DLL/config. | KromaRec uses the Inno workflow and launch modes, with Steam acquisition separated from setup for reproducibility. It excludes the game mirror, Steam app-ID override, metadata replacement, RecFlare binaries and the 2025/2026 branches. |
| KromaRec 0.7.0 | Ships a source overlay for observing connection-info and adding custom Photon auth. Startup and room protocol tests use local password login and the patched Luxon 1.5 build. | Preserves the existing HTTP and Photon contract. [Phase 2 details](BACKEND_REQUIREMENTS.md). |

| Target | Static evidence and changes | Verification pending on 20230414 |
| --- | --- | --- |
| `BestHTTP.HTTPManager.SendRequestImpl(HTTPRequest)` | Chokepoint documented by upstream analytics patch. Redirects only `ns.rec.net`, preserves path/query, observes exact HTTPS match origin. Adds per-request cache/redirect controls and a callback wrapper. | Generated property and delegate signatures, callback order, whether other transport paths need redirect handling, TLS with the trusted proxy. |
| `HPEENKELKDJ.MGKINLFMJLB()` | Obfuscated instance settings factory reported by pinned RecFlare source for this manifest. Configures UDP without protocol fallback, region, custom host/port and the Realtime UUID. | The factory in current generated proxies and the effective UDP NameServer endpoint, including the exact `ServerAddress` string. |
| `LoadBalancingPeer.OpAuthenticate` and `OpAuthenticateOnce` | Signatures come from the 0.7.0 overlay and public [Photon Realtime API](https://doc-api.photonengine.com/en/pun/current/class_photon_1_1_realtime_1_1_load_balancing_peer.html). The plugin reports both bindings and requires at least one. | Which operation the game uses, Harmony argument replacement across IL2CPP, payload encryption and serialization to parameter 216, application version consistency, reconnect and native handoff. |
| `RecRoom.AntiCheat.EACManager`, `CheatManager` | Retains the inherited private-server readiness/challenge stub, scene management, and DUID mismatch workaround. Removes DUID corruption, probe and response override tools. | Upstream obfuscated targets and exact out-parameter binding against this interop, whether login can proceed without applying additional account changes. |
| Telemetry, TLS and RSA | Keeps the managed Amplitude/Backtrace filters and Unity opt-out; upstream observed native Unity uploads continuing. Removes the global TLS pin-check and RSA verification overrides. | Native telemetry, whether a valid HTTPS certificate is sufficient for BestHTTP, and current image signature behavior. |
| HTTP/OAuth and credentials | The patch observes connection-info through the original game request and callback, with no additional copy of the login password or bearer handling. Logs omit raw URLs, arbitrary paths, bodies, tokens and account IDs. | Check the game's own logging and callback order; check client recovery on expired tickets, 401/503, account switch and outages. |

The game assemblies were unavailable here. [BUILDING.md](BUILDING.md) includes the interop verifier to check the critical signatures on the user's actual installation.

## Phase 2 against KromaRec 0.8.0

The shipped 0.8.0 artifacts retain the 0.7.0 ticket DTO, Realtime JWT audience/purpose, 15-minute expiry, and native Photon handoff tokens. The bundled Luxon binary matched the 0.7.0 binary byte for byte. The optional diagnostic hooks bind documented managed Photon callbacks by signature and log fixed stage/operation names and numeric status codes. Missing diagnostics leave the primary hooks installed. Their actual IL2CPP dispatch and Unity's LogCallback binding require the Windows run.

RecNet debug tracing observes the existing game requests and parses the discovery response only for classification and reporting. Startup warnings distinguish mismatched MatchUrl from invalid service maps. A failed preflight does not alter the game's DNS, trust policy, or certificate validation. The 0.8 synthetic HTTPS run verifies the release-side proxy and trust behavior; stock BestHTTP certificate handling still needs a native run.

Phase 1 keeps ownership of the game session and ticket lifecycle. OAuth, connection-info, match login/exclusive-login and logout invalidate prior cache generations. The Photon hook checks the game's UserId against the ticket subject when supplied. Test logout/account switching, OAuth refresh, and login/connection-info order on Windows. Reconnect still follows the game's native path. A trace is needed to determine whether any additional client retry logic is required.

Device-ID migration, platform-linked login, and Voice/Chat remain outside the verified scope. Phase 1's private-server EAC/DUID accommodations are still required candidates for native testing. Original TLS and global RSA checks remain enabled.
