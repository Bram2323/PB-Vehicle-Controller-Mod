using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using PolyTechFramework;
using System.Linq;
using Poly.Physics.Solver;
using Poly.Math;

namespace ControllerMod
{
    [BepInPlugin(pluginGuid, pluginName, pluginVerson)]
    [BepInProcess("Poly Bridge 2")]
    [BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
    public class ControllerMain : PolyTechMod
    {

        public const string pluginGuid = "polytech.vehiclecontrollermod";

        public const string pluginName = "Vehicle Controller Mod";

        public const string pluginVerson = "1.0.0";

        public ConfigDefinition modEnableDef = new ConfigDefinition(pluginName, "Enable/Disable Mod");
        public ConfigDefinition ChangeTargetDef = new ConfigDefinition(pluginName, "Change Target");
        public ConfigDefinition DriveDef = new ConfigDefinition(pluginName, "Drive");
        public ConfigDefinition DriveBackDef = new ConfigDefinition(pluginName, "Drive Backwards");
        public ConfigDefinition JumpDef = new ConfigDefinition(pluginName, "Jump");
        public ConfigDefinition FlipDef = new ConfigDefinition(pluginName, "Flip");
        public ConfigDefinition JumpStrengthDef = new ConfigDefinition(pluginName, "Jump Strength");
        public ConfigDefinition JumpModeDef = new ConfigDefinition(pluginName, "Jump Mode");

        public ConfigEntry<bool> mEnabled;

        public ConfigEntry<KeyboardShortcut> mChangeTarget;
        public bool ChangeTargetDown = false;
        public Vehicle Target;
        public PolyPhysics.Rigidbody[] TargetBodies;
        public PolyPhysics.Rigidbody[] TargetChassis;
        public List<PolyPhysics.Rigidbody> AllBodies;
        public int TargetIndex = 0;

        public ConfigEntry<KeyboardShortcut> mDrive;
        public bool DriveDown = false;
        public ConfigEntry<KeyboardShortcut> mDriveBack;
        public bool DriveBackDown = false;

        public ConfigEntry<KeyboardShortcut> mJump;
        public bool AddJump = false;

        public ConfigEntry<KeyboardShortcut> mFlip;

        public ConfigEntry<float> mJumpStrength;

        public ConfigEntry<JumpMode> mJumpMode;

        public static ControllerMain instance;

        public bool InUpdate = false;

        public bool ChangingState = false;

        public bool LoadBodies = false;

        public enum JumpMode
        {
            AlwaysUp,
            Directional
        }

        void Awake()
        {
            if (instance == null) instance = this;
            repositoryUrl = "https://github.com/Bram2323/PB-Vehicle-Controller-Mod/";
            authors = new string[] { "Bram2323" };

            int order = 0;

            Config.Bind(modEnableDef, true, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes { Order = order }));
            mEnabled = (ConfigEntry<bool>)Config[modEnableDef];
            mEnabled.SettingChanged += onEnableDisable;
            order--;

            mChangeTarget = Config.Bind(ChangeTargetDef, new KeyboardShortcut(KeyCode.Tab), new ConfigDescription("What button changes the selected vehicle", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mDrive = Config.Bind(DriveDef, new KeyboardShortcut(KeyCode.M), new ConfigDescription("What button makes the car drive", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mDriveBack = Config.Bind(DriveBackDef, new KeyboardShortcut(KeyCode.N), new ConfigDescription("What button makes the car drive backwards", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mJump = Config.Bind(JumpDef, new KeyboardShortcut(KeyCode.B), new ConfigDescription("What button makes the car jump", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            mFlip = Config.Bind(FlipDef, new KeyboardShortcut(KeyCode.V), new ConfigDescription("What button makes the car flip", null, new ConfigurationManagerAttributes { Order = order }));
            order--;

            Config.Bind(JumpStrengthDef, 1f, new ConfigDescription("How strong a jump is", null, new ConfigurationManagerAttributes { Order = order }));
            mJumpStrength = (ConfigEntry<float>)Config[JumpStrengthDef];
            order--;

            Config.Bind(JumpModeDef, JumpMode.AlwaysUp, new ConfigDescription("What direction the jump will be applied", null, new ConfigurationManagerAttributes { Order = order }));
            mJumpMode = (ConfigEntry<JumpMode>)Config[JumpModeDef];
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
            return mJumpStrength.Value + "|" + mJumpMode.Value;
        }

        public override void setSettings(string st)
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

        public bool CheckForCheating()
        {
            return mEnabled.Value && PolyTechMain.modEnabled.Value;
        }


        [HarmonyPatch(typeof(Main), "Update")]
        private static class patchUpdate
        {
            private static void Postfix()
            {
                if (!instance.CheckForCheating()) return;

                

                if (instance.mChangeTarget.Value.IsDown() && GameStateManager.GetState() == GameState.SIM)
                {
                    instance.InUpdate = true;

                    instance.TargetIndex++;
                    if (instance.TargetIndex > Vehicles.m_Vehicles.Count) instance.TargetIndex = 0;

                    if (instance.Target != null)
                    {
                        instance.Target.SetPhysicsVehicleTargetSpeed(0f, true);
                    }

                    instance.Target = null;
                    instance.TargetBodies = null;
                    if (instance.TargetIndex != 0)
                    {
                        instance.Target = Vehicles.m_Vehicles[instance.TargetIndex - 1];
                        if (instance.Target != null)
                        {
                            instance.Target.SetPhysicsVehicleTargetSpeed(0f, true);
                            instance.LoadBodies = true;
                            instance.Target.Physics.Execute();
                        }
                    }
                }

                instance.InUpdate = true;

                if (instance.mJump.Value.IsDown() && GameStateManager.GetState() == GameState.SIM && instance.Target)
                {
                    instance.AddJump = true;
                    instance.LoadBodies = true;
                    instance.Target.Physics.Execute();
                }

                if (instance.mFlip.Value.IsDown() && GameStateManager.GetState() == GameState.SIM)
                {
                    if (instance.Target) instance.Target.PhysicsVehicleFlip();
                }


                if (instance.DriveDown != (instance.mDrive.Value.IsPressed() && !instance.mDriveBack.Value.IsPressed()) && GameStateManager.GetState() == GameState.SIM)
                {
                    if (instance.Target)
                    {
                        if (instance.mDrive.Value.IsPressed()) instance.Target.SetPhysicsVehicleTargetSpeed(instance.Target.m_TargetSpeed, true);
                        else instance.Target.SetPhysicsVehicleTargetSpeed(0f, true);
                    }
                }
                instance.DriveDown = instance.mDrive.Value.IsPressed() && !instance.mDriveBack.Value.IsPressed();


                if (instance.DriveBackDown != (instance.mDriveBack.Value.IsPressed() && !instance.mDrive.Value.IsPressed()) && GameStateManager.GetState() == GameState.SIM)
                {
                    if (instance.Target)
                    {
                        if (instance.mDriveBack.Value.IsPressed()) instance.Target.SetPhysicsVehicleTargetSpeed(instance.Target.m_TargetSpeed * -1f, true);
                        else instance.Target.SetPhysicsVehicleTargetSpeed(0f, true);
                    }
                }
                instance.DriveBackDown = instance.mDriveBack.Value.IsPressed() && !instance.mDrive.Value.IsPressed();

                instance.InUpdate = false;
            }
        }

        [HarmonyPatch(typeof(Vehicle), "SetPhysicsVehicleTargetSpeed")]
        private static class patchSetTargetSpeed
        {
            private static bool Prefix(Vehicle __instance, float speed)
            {
                if (!instance.CheckForCheating()) return true;


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
                if (!instance.CheckForCheating()) return true;


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
                if (!instance.CheckForCheating()) return;

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

        [HarmonyPatch(typeof(GameStateManager), "ChangeState")]
        private static class patchChangeState
        {
            private static void Postfix(GameState state)
            {
                if (!instance.CheckForCheating()) return;

                if (state == GameState.SIM && instance.Target)
                {
                    
                }
            }
        }

        [HarmonyPatch(typeof(Solver), "IntegrateMotions")]
        private static class patchIntegrateMotions
        {
            private static void Postfix(Motion[] motionsPtr, float deltaTime)
            {
                if (!instance.CheckForCheating()) return;

                instance.InUpdate = true;

                if (instance.AddJump)
                {
                    if (instance.TargetBodies != null && motionsPtr != null && instance.AllBodies != null)
                    {
                        double angle = instance.TargetChassis[0].transform.rotation.eulerAngles.z * (Math.PI / 180);
                        Vec2 Jump = new Vec2(0, 0);
                        if (instance.mJumpMode.Value == JumpMode.AlwaysUp)
                        {
                            Jump = new Vec2(0f, instance.mJumpStrength.Value / 100 * 4);
                        }
                        else if (instance.mJumpMode.Value == JumpMode.Directional)
                        {
                            Jump = new Vec2(instance.mJumpStrength.Value * Mathf.Sin((float)angle) * -1, instance.mJumpStrength.Value * Mathf.Cos((float)angle)) / 100 * 4;
                        }

                        foreach (PolyPhysics.Rigidbody body in instance.TargetBodies)
                        {
                            int Index = instance.AllBodies.IndexOf(body);

                            if (Index >= 0 && Index < motionsPtr.Count())
                            {
                                motionsPtr[Index].linVel += Jump;
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
            }
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
