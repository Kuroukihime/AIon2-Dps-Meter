using AionDpsMeter.Core.Data;

namespace AionDpsMeter.Core.Models
{
    public class Mob : Entity
    {
        public int MobCode { get; set; }
        public int HpTotal { get; set; }
        public int HpCurrent { get; set; }
        public new string Name => GameDataProvider.Instance.GetMobName(MobCode);
    }
}
