# Kerbal Health

This mod introduces many aspects of astronauts' health management to KSP. It makes the game more challenging but also more realistic, encourages smarter mission planning, and adds to the fun. It works well alongside most popular mods.

**Features**

- Every kerbal has Health Points (HP).
- Maximum HP increase with kerbals' level. A newbie has 100 HP while a 5-level kerbal has 150 (by default).
- Kerbals' HP change, including in background and in timewarp, based on a range of factors such as living space, presence of crewmates, gravity, and specific ship parts. E.g., a level 1 kerbal will typically survive 11 days in a 1-man pod before becoming exhausted and turning into a Tourist.
- Kerbals need time to recuperate at the KSC between flights to restore full health.
- If a kerbal's health goes under 20%, he/she is exhausted and becomes a tourist. They will go back to work when health grows back to 25%.
- If a kerbal's health falls to 0, he/she dies!
- Kerbals experience radiation, both cosmic and artificial, which permanently affects their maximum health.
- You can protect from radiation by using shielding and choosing safer mission profiles.
- Kerbals may fall sick, have health accidents or panic attacks: prepare for contingences!
- Health Monitor lets you check on all your kerbals in KSC and in flight.
- Health Report in Editor helps design comfy and healthy craft.
- A configurable low health alert will warn you when you are about to lose a kerbal.
- Compatibility patches support a range of parts mods (see below).
- All of these settings are easily changed or disabled in-game and with ModuleManager patches.

**Health factors**

