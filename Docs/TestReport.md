# Test report: 2026-11-14

Editor 6000.6.4f1 · Input System 1.20.0 · uGUI 2.6.0 · Test Framework 1.8.0 · macOS (Darwin 25.5)

| Check | Result |
|---|---|
| Compile (batchmode project import) | Pass, 0 C# errors, 0 C# warnings |
| Catalogue validation (`kitchen_gate_08`, `kitchen_20`) | Pass: 21 boards solved; economy 1000 / 600 / 150 |
| EditMode tests | 29/29 passed |
| PlayMode boot smoke test | 1/1 passed (boot, Kitchen 1 on a new save, all screens, P1/P2/P3/P4/finale layouts) |

EditMode coverage by Section 27 ID: INPUT 01, ALLOC 01–05, DEFER 01–02, FINAL 01–04, REWARD 01, BADGE 01–02, ECON 01–02, SAVE 01–03, DATA 01–02, HINT 01, TRACE 01, plus new-save defaults and the gate-profile endpoint.

## Not run

- INPUT 02 and 03 (multitouch, backgrounding during a drag) as automated PlayMode pointer tests.
- UI 01/02, ACCESS 01, TEACH 01 as automated tests. The code paths exist but have not been checked by hand.
- SAVE 04 and schema 2/3 migrations: not implemented.
- No device tests: no Android or iOS hardware was used, there is no APK or iOS export, and no frame-time or thermal measurements.
- No player testing. Compiling and passing tests says nothing about whether the experience gate passes.

Note: the PlayMode smoke test resets the editor development save, both before and after it runs.
