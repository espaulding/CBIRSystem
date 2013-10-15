using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CBIR.UnitTests
{
    static class CBIRTests
    {
        public static void DoTests()
        {
            if (IntensityTest())
            {
                Console.WriteLine("passed: intensity function working");
            }
            else
            {
                Console.WriteLine("failed: intensity function is broken");
            }

            if (CoOccurrenceTest())
            {
                Console.WriteLine("passed: co-occurrence function working");
            }
            else
            {
                Console.WriteLine("failed: co-occurrence function is broken");
            }

            if (EnergyTest())
            {
                Console.WriteLine("passed: energy function working");
            }
            else
            {
                Console.WriteLine("failed: energy function is broken");
            }

            if (EntropyTest())
            {
                Console.WriteLine("passed: entropy function working");
            }
            else
            {
                Console.WriteLine("failed: entropy function is broken");
            }

            if (ContrastTest())
            {
                Console.WriteLine("passed: contrast function working");
            }
            else
            {
                Console.WriteLine("failed: contrast function is broken");
            }

            Console.ReadLine();
        }

        public static bool IntensityTest()
        {
            int I = (int)CBIRfunctions.CalcIntensity(Color.White);
            if (I == 255) { return true; }
            return false;
        }

        public static bool CoOccurrenceTest()
        {
            Bitmap i = Properties.Resources.testImage;
            Dictionary<int,Dictionary<int,double>> cooccur = CBIRfunctions.CalcCoOccurrence(i, 1, 1);
            if (cooccur[25][234] == 1) { return true; }
            return false;
        }

        public static bool EnergyTest()
        {
            Bitmap i = Properties.Resources.testImage;
            Dictionary<int, Dictionary<int, double>> cooccur = CBIRfunctions.CalcCoOccurrence(i, 1, 1);
            Dictionary<int, Dictionary<int, double>> grayTone = CBIRfunctions.CalcGrayTone(cooccur);
            double energy = CBIRfunctions.CalcEnergy(grayTone);
            if (energy == 0.11111111111111109) { return true; }
            return false;
        }

        public static bool EntropyTest()
        {
            Bitmap i = Properties.Resources.testImage;
            Dictionary<int, Dictionary<int, double>> cooccur = CBIRfunctions.CalcCoOccurrence(i, 1, 1);
            Dictionary<int, Dictionary<int, double>> grayTone = CBIRfunctions.CalcGrayTone(cooccur);
            double entropy = CBIRfunctions.CalcEntropy(grayTone);
            if (entropy == -3.1699250014423122) { return true; }
            return false;
        }

        public static bool ContrastTest()
        {
            Bitmap i = Properties.Resources.testImage;
            Dictionary<int, Dictionary<int, double>> cooccur = CBIRfunctions.CalcCoOccurrence(i, 1, 1);
            Dictionary<int, Dictionary<int, double>> grayTone = CBIRfunctions.CalcGrayTone(cooccur);
            double contrast = CBIRfunctions.CalcContrast(grayTone);
            if (contrast == 12581.666666666668) { return true; }
            return false;
        }
    }
}
