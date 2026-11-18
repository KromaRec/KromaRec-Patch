# KromaRec-Patch 0.3.1 development preview

- Differentiate missing game, loader and interop inputs, verifier compilation, incompatible metadata, main compiler failure and successful output using KRBUILD messages.
- Compile the verifier before executing it. Save its sanitized metadata report with build.ps1 -ReportPath.
- Condition the existing generated interop reference list on local file existence. Required common inputs still fail preflight and unresolved source types still fail compilation.
- Enumerate peer and authentication overload candidates, parameter types, auth fields and properties, enum constants, native wrapper field references and relevant numeric Photon constants. Report metadata matches separately from native member roles and Harmony dispatch.
- Add synthetic verifier tests for complete managed signatures, renamed peers, invalid signatures, static accessors and corrupt images. Add PowerShell syntax and build failure classification tests to hosted Windows CI.
- Probe every declared public writable string property on a fresh synthetic auth object. Keep the three previously reported names in the report even if absent.
- Add optional encrypted-channel state at the auth guard and a single warning for blocked KromaRec authentication. Debug logging remains disabled by default.
- Audit KromaRec 0.10.0 discovery, party fields, ticket format, private admission, notifications and logout. Preserve original request payloads and repeated AdditionalPlayerIds.
- Keep the 0.3.0 removal of EAC, device ID and CheatManager overrides. Native authentication, settings redirection, encryption and Master/Game handoff remain blocked on genuine 20230414 evidence.
