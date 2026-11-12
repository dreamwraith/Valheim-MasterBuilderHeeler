using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ConditionalConfigSync;
using Jotunn.Extensions;

namespace MasterBuilderHeeler.Configuration
{
    // every setting binds through one of these so the sync mode and the jotunn ordering stay in one place
    public sealed class ConfigSection
    {
        private readonly ConfigFile _config;
        private readonly string _name;
        private readonly Dictionary<Type, object> _entriesByType = new Dictionary<Type, object>();

        public ConfigSection(ConfigFile config, string name)
        {
            _config = config;
            _name = name;
        }

        public ConfigEntry<T> Client<T>(
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues = null,
            ConfigurationManagerAttributes? attributes = null)
        {
            ConfigEntry<T> entry = Bind(key, defaultValue, description, acceptableValues, attributes);
            ModConfig.ConfigSync.AddConfigEntry(entry, ConfigSyncMode.AlwaysClientControlled);
            return entry;
        }

        public ConfigEntry<T> Conditional<T>(
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues = null)
        {
            ConfigEntry<T> entry = Bind(key, defaultValue, description, acceptableValues, attributes: null);
            ModConfig.ConfigSync.AddConfigEntry(entry, ConfigSyncMode.Conditional, serverControlledByDefault: false);
            return entry;
        }

        public ConfigEntry<T> Synced<T>(
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues = null)
        {
            ConfigEntry<T> entry = Bind(key, defaultValue, description, acceptableValues, attributes: null);
            ModConfig.ConfigSync.AddConfigEntry(entry, ConfigSyncMode.Conditional, serverControlledByDefault: true);
            return entry;
        }

        // the list keeps filling as more settings bind so it can be handed out before the last one is in
        public IReadOnlyList<ConfigEntry<T>> All<T>() => EntriesOf<T>();

        // some wont like this but i dont really prefer jotunn's implementation for sync, so i stick with ccs
        // maybe jotunn can pull in some of ccs features at some point, because its nice for server side toggles
        // that said, jotunn ordering is pretty slick, so im still using that shit
        private ConfigEntry<T> Bind<T>(
            string key,
            T defaultValue,
            string description,
            AcceptableValueBase? acceptableValues,
            ConfigurationManagerAttributes? attributes)
        {
            ConfigEntry<T> entry = _config.BindConfigInOrder(
                _name,
                key,
                defaultValue,
                description,
                synced: false,
                sectionOrder: true,
                settingOrder: true,
                acceptableValues: acceptableValues,
                configAttributes: attributes);
            EntriesOf<T>().Add(entry);
            return entry;
        }

        private List<ConfigEntry<T>> EntriesOf<T>()
        {
            if (!_entriesByType.TryGetValue(typeof(T), out object entries))
            {
                entries = new List<ConfigEntry<T>>();
                _entriesByType[typeof(T)] = entries;
            }

            return (List<ConfigEntry<T>>)entries;
        }
    }
}
