# Kerbal Health

This mod introduces many aspects of astronauts' health management to KSP. It makes the game more challenging but also more realistic, encourages smarter mission planning, and adds to the fun. It works well alongside most popular mods.

## Overview

- Kerbals have Health Points (HP) that gradually reduce during missions and restored at KSC.
- Maximum HP is determined by kerbal's level (+10 HP per level).
- HP change based on several factors such as available living space, presence of crewmates, gravity, and specific ship parts.
- If a kerbal's health goes below 20%, he/she may become exhausted (technically, turn into a Tourist). They will eventually get back to work after health goes above 20% again.
- If a kerbal's health falls to 0, they will die!
- Kerbals are affected by radiation, which permanently reduces their maximum health. It can come from the Sun, including CMEs, radioactive parts, galactic cosmic rays and even killing planets (OPM only).
- You can protect from radiation by using shielding and choosing safer mission profiles. Planets and moons can reduce radiation within their magnetic fields and atmospheres.
- Kerbals may fall sick, have health accidents, panic attacks and other contingences.
- When kerbals level up, they can acquire quirks that affect their health reactions, to the better or to the worse.
- All necessary data is shown in the Health Monitor (at KSC and in flight) and Health Report (in the editor).
- Compatibility patches support a range of parts mods (see below).
- All of these settings are easily changed or disabled in-game and/or with ModuleManager patches.

## Health factors

