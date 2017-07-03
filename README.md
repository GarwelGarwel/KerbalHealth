# Kerbal Health

**Design goals**

- Increase KSP's realism, challenge, and fun by introducing health and related concepts
- Encourage the use of more sophisticated vessels, especially for long-haul flights and long-term habitation (no more 10-year mission to Eeloo in a Mk1 pod!)
- Add to individuality of kerbals
- Keep the mod as flexible as possible, including configuration options and integration with other mods
- Avoid micromanagement as much as possible

The mod looks stable enough for me to use in my own main playthrough, but a beta is still a beta. Some things may be poorly balanced; some changes may be backward-incompatible; some bugs are certainly out there. So you've been warned.

I will appreciate your bug reports (include output_log.txt) and feature suggestions.

**Features**

- Every kerbal, including Tourists, has Health Points (HP).
- Maximum HP increase with kerbals' level. A 5-level kerbal is 50% "healthier" than a newbie.
- Kerbals' HP are updated (currently once a second), including in background and in timewarp, based on a range of factors such as living space, presence of crewmates, gravity, and specific ship parts. E.g., a level 1 kerbal will typically survive 11 days in a 1-man pod before becoming exhausted.
- Kerbals need time to recuperate at the KSC between flights to restore full health.
- If a kerbal's health goes under 20%, he/she is exhausted and becomes a tourist. They will go back to work when health rises to 25%.
- If a kerbal's health falls to 0, he/she dies!
- Kerbals can unexpectedly fall sick, have health accidents or panic attacks: prepare for contingences!
- Health Monitor lets you check on all your kerbals in KSC and in flight.
- Health Report in Editor helps design comfy and healthy craft.
- Compatibility patches support MKS and USI-LS mods.
- All of these settings are easily changed or disabled in-game and with ModuleManager patches.

**Health factors**

- Assigned (kerbal is on a mission): -0.5 HP/day
- Crowded (effect scaled in proportion to crew/capacity ratio): -5 HP/day for a full vessel
- Loneliness (only 1 kerbal on the vessel, badass kerbals immune): -1 HP/day
- Microgravity (orbital or suborbital flight or under 0.1 g conditions, e.g. Minmus): -0.5 HP/day
- EVA: -30 HP/day (don't abandon your kerbals outside for long!)
- Sickness (the kerbal is marked as Sick, cures after some time): -5 HP/day
- Connected (having a working CommNet to home): +0.5 HP/day
- Home (at Kerbin at altitude of under 18 km): +2 HP/day
- KSC (kerbal is recuperating in KSC, i.e. available): +5 HP/day

These values, like most others in the mod, are adjustable in Difficulty Settings.

Certain parts (Hitchhiker, crew cabin, and the Cupola) can reduce the effect of a health factor (Overpopulation in this case) allowing for much longer and healthier flights. It requires EC though. Hab rings (e.g. in MKS) can help overcome microgravity issues for long-term stations and interplanetary missions. You can patch any part to change (reduce or increase) any factor.

**Marginal health change**

The Cupola (as well as some advanced mod parts) provides so-called marginal health change bonuses. If a kerbal receives, say, a 3% marginal health change bonus, he/she will recover 3% of their lacking health (i.e. of the difference between their current HP and the maximum HP) every day. This change works in parallel with the normal health factors above.

*Example: A 5-star kerbal (maximum HP = 150) currently has 50 Health Points and is in a vessel that gives him a 5% marginal change bonus. The vessel is full and he has a crewmate. It means that he recovers (150 - 50) x 5% = 5 HP per day and loses (1 + 7 - 1) = 5 HP per day. It means that the marginal change balances out the "normal" change and his health will stay around 50 HP (33%) until the situation changes.*

As you see, this mechanics may allow some kerbals to stay relatively healthy for an unlimited time. It may look cheaty, but the point is that: (1) there should be a way to design long-term missions without spamming crew space, (2) it requires a lot of heavy parts and therefore still difficult, (3) the balanced health level is usually far from 100% and may fall lower if circumstances change (e.g., new crew arrives and fills the station), (4) these bonuses require a lot of EC.

**Random events**

Kerbals' organisms, like ours own, are not always predictable. Sometimes, very rarely, you may see random events that can impact your whole mission. Now you need to prepare for contingencies like real space programs do. You may completely disable or fine-tune the event system in settings.

- Sickness/Curing: a kerbal can become sick and start losing health quickly. This condition heals itself after some time, but you may have to evacuate the kerbal to KSC (or bar him/her from flights) before their health falls too low. On average, kerbals catch flu once every 100 days and cure after 10 days or so.

- Panic Attack: when your kerbal's is on a mission, he/she may epxerience a panic attack and become uncontrollable for up to 3 hours. This event's probability depends on the kerbal's health (the lower, the more likely) and courage (the lower, again, the more likely) of the kerbal, the maximum average frequency is once per 100 days.

- Accident: your kerbals loses a random percentage of their current health (20 to 50%). It happens very rarely (every 1,000 days on average), but becomes an important factor for very long missions.


**Requirements**

- ModuleManager

**Supported mods**

- [KSP-AVC](http://ksp-avc.cybutek.net)
- [Blizzy's Toolbar](http://forum.kerbalspaceprogram.com/index.php?/topic/55420-120-toolbar-1713-common-api-for-draggableresizable-buttons-toolbar/)
- [MKS](https://github.com/BobPalmer/MKS)
- [USI-LS](https://github.com/BobPalmer/USI-LS)
- [KPBS](http://forum.kerbalspaceprogram.com/index.php?/topic/133606-130-kerbal-planetary-base-systems-v144-6-june-2017/)

**Conflicts & Incompatibilities**

- Any mod (including USI-LS), which can temporarily make kerbals Tourists, can conflict with Kerbal Health if both mods change kerbals' status and then rutn it back. In some situations it may mean that your kerbals will remain Tourists indefinitely or become active too soon. Kerbal Health tries to fix some of these situations, but cannot prevent all of them.
- It is recommended to disable habitation mechanics of USI-LS' (and other mods') as these largely have the same goal as Kerbal Health.
- Renaming kerbals may cause issues and is not recommended.
- Deep Freeze is not supported at the moment: your kerbals can die of health issues even when frozen. A fix is in the works.
- RemoteTech's connection state is not supported for the purpose of the Connected Factor status. This issue will be resolved when RemoteTech 2.0 is released. Meanwhile, you may disable both Connected and Assigned factors.

**Next objectives**

- More informative UI (Health Monitor)
- Support patches for DeepFreeze, Kerbalism, etc.

**Future features**

- More factors (e.g. radiation)
- New mechanics: injuries, medical supplies, quarantine, etc.
- API for collaboration with other mods
