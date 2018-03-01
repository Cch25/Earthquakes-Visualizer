using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace T95.EarthquakeViz2D
{
    public static class ExtensionClass
    {
        public static string[] SplitWithQualifier(this string text, char delimiter, char qualifier, bool stripQualifierFromResult)
        {
            string pattern = string.Format(
               @"{0}(?=(?:[^{1}]*{1}[^{1}]*{1})*(?![^{1}]*{1}))", Regex.Escape(delimiter.ToString()), Regex.Escape(qualifier.ToString())
           );

            string[] split = Regex.Split(text, pattern);

            if (stripQualifierFromResult)
                return split.Select(s => s.Trim().Trim(qualifier)).ToArray();
            return split;
        }

        public static double DegreeToRadian(float angle)
        {
            return Math.PI * angle / 180.0;
        }

        public static float Map(float value, float from1, float to1, float from2, float to2)
        {
            return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
        }

    }
}
