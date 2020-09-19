using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipesServer
{
    public class Client
    {
        private Int32 pipeHandle = -1;

        public Int32 PipeHandle
        {
            get { return pipeHandle; }
            set { pipeHandle = value; }
        }
    }
}
