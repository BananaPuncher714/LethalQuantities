using BepInEx;
using BepInEx.Configuration;
using System;

namespace LethalQuantities.Objects
{
    public abstract class CustomEntry<T>
    {
        // Get the value of this entry. The parameter passed is the current value.
        public abstract T Value(T value);
        public abstract bool isDefault();
        public abstract bool Set(ref T value);
    }

    public class EmptyEntry<T> : CustomEntry<T>
    {
        Func<T, T> defaultValue;

        public EmptyEntry(Func<T, T> defaultValue)
        {
            this.defaultValue = defaultValue;
        }

        public EmptyEntry(T defaultValue)
        {
            this.defaultValue = v => defaultValue;
        }

        public EmptyEntry()
        {
            defaultValue = v => v;
        }

        public override T Value(T value)
        {
            return defaultValue(value);
        }

        public override bool isDefault()
        {
            return true;
        }

        public override bool Set(ref T value)
        {
            return true;
        }
    }

    public class GlobalConfigEntry<T> : CustomEntry<T>
    {
        public static readonly string DEFAULT_OPTION = "DEFAULT";
        public static readonly string GLOBAL_OPTION = "GLOBAL";

        Func<T, T> globalGetter;
        ConfigEntry<string> entry;
        TypeConverter converter;
        T defaultValue;

        public GlobalConfigEntry(CustomEntry<T> globalEntry, ConfigEntry<string> entry, T defaultValue, TypeConverter converter)
        {

            globalGetter = globalEntry.Value;
            this.entry = entry;
            this.defaultValue = defaultValue;
            this.converter = converter;

            if (isGlobal())
            {
                entry.Value = "";
            }
        }

        public GlobalConfigEntry(ConfigEntry<T> globalEntry, ConfigEntry<string> entry, T defaultValue, TypeConverter converter)
        {
            globalGetter = (v) => globalEntry.Value;
            this.entry = entry;
            this.defaultValue = defaultValue;
            this.converter = converter;

            if (isGlobal())
            {
                entry.Value = "";
            }
        }

        public override T Value(T value)
        {
            string val = entry.Value;
            if (val.ToUpper() == DEFAULT_OPTION)
            {
                return defaultValue;
            }
            else if (val.ToUpper() == GLOBAL_OPTION || entry.Value.IsNullOrWhiteSpace())
            {
                return globalGetter(defaultValue);
            }
            else
            {
                return (T) converter.ConvertToObject(val, typeof(T));
            }
        }

        public override bool isDefault()
        {
            return entry.Value.ToUpper() == DEFAULT_OPTION;
        }

        public virtual bool isGlobal()
        {
            return entry.Value.ToUpper() == GLOBAL_OPTION || entry.Value.IsNullOrWhiteSpace();
        }

        public override bool Set(ref T value)
        {
            T current = Value(value);
            bool same = value.Equals(current);
            if (!isDefault())
            {
                value = current;
            }
            return same;
        }
    }

    public class DefaultableConfigEntry<T> : CustomEntry<T>
    {
        public static readonly string DEFAULT_OPTION = "DEFAULT";
        ConfigEntry<string> entry;
        T defaultValue;
        TypeConverter converter;

        public DefaultableConfigEntry(ConfigEntry<string> entry, T defaultValue, TypeConverter converter)
        {
            this.entry = entry;
            this.defaultValue = defaultValue;
            this.converter = converter;

            if (isDefault())
            {
                entry.Value = "";
            }
        }

        public override T Value(T value)
        {
            string val = entry.Value;
            if (val.ToUpper() == DEFAULT_OPTION || val.IsNullOrWhiteSpace())
            {
                return defaultValue;
            }
            else
            {
                return (T) converter.ConvertToObject(val, typeof(T));
            }
        }

        public override bool isDefault()
        {
            return entry.Value.ToUpper() == DEFAULT_OPTION || entry.Value.IsNullOrWhiteSpace();
        }

        public override bool Set(ref T value)
        {
            T current = Value(value);
            bool same = value.Equals(current);
            if (!isDefault())
            {
                value = current;
            }
            return same;
        }
    }

    public class NonDefaultableConfigEntry<T> : CustomEntry<T>
    {
        public static readonly string DEFAULT_OPTION = "DEFAULT";
        ConfigEntry<string> entry;
        TypeConverter converter;

        public NonDefaultableConfigEntry(ConfigEntry<string> entry, TypeConverter converter)
        {
            this.entry = entry;
            this.converter = converter;

            if (isDefault())
            {
                entry.Value = "";
            }
        }

        public override T Value(T defaultValue)
        {
            string val = entry.Value;
            if (val.ToUpper() == DEFAULT_OPTION || val.IsNullOrWhiteSpace())
            {
                return defaultValue;
            }
            else
            {
                return (T)converter.ConvertToObject(val, typeof(T));
            }
        }

        public override bool isDefault()
        {
            return entry.Value.ToUpper() == DEFAULT_OPTION || entry.Value.IsNullOrWhiteSpace();
        }

        public override bool Set(ref T value)
        {
            value = Value(value);
            return isDefault();
        }
    }
}
