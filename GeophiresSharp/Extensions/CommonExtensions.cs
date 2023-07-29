using System;
using System.Collections.Generic;

namespace GeophiresSharp.Extensions
{
    public static class CommonExtensions
    {
        public static string GetStringFromContent(this string[] content, string parameter)
        {
            string str = "";
            if (content.Length < 1) return str;
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                foreach (var line in content)
                {
                    if (line.Contains(parameter))
                    {
                        string[] lineSplit = line.Split(",");
                        str = lineSplit[1];
                        break;
                    }
                }
            }
            return str;
        }

        public static int GetIntParameter(this string[] content, string parameterName, int defaultParameter, List<int> validParameters)
        {
            int parameter = defaultParameter;
            int? i = 0;
            i = content.GetIntFromContent(parameterName);
            if (i == null) Console.WriteLine($"Warning: No valid {parameterName} provided. GEOPHIRES will assume default {parameterName} {defaultParameter}");
            else parameter = (int)i;
            if (!validParameters.Contains(parameter))
            {
                parameter = defaultParameter;
                Console.WriteLine($"Warning: Provided {parameterName} is not valid. GEOPHIRES will assume default {parameterName} {defaultParameter}");
            }
            return parameter;
        }

        public static int? GetIntFromContent(this string[] content, string parameter)
        {
            int? number = null;
            if (content.Length < 1) return null;
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                foreach (var line in content)
                {
                    if (line.Contains(parameter))
                    {
                        string[] lineSplit = line.Split(",");
                        number = lineSplit[1].GetIntFromString();
                        break;
                    }
                }
            }
            return number;
        }

        public static double GetDoubleParameter(this string[] content, string parameterName, double defaultParameter, double min, double max)
        {
            double parameter = defaultParameter;
            double? d = 0;
            d = content.GetDoubleFromContent(parameterName);
            if (d == null) Console.WriteLine($"Warning: No valid {parameterName} provided. GEOPHIRES will assume default {parameterName} {defaultParameter}");
            else parameter = (double)d;
            if (parameter < min || parameter > max)
            {
                parameter = defaultParameter;
                Console.WriteLine($"Warning: Provided {parameterName} is not valid. GEOPHIRES will assume default {parameterName} {defaultParameter}");
            }
            return parameter;
        }

        public static double? GetDoubleFromContent(this string[] content, string parameter)
        {
            double? number = null;
            if (content.Length < 1) return null;
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                foreach (var line in content)
                {
                    if (line.Contains(parameter))
                    {
                        string[] lineSplit = line.Split(",");
                        number = lineSplit[1].GetDoubleFromString();
                        break;
                    }
                }
            }
            return number;
        }

        public static double? GetDoubleFromString(this string token)
        {
            double? number = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                double value;
                if (double.TryParse(token, out value)) number = value;
            }
            return number;
        }

        public static int? GetIntFromString(this string token)
        {
            int? number = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                int value;
                if (int.TryParse(token, out value)) number = value;
            }
            return number;
        }
    }
}
