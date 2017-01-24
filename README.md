# KerbalHealth

**Design goals**

- Increase KSP's realism, challenge, and fun by introducing health and related concepts
- Encourage the use of more sophisticated vessels, especially for long-haul flights and long-term habitation (no more 10-year mission to Eeloo in a Mk1 pod!)
- Add to individuality of kerbals
- Keep the mod as flexible as possible, including configuration options and integration with other mods
- Avoid micromanagement as much as possible

The mod is currently in beta. It has all main systems in place, but will continue to change and to grow. It is not balanced and it requires testing. Future changes can be backward-incompatible, so be careful using it in your main playthrough. This is my first attempt to mod KSP and I'm not even a professional programmer, so you've been warned.

I will appreciate your bug reports (include output_log.txt) and feature suggestions here or on Github.

**Features as of 0.4.0 (beta)**

- Every kerbal, including Tourists, has Health Points (HP).
- HP increase with kerbals' level. A 5-level kerbal is 50% "healthier" than a newbie.
- Kerbals' HP are updated (currently once a second, including in background and in timewarp).
- Health decreases when on a mission in proportion to crew/capacity ratio. A kerbal in a full vessel loses all his/her health in 10-15 days (depending on the level).
- Kerbals traveling with crewmates lose health slower. It's always good to have a company! Even if it's Bob.
- Certain parts (currently Cupola and Hitchhiker) positively affect health of either the whole crew (Cupola) or their inhabitans (Hitchhiker).
- If a kerbal's health is under 20%, he/she is exhausted and becomes a tourist. They will go back to work when health rises to 25%.
- If a kerbal's health falls to 0, he/she dies!
- In KSC, kerbals gradually recover their health.
- Health Monitor lets you check on all your kerbals in KSC and in flight.
- Health Report in Editor helps design comfy and healthy crafts.
- All of these settings are easily changed in-game or with ModuleManager patches.

**Next objectives**

- Improve UI by making Health Monitor (in KSC and in flight) and Health Report (in editor) - *DONE*
- Add more factors influencing health (e.g. SOI, Kerbin low altitude, zero g, high g, EVA, loneliness, etc.) - *PARTIALLY DONE*
- Add configuration options (through Difficulty Settings or, initially, cfg files) - *DONE*

**Future features**

- Add random health events
- Add integration with major mods (parts packs, life support, etc.)
- Add ability to recuperate in flight - *PARTIALLY DONE*
- Add more health conditions
- Add new mechanics: infectious diseases, medics, medical supplies, quarantine, etc.
- Add API for collaboration with other mods

**Requirements**

- ModuleManager
