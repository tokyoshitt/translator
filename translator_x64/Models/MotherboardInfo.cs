using System.Management;

namespace translator_x64.Models
{
    public static class MotherboardInfo
    {
        private static string _name;
        private static string _serialNumber;

        public static string Name
        {
            get
            {
                if (_name == null)
                {
                    _name = GetMotherboardInfo("Product");
                }
                return _name;
            }
        }

        public static string SerialNumber
        {
            get
            {
                if (_serialNumber == null)
                {
                    _serialNumber = GetMotherboardInfo("SerialNumber");
                }
                return _serialNumber;
            }
        }

        private static string GetMotherboardInfo(string property)
        {
            string result = "";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            foreach (ManagementObject obj in searcher.Get())
            {
                try
                {
                    result = obj[property].ToString();
                    break;
                }
                catch
                {
                }
            }
            return result;
        }
    }
}
