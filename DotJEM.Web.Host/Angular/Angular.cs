using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Angular
{
    public static class Angular
    {
        public static readonly IAngularContext Current = new AngularContext();

        public static IAngularContext Load(params string[] paths)
        {
            return Current.Load(paths);
        }

        public static IEnumerable<string> GetSources(params string[] paths)
        {
            var context = new AngularContext(paths);
            return context.Sources;
        }
    }


    //angular.module('dotjem', ['dotjem.core', 'dotjem.common', 'dotjem.plugin.blog', 'dotjem.routing', 'dotjem.angular.tree']);



    //<script src="assets/scripts/core/dotjem.core.module.js"></script>
    //<script src="assets/scripts/common/dotjem.common.module.js"></script>


    //<script src="assets/plugins/blog/scripts/dotjem.plugin.blog.module.js"></script>

    //<script src="assets/scripts/dotjem/dotjem.module.js"></script>
    //<script src="assets/scripts/dotjem/controllers/appController.js"></script>
    //<script src="assets/scripts/dotjem/controllers/authController.js"></script>
    //<script src="assets/scripts/dotjem/directives/dxSiteFooter.js"></script>
    //<script src="assets/scripts/dotjem/directives/dxSiteHeader.js"></script>
}