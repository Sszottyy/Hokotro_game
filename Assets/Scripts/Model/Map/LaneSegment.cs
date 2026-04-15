using System;
using UnityEngine;

namespace SnowPlow.Model.Map
{
    public class LaneSegment
    {
        //NEW! - kesobb valtoztathato, a celja, hogy ne lehessen tulsagosan stackelni
        public const int MaxSaltPower = 12;

        public int SnowLevel { get; private set; }
        public bool HasIce { get; private set; }
        public bool HasAccident { get; private set; }

        //NEW!
        public int SaltPower { get; private set; }

        //NEW! - adjusted to salt
        public LaneSegment(int snowLevel = 0, bool hasIce = false, bool hasAccident = false, int saltpower = 0)
        {
            if (snowLevel < 0) throw new ArgumentOutOfRangeException(nameof(snowLevel), "Snow level cannot be negative.");
            if (saltpower < 0) throw new ArgumentOutOfRangeException(nameof(saltpower), "Salt Power cannot be negative.");

            HasIce = hasIce;
            HasAccident = hasAccident;
            SnowLevel = hasIce ? 0 : snowLevel;
            SaltPower = saltpower;
        }

        public void AddSnow(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Snow amount cannot be negative.");

            if (HasIce || amount == 0) return;

            SnowLevel += amount;
        }

        public void AddSnow()
        {
            AddSnow(1);
        }

        //NEW!
        public void RemoveSnow(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Snow amount cannot be negative.");
            if (amount == 0) return;

            SnowLevel = Math.Max(0, SnowLevel - amount);
        }

        //NEW!
        public void RemoveSnow()
        {
            RemoveSnow(1);
        }

        public void RemoveAllSnow()
        {
            SnowLevel = 0;
        }

        public void SetIce(bool value)
        {
            HasIce = value;

            if (HasIce)
            {
                SnowLevel = 0;
            }
        }

        public void SetAccident(bool value)
        {
            HasAccident = value;
        }

        //NEW!
        public void AddSaltPower(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Salt power amount cannot be negative.");
            if (amount == 0) return;

            SaltPower = Math.Min(MaxSaltPower, SaltPower + amount);
        }

        //NEW!
        public void ConsumeSaltPower(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Salt power amount cannot be negative.");
            if (amount == 0) return;

            SaltPower = Math.Max(0, SaltPower - amount);
        }

        //NEW!
        public void ConsumeSaltPower()
        {
            ConsumeSaltPower(1);
        }

        //NEW!
        public bool HasSalt()
        {
            return SaltPower > 0;
        }

        public bool IsBlocked()
        {
            return HasAccident || SnowLevel >= 3;
        }
    }
}