The following factors may affect kerbal's health:
- Assigned (kerbal is on a mission): -0.5 HP/day
- Crowded (scaled effect): -3 x <Crew> / <Living Space> HP/day
- Loneliness (only 1 kerbal on the vessel, badass kerbals are immune): -1 HP/day
- Microgravity (orbital or suborbital flight or under 0.1 g conditions, e.g. Minmus): -0.5 HP/day
- EVA: -10 HP/day (don't abandon your kerbals outside for long!)
- Sickness (the kerbal is marked as Sick, cures after some time): -5 HP/day
- Connected (having a working CommNet to home): +0.5 HP/day
- Home (on Kerbin at altitude of under 18 km): +2 HP/day
- KSC (kerbal is recuperating in KSC, i.e. available): +5 HP/day

These values, like most others in the mod, are adjustable in Difficulty Settings.

Certain parts (Hitchhiker, crew cabin, and the Cupola) can reduce the effect of a health factor (Crowded in this case) allowing for much longer and healthier flights. It requires EC though. Hab rings (e.g. in MKS) can help overcome microgravity issues for long-term stations and interplanetary missions. All these features can be changed using ModuleManager patches.

**Health Recuperation and Health Decay**

Certain parts (such as the Cupola) provide Recuperation bonuses. If a kerbal receives, say, a 1% recuperation bonus, he/she will recover 1% of their lacking health (i.e. of the difference between their current HP and the maximum HP) every day. This change works in parallel with the normal health factors above.

*Example: A 5-star kerbal (maximum HP = 150) currently has 40 Health Points and is in a vessel that gives him 1% recuperation. The vessel has 10 units of living space and he has connection and he has a crewmate. Therefore he recovers (150 - 40) x 1% = 1.1 HP per day and loses also (0.5 + 3 x 2 / 10 + 0.5 - 0.5) = 1.15 HP per day. It means that the marginal change balances out the "normal" change and his health will stay around 40 HP (27%) until the situation changes.*

As you see, this mechanics may allow some kerbals to stay relatively healthy indefinitely. It may look cheaty, but the point is that: (1) there should be a way to design long-term missions without spamming crew space, (2) it requires a lot of heavy parts and therefore still difficult, (3) the balanced health level is usually far from 100% and may fall lower if circumstances change (e.g., new crew arrives and fills the station), (4) these bonuses require a lot of EC, (5) radiation still keeps mounting (see below).

Note that, since v1.1, Recuperation is not stacked and has crew cap. It means that one Cupola provides 1% Recup for 2 kerbals, 2 Cupolas give 1% for 4 kerbals (not 2%!), etc. If you have more kerbals than the crew cap, Recuperation will be split among them evenly (e.g. 4 kerbals with 1 Cupola will get 0.5% Recup).

**Radiation**

All kerbals on missions are affected by radiation, which slowly but permanently reduces their maximum HP. Radiation is measured in banana equivalent doses (about 1e-7 Sv), or simply bananas. 1e7 (10,000,000) bananas reduce max HP by 25%; 4e7 bananas kill a kerbal. Currently, there is no way to reduce the dose and, if it is added in the future, it's going to be really hard.

The amount of radiation a kerbal receives depends on many factors. Most importantly, it is determined by their location. Planetary magnetic fields and atmospheres reduce radiation substantially; being very close to a celestial body may screen half of the rays too. E.g., radiation level at Kerbin's surface is 2,000 times lower than in interplanetary space just outside Kerbin's SOI. Cosmic radiation is also greater closer to the Sun. Being on EVA takes away all the protection your ship provides and dramatically increases radiation level. Artificial radiation is created by certain part like atomic engines and nuclear reactors.

You can protect kerbals from radiation (both cosmic and artificial) by adding shielding to the vessel. It is provided by some parts, like structural panels, heat shields and mk3 cargo bays. These parts and most crew pods can be improved by adding Radiation Shielding to them in the Editor. You can never eliminate all radiation, but you can reduce it significantly.

You may use this [tool to calculate radiation effects](https://docs.google.com/spreadsheets/d/1uAGrzg9ACDa8Uhtq9Fa45t2zmLikVAC9YH13eZiFzHw/edit?usp=sharing). Copy it to your Google Drive or download as XLSX to edit.

**Random events**

Kerbals' organisms, like ours own, are not always predictable. Sometimes, very rarely, you may see random events that can impact your whole mission. Now you need to prepare for contingencies like real space programs do. You may completely disable or fine-tune the event system in settings.

- Sickness/Curing: a kerbal can become sick and start losing health quickly. This condition heals itself after some time, but you may have to evacuate the kerbal to KSC (or bar him/her from flights) before their health falls too low. On average, kerbals catch flu once every 200 days and cure after 20 days or so. Note that these diseases have incubation periods, so it may be wise to quarantine kerbals for several weeks before sending them on an interplanetary trip.

- Panic Attack: when your kerbal's is on a mission, he/she may epxerience a panic attack and become uncontrollable for up to 3 hours. This event's probability depends on the kerbal's health (the lower, the more likely) and courage (the lower, again, the more likely) of the kerbal, the maximum average frequency is once per 100 days.

- Accident: your kerbals loses a random percentage of their current health (20 to 50%). This event's chance depends on kerbal's stupidity, but generally happens very rarely (every 1,000 days on average with 50% stupidity). However, it can become an important factor for very long missions.


**Requirements**

- ModuleManager

**Supported mods**

Kerbal Health should work well alongside most other mods and will try to adapt to them with smart MM patches. Some have better, native support though:

- [B9 Aerospace](https://github.com/blowfishpro/B9-Aerospace)
- [Blizzy's Toolbar](http://forum.kerbalspaceprogram.com/index.php?/topic/55420-120-toolbar-1713-common-api-for-draggableresizable-buttons-toolbar/)
- [Bluedog Design Bureau](https://forum.kerbalspaceprogram.com/index.php?/topic/122020-131-bluedog-design-bureau-stockalike-saturn-apollo-and-more-v141-атлас-1jan2018)
- [Deadly Reentry Continued](https://forum.kerbalspaceprogram.com/index.php?/topic/50296-122-deadly-reentry-v760-june-9-2017-the-ariel-edition/)
- [DeepFreeze Continued](http://forum.kerbalspaceprogram.com/index.php?/topic/112328-11-deepfreeze-continued)
- [FASA](http://forum.kerbalspaceprogram.com/index.php?/topic/22888-105-fasa-544/)
- [Kerbal Atomics](http://forum.kerbalspaceprogram.com/index.php?/topic/130503-10511-kerbal-atomics-fancy-nuclear-engines-initial-11-test/)
- [KPBS](http://forum.kerbalspaceprogram.com/index.php?/topic/133606-130-kerbal-planetary-base-systems-v144-6-june-2017/)
- [KSP-AVC](http://ksp-avc.cybutek.net)
- [KSP Interstellar Extended](https://forum.kerbalspaceprogram.com/index.php?/topic/155255-12213-kspi-extended)
- [Spacetux Recycled Parts](https://forum.kerbalspaceprogram.com/index.php?/topic/164829-131-spacetux-industries-recycled-parts/) (Atomic Age, FTmN, FTmN-New, RSCapsuledyne)
- [SpaceY Heavy Lifters](https://forum.kerbalspaceprogram.com/index.php?/topic/90545-12213-spacey-heavy-lifter-parts-pack-v116-2017-01-30/)
- [USI Kolonization Systems (MKS/OKS)](https://github.com/BobPalmer/MKS)
- [USI-LS](https://github.com/BobPalmer/USI-LS)

If you would like to include native support for your (or your favorite) mod, let me know.

**Conflicts & Incompatibilities**

- Any mod (including USI-LS), which can temporarily make kerbals Tourists, can conflict with Kerbal Health if both mods change kerbals' status and then rutn it back. In some situations it may mean that your kerbals will remain Tourists indefinitely or become active too soon. Kerbal Health tries to fix some of these situations, but cannot prevent all of them.
- It is recommended to disable habitation mechanics of USI-LS' (and other mods') as these largely have the same goal as Kerbal Health.
- RemoteTech's connection state is not supported for the purpose of the Connected Factor status. This issue will be resolved once RemoteTech 2.0 is released. Meanwhile, you may disable both Connected and Assigned factors to keep balance.

**Future features**

- New mechanics: health traits, injuries, medical supplies, quarantine, and whatnot...
- API for collaboration with other mods

**Copyright and License**

This mod has been created by [Garwel](https://forum.kerbalspaceprogram.com/index.php?/profile/141813-garwel/) and is distributed under MIT license.
