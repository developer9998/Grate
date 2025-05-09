using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using Grate.Networking;
using Grate.Tools;
using UnityEngine;

namespace Grate.Modules
{
    public abstract class GrateModule : MonoBehaviour
    {
        public List<ConfigEntryBase> ConfigEntries;
        public static GrateModule LastEnabled;
        public static Dictionary<string, bool> enabledModules = new Dictionary<string, bool>();
        public static string enabledModulesKey = "GrateEnabledModules";

        protected virtual void ReloadConfiguration() { }

        public abstract string GetDisplayName();
        protected void SettingsChanged(object sender, SettingChangedEventArgs e)
        {
            foreach (var field in this.GetType().GetFields())
            {
                if (e.ChangedSetting == field.GetValue(this))
                {
                    ReloadConfiguration();
                }
            }
        }

        public abstract string Tutorial();
        public ButtonController button;

        protected abstract void Cleanup();

        protected virtual void Start()
        {
            this.enabled = false;

        }

        protected virtual void OnEnable()
        {
            LastEnabled = this;
            Plugin.configFile.SettingChanged += SettingsChanged;
            if (this.button)
                this.button.IsPressed = true;
            SetStatus(true);
        }

        protected virtual void OnDisable()
        {
            Plugin.configFile.SettingChanged -= SettingsChanged;
            if (this.button)
                this.button.IsPressed = false;
            this.Cleanup();
            SetStatus(false);
        }

        public void SetStatus(bool enabled)
        {
            string name = GetDisplayName();
            if (enabledModules.ContainsKey(name))
                enabledModules[name] = enabled;
            else
                enabledModules.Add(name, enabled);
            NetworkPropertyHandler.Instance?.ChangeProperty(enabledModulesKey, enabledModules);
        }

        protected virtual void OnDestroy()
        {
            this.Cleanup();
        }

        public static List<Type> GetGrateModuleTypes()
        {
            try
            {
                var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(GrateModule).IsAssignableFrom(t)).ToList();
                types.Sort((x, y) =>
                {
                    FieldInfo xField = x.GetField("DisplayName", BindingFlags.Public | BindingFlags.Static);
                    FieldInfo yField = y.GetField("DisplayName", BindingFlags.Public | BindingFlags.Static);

                    if (xField == null || yField == null)
                        return 0;

                    string xValue = (string)xField.GetValue(null);
                    string yValue = (string)yField.GetValue(null);

                    return string.Compare(xValue, yValue);
                });
                return types;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logging.Exception(ex);
                Logging.Warning("Inner exceptions:");
                foreach (Exception inner in ex.LoaderExceptions)
                {
                    Logging.Exception(inner);
                }
            }
            return null;
        }

    }
}
