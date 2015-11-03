using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotJEM.Web.Host.Providers.Pipeline
{
    public class PipelineContext
    {
        public Hashtable Items { get; set; }

        public PipelineContext()
        {
            Items = new Hashtable();
        }
    }
}
