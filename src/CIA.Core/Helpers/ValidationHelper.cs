using System;
using System.Text.RegularExpressions;

namespace CIA.Core.Helpers
{
    public static class ValidationHelper
    {
        private static readonly Regex PhoneNumberRegex = new Regex(@"^(\+90|0090|90|0)?[5][0-9]{9}$", RegexOptions.Compiled);
        private static readonly Regex ImeiRegex = new Regex(@"^\d{15}$", RegexOptions.Compiled);
        private static readonly Regex ImsiRegex = new Regex(@"^\d{15}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex CgiRegex = new Regex(@"^\d{3}-\d{2,3}-\d{1,5}-\d{1,5}$", RegexOptions.Compiled);

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "");
            return PhoneNumberRegex.IsMatch(cleaned);
        }

        public static bool IsValidImei(string imei)
        {
            if (string.IsNullOrWhiteSpace(imei)) return false;
            var cleaned = imei.Replace(" ", "");
            if (!ImeiRegex.IsMatch(cleaned)) return false;
            return ValidateLuhn(cleaned);
        }

        public static bool IsValidImsi(string imsi)
        {
            if (string.IsNullOrWhiteSpace(imsi)) return false;
            return ImsiRegex.IsMatch(imsi.Replace(" ", ""));
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex.IsMatch(email);
        }

        public static bool IsValidCgi(string cgi)
        {
            if (string.IsNullOrWhiteSpace(cgi)) return false;
            return CgiRegex.IsMatch(cgi);
        }

        public static bool IsValidLatitude(double latitude)
        {
            return latitude >= -90.0 && latitude <= 90.0;
        }

        public static bool IsValidLongitude(double longitude)
        {
            return longitude >= -180.0 && longitude <= 180.0;
        }

        public static bool IsValidCoordinate(double latitude, double longitude)
        {
            return IsValidLatitude(latitude) && IsValidLongitude(longitude);
        }

        public static bool IsValidAzimuth(double azimuth)
        {
            return azimuth >= 0 && azimuth < 360;
        }

        public static bool IsValidPci(int pci)
        {
            return pci >= 0 && pci <= 503;
        }

        public static bool IsValidRsrp(double rsrp)
        {
            return rsrp >= -140.0 && rsrp <= -44.0;
        }

        public static bool IsValidRsrq(double rsrq)
        {
            return rsrq >= -19.5 && rsrq <= -3.0;
        }

        public static bool IsValidSinr(double sinr)
        {
            return sinr >= -23.0 && sinr <= 40.0;
        }

        public static string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return phoneNumber;
            var cleaned = phoneNumber.Replace(" ", "").Replace("-", "");

            if (cleaned.StartsWith("+90")) return "0" + cleaned.Substring(3);
            if (cleaned.StartsWith("0090")) return "0" + cleaned.Substring(4);
            if (cleaned.StartsWith("90") && cleaned.Length == 12) return "0" + cleaned.Substring(2);
            return cleaned;
        }

        private static bool ValidateLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int n = int.Parse(number[i].ToString());
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            return sum % 10 == 0;
        }
    }
}
