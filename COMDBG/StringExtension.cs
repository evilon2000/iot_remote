using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace COMDBG
{
    public static class StringExtension
    {
        /// <summary>
        /// 向字符串末尾补零到目标字符串长度
        /// </summary>
        /// <param name="s"></param>
        /// <param name="len">目标字符串长度</param>
        /// <returns></returns>
        public static string AddZero(this string s, int len)
        {
            if (s.Length == len) return s;
            string addZero = "";
            for (int i = 0; i < len - s.Length; i++)
            {
                addZero = $"0{addZero}";
            }
            return $"{addZero}{s}";
        }
        public static bool AddressChecked(this string s)
        {
            if (s.Length < 2) s = s.AddZero(2);
            int result = 0;
            try
            {
                result = Convert.ToInt32(s, 16);
                if (result >= 1 && result <= 255)
                {
                    return true;
                }
                return false;
            }
            catch 
            {
                return false;
            }
        }
        /// <summary>
        /// 字符串每隔两位做个调换
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string LSBReverse(this string s)
        {
            var charArry = s.ToCharArray().Reverse();
            var remainArry = charArry.Take(s.Length);
            var newChar = new char[] { };
            while (remainArry.Count() > 0)
            {
                newChar = newChar.Concat(charArry.Take(2).Reverse()).ToArray();
                remainArry = charArry.Skip(2);
                charArry = remainArry;
            }
            return new string(newChar);
        }
        public static string[] ParseToPulseReadingData(this string s)
        {
            var parseList = s.Split('-').ToList();
            var pulse1Unit = new string((int.Parse(parseList[0]) * 10).ToString("X8").LSBReverse().Take(4).ToArray());
            var pulse2Unit = new string((int.Parse(parseList[1]) * 10).ToString("X8").LSBReverse().Take(4).ToArray());
            var pulse1Accumulate = int.Parse(parseList[2]).ToString("X8").AddZero(8).LSBReverse();
            var pulse2Accumulate = int.Parse(parseList[3]).ToString("X8").AddZero(8).LSBReverse();

            return new string[] { pulse1Unit, pulse2Unit, pulse1Accumulate, pulse2Accumulate };
        }
    }
}
