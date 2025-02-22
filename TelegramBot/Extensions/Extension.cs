﻿using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramBot.Extensions
{
    public static class Extension
    {
        public static T Previous<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) - 1;
            return ( j<=0 ) ? Arr[0] : Arr[j];
        }
        public static string ToStringDDMMYYYY(this DateTime date)
        {
            return $"{date.Day}.{date.Month}.{date.Year}";
        }
        public static string ToStringTimeHHMM(this DateTime date)
        {
            return $"{date.Hour}:{date.Minute}";
        }

        public static string AddUnicodeSymbols(this int number,string unicodes)
        {
            char[] chars = number.ToString().ToCharArray();
            StringBuilder result = new();
            foreach(char item in chars)
            {
                result.Append(item+unicodes);
            }
            return result.ToString();
        }
    }

}
