using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;
using KSP.Localization;

namespace KerbalHealth
{
    /// <summary>
    /// Main class for processing kerbals' health
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR)]
    public class KerbalHealthScenario : ScenarioModule
    {
        static double lastUpdated;  // UT at last health update
        static double nextEventTime;  // UT when (or after) next event check occurs
        Version version;  // Current Kerbal Health version

        List<RadStorm> radStorms = new List<RadStorm>();
        bool checkUntrainedKerbals = false;
        ScreenMessage untrainedKerbalsWarningMessage;
        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        SortedList<ProtoCrewMember, KerbalHealthStatus> kerbals;
        bool dirty = false, crewChanged = false, vesselChanged = false;
        const int colNumMain = 8, colNumDetails = 6;  // # of columns in Health Monitor
        const int colWidth = 100;  // Width of a cell
        const int colSpacing = 10;
        const int gridWidthList = colNumMain * (colWidth + colSpacing) - colSpacing,
            gridWidthDetails = colNumDetails * (colWidth + colSpacing) - colSpacing;  // Grid width
        Rect monitorPosition = new Rect(0.5f, 0.5f, gridWidthList, 200);
        PopupDialog monitorWindow;  // Health Monitor window
        System.Collections.Generic.List<DialogGUIBase> gridContents;  // Health Monitor grid's labels
        KerbalHealthStatus selectedKHS = null;  // Currently selected kerbal for details view, null if list is shown
        int page = 1;  // Current page in the list of kerbals

        public void Start()
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.Start", Core.LogLevel.Important);
            if (Core.IsInEditor)
            {
                Core.Log("Skipping KerbalHealthScenario initialization in Editor.", Core.LogLevel.Important);
                return;
            }
            Core.Log(Core.Factors.Count + " factors initialized.");
            Core.KerbalHealthList.RegisterKerbals();
            vesselChanged = true;

            lastUpdated = Planetarium.GetUniversalTime();
            nextEventTime = lastUpdated + GetNextEventInterval();

            GameEvents.OnGameSettingsApplied.Add(OnGameSettingsApplied);
            GameEvents.onCrewOnEva.Add(OnKerbalEva);
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            GameEvents.onCrewKilled.Add(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Add(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Add(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Add(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Add(OnKerbalRemoved);
            GameEvents.onKerbalNameChanged.Add(OnKerbalNameChanged);
            GameEvents.OnProgressComplete.Add(OnProgressComplete);
            GameEvents.onVesselWasModified.Add(onVesselWasModified);

            if (!DFWrapper.InstanceExists)
            {
                Core.Log("Initializing DFWrapper...", Core.LogLevel.Important);
                DFWrapper.InitDFWrapper();
                if (DFWrapper.InstanceExists) Core.Log("DFWrapper initialized.", Core.LogLevel.Important);
                else Core.Log("Could not initialize DFWrapper.", Core.LogLevel.Important);
            }
            if (DFWrapper.InstanceExists)
            {
                EventData<Part, ProtoCrewMember> dfEvent;
                dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalFrozen");
                if (dfEvent != null) dfEvent.Add(OnKerbalFrozen);
                else Core.Log("Could not find onKerbalFrozen event!", Core.LogLevel.Error);
                dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalThaw");
                if (dfEvent != null) dfEvent.Add(OnKerbalThaw);
                else Core.Log("Could not find onKerbalThaw event!", Core.LogLevel.Error);
            }

            if (Core.ShowAppLauncherButton)
            {
                Core.Log("Registering AppLauncher button...", Core.LogLevel.Important);
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }
            if (ToolbarManager.ToolbarAvailable)
            {
                Core.Log("Registering Blizzy's Toolbar button...", Core.LogLevel.Important);
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthMonitor");
                toolbarButton.Text = "Kerbal Health Monitor";
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += (e) => { if (monitorWindow == null) DisplayData(); else UndisplayData(); };
            }

            // Automatically updating settings from older versions
            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != v)
            {
                Core.Log("Current mod version " + v + " is different from v" + version + " used to save the game. Most likely, Kerbal Health has been recently updated.", Core.LogLevel.Important);
                if ((version < new Version("1.1.0")) && (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().ConfinementBaseFactor != -3) && (Planetarium.GetUniversalTime() > 0))
                {
                    Core.Log("Confinement Factor is " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().ConfinementBaseFactor + " instead of -3. Automatically fixing.");
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().ConfinementBaseFactor = -3;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg1", v.ToString(3)), true);//"Kerbal Health has been updated to v" +  + ". Confinement factor value has been reset to -3. It is recommended that you load each crewed vessel briefly to update Kerbal Health cache."
                }
                if (version < new Version("1.2.1.2"))
                {
                    Core.Log("Pre-1.3 radiation settings: " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation.ToString("F0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation.ToString("F0"));
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEffect = 0.1f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient = 0.3f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient = 0.2f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient = 0.2f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient = 0.01f;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg2", v.ToString()), true);//"Kerbal Health has been updated to v" + + ". Radiation settings have been reset. It is recommended that you load each crewed vessel briefly to update Kerbal Health cache."
                }
                if (version < new Version("1.3.8.1"))
                {
                    Core.Log("Pre-1.3.9 Stress factor: " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().StressFactor);
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().StressFactor = -2;
                    Core.SolarRadiation = 2500;
                    Core.GalacticRadiation = 12500;
                    Core.InSpaceHighCoefficient = 0.4f;
                    Core.TrainingEnabled = false;
                    Core.ShowMessage(Localizer.Format("#KH_Versionmsg3", v.ToString(3)), true);
                }
                version = v;
            }
            else Core.Log("Kerbal Health v" + version);

            if (NeedsCheckForUntrainedCrew) checkUntrainedKerbals = true;

            Core.Log("KerbalHealthScenario.Start finished.", Core.LogLevel.Important);
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthScenario.OnDisable", Core.LogLevel.Important);
            if (Core.IsInEditor) return;
            UndisplayData();

            GameEvents.OnGameSettingsApplied.Remove(OnGameSettingsApplied);
            GameEvents.onCrewOnEva.Remove(OnKerbalEva);
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            GameEvents.onCrewKilled.Remove(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Remove(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Remove(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Remove(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Remove(OnKerbalRemoved);
            GameEvents.onKerbalNameChange.Remove(OnKerbalNameChanged);
            GameEvents.OnProgressComplete.Remove(OnProgressComplete);
            GameEvents.onVesselWasModified.Remove(onVesselWasModified);

            EventData<Part, ProtoCrewMember> dfEvent;
            dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalFrozen");
            if (dfEvent != null) dfEvent.Remove(OnKerbalFrozen);
            dfEvent = GameEvents.FindEvent<EventData<Part, ProtoCrewMember>>("onKerbalThaw");
            if (dfEvent != null) dfEvent.Remove(OnKerbalThaw);

            if (toolbarButton != null) toolbarButton.Destroy();
            if ((appLauncherButton != null) && (ApplicationLauncher.Instance != null))
                ApplicationLauncher.Instance.RemoveModApplication(appLauncherButton);
            Core.Log("KerbalHealthScenario.OnDisable finished.", Core.LogLevel.Important);
        }

        /// <summary>
        /// Called to check and reset Kerbal Health settings, if needed
        /// </summary>
        public void OnGameSettingsApplied()
        {
            Core.Log("OnGameSettingsApplied");
            if (KerbalHealthGeneralSettings.Instance.ResetSettings)
            {
                KerbalHealthGeneralSettings.Instance.Reset();
                KerbalHealthFactorsSettings.Instance.Reset();
                KerbalHealthQuirkSettings.Instance.Reset();
                KerbalHealthRadiationSettings.Instance.Reset();
                ScreenMessages.PostScreenMessage(Localizer.Format("#KH_MSG_SettingsReset"), 5);
            }
        }

        /// <summary>
        /// Marks the kerbal as being on EVA to apply EVA-only effects
        /// </summary>
        /// <param name="action"></param>
        public void OnKerbalEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (!Core.ModEnabled) return;
            Core.Log(action.to.protoModuleCrew[0].name + " went on EVA from " + action.from.name + ".", Core.LogLevel.Important);
            Core.KerbalHealthList.Find(action.to.protoModuleCrew[0]).IsOnEVA = true;
            vesselChanged = true;
            UpdateKerbals(true);
        }

        public void onCrewBoardVessel(GameEvents.FromToAction<Part, Part> action)
        {
            if (!Core.ModEnabled) return;
            Core.Log("onCrewBoardVessel(<'" + action.from.name + "', '" + action.to.name + "'>)");
            foreach (ProtoCrewMember pcm in action.to.protoModuleCrew)
                Core.KerbalHealthList.Find(pcm).IsOnEVA = false;
            vesselChanged = true;
            UpdateKerbals(true);
        }

        public void OnCrewKilled(EventReport er)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnCrewKilled(<'" + er.msg + "', " + er.sender + ", " + er.other + ">)", Core.LogLevel.Important);
            Core.KerbalHealthList.Remove(er.sender);
            dirty = crewChanged = true;
        }

        public void OnCrewmemberHired(ProtoCrewMember pcm, int i)
        {
            Core.Log("OnCrewmemberHired('" + pcm.name + "', " + i + ")", Core.LogLevel.Important);
            dirty = crewChanged = true;
        }

        public void OnCrewmemberSacked(ProtoCrewMember pcm, int i)
        {
            Core.Log("OnCrewmemberSacked('" + pcm.name + "', " + i + ")", Core.LogLevel.Important);
            Core.KerbalHealthList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalAdded(ProtoCrewMember pcm)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnKerbalAdded('" + pcm.name + "')", Core.LogLevel.Important);
            if ((pcm.type == ProtoCrewMember.KerbalType.Applicant) || (pcm.type == ProtoCrewMember.KerbalType.Unowned))
            {
                Core.Log("The kerbal is " + pcm.type + ". Skipping.", Core.LogLevel.Important);
                return;
            }
            Core.KerbalHealthList.Add(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalRemoved(ProtoCrewMember pcm)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnKerbalRemoved('" + pcm.name + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalNameChanged(ProtoCrewMember pcm, string name1, string name2)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnKerbalNameChanged('" + pcm.name + "', '" + name1 + "', '" + name2 + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Rename(name1, name2);
            dirty = true;
        }

        public void OnKerbalFrozen(Part part, ProtoCrewMember pcm)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnKerbalFrozen('" + part.name + "', '" + pcm.name + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Find(pcm).IsFrozen = true;
            dirty = true;
        }

        public void OnKerbalThaw(Part part, ProtoCrewMember pcm)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnKerbalThaw('" + part.name + "', '" + pcm.name + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Find(pcm).IsFrozen = false;
            dirty = true;
        }

        /// <summary>
        /// Checks if an anomaly has just been discovered and awards quirks to a random discoverer
        /// </summary>
        /// <param name="n"></param>
        public void OnProgressComplete(ProgressNode n)
        {
            if (!Core.ModEnabled) return;
            Core.Log("OnProgressComplete(" + n.Id + ")");
            if (n is KSPAchievements.PointOfInterest poi)
            {
                Core.Log("Reached anomaly: " + poi.Id + " on " + poi.body, Core.LogLevel.Important);
                if ((Core.rand.NextDouble() < HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().AnomalyQuirkChance) && (FlightGlobals.ActiveVessel.GetCrewCount() > 0))
                {
                    List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
                    ProtoCrewMember pcm = crew[Core.rand.Next(crew.Count)];
                    Quirk q = Core.KerbalHealthList.Find(pcm).AddRandomQuirk();
                    if (q != null) Core.Log(pcm.name + " was awarded " + q.Title + " quirk for discovering an anomaly.", Core.LogLevel.Important);
                }
            }
        }

        public void onVesselWasModified(Vessel v)
        {
            Core.Log("onVesselWasModified('" + v.name + "')");
            vesselChanged = true;
        }

        bool NeedsCheckForUntrainedCrew => Core.ModEnabled && Core.TrainingEnabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH;

        /// <summary>
        /// Checks the given vessel and displays an alert if any of the crew isn't fully trained
        /// </summary>
        /// <param name="v"></param>
        void CheckUntrainedCrewWarning(Vessel v)
        {
            Core.Log("CheckUntrainedCrewWarning('" + v.vesselName + "')");
            if (!NeedsCheckForUntrainedCrew)
            {
                Core.Log("Disabling untrained crew warning.");
                checkUntrainedKerbals = false;
                untrainedKerbalsWarningMessage.duration = 0;
                return;
            }
            string msg = "";
            int n = 0;
            foreach (ProtoCrewMember pcm in v.GetVesselCrew())
            {
                KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
                if (khs == null)
                {
                    Core.Log("KerbalHealthStatus for " + pcm.name + " in " + v.vesselName + " not found!", Core.LogLevel.Error);
                    continue;
                }
                Core.Log(pcm.name + " is trained " + khs.TrainingLevel.ToString("P1") + " / " + Core.TrainingCap.ToString("P1"));
                if (khs.TrainingLevel < Core.TrainingCap)
                {
                    msg += (msg.Length == 0 ? "" : ", ") + pcm.name;
                    n++;
                }
            }
            Core.Log(n + " kerbals are untrained: " + msg);
            if (n == 0) return;
            untrainedKerbalsWarningMessage = new ScreenMessage(Localizer.Format(n == 1 ? "#KH_TrainingAlert1" : "#KH_TrainingAlertMany", msg), 2 * Core.UpdateInterval, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(untrainedKerbalsWarningMessage);
        }

        public void TrainVessel(Vessel v)
        {
            if (v == null) return;
            Core.Log("KerbalHealthScenario.TrainVessel('" + v.vesselName + "')");
            foreach (ProtoCrewMember pcm in v.GetVesselCrew())
            {
                KerbalHealthStatus khs = Core.KerbalHealthList.Find(pcm);
                if (khs == null) Core.KerbalHealthList.Add(khs = new KerbalHealthStatus(pcm.name));
                khs.StartTraining(v.Parts, v.vesselName);
            }
        }

        /// <summary>
        /// Next event update is scheduled after a random period of time, between 0 and 2 days
        /// </summary>
        /// <returns></returns>
        double GetNextEventInterval() => Core.rand.NextDouble() * KSPUtil.dateTimeFormatter.Day * 2;

        void ProcessRadStorms()
        {
            Core.Log("ProcessRadStorms");
            Dictionary<int, RadStorm> targets = new Dictionary<int, RadStorm>
            { { Planetarium.fetch.Home.name.GetHashCode(), new RadStorm(Planetarium.fetch.Home) } };
            foreach (ProtoCrewMember pcm in HighLogic.fetch.currentGame.CrewRoster.Kerbals(ProtoCrewMember.RosterStatus.Assigned))
            {
                Vessel v = Core.KerbalVessel(pcm);
                if (v == null) continue;
                CelestialBody b = v.mainBody;
                Core.Log(pcm.name + " is in " + v.vesselName + " in " + b.name + "'s SOI.");
                int k;
                if (b == Planetarium.fetch.Sun)
                {
                    k = (int)v.persistentId;
                    if (!targets.ContainsKey(k)) targets.Add(k, new RadStorm(v));
                }
                else
                {
                    b = Core.GetPlanet(b);
                    k = b.name.GetHashCode();
                    if (!targets.ContainsKey(k)) targets.Add(k, new RadStorm(b));
                }
            }
            Core.Log(targets.Count + " potential radstorm targets found.");
            Core.Log("Current solar cycle phase: " + Core.SolarCyclePhase.ToString("P2") + " through. Radstorm chance: " + Core.RadStormChance);

            foreach (RadStorm t in targets.Values)
                if (Core.rand.NextDouble() < Core.RadStormChance * Core.RadStormFrequency)
                {
                    RadStormType rst = Core.GetRandomRadStormType();
                    double delay = t.DistanceFromSun / rst.GetVelocity();
                    t.Magnitutde = rst.GetMagnitude();
                    Core.Log("Radstorm will hit " + t.Name + " travel distance: " + t.DistanceFromSun.ToString("F0") + " m; travel time: " + delay.ToString("N0") + " s; magnitude " + t.Magnitutde.ToString("N0"));
                    t.Time = Planetarium.GetUniversalTime() + delay;
                    Core.ShowMessage(Localizer.Format("#KH_RadStorm_Alert", rst.Name, t.Name, KSPUtil.PrintDate(t.Time, true)), true);//A radiation storm of <color=\"yellow\">" + rst.Name + "</color> strength is going to hit <color=\"yellow\">" + t.Name + "</color> on <color=\"yellow\">" + KSPUtil.PrintDate(t.Time, true) + "</color>!
                    radStorms.Add(t);
                }
                else Core.Log("No radstorm for " + t.Name);
        }

        /// <summary>
        /// The main method for updating all kerbals' health and processing events
        /// </summary>
        /// <param name="forced">Whether to process kerbals regardless of the amount of time passed</param>
        void UpdateKerbals(bool forced)
        {
            double time = Planetarium.GetUniversalTime();
            double timePassed = time - lastUpdated;
            if (timePassed <= 0) return;
            if (forced || ((timePassed >= Core.UpdateInterval) && (timePassed >= Core.MinUpdateInterval * TimeWarp.CurrentRate)))
            {
                Core.Log("UT is " + time + ". Updating for " + timePassed + " seconds.");
                Core.ClearCache();
                if (HighLogic.LoadedSceneIsFlight && vesselChanged)
                {
                    Core.Log("Vessel has changed or just loaded. Ordering kerbals to train for it in-flight.");
                    foreach (Vessel v in FlightGlobals.VesselsLoaded) TrainVessel(v);
                    vesselChanged = false;
                }
                if (checkUntrainedKerbals) CheckUntrainedCrewWarning(FlightGlobals.ActiveVessel);

                // Processing radiation storms
                if (Core.RadiationEnabled && Core.RadStormsEnabled)
                {
                    for (int i = 0; i < radStorms.Count; i++)
                        if (time >= radStorms[i].Time)
                        {
                            int j = 0;
                            double m = radStorms[i].Magnitutde * KerbalHealthStatus.GetSolarRadiationProportion(radStorms[i].DistanceFromSun) * Core.RadStormMagnitude;
                            Core.Log("Radstorm " + i + " hits " + radStorms[i].Name + " with magnitude of " + m + " (" + radStorms[i].Magnitutde + " before modifiers).", Core.LogLevel.Important);
                            string s = Localizer.Format("#KH_RadStorm_report1", Core.PrefixFormat(m, 5), radStorms[i].Name);//Radstorm of nominal magnitude <color=\"yellow\">" + Core.PrefixFormat(m, 5) + " BED</color> has just hit <color=\"yellow\">" + radStorms[i].Name + "</color>. Affected kerbals:";
                            foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                                if (radStorms[i].Affects(khs.PCM))
                                {
                                    double d = m * KerbalHealthStatus.GetCosmicRadiationRate(Core.KerbalVessel(khs.PCM)) * khs.ShelterExposure;
                                    khs.AddDose(d);
                                    Core.Log("The radstorm irradiates " + khs.Name + " by " + d.ToString("N0") + " BED.");
                                    s += Localizer.Format("#KH_RadStorm_report2", khs.Name, Core.PrefixFormat(d, 5)); //\r\n- <color=\"yellow\">" + khs.Name + "</color> for <color=\"yellow\">" + Core.PrefixFormat(d, 5) + " BED</color>
                                    j++;
                                }
                            if (j > 0) Core.ShowMessage(s, true);
                            radStorms.RemoveAt(i--);
                        }
                    if (Core.GetYear(time) > Core.GetYear(lastUpdated))
                    {
                        Core.Log("Showing solar weather summary for year " + Core.GetYear(time) + ".", Core.LogLevel.Important);
                        Core.ShowMessage(Localizer.Format("#KH_RadStorm_AnnualReport", (Core.SolarCyclePhase * 100).ToString("N1"), Math.Floor(time / Core.SolarCycleDuration + 1).ToString("N0"), (1 / Core.RadStormChance / Core.RadStormFrequency).ToString("N0")), false); //You are " +  + " through solar cycle " +  + ". Current mean time between radiation storms is " +  + " days.
                    }
                }

                Core.KerbalHealthList.Update(timePassed);
                lastUpdated = time;

                // Processing events. It can take several turns of event processing at high time warp
                while (time >= nextEventTime)
                {
                    if (Core.ConditionsEnabled)
                    {
                        Core.Log("Processing conditions...");
                        foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                        {
                            ProtoCrewMember pcm = khs.PCM;
                            if (khs.IsFrozen || khs.IsDecontaminating || !Core.IsKerbalTrackable(pcm)) continue;
                            for (int i = 0; i < khs.Conditions.Count; i++)
                            {
                                HealthCondition hc = khs.Conditions[i];
                                foreach (Outcome o in hc.Outcomes)
                                    if (Core.rand.NextDouble() < o.GetChancePerDay(pcm) * HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().ConditionsChance)
                                    {
                                        Core.Log("Condition " + hc.Name + " has outcome: " + o);
                                        if (o.Condition != "") khs.AddCondition(o.Condition);
                                        if (o.RemoveOldCondition)
                                        {
                                            khs.RemoveCondition(hc);
                                            i--;
                                            break;
                                        }
                                    }
                            }
                            foreach (HealthCondition hc in Core.HealthConditions.Values)
                                if ((hc.ChancePerDay > 0) && (hc.Stackable || !khs.HasCondition(hc)) && hc.IsCompatibleWith(khs.Conditions) && hc.Logic.Test(pcm) && (Core.rand.NextDouble() < hc.GetChancePerDay(pcm) * HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthQuirkSettings>().ConditionsChance))
                                {
                                    Core.Log(khs.Name + " acquires " + hc.Name + " condition.");
                                    khs.AddCondition(hc);
                                }
                        }
                    }
                    if (Core.RadiationEnabled && Core.RadStormsEnabled) ProcessRadStorms();
                    nextEventTime += GetNextEventInterval();
                    Core.Log("Next event processing is scheduled at " + KSPUtil.PrintDateCompact(nextEventTime, true), Core.LogLevel.Important);
                }
                dirty = true;
            }
        }

        public void FixedUpdate()
        { if (Core.ModEnabled && !Core.IsInEditor) UpdateKerbals(false); }

        int LinesPerPage => HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().LinesPerPage;

        bool ShowPages => Core.KerbalHealthList.Count > LinesPerPage;

        int PageCount => (int)System.Math.Ceiling((double)(Core.KerbalHealthList.Count) / LinesPerPage);

        int FirstLine => (page - 1) * LinesPerPage;

        int LineCount => System.Math.Min(Core.KerbalHealthList.Count - FirstLine, LinesPerPage);

        void FirstPage()
        {
            dirty = page != PageCount;
            page = 1;
            if (!dirty) Invalidate();
        }

        void PageUp()
        {
            dirty = page != PageCount;
            if (page > 1) page--;
            if (!dirty) Invalidate();
        }

        void PageDown()
        {
            if (page < PageCount) page++;
            if (page == PageCount) Invalidate();
            else dirty = true;
        }

        void LastPage()
        {
            page = PageCount;
            Invalidate();
        }

        /// <summary>
        /// Shows Health monitor when the AppLauncher/Blizzy's Toolbar button is clicked
        /// </summary>
        public void DisplayData()
        {
            Core.Log("KerbalHealthScenario.DisplayData", Core.LogLevel.Important);
            if (HighLogic.LoadedSceneIsFlight) Core.Log("Current vessel id = " + FlightGlobals.ActiveVessel.persistentId);
            UpdateKerbals(true);
            if (selectedKHS == null)
            {
                Core.Log("No kerbal selected, showing overall list.");

                // Preparing a sorted list of kerbals
                kerbals = new SortedList<ProtoCrewMember, KerbalHealthStatus>(new KerbalComparer(HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthGeneralSettings>().SortByLocation));
                foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
                    kerbals.Add(khs.PCM, khs);

                DialogGUILayoutBase layout = new DialogGUIVerticalLayout(true, true);
                if (page > PageCount) page = PageCount;
                if (ShowPages) layout.AddChild(new DialogGUIHorizontalLayout(true, false,
                    new DialogGUIButton("<<", FirstPage, () => (page > 1), true),
                    new DialogGUIButton("<", PageUp, () => (page > 1), false),
                    new DialogGUIHorizontalLayout(TextAnchor.LowerCenter, new DialogGUILabel("Page " + page + "/" + PageCount)),
                    new DialogGUIButton(">", PageDown, () => (page < PageCount), false),
                    new DialogGUIButton(">>", LastPage, () => (page < PageCount), true)));
                gridContents = new List<DialogGUIBase>((Core.KerbalHealthList.Count + 1) * colNumMain);

                // Creating column titles
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_Name") + "</color></b>", true));//Name
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_Location") + "</color></b>", true));//Location
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_Condition") + "</color></b>", true));//Condition
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_Health") + "</color></b>", true));//Health
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_Changeperday") + "</color></b>", true));//Change/day
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_TimeLeft") + "</color></b>", true));//Time Left
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">" + Localizer.Format("#KH_HM_Radiation") + "</color></b>", true));//Radiation
                gridContents.Add(new DialogGUILabel("", true));

                // Initializing Health Monitor's grid with empty labels, to be filled in Update()
                for (int i = FirstLine; i < FirstLine + LineCount; i++)
                {
                    for (int j = 0; j < colNumMain - 1; j++)
                        gridContents.Add(new DialogGUILabel("", true));
                    gridContents.Add(new DialogGUIButton<int>(Localizer.Format("#KH_HM_Details"), (n) => { selectedKHS = kerbals.Values[n]; Invalidate(); }, i));//"Details"
                }
                layout.AddChild(new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(colWidth, 30), new Vector2(colSpacing, 10), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNumMain, gridContents.ToArray()));
                monitorPosition.width = gridWidthList + 10;
                monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("Health Monitor", "", Localizer.Format("#KH_HM_windowtitle"), HighLogic.UISkin, monitorPosition, layout), false, HighLogic.UISkin, false);//"Health Monitor"
            }

            else
            {
                // Creating the grid for detailed view, which will be filled in Update method
                Core.Log("Showing details for " + selectedKHS.Name + ".");
                gridContents = new List<DialogGUIBase>();
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DName")));//"Name:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DLevel")));//"Level:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DCondition")));//"Condition:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DQuirks")));//"Quirks:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DMaxHP")));//"Max HP:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DHp")));//"HP:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DHPChange")));//"HP Change:"
                gridContents.Add(new DialogGUILabel(""));
                if (Core.IsKerbalLoaded(selectedKHS.PCM) && !selectedKHS.HasCondition("Frozen"))
                    foreach (HealthFactor f in Core.Factors)
                    {
                        gridContents.Add(new DialogGUILabel(f.Title + ":"));
                        gridContents.Add(new DialogGUILabel(""));
                    }
                gridContents.Add(new DialogGUILabel("Training:"));
                gridContents.Add(new DialogGUIHorizontalLayout(
                    new DialogGUILabel(""),
                    new DialogGUIButton("?", OnTrainingInfo, 20, 20, false)));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DRecuperation")));//"Recuperation:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DExposure")));//"Exposure:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DShelterExposure")));//Shelter Exposure:
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DRadiation")));//"Radiation:"
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DLifetimeDose")));//"Lifetime Dose:"
                gridContents.Add(new DialogGUIHorizontalLayout(
                    new DialogGUILabel(""),
                    new DialogGUIButton(Localizer.Format("#KH_HM_DDecon"), OnDecontamination, 50, 20, false)));//"Decon"
                gridContents.Add(new DialogGUILabel(Localizer.Format("#KH_HM_DRadHPLoss")));//"Rad HP Loss:"
                gridContents.Add(new DialogGUILabel(""));
                monitorPosition.width = gridWidthDetails + 10;
                monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("Health Monitor", "", Localizer.Format("#KH_HM_Dwindowtitle"), HighLogic.UISkin, monitorPosition, new DialogGUIVerticalLayout(new DialogGUIGridLayout(new RectOffset(3, 3, 3, 3), new Vector2(colWidth, 40), new Vector2(colSpacing, 10), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNumDetails, gridContents.ToArray()), new DialogGUIButton(Localizer.Format("#KH_HM_backbtn"), () => { selectedKHS = null; Invalidate(); }, gridWidthDetails, 20, false))), false, HighLogic.UISkin, false);//"Health Details""Back"
            }
            dirty = true;
        }

        /// <summary>
        /// Hides the Health Monitor window
        /// </summary>
        public void UndisplayData()
        {
            if (monitorWindow != null)
            {
                Vector3 v = monitorWindow.RTrf.position;
                monitorPosition = new Rect(v.x / Screen.width + 0.5f, v.y / Screen.height + 0.5f, gridWidthList + 20, 50);
                monitorWindow.Dismiss();
            }
        }

        void Invalidate()
        {
            UndisplayData();
            DisplayData();
        }

        void OnTrainingInfo()
        {
            if (selectedKHS == null) return;
            string msg = (selectedKHS.TrainingVessel != null)
               ? Localizer.Format("#KH_TI_KerbalTraining", selectedKHS.Name, selectedKHS.TrainingVessel, selectedKHS.TrainingFor.Count, (selectedKHS.TrainingLevel * 100).ToString("N1"), (Core.TrainingCap * 100).ToString("N0"), Core.ParseUT(selectedKHS.TrainingETA, false, 10)) //<color=\"white\">" +  + "</color> is training for <color=\"white\">" +  + "</color> (" +  + " parts).\r\nProgress: <color=\"white\">" +  + "% / " +  + "%</color>.\r\n<color=\"white\">" +  + "</color> to go.
               : Localizer.Format("#KH_TI_KerbalNotTraining", selectedKHS.Name);//<color=\"white\">" + + "</color> is not currently training.
            if (selectedKHS.TrainedVessels.Count > 0)
            {
                msg += Localizer.Format("#KH_TI_TrainedVessels", selectedKHS.Name);//\r\n\n" + + " is trained for the following vessels:
                foreach (KeyValuePair<string, double> kvp in selectedKHS.TrainedVessels)
                    msg += Localizer.Format("#KH_TI_TrainedVessel", kvp.Key, (kvp.Value * 100).ToString("N1"));//\r\n- <color=\"white\">" + + ":\t" +  + "%</color>
            }
            if (selectedKHS.FamiliarPartTypes.Count > 0)
            {
                msg += Localizer.Format("#KH_TI_FamiliarParts", selectedKHS.Name);//\r\n\n<color=\"white\">" + + "</color> is familiar with the following part types:
                foreach (string s in selectedKHS.FamiliarPartTypes)
                    msg += "\r\n- <color=\"white\">" + (PartLoader.getPartInfoByName(s)?.title ?? s) + "</color>";
            }
            PopupDialog.SpawnPopupDialog(new MultiOptionDialog("Training Info", msg, Localizer.Format("#KH_TI_Title"), HighLogic.UISkin, new DialogGUIButton(Localizer.Format("#KH_TI_Close"), null, true)), false, HighLogic.UISkin);//Training Info""Close
        }

        void OnDecontamination()
        {
            if (selectedKHS == null) return;
            string msg = "<color=\"white\">";
            Callback ok = null;
            if (selectedKHS.IsDecontaminating)
            {
                Core.Log("User ordered to stop decontamination of " + selectedKHS.Name);
                msg = Localizer.Format("#KH_DeconMsg1", selectedKHS.Name);// + " is decontaminating. If you stop it, the process will stop and they will slowly regain health."
                ok = () => { selectedKHS.StopDecontamination(); Invalidate(); };
            }
            else
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) msg += Localizer.Format("#KH_DeconMsg2", Core.DecontaminationAstronautComplexLevel,Core.DecontaminationRNDLevel); //"Your Astronaut Complex has to be <color=\"yellow\">level " +  + "</color> and your R&D Facility <color=\"yellow\">level " +  + "</color> to allow decontamination.\r\n\r\n"
                if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER) || (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)) msg += Localizer.Format("#KH_DeconMsg3", (HighLogic.CurrentGame.Mode == Game.Modes.CAREER ?  Localizer.Format("#KH_DeconMsg3_CAREERMode", Core.DecontaminationFundsCost.ToString("N0")) : ""), Core.DecontaminationScienceCost.ToString("N0")); //"Decontamination will cost <color=\"yellow\">" +  +  + " science</color>. "( <<1>>" funds and ")
                msg += Localizer.Format("#KH_DeconMsg4", selectedKHS.Name,(Core.DecontaminationHealthLoss * 100).ToString("N0"),Core.DecontaminationRate.ToString("N0"),Core.ParseUT(selectedKHS.Dose / Core.DecontaminationRate * 21600, false, 2)); //"<<1>> needs to be at KSC at 100% health and have no health conditions for the process to start. Their health will be reduced by <<2>>% during decontamination.\r\n\r\nAt a rate of <<3>> banana doses/day, it is expected to take about <color="yellow"><<4>></color>."
                if (selectedKHS.IsReadyForDecontamination)
                    ok = () => { selectedKHS.StartDecontamination(); Invalidate(); };
                else msg += Localizer.Format("#KH_DeconMsg5");//"</color>\r\n<align=\"center\"><color=\"red\">You cannot start decontamination now.</color></align>"
            }
            PopupDialog.SpawnPopupDialog(new MultiOptionDialog("Decontamination", msg, Localizer.Format("#KH_DeconWinTitle"), HighLogic.UISkin, new DialogGUIButton(Localizer.Format("#KH_DeconWinOKbtn"), ok, () => selectedKHS.IsReadyForDecontamination, true), new DialogGUIButton(Localizer.Format("#KH_DeconWinCancelbtn"), null, true)), false, HighLogic.UISkin);//"Decontamination""OK""Cancel"
        }

        /// <summary>
        /// Displays actual values in Health Monitor
        /// </summary>
        public void Update()
        {
            if (!Core.ModEnabled)
            {
                if (monitorWindow != null) monitorWindow.Dismiss();
                return;
            }

            if ((monitorWindow == null) || !dirty) return;

            if (gridContents == null)
            {
                Core.Log("KerbalHealthScenario.gridContents is null.", Core.LogLevel.Error);
                monitorWindow.Dismiss();
                return;
            }

            if (selectedKHS == null)  // Showing list of all kerbals
            {
                if (crewChanged)
                {
                    Core.KerbalHealthList.RegisterKerbals();
                    Invalidate();
                    crewChanged = false;
                }
                // Fill the Health Monitor's grid with kerbals' health data
                for (int i = 0; i < LineCount; i++)
                {
                    KerbalHealthStatus khs = kerbals.Values[FirstLine + i];
                    bool healthFrozen = khs.IsFrozen || khs.IsDecontaminating;
                    double ch = khs.LastChangeTotal;
                    double b = khs.GetBalanceHP();
                    string formatTag = "", formatUntag = "", s;
                    if (healthFrozen || (ch == 0) || ((b - khs.NextConditionHP()) * ch < 0)) s = "—";
                    else
                    {
                        s = Core.ParseUT(khs.TimeToNextCondition(), false, 100);
                        if (ch < 0)
                        {
                            if (khs.TimeToNextCondition() < KSPUtil.dateTimeFormatter.Day) formatTag = "<color=\"red\">";
                            else formatTag = "<color=\"orange\">";
                            formatUntag = "</color>";
                        }
                    }
                    gridContents[(i + 1) * colNumMain].SetOptionText(formatTag + khs.FullName + formatUntag);
                    gridContents[(i + 1) * colNumMain + 1].SetOptionText(formatTag + khs.LocationString + formatUntag);
                    gridContents[(i + 1) * colNumMain + 2].SetOptionText(formatTag + khs.ConditionString + formatUntag);
                    gridContents[(i + 1) * colNumMain + 3].SetOptionText(formatTag + (100 * khs.Health).ToString("F2") + "% (" + khs.HP.ToString("F2") + ")" + formatUntag);
                    gridContents[(i + 1) * colNumMain + 4].SetOptionText(formatTag + ((healthFrozen || (khs.Health >= 1)) ? "—" : (((ch > 0) ? "+" : "") + ch.ToString("F2"))) + formatUntag);
                    gridContents[(i + 1) * colNumMain + 5].SetOptionText(formatTag + s + formatUntag);
                    gridContents[(i + 1) * colNumMain + 6].SetOptionText(formatTag + Core.PrefixFormat(khs.Dose, 3) + (khs.Radiation != 0 ? " (" + Core.PrefixFormat(khs.Radiation, 3, true) + "/day)" : "") + formatUntag);
                }
            }
            else  // Showing details for one particular kerbal
            {
                ProtoCrewMember pcm = selectedKHS.PCM;
                if (pcm == null)
                {
                    selectedKHS = null;
                    Invalidate();
                }
                bool healthFrozen = selectedKHS.IsFrozen || selectedKHS.IsDecontaminating;
                gridContents[1].SetOptionText("<color=\"white\">" + selectedKHS.Name + "</color>");
                gridContents[3].SetOptionText("<color=\"white\">" + pcm.experienceLevel + " " + pcm.trait + "</color>");
                gridContents[5].SetOptionText("<color=\"white\">" + selectedKHS.ConditionString + "</color>");
                string s = "";
                foreach (Quirk q in selectedKHS.Quirks)
                    if (q.IsVisible) s += ((s != "") ? ", " : "") + q.Title;
                if (s == "") s = Localizer.Format("#KH_HM_DNone");//None
                gridContents[7].SetOptionText("<color=\"white\">" + s + "</color>");
                gridContents[9].SetOptionText("<color=\"white\">" + selectedKHS.MaxHP.ToString("F2") + "</color>");
                gridContents[11].SetOptionText("<color=\"white\">" + selectedKHS.HP.ToString("F2") + " (" + selectedKHS.Health.ToString("P2") + ")" + "</color>");
                gridContents[13].SetOptionText("<color=\"white\">" + (healthFrozen ? "—" : selectedKHS.LastChangeTotal.ToString("F2")) + "</color>");
                int i = 15;
                if (Core.IsKerbalLoaded(selectedKHS.PCM) && !healthFrozen)
                    foreach (HealthFactor f in Core.Factors)
                    {
                        gridContents[i].SetOptionText("<color=\"white\">" + (selectedKHS.Factors.ContainsKey(f.Name) ? selectedKHS.Factors[f.Name].ToString("F2") : "N/A") + "</color>");
                        i += 2;
                    }
                gridContents[i].children[0].SetOptionText("<color=\"white\">" + (((selectedKHS.PCM.rosterStatus == ProtoCrewMember.RosterStatus.Assigned) || (selectedKHS.TrainingVessel != null)) ? ((selectedKHS.TrainingLevel * 100).ToString("N0") + "%/" + (Core.TrainingCap * 100).ToString("N0") + "%") : "N/A") + "</color>");
                gridContents[i + 2].SetOptionText("<color=\"white\">" + (healthFrozen ? "N/A" : (selectedKHS.LastRecuperation.ToString("F1") + "%" + (selectedKHS.LastDecay != 0 ? ("/ " + (-selectedKHS.LastDecay).ToString("F1") + "%") : "") + " (" + selectedKHS.MarginalChange.ToString("F2") + " HP)")) + "</color>");
                gridContents[i + 4].SetOptionText("<color=\"white\">" + selectedKHS.LastExposure.ToString("P1") + "</color>");
                gridContents[i + 6].SetOptionText("<color=\"white\">" + selectedKHS.ShelterExposure.ToString("P1") + "</color>");
                gridContents[i + 8].SetOptionText("<color=\"white\">" + selectedKHS.Radiation.ToString("N0") + "/day</color>");
                gridContents[i + 10].children[0].SetOptionText("<color=\"white\">" + Core.PrefixFormat(selectedKHS.Dose, 6) + "</color>");
                gridContents[i + 12].SetOptionText("<color=\"white\">" + (1 - selectedKHS.RadiationMaxHPModifier).ToString("P2") + "</color>");
            }
            dirty = false;
        }

        public override void OnSave(ConfigNode node)
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.OnSave", Core.LogLevel.Important);
            if (!Core.IsInEditor) UpdateKerbals(true);
            node.AddValue("version", version.ToString());
            node.AddValue("nextEventTime", nextEventTime);
            int i = 0;
            foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
            {
                node.AddNode(khs.ConfigNode);
                i++;
            }
            foreach (RadStorm rs in radStorms)
                if (rs.Target != RadStorm.TargetType.None)
                    node.AddNode(rs.ConfigNode);
            Core.Log("KerbalHealthScenario.OnSave complete. " + i + " kerbal(s) saved.", Core.LogLevel.Important);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!Core.Loaded) Core.LoadConfig();
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.OnLoad", Core.LogLevel.Important);
            version = new Version(Core.GetString(node, "version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            nextEventTime = Core.GetDouble(node, "nextEventTime", Planetarium.GetUniversalTime() + GetNextEventInterval());
            Core.KerbalHealthList.Clear();
            int i = 0;
            foreach (ConfigNode n in node.GetNodes("KerbalHealthStatus"))
            {
                Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
                i++;
            }
            Core.Log("" + i + " kerbal(s) loaded.", Core.LogLevel.Important);
            radStorms.Clear();
            foreach (ConfigNode n in node.GetNodes("RADSTORM"))
                radStorms.Add(new RadStorm(n));
            Core.Log(radStorms.Count + " radstorms loaded.", Core.LogLevel.Important);
            lastUpdated = Planetarium.GetUniversalTime();
        }
    }

    /// <summary>
    /// Class used for ordering vessels in Health Monitor
    /// </summary>
    public class KerbalComparer : Comparer<ProtoCrewMember>
    {
        readonly bool sortByLocation;

        public int CompareLocation(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (x.rosterStatus != ProtoCrewMember.RosterStatus.Assigned) return y.rosterStatus == ProtoCrewMember.RosterStatus.Assigned ? 1 : 0;
            if (y.rosterStatus != ProtoCrewMember.RosterStatus.Assigned) return -1;
            Vessel xv = Core.KerbalVessel(x), yv = Core.KerbalVessel(y);
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (xv.isActiveVessel) return yv.isActiveVessel ? 0 : -1;
                if (yv.isActiveVessel) return 1;
            }
            if (xv.isEVA) return yv.isEVA ? 0 : -1;
            return yv.isEVA ? 1 : string.Compare(xv.vesselName, yv.vesselName, true);
        }

        public override int Compare(ProtoCrewMember x, ProtoCrewMember y)
        {
            if (sortByLocation)
            {
                int l = CompareLocation(x, y);
                return (l != 0) ? l : string.Compare(x.name, y.name, true);
            }
            return string.Compare(x.name, y.name, true);
        }

        public KerbalComparer(bool sortByLocation) => this.sortByLocation = sortByLocation;
    }
}
