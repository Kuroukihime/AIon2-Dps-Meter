using System.Text.Json;

namespace AionDpsMeter.Core.Data
{
    public static class SkillIconResolver
    {
        private const string BaseUrl = "https://assets.playnccdn.com/static-aion2-gamedata/resources";
        private const string TheostonePrefix = "30";
        private const string TheostoneIconBase = "Icon_Item_Usable_Godstone_WP_r_";

        private static readonly IReadOnlyDictionary<string, string> PrefixToClassCode =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "11", "GL" }, // Gladiator
                { "12", "TE" }, // Templar
                { "13", "AS" }, // Assassin
                { "14", "RA" }, // Ranger
                { "15", "SO" }, // Sorcerer
                { "16", "EL" }, // Elementalist
                { "17", "CL" }, // Cleric
                { "18", "CH" }, // Chanter
            };

        private static readonly Dictionary<int, string> NameColorByCode = new Dictionary<int, string>
        {
            { 0, "#52b35c" }, // green
            { 1, "#3d94d8" }, // blue
            { 2, "#e9a43a" }  // yellow
        };

        private static IReadOnlyDictionary<string, string>? _skillIconMap;

        public static void LoadSkillIconMap(string json)
        {
            _skillIconMap = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }

        public static string? GetIconUrl(int skillCode, bool isTheostone)
        {

            if (isTheostone) return GetTheostoneIconUrl(skillCode.ToString());

            if (skillCode is < 10_000_000 or > 19_999_999)
                return null;

            string code = skillCode.ToString("D8")[..8];

            string prefix = code[..2];   // digits 0-1  ? class prefix
            string mid4   = code[2..6];  // digits 2-5  ? used for basic-attack detection
            string tail2  = code[6..8];  // digits 6-7

            
            if (mid4 == "0000" && tail2 != "00")
                return null;

            // Resolve class short code from prefix
            if (!PrefixToClassCode.TryGetValue(prefix, out string? classCode))
                return null;

            if (!int.TryParse(code[2..4], out int sub))
                return null;

            bool isPassive = sub >= 70;

            string base4 = code[..4];
            if (_skillIconMap?.TryGetValue(base4, out string? iconName) == true &&
                !string.IsNullOrEmpty(iconName))
            {
                return $"{BaseUrl}/{iconName}.png";
            }

            string suffix = "";
            if (isPassive)
            {
                suffix = "Passive_";
                sub -= 70;
            }
            string subPad  = sub.ToString("D3");
            return $"{BaseUrl}/ICON_{classCode}_SKILL_{suffix}{subPad}.png";
        }

        private static string? GetTheostoneIconUrl(string rawCode)
        {
            string code = rawCode ?? "";

            if (!code.StartsWith(TheostonePrefix) || code.Length < 7)
                return null;

            if (!int.TryParse(code[4].ToString(), out int qualityCode))
                return null;

            if (!int.TryParse(code.Substring(5, 2), out int iconCode) || iconCode <= 0)
                return null;

            string iconHex = iconCode.ToString("x3");

            NameColorByCode.TryGetValue(qualityCode, out string? nameColor);

            return $"{BaseUrl}/{TheostoneIconBase}{iconHex}.png";
        }
    }
}