The following factors may affect kerbal's health:
- **Stress** (kerbal is on a mission): -2 x (1 - *Training Level*) x *Number of Colleagues* HP/day (see details below)
- **Confinement**: -2 x *Crew* / *Living Space* HP/day (Living Space is provided by most crewed parts depending on their size, capacity, function etc.)
- **Loneliness** (only one kerbal in the vessel): -1 HP/day
- **Microgravity** (orbital or suborbital flight or under 0.1 g, e.g. Minmus): -1 HP/day
- **Isolation** (no working CommNet connection to home): -0.5 HP/day
- **EVA**: -10 HP/day
- **Home** (in Kerbin's low atmosphere): +2 HP/day
- **KSC** (kerbal is recuperating at KSC, i.e. available): +3 HP/day

You can adjust all factors' effects in the Difficulty Settings. Quirks of a kerbal may also affect their health factors. You can check current values for a specific crew member in the Health Monitor.

Certain parts (such as Hitchhiker and crew cabins) can additionally reduce the effect of a health factor (Confinement in this case) allowing for much longer and healthier flights, at a cost of Electric Charge. Hab rings in some mods can help overcome Microgravity issues for long-term stations and interplanetary missions while cupolas alleviate Loneliness.

## Stress and Training

One of the drains on kerbals' health is Stress. You reduce stress by training your kerbals for the parts in the vessel (mostly the crewable ones). To start training, load the vessel in the Editor, open the Health Report and click "Training Info" button. From there, you can select which kerbals to train. Training takes time, which is determined by the number of part types to train for, their complexity (e.g. command pods are more complex than airlocks) and kerbals' stupidity. Note that you can't train kerbals who have health conditions (e.g. sick). When a kerbal has trained for a certain part type, they won't have to train for it again. Kerbals don't recover their health while they are training at the KSC.

Kerbals also automatically train for parts in their vessel as they fly thereby gradually reducing their stress. But it is not recommended to send untrained astronauts for long missions, because they may lose too much health while they get used to the unfamiliar ship.

Training level is capped based on your Astronaut Complex facility level:
- Level 1: 40% reduction (i.e. -1.2 HP/day)
- Level 2: 60% reduction (i.e. -0.8 HP/day)
- Level 3: 75% reduction (i.e. -0.5 HP/day)

If you disable Training in the Settings, kerbals are assumed to be fully trained for all parts.

Another way to reduce Stress is by having more than one kerbal of a certain profession on a vessel. Then they are assumed working in shifts or helping each other, and their Stress level is reduced accordingly. For instance, a Pilot that loses 0.8 HP/day to Stress will lose 0.4 HP/day if there is another Pilot on the ship.

Tourists don't benefit from having more "colleagues" on a vessel. Instead, they get peace of mind from knowing that there is professional crew to look after them. Their Stress is reduced by the number of non-Tourist crew members on the vessel increased by 1. For example, a Tourist that normally loses 0.8 HP/day to Stress will lose 0.4 HP/day if there is one crew member, ~0.27 if there are two crew members etc.

## Health Recuperation and Health Decay

Certain parts (such as the Cupola) provide Recuperation bonuses. If a kerbal receives, say, a 1% recuperation bonus, he/she will recover 1% of their lacking health (i.e. of the difference between their current HP and the maximum HP) every day. This change works in parallel with the normal health factors above.

*Example: A 5-star kerbal (maximum HP = 150) currently has 55 Health Points and is in a vessel that gives him 2% recuperation. The vessel has 10 units of living space, he has connection and he has a crewmate. Therefore he recovers (150 - 55) x 2% = 1.9 HP per day and loses also (0.5 + 2 x 2 / 10 + 1) = 1.9 HP per day. It means that the marginal change balances out the "normal" change and his health will stay around 55 HP (37%) until the situation changes.*

As you see, this mechanics may allow some kerbals to stay relatively healthy indefinitely. It may look cheaty, but the point is that: (1) there should be a way to design long-term missions without spamming crew space, (2) it requires a lot of heavy parts and therefore still difficult, (3) the balanced health level is usually far from 100% and may fall lower if circumstances change (e.g., new crew arrives and fills the station), (4) these bonuses require a lot of EC, (5) radiation still keeps mounting (see below).

Recuperation is not stacked and has crew cap. It means that one Cupola provides 2% Recup for 1 kerbal, 2 Cupolas give 2% for 2 kerbals (not 4%!), etc. If you have more kerbals than the crew cap, Recuperation will be split among them evenly (e.g. 2 kerbals with 1 Cupola will get 1% Recup).

Decay is the opposite to Recuperation: for every percentage point of Decay, your kerbal will lose 1% of their remaining health per day. Fortunately, it is very rare.

## Radiation

All kerbals on missions are affected by radiation, which slowly but permanently reduces their maximum HP. Radiation is measured in banana equivalent doses (when you eat a banana, you get approximately 1e-7 Sv of radiation). 1e7 (10,000,000) bananas reduce max HP by 10%; 1e8 (100,000,000) bananas kill a kerbal.

The amount of Radiation a kerbal receives depends on many factors. Most importantly, it is determined by their location. Many planets and some moons have magnetic fields that stop some radiation; atmospheres are also very effective in shielding it (see [wiki](https://github.com/GarwelGarwel/KerbalHealth/wiki/Radiation) for more). Being close to a celestial body helps screen some rays too. E.g., radiation level at Kerbin's sea level is 1,000 times lower than in interplanetary space just outside Kerbin's SOI. Cosmic radiation is also a bit higher closer to the Sun. To check environment before sending astronauts, you can use magnetometers and Geiger counters provided by supported mods or embedded in advanced stock probe cores.

Being on EVA takes away all the protection your ship provides and dramatically increases radiation level. Artificial radiation is created by certain parts like atomic engines and nuclear reactors.

You can protect kerbals from radiation (both cosmic and artificial) by adding shielding to the vessel. It is provided by some parts, like structural panels, heat shields and mk3 cargo bays. These parts and most crew pods can be improved by adding Radiation Shielding to them in the Editor. You can never eliminate all radiation, but you can reduce it to non-dangerous levels.

Beware of solar radiation storms! They can blast your kerbals in a planet's SOI or in interplanetary space with amounts of radiation ranging from high to enormous. You will be warned, usually, a few hours in advance and the kerbals will automatically take cover in the shelter, if there is one. The shelter is determined as the most protected part (or set of parts) that can fit the entire crew. You will see shelter exposure in the Health Report and Health Monitor details. The frequence of solar storms depends on the phase of the solar cycle going (on average) from one storm every 6,667 days to one storm every 437 days.

If you have Kerbalism installed and enabled "Use Kerbalism Radiation" option in the settings, Kerbal Health's radiation calculations will be replaced with those of Kerbalism (but Kerbal Health shielding will still apply). Kerbalism has a more realistic and complex radiation model, but its balance is very different from Kerbal Health's.

It is possible to cure radiation by decontaminating a kerbal, but it is hard. To start decontamination, the kerbal has to be at KSC at full health and with no health conditions. You also need fully upgraded R&D Facility and Astronaut Complex. Every decontamination costs 100,000 funds (in Career mode) and 1,000 science points (in Career and Science mods). It cures 100,000 banana doses per Kerbin day and stops if you send the kerbal on a mission. The kerbal undergoing decontamination temporarily loses 70% of their health and will need to rest afterwards. As always, each value can be adjusted in-game. If your astronaut discovers an anomaly, they may also be miraculously decontaminated by unknown forces (but only one kerbal per anomaly).

## Quirks

Whenever a kerbal levels up, there is a 25% chance that he or she will acquire a health quirk (unless he/she already has two). Discovering an anomaly can also grant a free quirk. These can be positive or negative and usually affect kerbals' vulnerability to various health factors and dangers. Chances of getting some quirks depend on courage and stupidity of a particular kerbal. The [full list](https://github.com/GarwelGarwel/KerbalHealth/wiki/Quirks) can be found in the Kerbal Health Wiki.

## Conditions and Random Events

Kerbals' organisms, like our own, are not always predictable. Sometimes, not very often, you may see unexpected events that can impact your whole mission. Kerbals acquire (or lose) certain conditions as a result of these events. Having parts with a Sick Bay (such as the stock Science Lab) helps alleviate the symptoms.

- **Sickness**: A kerbal can become sick and start losing health quickly. His/her crewmates may catch the disease too, including during the incubation period. This condition usually heals itself after some time, but it can also lead to a pneumonia, a really dangerous condition. Having Scientists or Medics aboard helps.
- **Injuries**: A kerbal can immediately lose some health in an accident. Stupid kerbals are naturally predisposed to this condition. It may cause sepsis, which is mortally dangerous. Bring your kerbal home immediately or pray Kraken it will heal on its own.
- **Food poisoning**: Kerbals are known for their love of snacks and they don't always wash their hands. Food poisoning may not be as bad as some other conditions on its own, but if dehydration develops, they will become unable to do any work at all and effectively turn into space Tourists until it passes.
- **Panic attacks**: Though not posing danger to health per se, panic prevents kerbals from doing any useful work. At least it doesn't last long and courageous kerbals are less prone to it.

Conditions can be disabled or their chances and effects changed in game settings. You can also easily add, modify or remove conditions (see wiki for details).

## Requirements

- [Module Manager](https://forum.kerbalspaceprogram.com/threads/55219)

## Supported Mods

Kerbal Health should work well alongside most other mods and will try to adapt to them with smart MM patches. Some have better, manually balanced support though:

- [Atomic Age](https://forum.kerbalspaceprogram.com/index.php?/topic/94519-105-atomic-age-nuclear-propulsion-red-hot-radiators/)
- [B9 Aerospace](https://github.com/blowfishpro/B9-Aerospace)
- [Blizzy's Toolbar](http://forum.kerbalspaceprogram.com/index.php?/topic/55420-120-toolbar-1713-common-api-for-draggableresizable-buttons-toolbar/)
- [Bluedog Design Bureau](https://forum.kerbalspaceprogram.com/index.php?/topic/122020-131-bluedog-design-bureau-stockalike-saturn-apollo-and-more-v141-атлас-1jan2018)
- [Connected Living Space](http://forum.kerbalspaceprogram.com/index.php?showtopic=192130) (integration can be toggled in the settings)
- [Crew R&R](https://forum.kerbalspaceprogram.com/index.php?/topic/159299-151-161-172-crew-rr-crew-rest-rotation/)
- [Deadly Reentry Continued](https://forum.kerbalspaceprogram.com/index.php?/topic/50296-122-deadly-reentry-v760-june-9-2017-the-ariel-edition/)
- [DeepFreeze Continued](http://forum.kerbalspaceprogram.com/index.php?/topic/112328-11-deepfreeze-continued)
- [Deep Space Exploration Vehicles](https://forum.kerbalspaceprogram.com/index.php?/topic/122162-14x-deep-space-exploration-vessels-build-nasa-inspired-ships-in-ksp/)
- [DMagic Orbital Science](https://forum.kerbalspaceprogram.com/index.php?/topic/59009-14x-dmagic-orbital-science-new-science-parts-v1312-3152018/)
- [Extraplanetary Launchpads](https://forum.kerbalspaceprogram.com/index.php?/topic/54284-*)
- [Far Future Technologies](https://forum.kerbalspaceprogram.com/index.php?/topic/199070-1)
- [FASA](http://forum.kerbalspaceprogram.com/index.php?/topic/22888-105-fasa-544/)
- [Feline Utility Rovers](https://forum.kerbalspaceprogram.com/index.php?/topic/155344-13x14x-feline-utility-rovers-v128-10october-2018/)
- [FTmN Atomic Rockets](https://forum.kerbalspaceprogram.com/index.php?/topic/164829-18x-spacetux-industries-recycled-parts/) (classic and Improved)
- [JNSQ](https://forum.kerbalspaceprogram.com/index.php?/topic/184880-17x-jnsq-07-17-june-2019/)
- [Kerbal Atomics](http://forum.kerbalspaceprogram.com/index.php?/topic/130503-10511-kerbal-atomics-fancy-nuclear-engines-initial-11-test/)
- [Kerbalism](https://kerbalism.github.io/Kerbalism/) (stress, comforts, living space, and radiation features are disabled by Kerbal Health by default; you can use Kerbalism's radiation model instead of Kerbal Health's)
- [Kerbal Planetary Base Systems](http://forum.kerbalspaceprogram.com/index.php?/topic/133606-130-kerbal-planetary-base-systems-v144-6-june-2017/)
- [KerbalReusabilityExpansion](https://forum.kerbalspaceprogram.com/index.php?/topic/195546-110-kre-kerbal-reusability-expansion/)
- [KSP-AVC](http://ksp-avc.cybutek.net)
- [KSP Interstellar Extended](https://forum.kerbalspaceprogram.com/index.php?/topic/155255-12213-kspi-extended)
- [Malemute](https://forum.kerbalspaceprogram.com/index.php?/topic/139668-13-the-malemute-rover-020/)
- [Near Future Technologies](http://forum.kerbalspaceprogram.com/index.php?/topic/155465-122-near-future-technologies) (Aeronautics, Electrics, Exploration, Launch Vehicles, Spacecraft)
- [Outer Planets Mod](https://forum.kerbalspaceprogram.com/index.php?/topic/165854-ksp-142-outer-planets-mod221-15-april-2018/)
- [Probes Before Crew](https://forum.kerbalspaceprogram.com/index.php?/topic/181013-ksp-181-probes-before-crew-pbc-version-28/)
- [Rational Resources & Rational Resources Parts](https://forum.kerbalspaceprogram.com/index.php?/topic/184875-*/)
- [ReStock+](https://forum.kerbalspaceprogram.com/index.php?/topic/182679-161-restock-revamping-ksps-art/)
- [RLA Reborn](https://forum.kerbalspaceprogram.com/index.php?/topic/175512-14-rla-reborn/)
- [RSCapsuledyne](https://forum.kerbalspaceprogram.com/index.php?/topic/164829-18x-spacetux-industries-recycled-parts/)
- [Sin Phi Heavy Industries](https://forum.kerbalspaceprogram.com/index.php?/topic/162654-130-sin-phi-heavy-industries-centrifuge-habitat-halberd/)
- [Spacetux Recycled Parts](https://forum.kerbalspaceprogram.com/index.php?/topic/164829-131-spacetux-industries-recycled-parts/) (Atomic Age, FTmN, FTmN-New, RSCapsuledyne)
- [SpaceY Heavy Lifters](https://forum.kerbalspaceprogram.com/index.php?/topic/90545-12213-spacey-heavy-lifter-parts-pack-v116-2017-01-30/)
- [Snacks](https://forum.kerbalspaceprogram.com/index.php?/topic/149604-18x-snacks-friendly-simplified-life-support/)
- [SSTU](https://github.com/shadowmage45/SSTULabs)
- [Stockalike Station Parts Expansion Redux](https://forum.kerbalspaceprogram.com/index.php?/topic/170211-*)
- [Surface Experiment Pack](https://forum.kerbalspaceprogram.com/index.php?/topic/155382-14x-surface-experiment-pack-deployable-science-for-kiskas-v26-20mar18/&tab=comments#comment-2930055)
- [Tantares](http://forum.kerbalspaceprogram.com/index.php?/topic/73686-Tantares)
- [Tokamak Insustries Refurbished Parts](https://forum.kerbalspaceprogram.com/index.php?/topic/163166-151-tokamak-industries-refurbished-parts-featuring-porkjets-hab-pack/)
- [USI Freight Transportation Technologies](https://forum.kerbalspaceprogram.com/index.php?/topic/82730-13-freight-transport-technologies-v060/)
- [USI Kolonization Systems (MKS/OKS) + Karibou](https://github.com/BobPalmer/MKS)
- [USI-LS](https://github.com/BobPalmer/USI-LS) (see notes below)

Making History is supported but not required.

If you would like to include special support for your (or your favorite) mod, let me know.

## Conflicts and Incompatibilities

- Any mod, which can temporarily make kerbals Tourists, can conflict with Kerbal Health if both mods change kerbals' status and then turn it back. In some situations it may mean that your kerbals will remain Tourists indefinitely or become active too soon. Kerbal Health tries to fix some of these situations, but cannot prevent all of them.
- Custom solar systems with multiple stars can work in strange ways with radiation mechanics. Disable radiation in the settings if you have problems.
- It is recommended to disable habitation mechanics of USI-LS (and other similar mods) as these largely have the same goal as Kerbal Health.
- RemoteTech's connection state is not supported for the purpose of the Connected Factor status. This issue will be resolved once RemoteTech 2.0 is released. Meanwhile, you may disable both Connected and Assigned factors to keep balance.

## Copyright and License

This mod has been created by [Garwel](https://forum.kerbalspaceprogram.com/index.php?/profile/141813-garwel/) and is distributed under [MIT license](https://opensource.org/licenses/MIT).
