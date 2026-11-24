# Photon metadata and native probe

For the first Windows report run the commands in NEXT_STEPS.md. The readiness script reconstructs fixed events and numeric HTTP status codes from the newest BepInEx log. It omits unknown messages and raw lines. A missing event means no matching log entry was observed. Metadata success covers managed shapes only. The probe and main plugin must be tested on separate launches.

Use the exact Rec Room PC 20230414 depot and BepInEx 6.0.0-pre.2 IL2CPP. Start the game once to generate interop and close it. Run from your existing KromaRec-Patch directory.

## Windows probe 1.3.0 readback, 2026-11-19

The runtime probe completed against PhotonRealtime SHA-256 `2E6A8754C61D6E14B3F00346B5AEB1E70A0C50D56AB474C05EA1DA59AD09453E`. The following observations apply to fresh synthetic authentication objects.

| Property | Corresponding member | Observation |
| --- | --- | --- |
| BAADEALBCAN | _JEHPFMLLKBE_k__BackingField | Both readers tracked both constructor arguments. Writes tracked in both directions. Candidate UserId. |
| EKJCBGGAFCG | _LGKMOEHCOOP_k__BackingField | Writes tracked in both directions. Neither reader tracked the constructor argument. |
| DHECLPBPMEI | _MPGGEFPDICH_k__BackingField | The getter tracked both writes to the backing member. Reverse direction was not tested. |
| MBDLFKMIGJE | DIDPLGKMDIO | Both readers tracked writes in both directions for None and Custom. |

The label probes remained inconclusive. The readback does not identify the source of parameter 216 or 221, or verify native serialization to 217 or 225. No KromaRec ticket was sent. Keep native auth guarded. Next inspect the declared methods and accessor visibility on LCBFGLBDFHG, then correlate a supported parameter-building operation or native serialization with the observed members. Never infer the ticket slot from setter visibility alone.

## Windows metadata failure on 2026-11-18

Both supplied runs reported the BestHTTP and Photon signatures present. The old verifier required a CLR event named sceneLoaded with a public static add accessor on UnityEngine.SceneManagement.SceneManager. Its absence alone set the aggregate to FAIL (exit 3). Application.logMessageReceived was also unavailable and already optional. The report does not identify whether generation dropped an event row, omitted the accessor or exposed a different member.

REQUIRED_FAILURE names each missing or incompatible mandatory member. OPTIONAL reports owner count, event rows, add methods, fields and properties with the exact name. A standalone accessor or delegate field does not authorize a replacement hook. The plugin attempts each optional subscription once during startup if used. A missing scene event removes the scene notification and per-scene telemetry retries. Startup telemetry opt-out and required BestHTTP and Photon guards still run. A missing Unity log callback removes the optional scene-error log observer. No scene transitions depend on either subscription.

The independent runtime statuses for authentication roles, encryption and handoff remain UNVERIFIED even when all required metadata matches.

## Offline report

~~~powershell
$game = 'C:\Games\KromaRec-20230414'
.\build.ps1 -GamePath $game -VerifyOnly -ReportPath .\photon-interop.txt
Get-Content -LiteralPath .\photon-interop.txt
~~~

The file contains the checker output only. Build output is displayed separately. A failed metadata check still writes its available report before returning KRBUILD_METADATA. Copy the report into the review conversation. It contains assembly fingerprints, declared type/member names, signatures and selected numeric constants. It omits runtime strings, account IDs, URLs and authentication values.

The checker identifies peers by their declared PhotonPeer base, lists plausible overload shapes and validates the exact previously reported candidate names. A renamed candidate is reported and rejected for automatic hook installation. It also reports auth constructors, fields, writable instance properties, generated native field references and enum values. PHOTON_CONSTANT means the value was found in managed metadata. A numeric value alone does not establish its native purpose.

HARMONY_TARGET describes the verified method signature. NATIVE_ROLES, ENCRYPTION and HANDOFF remain UNVERIFIED. Managed proxy method bodies cannot establish the original native serialization.

Exit 0 means the required managed signatures matched. Exit 2 means a missing input directory or invalid invocation. Exit 3 means invalid or unreadable metadata. These outcomes apply to the checker.

## Independent native probe

Move old KromaRec and RecFlare plugin DLLs outside BepInEx/plugins to isolate the probe. Compile it separately while the game is closed.

~~~powershell
$ErrorActionPreference = 'Stop'
$game = 'C:\Games\KromaRec-20230414'
.\build.ps1 -GamePath $game -ProbeOnly
$dll = '.\tools\PhotonAuthProbe\bin\Release\net6.0\KromaRec.PhotonAuthProbe.dll'
$plugins = Join-Path $game 'BepInEx\plugins'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
Copy-Item -LiteralPath $dll -Destination $plugins -Force
Start-Process -FilePath (Join-Path $game 'RecRoom.exe') -WorkingDirectory $game
~~~

The probe creates fresh auth objects and writes synthetic values to each declared public writable string property, one object per test. It also checks writable enum properties for a named None-to-Custom transition. Close the game after the loader runs and before signing in.

~~~powershell
$log = Get-ChildItem -LiteralPath (Join-Path $game 'BepInEx') -File |
    Where-Object { $_.Name -in 'LogOutput.log', 'LogOutput.txt' } |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($log) {
    Select-String -LiteralPath $log.FullName -SimpleMatch '[KR-PHOTON-PROBE]' |
        ForEach-Object { $_.Line }
}
~~~

Send BEGIN through END together with the offline report. Keep other game logs private. If the plugin fails before BEGIN, send the first relevant compiler or loader error with private paths redacted.

~~~powershell
Remove-Item -LiteralPath (Join-Path $game 'BepInEx\plugins\KromaRec.PhotonAuthProbe.dll')
~~~

An INCONCLUSIVE or AMBIGUOUS result requires native disassembly or another direct serialization observation. Correlate writes for Photon parameter keys 216, 217, 221 and 225 with the auth members, then inspect the encrypted NameServer request and native Master/Game token path. Keep credentials and packet payloads out of shared reports. The guarded patch requires that evidence before ticket injection and settings redirection can be implemented.
