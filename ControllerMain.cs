using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using PolyTechFramework;
using System.Linq;
using Poly.Physics.Solver;
using Poly.Math;
using Component = UnityEngine.Component;

namespace ControllerMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInProcess("Poly Bridge 2")]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public class ControllerMain : PolyTechMod
    {
        public const string pluginGuid = "polytech.vehiclecontrollermod";

        public const string pluginName = "Vehicle Controller Mod";

        public const string pluginVerson = "1.1.1";


        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginName, "Enable/Disable Mod");

        public const string VehicleSettingsHeader = "Vehicle Control";
        public ConfigDefinition VehicleControlDef = new ConfigDefinition(VehicleSettingsHeader, "Enable/Disable");
        public ConfigDefinition ChangeTargetDef = new ConfigDefinition(VehicleSettingsHeader, "Change Target");
        public ConfigDefinition DriveTypeDef = new ConfigDefinition(VehicleSettingsHeader, "Driving Mode");
        public ConfigDefinition DriveDef = new ConfigDefinition(VehicleSettingsHeader, "Drive");
        public ConfigDefinition DriveBackDef = new ConfigDefinition(VehicleSettingsHeader, "Drive Backwards");
        public ConfigDefinition BrakesDef = new ConfigDefinition(VehicleSettingsHeader, "Brakes");
        public ConfigDefinition BrakesEnabledDef = new ConfigDefinition(VehicleSettingsHeader, "Brakes Enabled");
        public ConfigDefinition BrakesStrengthDef = new ConfigDefinition(VehicleSettingsHeader, "Brakes Intensity");
        public ConfigDefinition FlipDef = new ConfigDefinition(VehicleSettingsHeader, "Flip");
        public ConfigDefinition JumpDef = new ConfigDefinition(VehicleSettingsHeader, "Jump");
        public ConfigDefinition JumpEnabledDef = new ConfigDefinition(VehicleSettingsHeader, "Jump Enabled");
        public ConfigDefinition AirJumpDef = new ConfigDefinition(VehicleSettingsHeader, "Mid Air Jump");
        public ConfigDefinition JumpStrengthDef = new ConfigDefinition(VehicleSettingsHeader, "Jump Strength");
        public ConfigDefinition JumpModeDef = new ConfigDefinition(VehicleSettingsHeader, "Jump Mode");
        public ConfigDefinition TorqueEnabledDef = new ConfigDefinition(VehicleSettingsHeader, "Torque Enabled");
        public ConfigDefinition TorqueStrengthDef = new ConfigDefinition(VehicleSettingsHeader, "Torque Strength");

        public const string PhaseSettingsHeader = "Phase Control";
        public ConfigDefinition PhaseControlDef = new ConfigDefinition(PhaseSettingsHeader, "Enable/Disable");
        public ConfigDefinition StartPhaseDef = new ConfigDefinition(PhaseSettingsHeader, "Start Phase");

        public const string HydroSettingsHeader = "Hydraulic Control";
        public ConfigDefinition HydroControlDef = new ConfigDefinition(HydroSettingsHeader, "Enable/Disable");
        public ConfigDefinition HydroTypeDef = new ConfigDefinition(HydroSettingsHeader, "Hydraulic Control Type");
        public ConfigDefinition ActivateHydroDef = new ConfigDefinition(HydroSettingsHeader, "Activate Hydraulics");

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ConfigEntry<bool> mEnabled;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ConfigEntry<bool> mVehicleControl;

        public ConfigEntry<KeyboardShortcut> mChangeTarget;
        public Vehicle Target;
        public PolyPhysics.Rigidbody[] TargetBodies;
        public PolyPhysics.Rigidbody[] TargetChassis;
        public List<PolyPhysics.Rigidbody> AllBodies;
        public int TargetIndex = 0;

        public ConfigEntry<DriveType> mDriveType;
        public ConfigEntry<KeyboardShortcut> mDrive;
        public bool DriveDown = false;
        public ConfigEntry<KeyboardShortcut> mDriveBack;
        public bool DriveBackDown = false;

        //public ConfigEntry<KeyboardShortcut> mBrakes;
        //public ConfigEntry<bool> mBrakesEnabled;
        //public ConfigEntry<float> mBrakesStrength;
        //public float TargetBrakingMultiplier = 0f;
        //public bool BrakesDown = false;

        public ConfigEntry<KeyboardShortcut> mFlip;
        public List<Vehicle> FlippedVehicles = new List<Vehicle>();

        public ConfigEntry<bool> mJumpEnabled;
        public ConfigEntry<bool> mAirJump;
        public ConfigEntry<KeyboardShortcut> mJump;
        public bool AddJump = false;
        public ConfigEntry<float> mJumpStrength;
        public ConfigEntry<JumpMode> mJumpMode;
        //public float LastDistance = 0f;
        //public float EvenMoreLastDistance = 0f;

        public ConfigEntry<bool> mTorqueEnabled;
        public ConfigEntry<float> mTorqueStrength;

        public PolyPhysics.WorldCollisionOutput CollisionsOutput;
        public FastList<PolyPhysics.CollisionInfo> infos = new FastList<PolyPhysics.CollisionInfo>();
        public FastList<Poly.Physics.CollisionEvent> events = new FastList<Poly.Physics.CollisionEvent>();

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ConfigEntry<bool> mPhaseControl;

        public ConfigEntry<KeyboardShortcut> mStartPhase;
        public bool PhaseTyping = false;
        public int SelectedPhase = 0;
        public string[] StageLabelLookupTable = new string[]
        {
            "A",
            "B",
            "C",
            "D",
            "E",
            "F",
            "G",
            "H",
            "I",
            "J",
            "K",
            "L",
            "M",
            "N",
            "O",
            "P",
            "Q",
            "R",
            "S",
            "T",
            "U",
            "V",
            "W",
            "X",
            "Y",
            "Z"
        };

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ConfigEntry<bool> mHydroControl;

        //public ConfigEntry<HydroType> mHydroType;

        public ConfigEntry<KeyboardShortcut> mActivateHydro;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static ControllerMain instance;

        public bool InUpdate = false;

        public bool ChangingState = false;

        public bool LoadBodies = false;

        public const int SettingsVersion = 1;

        public enum DriveType
        {
            WithOrientation,
            Directional
        }

        public enum JumpMode
        {
            AlwaysUp,
            Directional
        }

        public enum HydroType
        {
            [Description("Only when button is pressed")]
            Button,
            [Description("Only when clicked")]
            Clickable,
            [Description("When clicked or button is pressed")]
            ButtonAndClickable
        }

        void Awake()
        {
            if (instance == null) instance = this;
            repositoryUrl = "https://github.com/Bram2323/PB-Vehicle-Controller-Mod/";
            authors = new string[] { "Bram2323", "Masonator" };

            int order = 0;

            mEnabled = Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = order }));
            mEnabled.SettingChanged += onEnableDisable;
            order--;



            mVehicleControl = Config.Bind(VehicleControlDef, true, new ConfigDescription("If your able to control vehicles", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mChangeTarget = Config.Bind(ChangeTargetDef, new KeyboardShortcut(KeyCode.Tab), new ConfigDescription("What button changes the selected vehicle", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mDriveType = Config.Bind(DriveTypeDef, DriveType.WithOrientation, new ConfigDescription("If \"With Orientation\", forward and reverse depend on which way the vehicle is flipped.\nIf \"Directional\", forward is always to the right and reverse is always to the left", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mDrive = Config.Bind(DriveDef, new KeyboardShortcut(KeyCode.D), new ConfigDescription("Drives forward if driving type is set to \"With Orientation\".\nDrives to the right if driving type is set to \"Directional\"", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mDriveBack = Config.Bind(DriveBackDef, new KeyboardShortcut(KeyCode.A), new ConfigDescription("Drives in reverse if driving type is set to \"With Orientation\".\nDrives to the left if driving type is set to \"Directional\"", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            //mBrakes = Config.Bind(BrakesDef, new KeyboardShortcut(KeyCode.LeftShift), new ConfigDescription("Makes the car brake", null, new ConfigurationManagerAttributes { Order = order }));
            //order--;

            //mBrakesEnabled = Config.Bind(BrakesEnabledDef, true, new ConfigDescription("Enable/Disable braking", null, new ConfigurationManagerAttributes { Order = order }));
            //order--;

            //mBrakesStrength = Config.Bind(BrakesStrengthDef, 1f, new ConfigDescription("The intensity of the brakes", null, new ConfigurationManagerAttributes { Order = order }));
            //order--;

            mFlip = Config.Bind(FlipDef, new KeyboardShortcut(KeyCode.S), new ConfigDescription("Makes the car flip", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mJumpEnabled = Config.Bind(JumpEnabledDef, true, new ConfigDescription("Enable/Disable jumping", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mAirJump = Config.Bind(AirJumpDef, false, new ConfigDescription("Controls if you can jump mid air", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mJump = Config.Bind(JumpDef, new KeyboardShortcut(KeyCode.W), new ConfigDescription("Makes the car jump", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mJumpStrength = Config.Bind(JumpStrengthDef, 1f, new ConfigDescription("How strong a jump is", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mJumpMode = Config.Bind(JumpModeDef, JumpMode.AlwaysUp, new ConfigDescription("Which direction the jump will be applied", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mTorqueEnabled = Config.Bind(TorqueEnabledDef, true, new ConfigDescription("Enable/Disable Torque", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mTorqueStrength = Config.Bind(TorqueStrengthDef, 1f, new ConfigDescription("How strong the mid air torque is when driving", null, new ConfigurationManagerAttributes { Order = order }));
            order--;



            mPhaseControl = Config.Bind(PhaseControlDef, true, new ConfigDescription("If your able to control wich phases start and when they start", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mStartPhase = Config.Bind(StartPhaseDef, new KeyboardShortcut(KeyCode.Q), new ConfigDescription("Starts a phase based on the numbers you type while you press the key", null, new ConfigurationManagerAttributes { Order = order }));
            order--;



            mHydroControl = Config.Bind(HydroControlDef, true, new ConfigDescription("If your able to activate hydraulics (One hyrdaulic phase has to exist for this to work)", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mActivateHydro = Config.Bind(ActivateHydroDef, new KeyboardShortcut(KeyCode.E), new ConfigDescription("What button activates all the hydraulics", null, new ConfigurationManagerAttributes { Order = order }));
            order--;


            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            isCheat = true;
            isEnabled = mEnabled.Value;

            PolyTechMain.registerMod(this);
        }


        public void onEnableDisable(object sender, EventArgs e)
        {
            isEnabled = mEnabled.Value;
        }


        public override void enableMod()
        {
            mEnabled.Value = true;
            onEnableDisable(null, null);
        }

        public override void disableMod()
        {
            mEnabled.Value = false;
            onEnableDisable(null, null);
        }

        public override string getSettings()
        {
            SettingsObj settings = new SettingsObj();

            settings.version = SettingsVersion;

            settings.VehicleControl = mVehicleControl.Value;
            settings.JumpEnabled = mJumpEnabled.Value;
            settings.MidAirJump = mAirJump.Value;
            settings.JumpStrength = mJumpStrength.Value;
            settings.JumpMode = mJumpMode.Value;
            settings.TorqueEnabled = mTorqueEnabled.Value;
            settings.TorqueStrength = mTorqueStrength.Value;

            settings.PhaseControl = mPhaseControl.Value;

            settings.HydroControl = mHydroControl.Value;

            string json = JsonUtility.ToJson(settings);

            return json;
        }

        public override void setSettings(string st)
        {
            try
            {
                SettingsObj settings = JsonUtility.FromJson<SettingsObj>(st);

                int version = settings.version;

                if (version > SettingsVersion)
                {
                    PopUpWarning.Display("Layout was created with newer version of vehicle controller mod!\nSome settings may be lost!");
                }

                mVehicleControl.Value = settings.VehicleControl;

                mVehicleControl.Value = settings.VehicleControl;
                mJumpEnabled.Value = settings.JumpEnabled;
                mAirJump.Value = settings.MidAirJump;
                mJumpStrength.Value = settings.JumpStrength;
                mJumpMode.Value = settings.JumpMode;
                mTorqueEnabled.Value = settings.TorqueEnabled;
                mTorqueStrength.Value = settings.TorqueStrength;

                mPhaseControl.Value = settings.PhaseControl;

                mHydroControl.Value = settings.HydroControl;
            }
            catch
            {
                string[] Settings = st.Split('|');
                if (Settings.Length > 0 && float.TryParse(Settings[0], out float fl))
                {
                    mJumpStrength.Value = fl;
                }
                else
                {
                    Debug.Log("Something went wrong while trying to set setting JumpStrength for Vehicle Controller Mod");
                }
                if (Settings.Length > 1 && Enum.TryParse(Settings[1], out JumpMode jm))
                {
                    mJumpMode.Value = jm;
                }
                else
                {
                    Debug.Log("Something went wrong while trying to set setting JumpMode for Vehicle Controller Mod");
                }
            }
        }

        public bool CheckForCheating()
        {
            return mEnabled.Value && PolyTechMain.modEnabled.Value;
        }


        private void Update()
        {
            if (!CheckForCheating()) return;

            if (GameStateManager.GetState() == GameState.SIM)
            {
                if (mVehicleControl.Value && mChangeTarget.Value.IsDown())
                {
                    InUpdate = true;

                    if (Target)
                    {
                        //Target.Physics.brakingForceMultiplier = TargetBrakingMultiplier;
                        Target.SetPhysicsVehicleTargetSpeed(0f, true);
                    }

                    TargetIndex++;
                    if (TargetIndex > Vehicles.m_Vehicles.Count) TargetIndex = 0;

                    Target = null;
                    TargetBodies = null;
                    if (TargetIndex != 0)
                    {
                        Target = Vehicles.m_Vehicles[TargetIndex - 1];
                        if (Target)
                        {
                            Target.SetPhysicsVehicleTargetSpeed(0f, true);
                            LoadBodies = true;
                            Target.Physics.Execute();
                            //TargetBrakingMultiplier = Target.Physics.brakingForceMultiplier;
                        }
                    }
                }

                InUpdate = true;

                if (mVehicleControl.Value && Target && Target.Physics)
                {
                    LoadBodies = true;
                    Target.Physics.Execute();

                    if (mJumpEnabled.Value && mJump.Value.IsDown()) AddJump = true;

                    if (mFlip.Value.IsDown())
                    {
                        Target.PhysicsVehicleFlip();
                        if (FlippedVehicles.Contains(Target)) FlippedVehicles.Remove(Target);
                        else FlippedVehicles.Add(Target);
                    }

                    if (DriveDown != (mDrive.Value.IsPressed() && !mDriveBack.Value.IsPressed()))
                    {
                        if (mDriveType.Value == DriveType.WithOrientation)
                        {
                            if (mDrive.Value.IsPressed()) Target.SetPhysicsVehicleTargetSpeed(Target.m_TargetSpeed, true);
                            else Target.SetPhysicsVehicleTargetSpeed(0f, true);
                        }
                        else
                        {
                            if (mDrive.Value.IsPressed())
                            {
                                int flipmod;
                                if (FlippedVehicles.Contains(Target)) flipmod = -1;
                                else flipmod = 1;
                                Target.SetPhysicsVehicleTargetSpeed(Target.m_TargetSpeed * flipmod, true);
                            }
                            else Target.SetPhysicsVehicleTargetSpeed(0f, true);
                        }
                    }
                    DriveDown = mDrive.Value.IsPressed() && !mDriveBack.Value.IsPressed();

                    if (DriveBackDown != (mDriveBack.Value.IsPressed() && !mDrive.Value.IsPressed()))
                    {
                        if (mDriveType.Value == DriveType.WithOrientation)
                        {
                            if (mDriveBack.Value.IsPressed()) Target.SetPhysicsVehicleTargetSpeed(-Target.m_TargetSpeed, true);
                            else Target.SetPhysicsVehicleTargetSpeed(0f, true);
                        }
                        else
                        {
                            if (mDriveBack.Value.IsPressed())
                            {
                                int flipmod;
                                if (FlippedVehicles.Contains(Target)) flipmod = 1;
                                else flipmod = -1;
                                Target.SetPhysicsVehicleTargetSpeed(Target.m_TargetSpeed * flipmod, true);
                            }
                            else Target.SetPhysicsVehicleTargetSpeed(0f, true);
                        }
                    }
                    DriveBackDown = mDriveBack.Value.IsPressed() && !mDrive.Value.IsPressed();

                    /*
                    if (BrakesDown != (mBrakes.Value.IsPressed() && !DriveDown && !DriveBackDown && mBrakesEnabled.Value))
                    {
                        if (BrakesDown)
                        {
                            Target.Physics.idleOnDownhill = false;
                            Target.Physics.brakingForceMultiplier = mBrakesStrength.Value;
                        }
                        else
                        {
                            Target.Physics.brakingForceMultiplier = TargetBrakingMultiplier;
                        }
                    }
                    BrakesDown = mBrakes.Value.IsPressed() && !DriveDown && !DriveBackDown && mBrakesEnabled.Value;
                    */
                }

                if (mPhaseControl.Value)
                {
                    if (mStartPhase.Value.IsPressed())
                    {
                        if (!PhaseTyping) SelectedPhase = 0;

                        for (int i = 0; i < 10; i++)
                        {
                            KeyCode key = (KeyCode)(48 + i);
                            if (Input.GetKeyDown(key))
                            {
                                SelectedPhase *= 10;
                                SelectedPhase += i;
                            }
                        }
                        GameUI.m_Instance.m_TopBar.m_MessageTopCenter.ShowMessage(string.Format("Phase: {0} ({1})", SelectedPhase, FormatStageLabel(SelectedPhase - 1)), 0.1f);
                        PhaseTyping = true;
                    }
                    else if (PhaseTyping)
                    {
                        int phaseNum = SelectedPhase;
                        SelectedPhase--;
                        if (SelectedPhase >= 0)
                        {
                            if (EventTimelines.m_Timelines.Count > 0)
                            {
                                for (int i = 0; i < EventTimelines.m_Timelines.Count; i++)
                                {
                                    EventTimeline timeline = EventTimelines.m_Timelines[i];
                                    if (SelectedPhase < timeline.m_Stages.Count)
                                    {
                                        for (int j = 0; j < timeline.m_Stages[SelectedPhase].m_Units.Count; j++)
                                        {
                                            EventStage stage = timeline.m_Stages[SelectedPhase];
                                            EventUnit unit = stage.m_Units[j];
                                            switch (unit.m_Type)
                                            {
                                                case EventUnitType.HYDRAULICS_PHASE:
                                                case EventUnitType.VEHICLE:
                                                case EventUnitType.VEHICLE_RESTART_PHASE:
                                                    unit.StartSimulation();
                                                    break;
                                                case EventUnitType.ZED_AXIS_VEHICLE:
                                                    ZedAxisVehicle boat = unit.GetZedAxisVehicle();
                                                    boat.StopLoopSoundImmediate();
                                                    boat.EndSimulation();
                                                    boat.EnablePhysics();
                                                    boat.StartSimulation();
                                                    break;
                                                default:
                                                    break;
                                            }
                                            GameUI.m_Instance.m_TopBar.m_MessageTopCenter.ShowMessage(string.Format("Activated phase: {0} ({1})", phaseNum, FormatStageLabel(stage.m_AbsoluteStageIndex)), 3);
                                        }
                                        break;
                                    }
                                    else SelectedPhase -= timeline.m_Stages.Count;
                                }
                            }
                        }

                        SelectedPhase = 0;
                        PhaseTyping = false;
                    }
                }

                if (mHydroControl.Value)
                {
                    if (mActivateHydro.Value.IsDown())
                    {
                        PolyPhysics.HydraulicController.instance.Activate();
                    }
                }
            }

            InUpdate = false;
        }


        [HarmonyPatch(typeof(Vehicle), "SetPhysicsVehicleTargetSpeed")]
        private static class patchSetTargetSpeed
        {
            private static bool Prefix(Vehicle __instance, float speed)
            {
                if (!instance.CheckForCheating() || !instance.mVehicleControl.Value) return true;


                if (__instance != instance.Target)
                {
                    if (speed != 0f) return false;
                }
                else if (!instance.InUpdate)
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Vehicle), "PhysicsVehicleFlip")]
        private static class patchFlipVehicle
        {
            private static bool Prefix(Vehicle __instance)
            {
                if (!instance.CheckForCheating() || !instance.mVehicleControl.Value) return true;


                if (__instance != instance.Target || !instance.InUpdate)
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(PolyPhysics.Vehicle), "OnValidate")]
        private static class patchValidate
        {
            private static void Postfix(PolyPhysics.Vehicle __instance, PolyPhysics.Rigidbody[] ___allBodies)
            {
                if (!instance.CheckForCheating() || !instance.mVehicleControl.Value) return;

                __instance.targetVelocity = 0f;
            }
        }

        [HarmonyPatch(typeof(PolyPhysics.Vehicle), "Execute")]
        private static class patchExecute
        {
            private static void Postfix(PolyPhysics.Vehicle __instance, PolyPhysics.Rigidbody[] ___allBodies, PolyPhysics.Rigidbody[] ___chassis)
            {
                if (!instance.CheckForCheating()) return;

                if (instance.LoadBodies)
                {
                    instance.TargetBodies = ___allBodies;
                    instance.TargetChassis = ___chassis;
                }

                instance.LoadBodies = false;
            }
        }

        [HarmonyPatch(typeof(Solver), "Solve")]
        private static class patchSolve
        {
            private static void Postfix(FastList<Motion> motionsAL, List<PolyPhysics.Rigidbody> bodies)
            {
                if (!instance.CheckForCheating()) return;

                instance.AllBodies = bodies;
            }
        }

        [HarmonyPatch(typeof(PolyPhysics.Collide), "DetectCollisions")]
        private static class patchDetectCollisions
        {
            private static void Postfix(PolyPhysics.WorldCollisionOutput output)
            {
                if (!instance.CheckForCheating()) return;

                instance.CollisionsOutput = output;
            }
        }

        [HarmonyPatch(typeof(PolyPhysics.PersistentCollisionCache), "UpdateCachesFromCollisionInfos_Rigidbodies")]
        private static class patchUpdateCashes
        {
            private static void Postfix(FastList<PolyPhysics.CollisionInfo> collisionInfos, FastList<Poly.Physics.CollisionEvent> collisionEvents)
            {
                if (!instance.CheckForCheating()) return;

                instance.infos = collisionInfos;
                instance.events = collisionEvents;
            }
        }

        [HarmonyPatch(typeof(Solver), "IntegrateMotions")]
        private static class patchIntegrateMotions
        {
            private static void Postfix(Motion[] motionsPtr, float deltaTime)
            {
                if (!instance.CheckForCheating() || !instance.mVehicleControl.Value) return;

                instance.InUpdate = true;

                if (instance.TargetBodies != null && motionsPtr != null && instance.AllBodies != null
                    && instance.TargetChassis != null && instance.TargetChassis.Length > 0 && instance.TargetChassis[0] != null)
                {
                    double angle = instance.TargetChassis[0].transform.rotation.eulerAngles.z * (Math.PI / 180);
                    Vec2 Jump = new Vec2(0, 0);
                    if (instance.mJumpMode.Value == JumpMode.AlwaysUp)
                    {
                        Jump = new Vec2(0f, instance.mJumpStrength.Value / 100 * 4);
                    }
                    else if (instance.mJumpMode.Value == JumpMode.Directional)
                    {
                        Jump = new Vec2(
                            instance.mJumpStrength.Value * Mathf.Sin((float)angle) * -1,
                            instance.mJumpStrength.Value * Mathf.Cos((float)angle)
                            ) / 100 * 4;
                    }

                    bool inAir = true;

                    if (instance.mTorqueEnabled.Value || instance.mJumpEnabled.Value)
                    {

                        List<int> eventIndexs = new List<int>();
                        foreach (PolyPhysics.CollisionInfo info in instance.infos.array)
                        {
                            if (info.sumVelImpulses_InFrame != 0f || info.sumFrictionImpulses_InFrame != 0f)
                            {
                                if (info.collisionEventIdx >= 0) eventIndexs.Add(info.collisionEventIdx);
                            }
                        }

                        foreach (int index in eventIndexs)
                        {
                            if (index < instance.events.Count)
                            {
                                Poly.Physics.CollisionEvent collisionEvent = instance.events[index];

                                Component compA;
                                Component compB;
                                try
                                {
                                    compA = collisionEvent.a.Value.GetUnityComponent();
                                }
                                catch { compA = null; }
                                try
                                {
                                    compB = collisionEvent.b.Value.GetUnityComponent();
                                }
                                catch { compB = null; }

                                foreach (PolyPhysics.Rigidbody body in instance.TargetBodies)
                                {
                                    if (body.gameObject && body.name.ToLower().Contains("wheel"))
                                    {
                                        //float lastDis = instance.LastDistance;
                                        //float evenLastDis = instance.EvenMoreLastDistance;

                                        if (compA && compA.gameObject && body.gameObject == compA.gameObject)
                                        {
                                            float dis = collisionEvent.point0.distance;
                                            if (dis < 0.01f) // && !(lastDis == dis && lastDis == evenLastDis)
                                            {
                                                //instance.LastDistance = dis;
                                                //instance.EvenMoreLastDistance = lastDis;
                                                inAir = false;
                                                break;
                                            }
                                        }
                                        else if (compB && compB.gameObject && body.gameObject == compB.gameObject)
                                        {
                                            float dis = collisionEvent.point0.distance;
                                            if (dis < 0.01f) // && !(lastDis == dis && lastDis == evenLastDis)
                                            {
                                                //instance.LastDistance = dis;
                                                //instance.EvenMoreLastDistance = lastDis;
                                                inAir = false;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }



                    foreach (PolyPhysics.Rigidbody body in instance.TargetBodies)
                    {
                        int index = instance.AllBodies.IndexOf(body);

                        if (index >= 0 && index < motionsPtr.Count())
                        {
                            ref Motion motion = ref motionsPtr[index];

                            if ((!inAir || instance.mAirJump.Value) && instance.AddJump && instance.mJumpEnabled.Value)
                            {
                                motion.linVel += Jump;
                            }

                            if (inAir && instance.mTorqueEnabled.Value)
                            {
                                float vel = 0;
                                float accVel = instance.mTorqueStrength.Value / 80000 * 4;
                                if (instance.mDrive.Value.IsPressed() && !instance.mDriveBack.Value.IsPressed()) vel = accVel;
                                else if (instance.mDriveBack.Value.IsPressed() && !instance.mDrive.Value.IsPressed()) vel = -accVel;

                                if (vel != 0f) motion.angVel += vel;
                            }
                        }
                    }

                    instance.AddJump = false;
                }

                instance.InUpdate = false;
            }
        }

        [HarmonyPatch(typeof(GameStateSim), "StartSimulation")]
        private static class patchStartSim
        {
            private static void Postfix()
            {
                if (!instance.CheckForCheating() || GameStateManager.GetState() != GameState.SIM) return;
                if (instance.TargetIndex > Vehicles.m_Vehicles.Count) instance.TargetIndex = 0;
                instance.FlippedVehicles.Clear();
                foreach (Vehicle vehicle in Vehicles.m_Vehicles)
                {
                    if (vehicle.m_Flipped) instance.FlippedVehicles.Add(vehicle);
                }
            }
        }



        [HarmonyPatch(typeof(EventStage), "StartSimulation")]
        private static class patchEventStage
        {
            private static bool Prefix()
            {
                if (!instance.CheckForCheating() || !instance.mPhaseControl.Value) return true;
                else return false;
            }
        }

        public string FormatStageLabel(int stageNumber)
        {
            if (stageNumber < 0) return "Out Of Bounds!";
            int num = stageNumber / 26;
            stageNumber %= 26;
            if (num == 0) return StageLabelLookupTable[stageNumber];
            else if (num == 1) return StageLabelLookupTable[num - 1] + StageLabelLookupTable[stageNumber];
            else return "Out Of Bounds!";
        }


        [HarmonyPatch(typeof(KeyboardShortcut), "ModifierKeyTest")]
        private static class patchKeyBinds
        {
            private static void Postfix(ref bool __result, KeyboardShortcut __instance)
            {
                if (!instance.CheckForCheating() || !instance.InUpdate) return;
                KeyCode mainKey = __instance.MainKey;
                __result = __instance.Modifiers.All((KeyCode c) => c == mainKey || Input.GetKey(c));
            }
        }
    }


    [Serializable]
    public class SettingsObj
    {
        public int version = 0;

        public bool VehicleControl = true;
        public bool JumpEnabled = true;
        public bool MidAirJump = false;
        public float JumpStrength = 1f;
        public ControllerMain.JumpMode JumpMode = ControllerMain.JumpMode.AlwaysUp;
        public bool TorqueEnabled = true;
        public float TorqueStrength = 1f;

        public bool PhaseControl = false;

        public bool HydroControl = false;
    }



    /// <summary>
    /// Class that specifies how a setting should be displayed inside the ConfigurationManager settings window.
    /// 
    /// Usage:
    /// This class template has to be copied inside the plugin's project and referenced by its code directly.
    /// make a new instance, assign any fields that you want to override, and pass it as a tag for your setting.
    /// 
    /// If a field is null (default), it will be ignored and won't change how the setting is displayed.
    /// If a field is non-null (you assigned a value to it), it will override default behavior.
    /// </summary>
    /// 
    /// <example> 
    /// Here's an example of overriding order of settings and marking one of the settings as advanced:
    /// <code>
    /// // Override IsAdvanced and Order
    /// Config.AddSetting("X", "1", 1, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));
    /// // Override only Order, IsAdvanced stays as the default value assigned by ConfigManager
    /// Config.AddSetting("X", "2", 2, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 1 }));
    /// Config.AddSetting("X", "3", 3, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 2 }));
    /// </code>
    /// </example>
    /// 
    /// <remarks> 
    /// You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
    /// You can optionally remove fields that you won't use from this class, it's the same as leaving them null.
    /// </remarks>
#pragma warning disable 0169, 0414, 0649
    internal sealed class ConfigurationManagerAttributes
    {
        /// <summary>
        /// Should the setting be shown as a percentage (only use with value range settings).
        /// </summary>
        public bool? ShowRangeAsPercent;

        /// <summary>
        /// Custom setting editor (OnGUI code that replaces the default editor provided by ConfigurationManager).
        /// See below for a deeper explanation. Using a custom drawer will cause many of the other fields to do nothing.
        /// </summary>
        public System.Action<BepInEx.Configuration.ConfigEntryBase> CustomDrawer;

        /// <summary>
        /// Show this setting in the settings screen at all? If false, don't show.
        /// </summary>
        public bool? Browsable;

        /// <summary>
        /// Category the setting is under. Null to be directly under the plugin.
        /// </summary>
        public string Category;

        /// <summary>
        /// If set, a "Default" button will be shown next to the setting to allow resetting to default.
        /// </summary>
        public object DefaultValue;

        /// <summary>
        /// Force the "Reset" button to not be displayed, even if a valid DefaultValue is available. 
        /// </summary>
        public bool? HideDefaultButton;

        /// <summary>
        /// Force the setting name to not be displayed. Should only be used with a <see cref="CustomDrawer"/> to get more space.
        /// Can be used together with <see cref="HideDefaultButton"/> to gain even more space.
        /// </summary>
        public bool? HideSettingName;

        /// <summary>
        /// Optional description shown when hovering over the setting.
        /// Not recommended, provide the description when creating the setting instead.
        /// </summary>
        public string Description;

        /// <summary>
        /// Name of the setting.
        /// </summary>
        public string DispName;

        /// <summary>
        /// Order of the setting on the settings list relative to other settings in a category.
        /// 0 by default, higher number is higher on the list.
        /// </summary>
        public int? Order;

        /// <summary>
        /// Only show the value, don't allow editing it.
        /// </summary>
        public bool? ReadOnly;

        /// <summary>
        /// If true, don't show the setting by default. User has to turn on showing advanced settings or search for it.
        /// </summary>
        public bool? IsAdvanced;

        /// <summary>
        /// Custom converter from setting type to string for the built-in editor textboxes.
        /// </summary>
        public System.Func<object, string> ObjToStr;

        /// <summary>
        /// Custom converter from string to setting type for the built-in editor textboxes.
        /// </summary>
        public System.Func<string, object> StrToObj;
    }
}
