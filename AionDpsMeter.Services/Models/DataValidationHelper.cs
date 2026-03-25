using System.Reflection.PortableExecutable;

namespace AionDpsMeter.Services.Models
{
    internal class DataValidationHelper
    {

        public static bool IsReasonableSkillCode(int code)
        {

            // OUT OF RANGE CHECK
            if (code < 1L || code > 299_999_999L)
            {

                return false;
            }

            //Theostone 
            if (code >= 3_000_000L && code <= 3_099_999L)
            {
                return false;
            }

            //NPC skills
            if (code is >= 1_000_000 and <= 9_999_999)
            {
                return false;
            }

            // Pet skills
            if (code >= 100_000 && code < 200_000)
            {
                return true;
            }

            // Player skills (11xxxxxx - 18xxxxxx)
            if (code >= 11_000_000 && code < 19_000_000)
            {
                return true;
            }

            // Check with offsets for skill variations
            int[] possibleOffsets = [0, 10, 20, 30, 40, 50, 120, 130, 140, 150, 230, 240, 250, 340, 350, 450];
            foreach (var offset in possibleOffsets)
            {
                int baseCode = code - offset;
                if (baseCode >= 11_000_000 && baseCode < 19_000_000)
                {
                    return true;
                }
                if (baseCode >= 100_000 && baseCode < 200_000)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
