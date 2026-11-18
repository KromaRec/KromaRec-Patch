# KromaRec Patch for Rec Room 20230414

BepInEx 6 IL2CPP and HarmonyX plugin for a user-owned Rec Room 20230414 Windows x64 installation. It adapts RecFlare patch commit [68f42eb](https://github.com/recflare/patch/tree/68f42eb6614e4fa788603bc0dd8124527c382e30) and the KromaRec 0.7.0 Photon ticket overlay.

The 0.3.1 development preview targets the existing KromaRec 0.10.1 HTTP and ticket contract with patched Luxon 1.5. It redirects discovery and observes the game's connection-info response for startup testing. KromaRec Photon authentication is gated while the roles of the obfuscated auth members are verified. Start with the [Photon diagnostic](PHOTON_DIAGNOSTICS.md).

- [First Windows build](NEXT_STEPS.md)
- [Build](BUILDING.md)
- [Install](INSTALLATION.md)
- [Configure](CONFIGURATION.md)
- [Troubleshoot](TROUBLESHOOTING.md)
- [Audit and compatibility](COMPATIBILITY.md)
- [Test results](TEST_RESULTS.md)
- [Changelog](CHANGELOG-0.3.1.md)
- [Windows and two-client acceptance](NATIVE-TESTING.md)
- [Phase 2 backend handoff](BACKEND_REQUIREMENTS.md)

The standalone diagnostic builds with .NET 6.0.428. The full Windows build, auth wiring and multiplayer acceptance are pending.
