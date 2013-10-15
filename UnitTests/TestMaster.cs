using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CBIR.UnitTests
{
    static class TestMaster
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        static void Main()
        {
            Console.WriteLine(System.Environment.NewLine + "Starting CBIR function tests");
            Console.WriteLine("-----------------------------------------");
            CBIRTests.DoTests();
            Console.WriteLine("-----------------------------------------"+System.Environment.NewLine);
        }
    }
}
