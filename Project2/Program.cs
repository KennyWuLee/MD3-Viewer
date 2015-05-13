using System;
using System.Collections.Generic;

namespace Project2
{
    /*
     * animation issue:
     * is: frame issue
     * could be caused by grabbing the wrong frame somewhere
     * could ALSO be: a tag issue, because of some of the transforms
     * 
     * */

    /// <summary>
    /// Simple Project2 application using SharpDX.Toolkit.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
#if NETFX_CORE
        [MTAThread]
#else
        [STAThread]
#endif
        static void Main()
        {
            using (var program = new Project2())
                program.Run();

        }
    }
}