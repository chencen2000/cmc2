using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace twController
{
    class profileClass
    {
        static public profileClass CreateFormProfileName(string profileName)
        {
            profileClass ret = null;
            try
            {
                if (!string.IsNullOrEmpty(profileName))
                {
                    string profileFolder = System.IO.Path.Combine(envClass.getInstance().getProfilePoolFolder(), profileName);
                    if (System.IO.Directory.Exists(profileFolder))
                    {
                        string profileXml = System.IO.Path.Combine(profileFolder, "profile.xml");
                        if (System.IO.File.Exists(profileXml))
                        {
                            profileClass pc = new profileClass(profileXml);
                            if (pc.isValid())
                            {
                                ret = pc;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                ret = null;
            }
            return ret;
        }
        public override string ToString()
        {
            return getPropertyByName("profile");
        }
        string _profileXml = string.Empty;
        XmlDocument _profileDom = null;
        profileClass(string profileXml) 
        {
            _profileXml = profileXml;
        }
        public bool isValid()
        {
            bool ret = true;
            try
            {
                _profileDom=new XmlDocument();
                _profileDom.Load(_profileXml);
                if (_profileDom.DocumentElement!=null)
                {
                    ret=true;
                }
                else
                    ret=false;
            }
            catch (System.Exception ex)
            {
            	ret=false;
            }
            return ret;
        }
        public string getPropertyByName(string propertyName)
        {
            string ret = string.Empty;
            if (_profileDom!=null && !string.IsNullOrEmpty(propertyName))
            {
                try
                {
                    XmlNode n = _profileDom.SelectSingleNode(string.Format("/profilexml/{0}", propertyName));
                    ret = (n != null) ? n.InnerText : string.Empty;
                }
                catch (System.Exception ex)
                {
                	
                }
            }
            return ret;
        }
        public string getProfileName()
        {
            string ret = string.Empty;
            try
            {
                ret = System.IO.Path.GetDirectoryName(_profileXml);
                DirectoryInfo info = new DirectoryInfo(ret);
                ret = info.Name; 
            }
            catch (System.Exception ex)
            {

            }
            return ret;
        }
        public string getProfileFullPath()
        {
            return _profileXml;
        }
        public string getKittingXmlFile()
        {
            string ret = string.Empty;
            try
            {          
                ret = System.IO.Path.GetDirectoryName(_profileXml);
                ret = System.IO.Path.Combine(ret, "kitting.xml");          
            }
            catch (System.Exception ex)
            {
            	
            }
            return ret;
        }
        public string getPhonebookFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("contacts");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "phonebook", s);
            }
            return ret;
        }
        public string getCalendarFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("calendars");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "calendar", s);
            }
            return ret;
        }
        public string getDocumentFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("documents");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "document");
            }
            return ret;
        }
        public string getImageFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("images");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "image");
            }
            return ret;
        }
        public string getApplicationFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("applications");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "application");
            }
            return ret;
        }
        public string getRingtoneFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("ringtones");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "ringtones");
            }
            return ret;
        }
        public string getMusicFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("music");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "audio");
            }
            return ret;
        }
        public string getVideoFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("video");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "video");
            }
            return ret;
        }
        public string getThemeFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("theme");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "theme");
            }
            return ret;
        }
        public string getWallpaperFullPath()
        {
            string ret = string.Empty;
            string s = getPropertyByName("wallpaper");
            if (!string.IsNullOrEmpty(s))
            {
                ret = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_profileXml), "wallpaper");
            }
            return ret;
        }
        public bool hasPSTTask()
        {
            bool ret = false;
            return ret;
        }
        public bool hasRMSTask()
        {
            bool ret = false;
            if (!string.IsNullOrEmpty(getPropertyByName("wallpaper")) ||
                !string.IsNullOrEmpty(getPropertyByName("theme")) ||
                !string.IsNullOrEmpty(getPropertyByName("video")) ||
                !string.IsNullOrEmpty(getPropertyByName("music")) ||
                !string.IsNullOrEmpty(getPropertyByName("ringtones")) ||
                !string.IsNullOrEmpty(getPropertyByName("applications")) ||
                !string.IsNullOrEmpty(getPropertyByName("images")) ||
                !string.IsNullOrEmpty(getPropertyByName("documents")) ||
                !string.IsNullOrEmpty(getPropertyByName("calendars")) ||
                !string.IsNullOrEmpty(getPropertyByName("contacts")))
                ret = true;
            return ret;
        }
        public bool hasKittingTask()
        {
            bool ret = false;
            string kittingfile = getKittingXmlFile();
            if (!System.IO.File.Exists(kittingfile))
            {

            }
            else
            {
                //if user merely clicks on setting tab on PM but does not configure any settings, even then a kitting.xml is generated in profile folder.
                //check if it is a kitting.xml without any settings configured
                XmlDocument dom = new XmlDocument();
                dom.Load(kittingfile);
                if (dom.DocumentElement != null)
                {
                    XmlNodeList Change = dom.SelectNodes("//view[@change='true']");
                    if (Change.Count != 0)
                    {
                        ret = true;
                    }
                }
            }
            return ret;
        }
    }
}
