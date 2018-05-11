using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Mindbite.Mox.Attributes;
using Mindbite.Mox.Configuration.Apps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mindbite.Mox.Configuration
{ 
    public class Config
    {
        public AppCollection Apps { get; set; }
        public string Path { get; internal set; }
        public string SiteTitle { get; set; }

        public Config()
        {
            this.Apps = new AppCollection();
        }
    }
}
