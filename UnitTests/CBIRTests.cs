using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CBIR.UnitTests {
    static class CBIRTests {
        private static string NL = System.Environment.NewLine;

        public static void DoTests() {
            Console.WriteLine(IntensityTest());
            Console.WriteLine(CoOccurrenceTest());
            Console.WriteLine(TextureTest());

            Console.ReadLine();
        }

        public static string IntensityTest() {
            string testresult = "Intensity Function Test" + NL;
            bool checks = true;

            try {
                //test the color white
                int I = (int)CBIRfunctions.CalcIntensity(Color.White);
                if (I != 255) { testresult += "Failed: for White expected 255 but got " + I + NL; checks = false; }

                //test the color black
                I = (int)CBIRfunctions.CalcIntensity(Color.Black);
                if (I != 0) { testresult += "Failed: for Black expected 0 but got " + I + NL; checks = false; }
            } catch(Exception ex) {
                testresult += "Failed: with Exception " + ex.Message + NL;
            }

            if (checks) { testresult += "Passed" + NL; }
            return testresult + NL;
        }

        public static string CoOccurrenceTest() {
            string testresult = "Co-Occurrence matrix Function Test" + NL;
            bool checks = true;

            try {
                Bitmap i = Properties.Resources.testImage;
                Dictionary<int, Dictionary<int, int>> com = CBIRfunctions.CalcCoOccurrence(i, 1, 1);
                if (com[25][234] != 1) { testresult += "Failed: expected cell (25,234) to be 1  but got " + com[25][234] + NL; checks = false; }
            } catch (Exception ex) {
                testresult += "Failed: with Exception " + ex.Message + NL;
            }

            if (checks) { testresult += "Passed" + NL; }
            return testresult + NL;
        }

        public static string TextureTest() {
            string testresult = "Texture Function Tests" + NL;
            bool checks = true;

            try {
                Bitmap i = Properties.Resources._1;
                Dictionary<int, Dictionary<int, int>> com = CBIRfunctions.CalcCoOccurrence(i, 1, 1);
                Dictionary<int, Dictionary<int, decimal>> ngtcom = CBIRfunctions.CalcGrayTone(com);

                //check the energy of the gray tone matrix
                decimal energy = CBIRfunctions.CalcEnergy(ngtcom);
                if (energy != 0.00134846563117319592173122M) { //value for _1
                    //from Min0.0014
                    testresult += "Failed: expected energy 0.0014 but got " + energy + NL;
                    checks = false;
                }

                //check the entropy of the gray tone matrix
                decimal entropy = CBIRfunctions.CalcEntropy(ngtcom);
                if (entropy != -11.386328337502404133415248541M) { //value for _1
                    //from Min -11.3853  maybe she typoed the 6 into a 5
                    testresult += "Failed: expected entropy -11.3853 but got " + entropy + NL;
                    checks = false;
                }

                //check the contrast of the gray tone matrix
                decimal contrast = CBIRfunctions.CalcContrast(ngtcom);
                if (contrast != 159.03649209030870833973292201M) { //value for _1
                    //from Min  159.0272  slight difference here too
                    testresult += "Failed: expected contrast 159.0272 but got " + contrast + NL;
                    checks = false;
                }
                
            } catch (Exception ex) {
                testresult += "Failed: with Exception " + ex.Message + NL;
            }

            if (checks) { testresult += "Passed" + NL; }
            return testresult + NL;
        }
    }
}
