# Troubleshooting

| Symptom | Check |
| --- | --- |
| Missing game DLL or wrong build in Setup | Restore manifest `6426603215211043630`. Setup checks the executable, native game DLL, UnityPlayer and original IL2CPP metadata against the Steam manifest. |
| Build says interop is missing | Start the exact game once with BepInEx 6 IL2CPP, then close it and rerun `build.ps1`. |
| Could not bind integration | Compare the verifier's output with the target table in [COMPATIBILITY.md](COMPATIBILITY.md). Check the running game's actual interop and remove any second RecFlare plugin. |
| Discovery lacks a response | Check DNS, Host routing and Windows trust for the proxy certificate. Test the HTTPS discovery URL independently, then reproduce in the game. If the game still rejects it, check for actual per-service certificate pinning. |
| KromaRec Photon authentication blocked by the preview | Run the [standalone diagnostic](PHOTON_DIAGNOSTICS.md) and collect its safe report. |
| No fresh ticket | Confirm local login, the exact `MatchUrl`, a 200 connection-info response, matching UUID/region and correct client time. Retry from the game or sign in again so it issues another request. |
| No encrypted channel | Check Luxon UDP connectivity and Photon handshake logs. Capture the first failing stage without exporting packet payloads, auth blobs or debug dumps. |
| NameServer works, Master or Game fails | Check the advertised UDP IPs/ports, native token handoff, ticket age and live KromaRec reservation. |
| Room entered but scene or images fail | Record room instance ID and the built-in scene key. Check asset routing and original game signature validation. Custom room saves are still pending. |

Enable `General.Debug` for diagnosis and inspect the latest `BepInEx/LogOutput.log` or `BepInEx/LogOutput.txt`. KromaRec hook diagnostics omit credentials and payloads. Check game, SDK, Luxon and proxy logs for sensitive data before sharing.
