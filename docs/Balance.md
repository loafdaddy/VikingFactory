# Balance

These numbers are the master prompt's proposed defaults. They are not measured Valheim rates and they have not been playtested. `BalanceDefaults` in the core locks the subset used by the first tests.

## Progress

The cross-off list is `docs/ImplementationStatus.md`. The public summary is `ROADMAP.md`.

- [x] 16 RPM, hand crank 8 DU, water wheel 48 DU, feeder 6 DU, belt 1 DU per segment
- [x] Core tests: one feeder runs, two stall, released crank, clutch, unverified water wheel
- [ ] In-game rates, stamina, spacing, immersion, or any later source and consumer

| Source or consumer | Proposed value | In the tested core |
|---|---|---|
| Milestone speed | 16 RPM | Yes. Ratios are not implemented. |
| Hand crank | 8 DU, only while held | Yes |
| Water wheel | 48 DU | Capacity constant. The core supplies 0 unless a placement probe allows the site. The plugin allows every placed wheel, because the piece is marked water-only. Spacing and immersion are not checked. |
| Bronze feeder | 6 DU | Yes |
| Timber belt, per 2 m | 1 DU | Yes, as a consumer load |
| One crank, one feeder | Runs | Tested |
| One crank, two feeders | Stalls, 12 DU reserved | Tested |
| Water wheel, 3 feeders, 4 belt segments | 22 DU reserved of 48 | Tested only with a fake placement probe |

Steam, sail, eitr motor, quarry, coppice, and recipe-mill times from the master prompt are not implemented. Do not show them as in-game rates.
