# KerbalHealth

**Design goals**

- Increase KSP's realism, challenge, and fun by introducing health and related concepts
- Encourage the use of more sophisticated vessels, especially for long-haul flights and long-term habitation (no more 10-year mission to Eeloo in a Mk1 pod!)
- Add to individuality of kerbals
- Keep the mod as flexible as possible, including configuration options and integration with other mods
- Avoid micromanagement as much as possible

The mod is currently in beta. It has most main systems in place, but will continue to change and to grow. It requires testing and balancing. Future changes can be backward-incompatible, so be careful using it in your main playthrough. This is my first attempt to mod KSP and I'm not even a professional programmer, so you've been warned.

I will appreciate your bug reports (include output_log.txt) and feature suggestions.

**Features as of 0.4.4 (beta)**

- Every kerbal, including Tourists, has Health Points (HP).
- Maximum HP increase with kerbals' level. A 5-level kerbal is 50% "healthier" than a newbie.
- Kerbals' HP are updated (currently once a second), including in background and in timewarp, based on a range of factors such as living space, presence of crewmates, gravity, and specific ship parts. E.g., a level 1 kerbal will typically survive 11 days in a 1-man pod before becoming exhausted.
- Kerbals need time to recuperate at the KSC between flights to restore full health.
- If a kerbal's health goes under 20%, he/she is exhausted and becomes a tourist. They will go back to work when health rises to 25%.
- If a kerbal's health falls to 0, he/she dies!
- Health Monitor lets you check on all your kerbals in KSC and in flight.
- Health Report in Editor helps design comfy and healthy craft.
- Compatibility patches support MKS and USI-LS patches.
- All of these settings are easily changed in-game or with ModuleManager patches.

**Health factors**

- Assigned (kerbal is on a mission): -0.5 HP/day
- Overpopulation (scaled in proportion to crew/capacity ratio): -6 HP/day for a full vessel
- Loneliness (only 1 kerbal on the vessel, badass kerbals immune): -1 HP/day
- Microgravity (orbital or suborbital flight or under 0.1 g conditions, e.g. Minmus): -0.5 HP/day
- EVA: -50 HP/day (don't abandon your kerbals outside for long!)
- Connected (having a working CommNet to home): +0.5 HP/day
- Home (at Kerbin at altitude of under 25 km): +1 HP/day
- KSC (kerbal is recuperating in KSC, i.e. available): +5 HP/day

Certain parts (Hitchhiker, crew cabin, and the Cupola) can reduce the effect of a health factor (Overpopulation in this case) allowing for much longer and healthier flights. It requires EC though. Hab rings (e.g. in MKS) can help overcome microgravity issues for long-term stations and interplanetary missions. You can patch any part to change (reduce or increase) any factor.

**Marginal health change**

The Cupola (as well as some advanced mod parts) provides so-called marginal health change bonuses. If a kerbal receives, say, a 3% marginal health change bonus, he/she will recover 3% of their lacking health (i.e. of the difference between their current HP and the maximum HP) every day. This change works in parallel with the normal health factors above.

*Example: A 5-star kerbal (maximum HP = 150) currently has 50 Health Points and is in a vessel that gives him a 5% marginal change bonus. The vessel is full and he has a crewmate. It means that he recovers (150 - 50) x 5% = 5 HP per day and loses (1 + 7 - 1) = 5 HP per day. It means that the marginal change balances out the "normal" change and his health will stay around 50 HP (33%) until the situation changes.*

As you see, this mechanics may allow some kerbals to stay relatively healthy for an unlimited time. It may look cheaty, but the point is that: (1) there should be a way to design long-term missions without spamming crew space, (2) it requires a lot of heavy parts and therefore still difficult, (3) the balanced health level is usually far from 100% and may fall lower if circumstances change (e.g., new crew arrives and fills the station), (4) these bonuses require a lot of EC.

**Supported mods**

- [KSP-AVC](http://ksp-avc.cybutek.net)
- [MKS](https://github.com/BobPalmer/MKS)
- [USI-LS](https://github.com/BobPalmer/USI-LS)

**Next objectives**

- Add integration with major mods (parts packs, life support, etc.)
- Add more health conditions
- Add random health events

**Future features**

- Add new mechanics: infectious diseases, medics, medical supplies, quarantine, etc.
- Add API for collaboration with other mods

**Requirements**

- ModuleManager
