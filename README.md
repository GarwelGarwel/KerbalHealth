# KerbalHealth

**Design goals**

- Increase KSP's realism, challenge, and fun by introducing health and related concepts
- Encourage the use of more sophisticated vessels, especially for long-haul flights and long-term habitation (no more 10-year mission to Eeloo in a Mk1 pod!)
- Add to individuality of kerbals
- Keep the mod as flexible as possible, including configuration options and integration with other mods
- Avoid micromanagement as much as possible

The mod is currently in beta. It has all main systems in place, but will continue to change and to grow. It requires testing and balancing. Future changes can be backward-incompatible, so be careful using it in your main playthrough. This is my first attempt to mod KSP and I'm not even a professional programmer, so you've been warned.

I will appreciate your bug reports (include output_log.txt) and feature suggestions.

**Features as of 0.4.1 (beta)**

- Every kerbal, including Tourists, has Health Points (HP).
- HP increase with kerbals' level. A 5-level kerbal is 50% "healthier" than a newbie.
- Kerbals' HP are updated (currently once a second), including in background and in timewarp, based on a range of factors such as living space, presence of crewmates, and specific ship parts. E.g., a level 1 kerbal will survive 11 days in a 1-man pod until becoming exhausted.
- If a kerbal's health goes under 20%, he/she is exhausted and becomes a tourist. They will go back to work when health rises to 25%.
- If a kerbal's health falls to 0, he/she dies!
- In KSC, kerbals gradually recover their health.
- Health Monitor lets you check on all your kerbals in KSC and in flight.
- Health Report in Editor helps design comfy and healthy craft.
- All of these settings are easily changed in-game or with ModuleManager patches.

**Health factors**

- Assigned (kerbal is on a mission): -1 HP/day
- Living Space (scaled in proportion to crew/capacity ratio): -7 HP/day for a full vessel
- Not Alone (more than 1 kerbal on the vessel): +1 HP/day
- KSC (kerbal is recuperating in KSC, i.e. available): +5 HP/day

**Marginal health change**

Certain parts (Hitchhiker, crew cabins, and the Cupola) provide "marginal health change" bonuses. If a kerbal receives, say, a 3% marginal health change bonus, he/she will recover 3% of their lacking health (i.e. of the difference between their current HP and the maximum HP) every day. This change works in parallel with the normal health factors above.

*Example: A 5-star kerbal (maximum HP = 150) currently has 50 Health Points and is in a vessel that gives him a 5% marginal change bonus. The vessel is full and he has a crewmate. It means that he recovers (150 - 50) x 5% = 5 HP per day and loses (1 + 7 - 1) = 5 HP per day. It means that the marginal change balances out the "normal" change and his health will stay around 50 HP (33%) until the situation changes.*

As you see, this mechanics may allow some kerbals to stay relatively healthy for an unlimited time. It may look cheaty, but the point is that: (1) there should be a way to design long-term missions without spamming crew space, (2) it requires a lot of heavy parts and therefore still difficult, (3) the balanced health level is usually far from 100% and may fall lower if circumstances change (e.g., new crew arrives and fills the station), (4) these boni require a lot of EC.

These stock parts give marginal bonuses:
- Hitchhiker: +3%, 1 EC/kerbal (part crew only)
- Mk1 Crew Cabin: +1%, 0.6 EC/kerbal (part crew only)
- Mk2 Crew Cabin: +2%, 1 EC/kerbal (part crew only)
- Mk3 Passenger Module: +2%, 0.75 EC/kerbal (part crew only)
- PPD-12 Cupola Module: +1%, 3 EC total (affects entire vessel)

**Next objectives**

- Add more factors influencing health (e.g. SOI, Kerbin low altitude, zero g, high g, EVA, loneliness, etc.) - *PARTIALLY DONE*

**Future features**

- Add integration with major mods (parts packs, life support, etc.)
- Add more health conditions
- Add random health events
- Add new mechanics: infectious diseases, medics, medical supplies, quarantine, etc.
- Add API for collaboration with other mods

**Requirements**

- ModuleManager
