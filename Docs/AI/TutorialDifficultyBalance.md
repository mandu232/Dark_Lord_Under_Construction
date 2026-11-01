# Tutorial and difficulty balance — 2026-11-01

The opening battle retains its four protected tutorial slimes until the corridor collapses. Collapse kills three through normal death handling and leaves one living Blood Slime. The recovery tutorial's first successful random summon is guaranteed to be a Blood Slime, giving the player two slimes before normal waves. Recruitment text explicitly names that summon. Later recruitment uses normal randomness.

The casualty-resolution flag persists in saves. Old collapsed recovery-stage saves migrate once; advanced tutorial saves retain their recruited army. Failed summons do not consume the guarantee.

## Wave progression

Default settings, waves 1–5:

| Difficulty | Enemy counts | HP per enemy |
| --- | --- | --- |
| Easy | 2, 2, 3, 5, 6 | 29, 35, 42, 49, 58 |
| Normal | 2, 3, 4, 6, 8 | 36, 44, 52, 61, 72 |
| Hard | 2, 4, 5, 7, 10 | 41, 51, 60, 70, 83 |
| Nightmare | 3, 4, 6, 8, 11 | 47, 57, 68, 79, 94 |

Normal preparation means are 180, 165, 150, 135 and 120 seconds, with ±8% variation. Hard uses 90% and Nightmare 80% of these times. Easy remains manual. Existing saved timers are preserved. Rewards are unchanged.

Chargers appear in wave 2, healers in wave 3 and shield units in wave 4. The final wave reserves most slots for warriors; Normal wave 5 has five warriors and one of each special role. Beyond wave 5 the count and HP continue growing.

## Validation

- Unity compilation succeeded.
- WaveProgressionTests: 5/5 through the standard runner.
- Actual Play Mode coroutine assertions: CorpseTrainingTutorialTests 11/11, StartingTutorialTests 10/10, AutomaticWaveTests 11/11 and InvaderRoleTests 14/14.
- The standard runner returned zero tests for these coroutine fixtures. Tools/RunTutorialProbe.cs and the test-only TutorialRegressionProbe invoke their existing setup, coroutine assertions and teardown in live Play Mode; 46/46 passed through this fallback. A zero-test result was not counted as a pass.
- Coverage includes three deaths/one survivor, multiple random seeds, first-summon UI text, save restoration and legacy migration, automatic scheduling and mixed roles. The normal first wave was defeated with base player stats and the two recovery slimes, using actual AI and player attack methods without stat buffs.
- Live preview confirmed one survivor at the Summon step and the label 혈 슬라임 소환.

This validates the tutorial and first normal battle, not a full campaign playthrough or player build. Subjective late-game balance still benefits from player feedback.
