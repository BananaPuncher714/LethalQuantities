using BepInEx;
using BepInEx.Configuration;
using System;

namespace LethalQuantities.Objects
{
    public abstract class BaseEntry
    {
        public abstract Type SettingType();

        public virtual bool hasDefault()
        {
            return false;
        }
        public virtual string DefaultString()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class CustomEntry<T> : BaseEntry, IDefaultable
    {
        // Get the value of this entry. The parameter passed is the current value.
        public abstract T Value(T value);
        public abstract bool isDefault();
        public virtual bool isUnset()
        {
            return isDefault();
        }

        public virtual bool isLocallySet()
        {
            return !isDefault();
        }

        public abstract bool Set(ref T value);
        public override Type SettingType()
        {
            return typeof(T);
        }
        public virtual void setDefaultValue(T value)
        {
            throw new NotImplementedException();
        }
        public virtual T DefaultValue()
        {
            throw new NotImplementedException();
        }
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

        public override void setDefaultValue(T value)
        {
            defaultValue = v => value;
        }

        public override T DefaultValue()
        {
            return defaultValue(default(T));
        }
    }

    public class GlobalConfigEntry<T> : CustomEntry<T>
    {
        public static readonly string DEFAULT_OPTION = "DEFAULT";
        public static readonly string GLOBAL_OPTION = "GLOBAL";

        CustomEntry<T> parentEntry;
        ConfigEntry<string> entry;
        TypeConverter converter;
        T defaultValue;

        public GlobalConfigEntry(CustomEntry<T> globalEntry, ConfigEntry<string> entry, T defaultValue, TypeConverter converter)
        {
            parentEntry = globalEntry;
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
                return parentEntry.Value(defaultValue);
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

        public override bool isUnset()
        {
            return isDefault() || (isGlobal() && (parentEntry.isDefault() || (parentEntry is GlobalConfigEntry<T> && (parentEntry as GlobalConfigEntry<T>).isUnset())));
        }

        public override bool isLocallySet()
        {
            return !(isDefault() || isGlobal());
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

        public override bool hasDefault()
        {
            return true;
        }

        public override string DefaultString()
        {
            return converter.ConvertToString(defaultValue, typeof(T));
        }

        public override void setDefaultValue(T value)
        {
            defaultValue = value;
        }
        public override T DefaultValue()
        {
            return defaultValue;
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

        public override bool hasDefault()
        {
            return true;
        }

        public override string DefaultString()
        {
            return converter.ConvertToString(defaultValue, typeof(T));
        }

        public override void setDefaultValue(T value)
        {
            defaultValue = value;
        }
        public override T DefaultValue()
        {
            return defaultValue;
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
