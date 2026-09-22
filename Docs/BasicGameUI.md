# Basic game UI

`SampleScene` contains a screen-space uGUI HUD with TextMeshPro labels.

- Top-right: Gold, Souls, and Dark Crystal balances.
- Bottom-left: player health.
- Bottom-right: worker/guard counts and control hints.
- `CurrencyWallet` owns balances and exposes `GetAmount`, `CanAfford`, `TrySpend`, and `Add`.
- Starting prototype balances are 250 Gold, 40 Souls, and 5 Dark Crystals.

Use **Tools > Dark Lord > Setup Basic Game UI** to rebuild the HUD and reconnect it to the Player. The command is idempotent and saves the active scene.
