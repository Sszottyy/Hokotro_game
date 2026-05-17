using System;
using UnityEngine;

namespace SnowPlow.Model.Map
{
    public class LaneSegment
    {
        public const int MaxSaltPower = 12;
public const int IceFormationVehicleThreshold = 3;

public int PassedVehicleCount { get; private set; }
        private int _snowLevel;
        public int SnowLevel { 
            get { return _snowLevel; }
            set { //_snowLevel = Math.Min(value, 2); //npc debugra hasznaltam, ha elfelejtettem torolni, nyugodtan tedd meg 
                _snowLevel = value;
                if (_snowLevel == 0) {
                    PassedVehicleCount = 0; // A hó eltűntével visszaállítjuk a járművek számát, hogy újra lehessen számolni
                }
            }
        }
        public bool HasIce { get; private set; }
        public bool HasAccident { get; private set; }

        public int SaltPower { get; private set; }

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

            if (HasIce || amount == 0 || SaltPower > 0) return;

            SnowLevel += amount;
        }

        public void AddSnow()
        {
            AddSnow(1);
        }

        public void RemoveSnow(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Snow amount cannot be negative.");
            if (amount == 0) return;

            SnowLevel = Math.Max(0, SnowLevel - amount);
        }

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

        public void RegisterVehiclePassForIceFormation()
        {

            if (HasIce) return;

            if (SaltPower > 0) return;

            if (SnowLevel <= 0)
            {
                PassedVehicleCount = 0;
                return;
            }

            PassedVehicleCount++;

            if (PassedVehicleCount >= IceFormationVehicleThreshold)
            {
                SetIce(true);
            }
        }

        public void SetAccident(bool value)
        {
            HasAccident = value;
        }

        public void AddSaltPower(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Salt power amount cannot be negative.");
            if (amount == 0) return;

            SaltPower = Math.Min(MaxSaltPower, SaltPower + amount);
        }
        public void SetSaltPower(int value)
        {
            SaltPower = Mathf.Clamp(value, 0, MaxSaltPower);
        }

        public void ConsumeSaltPower(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Salt power amount cannot be negative.");
            if (amount == 0) return;

            SaltPower = Math.Max(0, SaltPower - amount);
        }

        public void ConsumeSaltPower()
        {
            ConsumeSaltPower(1);
        }

        public bool HasSalt()
        {
            return SaltPower > 0;
        }

        public void UpdateSalt()
        {
            if (SaltPower > 0)
            {
                ConsumeSaltPower();
                RemoveSnow();
                HasIce = false;
            }
        }
        public bool TooMuchSnow()
        {
            return SnowLevel > 3; // Példa küszöbérték, igény szerint módosítható
        }
    }
}