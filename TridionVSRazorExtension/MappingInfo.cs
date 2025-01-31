﻿using System.Collections.Generic;
using System.Xml.Serialization;

namespace SDL.TridionVSRazorExtension
{
    public class MappingInfo
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Host")]
        public string Host { get; set; }

        [XmlIgnore]
        public string Username
        {
            get
            {
                return Functions.GetFromIsolatedStorage(this.Name + "_user");
            }
            set
            {
                Functions.SaveToIsolatedStorage(this.Name + "_user", value);
            }
        }

        [XmlIgnore]
        public string Password
        {
            get
            {
                return Functions.GetFromIsolatedStorage(this.Name + "_pwd");
            }
            set
            {
                Functions.SaveToIsolatedStorage(this.Name + "_pwd", value);
            }
        }

        [XmlAttribute("TimeZoneId")]
        public string TimeZoneId { get; set; }

        public List<TridionFolderInfo> TridionFolders { get; set; }

        public List<ProjectFolderInfo> ProjectFolders { get; set; }

        [XmlIgnore]
        public bool Valid
        {
            get
            {
                return Functions.GetFromIsolatedStorage(this.Name + "_valid") == "1";
            }
            set
            {
                Functions.SaveToIsolatedStorage(this.Name + "_valid", value ? "1" : "");
            }
        }

    }
}