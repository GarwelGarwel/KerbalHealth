using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalHealth
{
    /// <summary>
    /// Main class for processing kerbals' health
    /// </summary>
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT)]
    public class KerbalHealthScenario : ScenarioModule
    {
        static double lastUpdated;  // UT at last health update
        static double nextEventTime;  // UT when (or after) next event check occurs
        Version version;  // Current Kerbal Health version

        ApplicationLauncherButton appLauncherButton;
        IButton toolbarButton;
        SortedList<ProtoCrewMember, KerbalHealthStatus> kerbals;
        bool dirty = false, crewChanged = false;
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
            Core.Log(Core.Factors.Count + " factors initialized.");
            Core.KerbalHealthList.RegisterKerbals();

            GameEvents.onCrewOnEva.Add(OnKerbalEva);
            GameEvents.onCrewKilled.Add(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Add(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Add(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Add(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Add(OnKerbalRemoved);
            GameEvents.onKerbalNameChanged.Add(OnKerbalNameChanged);
            GameEvents.OnProgressComplete.Add(OnProgressComplete);

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

            if (ToolbarManager.ToolbarAvailable && Core.UseBlizzysToolbar)
            {
                Core.Log("Registering Blizzy's Toolbar button...", Core.LogLevel.Important);
                toolbarButton = ToolbarManager.Instance.add("KerbalHealth", "HealthMonitor");
                toolbarButton.Text = "Kerbal Health Monitor";
                toolbarButton.TexturePath = "KerbalHealth/toolbar";
                toolbarButton.ToolTip = "Kerbal Health";
                toolbarButton.OnClick += (e) => { if (monitorWindow == null) DisplayData(); else UndisplayData(); };
            }
            else
            {
                Core.Log("Registering AppLauncher button...", Core.LogLevel.Important);
                Texture2D icon = new Texture2D(38, 38);
                icon.LoadImage(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "icon.png")));
                appLauncherButton = ApplicationLauncher.Instance.AddModApplication(DisplayData, UndisplayData, null, null, null, null, ApplicationLauncher.AppScenes.ALWAYS, icon);
            }
            lastUpdated = Planetarium.GetUniversalTime();
            nextEventTime = lastUpdated + GetNextEventInterval();

            // Automatically updating settings from older versions
            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != v)
            {
                Core.Log("Current mod version " + v + " is different from v" + version + " used to save the game. Most likely, Kerbal Health has been recently updated.", Core.LogLevel.Important);
                if ((version < new Version("1.1.0")) && (HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().ConfinementBaseFactor != -3) && (Planetarium.GetUniversalTime() > 0))
                {
                    Core.Log("Confinement Factor is " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().ConfinementBaseFactor + " instead of -3. Automatically fixing.");
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthFactorsSettings>().ConfinementBaseFactor = -3;
                    Core.ShowMessage("Kerbal Health has been updated to v" + v.ToString(3) + ". Confinement factor value has been reset to -3. It is recommended that you load each crewed vessel briefly to update Kerbal Health cache.", true);
                }
                if (version < new Version("1.2.1.2"))
                {
                    Core.Log("Pre-1.3 radiation settings: " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient.ToString("P0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation.ToString("F0") + " / " + HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation.ToString("F0"));
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().RadiationEffect = 0.1f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceHighCoefficient = 0.3f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().InSpaceLowCoefficient = 0.2f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().StratoCoefficient = 0.2f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().TroposphereCoefficient = 0.01f;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().SolarRadiation = 5000;
                    HighLogic.CurrentGame.Parameters.CustomParams<KerbalHealthRadiationSettings>().GalacticRadiation = 15000;
                    Core.ShowMessage("Kerbal Health has been updated to v" + v.ToString() + ". Radiation settings have been reset. It is recommended that you load each crewed vessel briefly to update Kerbal Health cache.", true);
                }
                version = v;
            }
            else Core.Log("Kerbal Health v" + version);
            Core.Log("KerbalHealthScenario.Start finished.", Core.LogLevel.Important);
        }

        public void OnDisable()
        {
            Core.Log("KerbalHealthScenario.OnDisable", Core.LogLevel.Important);
            UndisplayData();

            GameEvents.onCrewOnEva.Remove(OnKerbalEva);
            GameEvents.onCrewKilled.Remove(OnCrewKilled);
            GameEvents.OnCrewmemberHired.Remove(OnCrewmemberHired);
            GameEvents.OnCrewmemberSacked.Remove(OnCrewmemberSacked);
            GameEvents.onKerbalAdded.Remove(OnKerbalAdded);
            GameEvents.onKerbalRemoved.Remove(OnKerbalRemoved);
            GameEvents.onKerbalNameChange.Remove(OnKerbalNameChanged);
            GameEvents.OnProgressComplete.Remove(OnProgressComplete);
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
        /// Marks the kerbal as being on EVA to apply EVA-only effects
        /// </summary>
        /// <param name="action"></param>
        public void OnKerbalEva(GameEvents.FromToAction<Part, Part> action)
        {
            if (!Core.ModEnabled) return;
            Core.Log(action.to.protoModuleCrew[0].name + " went on EVA from " + action.from.name + ".", Core.LogLevel.Important);
            Core.KerbalHealthList.Find(action.to.protoModuleCrew[0]).IsOnEVA = true;
            UpdateKerbals(true);
        }

        public void OnCrewKilled(EventReport er)
        {
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
            Core.Log("OnKerbalRemoved('" + pcm.name + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Remove(pcm.name);
            dirty = crewChanged = true;
        }

        public void OnKerbalNameChanged(ProtoCrewMember pcm, string name1, string name2)
        {
            Core.Log("OnKerbalNameChanged('" + pcm.name + "', '" + name1 + "', '" + name2 + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Rename(name1, name2);
            dirty = true;
        }

        public void OnKerbalFrozen(Part part, ProtoCrewMember pcm)
        {
            Core.Log("OnKerbalFrozen('" + part.name + "', '" + pcm.name + "')", Core.LogLevel.Important);
            Core.KerbalHealthList.Find(pcm).IsFrozen = true;
            dirty = true;
        }

        public void OnKerbalThaw(Part part, ProtoCrewMember pcm)
        {
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

        /// <summary>
        /// Next event update is scheduled after a random period of time, between 0 and 2 days
        /// </summary>
        /// <returns></returns>
        double GetNextEventInterval() => Core.rand.NextDouble() * KSPUtil.dateTimeFormatter.Day * 2;

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
                Core.KerbalHealthList.Update(timePassed);
                lastUpdated = time;
                if (Core.ConditionsEnabled)
                    while (time >= nextEventTime)  // Can take several turns of event processing at high time warp
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
                        nextEventTime += GetNextEventInterval();
                        Core.Log("Next event processing is scheduled at " + KSPUtil.PrintDateCompact(nextEventTime, true), Core.LogLevel.Important);
                    }
                dirty = true;
            }
        }

        public void FixedUpdate()
        { if (Core.ModEnabled) UpdateKerbals(false); }

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
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Name</color></b>", true));
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Location</color></b>", true));
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Condition</color></b>", true));
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Health</color></b>", true));
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Change/day</color></b>", true));
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Time Left</color></b>", true));
                gridContents.Add(new DialogGUILabel("<b><color=\"white\">Radiation</color></b>", true));
                gridContents.Add(new DialogGUILabel("", true));

                // Initializing Health Monitor's grid with empty labels, to be filled in Update()
                for (int i = FirstLine; i < FirstLine + LineCount; i++)
                {
                    for (int j = 0; j < colNumMain - 1; j++)
                        gridContents.Add(new DialogGUILabel("", true));
                    gridContents.Add(new DialogGUIButton<int>("Details", (n) => { selectedKHS = kerbals.Values[n]; Invalidate(); }, i));
                }
                layout.AddChild(new DialogGUIGridLayout(new RectOffset(0, 0, 0, 0), new Vector2(colWidth, 30), new Vector2(colSpacing, 10), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNumMain, gridContents.ToArray()));
                monitorPosition.width = gridWidthList + 10;
                monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("Health Monitor", "", "Health Monitor", HighLogic.UISkin, monitorPosition, layout), false, HighLogic.UISkin, false);
            }

            else
            {
                // Creating the grid for detailed view, which will be filled in Update method
                Core.Log("Showing details for " + selectedKHS.Name + ".");
                gridContents = new List<DialogGUIBase>();
                gridContents.Add(new DialogGUILabel("Name:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Level:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Condition:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Quirks:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Max HP:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("HP:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("HP Change:"));
                gridContents.Add(new DialogGUILabel(""));
                if (Core.IsKerbalLoaded(selectedKHS.PCM) && !selectedKHS.HasCondition("Frozen"))
                    foreach (HealthFactor f in Core.Factors)
                    {
                        gridContents.Add(new DialogGUILabel(f.Title + ":"));
                        gridContents.Add(new DialogGUILabel(""));
                    }
                gridContents.Add(new DialogGUILabel("Recuperation:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Exposure:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Radiation:"));
                gridContents.Add(new DialogGUILabel(""));
                gridContents.Add(new DialogGUILabel("Lifetime Dose:"));
                gridContents.Add(new DialogGUIHorizontalLayout(
                    TextAnchor.UpperLeft,
                    new DialogGUILabel(""),
                    new DialogGUIButton("Decon", OnDecontamination, 0, 40, false)));
                gridContents.Add(new DialogGUILabel("Rad HP Loss:"));
                gridContents.Add(new DialogGUILabel(""));
                monitorPosition.width = gridWidthDetails + 10;
                monitorWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("Health Monitor", "", "Health Details", HighLogic.UISkin, monitorPosition, new DialogGUIVerticalLayout(new DialogGUIGridLayout(new RectOffset(3, 3, 3, 3), new Vector2(colWidth, 40), new Vector2(colSpacing, 10), UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft, UnityEngine.UI.GridLayoutGroup.Axis.Horizontal, TextAnchor.MiddleCenter, UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount, colNumDetails, gridContents.ToArray()), new DialogGUIButton("Back", () => { selectedKHS = null; Invalidate(); }, gridWidthDetails, 20, false))), false, HighLogic.UISkin, false);
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

        void OnDecontamination()
        {
            if (selectedKHS == null) return;
            string msg = "";
            Callback ok = null;
            if (selectedKHS.IsDecontaminating)
            {
                Core.Log("User ordered to stop decontamination of " + selectedKHS.Name);
                msg = selectedKHS.Name + " is decontaminating. If you stop it, the process will stop and they will slowly regain health.";
                ok = () => { selectedKHS.StopDecontamination(); Invalidate(); };
            }
            else
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) msg += "Your Astronaut Complex has to be level " + Core.DecontaminationAstronautComplexLevel + " and your R&D Facility level " + Core.DecontaminationRNDLevel + " to allow decontamination. ";
                if ((HighLogic.CurrentGame.Mode == Game.Modes.CAREER) || (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX)) msg += "Decontamination will cost " + (HighLogic.CurrentGame.Mode == Game.Modes.CAREER ? (Core.DecontaminationFundsCost.ToString("N0") + " funds and ") : "") + Core.DecontaminationScienceCost.ToString("N0") + " science. ";
                msg += selectedKHS.Name + " needs to be at KSC at 100% health and have no health conditions for the process to start. Their health will be reduced by " + Core.DecontaminationHealthLoss.ToString("P0") + " during decontamination. At a rate of " + Core.DecontaminationRate.ToString("N0") + " banana doses/day, it is expected to take about " + KSPUtil.PrintDateDelta(Math.Ceiling(selectedKHS.Dose / Core.DecontaminationRate) * 21600, false) + ".";
                if (selectedKHS.IsReadyForDecontamination)
                    ok = () => { selectedKHS.StartDecontamination(); Invalidate(); };
                else msg += "\r\n<color=\"red\">You cannot start decontamination now.</color>";
            }
            PopupDialog.SpawnPopupDialog(new MultiOptionDialog("Decontamination", msg, "Decontamination", HighLogic.UISkin, new DialogGUIButton("OK", ok, () => selectedKHS.IsReadyForDecontamination, true), new DialogGUIButton("Cancel", null, true)), false, HighLogic.UISkin);
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
                Core.Log(kerbals.Count + " kerbals in Health Monitor list.");
                // Fill the Health Monitor's grid with kerbals' health data
                for (int i = 0; i < LineCount; i++)
                {
                    KerbalHealthStatus khs = kerbals.Values[FirstLine + i];
                    bool healthFrozen = khs.IsFrozen || khs.IsDecontaminating;
                    double ch = khs.LastChangeTotal;
                    double b = khs.GetBalanceHP();
                    string formatTag = "", formatUntag = "";
                    string s = "";
                    if (healthFrozen || ((b - khs.NextConditionHP()) * ch <= 0)) s = "—";
                    else
                    {
                        s = Core.ParseUT(khs.TimeToNextCondition(), true, 100);
                        if (ch < 0)
                        {
                            if (khs.TimeToNextCondition() < KSPUtil.dateTimeFormatter.Day) formatTag = "<color=\"red\">";
                            else formatTag = "<color=\"orange\">";
                            formatUntag = "</color>";
                        }
                    }
                    gridContents[(i + 1) * colNumMain].SetOptionText(formatTag + khs.Name + formatUntag);
                    gridContents[(i + 1) * colNumMain + 1].SetOptionText(formatTag + khs.LocationString + formatUntag);
                    gridContents[(i + 1) * colNumMain + 2].SetOptionText(formatTag + khs.ConditionString + formatUntag);
                    gridContents[(i + 1) * colNumMain + 3].SetOptionText(formatTag + (100 * khs.Health).ToString("F2") + "% (" + khs.HP.ToString("F2") + ")" + formatUntag);
                    gridContents[(i + 1) * colNumMain + 4].SetOptionText(formatTag + ((healthFrozen || (khs.Health >= 1)) ? "—" : (((ch > 0) ? "+" : "") + ch.ToString("F2"))) + formatUntag);
                    gridContents[(i + 1) * colNumMain + 5].SetOptionText(formatTag + s + formatUntag);
                    gridContents[(i + 1) * colNumMain + 6].SetOptionText(formatTag + Core.PrefixFormat(khs.Dose, 5) + (khs.Radiation != 0 ? " (" + Core.PrefixFormat(khs.Radiation, 4, true) + "/day)" : "") + formatUntag);
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
                gridContents[3].SetOptionText("<color=\"white\">" + pcm.experienceLevel + "</color>");
                gridContents[5].SetOptionText("<color=\"white\">" + selectedKHS.ConditionString + "</color>");
                string s = "";
                foreach (Quirk q in selectedKHS.Quirks)
                    if (q.IsVisible) s += ((s != "") ? ", " : "") + q.Title;
                if (s == "") s = "None";
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
                gridContents[i].SetOptionText("<color=\"white\">" + (healthFrozen ? "N/A" : (selectedKHS.LastRecuperation.ToString("F1") + "%" + (selectedKHS.LastDecay != 0 ? ("/ " + (-selectedKHS.LastDecay).ToString("F1") + "%") : "") + " (" + selectedKHS.MarginalChange.ToString("F2") + " HP)")) + "</color>");
                gridContents[i + 2].SetOptionText("<color=\"white\">" + selectedKHS.LastExposure.ToString("P2") + "</color>");
                gridContents[i + 4].SetOptionText("<color=\"white\">" + selectedKHS.Radiation.ToString("N0") + "/day</color>");
                gridContents[i + 6].children[0].SetOptionText("<color=\"white\">" + selectedKHS.Dose.ToString("N0") + "</color>");
                gridContents[i + 8].SetOptionText("<color=\"white\">" + (1 - selectedKHS.RadiationMaxHPModifier).ToString("P2") + "</color>");
            }
            dirty = false;
        }

        public override void OnSave(ConfigNode node)
        {
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.OnSave", Core.LogLevel.Important);
            UpdateKerbals(true);
            node.AddValue("version", version.ToString());
            node.AddValue("nextEventTime", nextEventTime);
            int i = 0;
            foreach (KerbalHealthStatus khs in Core.KerbalHealthList.Values)
            {
                Core.Log("Saving " + khs.Name + "'s health.");
                node.AddNode(khs.ConfigNode);
                i++;
            }
            Core.Log("KerbalHealthScenario.OnSave complete. " + i + " kerbal(s) saved.", Core.LogLevel.Important);
        }

        public override void OnLoad(ConfigNode node)
        {
            if (!Core.Loaded) Core.LoadConfig();
            if (!Core.ModEnabled) return;
            Core.Log("KerbalHealthScenario.OnLoad", Core.LogLevel.Important);
            version = new Version(Core.GetString(node, "version", "0.0"));
            nextEventTime = Core.GetDouble(node, "nextEventTime", Planetarium.GetUniversalTime() + GetNextEventInterval());
            Core.KerbalHealthList.Clear();
            int i = 0;
            foreach (ConfigNode n in node.GetNodes("KerbalHealthStatus"))
            {
                Core.KerbalHealthList.Add(new KerbalHealthStatus(n));
                i++;
            }
            lastUpdated = Planetarium.GetUniversalTime();
            Core.Log("" + i + " kerbal(s) loaded.", Core.LogLevel.Important);
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
