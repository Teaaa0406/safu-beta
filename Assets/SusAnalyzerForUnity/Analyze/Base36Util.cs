using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;

namespace Tea.Safu.Util
{
    public static class Base36Util
    {
        private const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static int Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Empty value.");
            value = value.ToUpper();
            bool negative = false;
            if (value[0] == '-')
            {
                negative = true;
                value = value.Substring(1, value.Length - 1);
            }
            if (value.Any(c => !Digits.Contains(c)))
                throw new ArgumentException("Invalid value: \"" + value + "\".");
            var decoded = 0;
            for (var i = 0; i < value.Length; ++i)
                decoded += Digits.IndexOf(value[i]) * (int)BigInteger.Pow(Digits.Length, value.Length - i - 1);
            return negative ? decoded * -1 : decoded;
        }

        public static string Encode(long value)
        {
            if (value == int.MinValue)
            {
                return "-1Y2P0IJ32E8E8";
            }
            bool negative = value < 0;
            value = Math.Abs(value);
            string encoded = string.Empty;
            do
                encoded = Digits[(int)(value % Digits.Length)] + encoded;
            while ((value /= Digits.Length) != 0);
            return negative ? "-" + encoded : encoded;
        }
    }
}