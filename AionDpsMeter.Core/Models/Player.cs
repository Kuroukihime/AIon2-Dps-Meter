namespace AionDpsMeter.Core.Models
{
    public class Player : Entity
    {
        public string? Icon { get; set; }
        public int CharacterLevel { get; set; }
        public int CombatPower { get; set; }
        public int ServerId { get; set; }
        public string ServerName { get; set; } = "";
        public bool IsUser { get; set; }
        public bool IsIdentified { get; set; }
        public CharacterClass? CharacterClass { get; set; }
        public int? GlobalId { get; set; }
    }
}