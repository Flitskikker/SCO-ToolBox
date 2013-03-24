using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace INI
{
    /// <summary>

    /// Create a New INI file to store or load data

    /// </summary>

    public class IniFile
    {
        public string path;

        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern long WritePrivateProfileString(string section,
            string key, string val, string filePath);
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern int GetPrivateProfileString(string section,
                 string key, string def, StringBuilder retVal,
            int size, string filePath);
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileSectionNamesA")]
        static extern int GetSectionNamesListA(byte[] lpszReturnBuffer, int nSize, string lpFileName);

        /// <summary>

        /// INIFile Constructor.

        /// </summary>

        /// <PARAM name="INIPath"></PARAM>

        public IniFile(string INIPath)
        {
            path = INIPath;
        }
        /// <summary>

        /// Write Data to the INI File

        /// </summary>

        /// <PARAM name="Section"></PARAM>

        /// Section name

        /// <PARAM name="Key"></PARAM>

        /// Key Name

        /// <PARAM name="Value"></PARAM>

        /// Value Name

        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>

        /// Read Data Value From the Ini File

        /// </summary>

        /// <PARAM name="Section"></PARAM>

        /// <PARAM name="Key"></PARAM>

        /// <PARAM name="Path"></PARAM>

        /// <returns></returns>

        public string IniReadValue(string Section, string Key, string Default)
        {
            StringBuilder temp = new StringBuilder(1023);
            int i = GetPrivateProfileString(Section, Key, Default, temp,
                                            1023, this.path);
            return temp.ToString();
        }

        public ArrayList IniGetSections()
        {

            //Get The Sections List Method

            ArrayList arrSec = new ArrayList();
            byte[] buff = new byte[1048576];
            GetSectionNamesListA(buff, buff.Length, this.path);
            String s = Encoding.Default.GetString(buff);
            String[] names = s.Split('\0');
            foreach (String name in names)
            {
                if (name != String.Empty)
                {
                    arrSec.Add(name);
                }
            }
            return arrSec;
        }

        public string IniReadBaseData(string Section, string Key, string Default)
        {
            StringBuilder temp = new StringBuilder(65536);
            int i = GetPrivateProfileString(Section, Key, Default, temp,
                                            65536, this.path);
            return temp.ToString();
        }
    }
}
