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

            try { //loading image testImage
                Bitmap i = Properties.Resources.testImage;
                int[][] image = CBIRfunctions.CalcIntensityMatrix(i);
                Console.WriteLine(HF.PrintMatrix(image) + "\n");
                Dictionary<int, Dictionary<int, int>> com = CBIRfunctions.CalcCoOccurrence(image, 1, 1);
                Console.WriteLine(HF.PrintMatrix(com) + "\n");
                if (com[25][234] != 1) { testresult += "Failed: expected cell (25,234) to be 1  but got " + com[25][234] + NL; checks = false; }
            } catch (Exception ex) {
                testresult += "Failed: with Exception " + ex.Message + NL;
            }

            try { //loading image testImage
                int[][] image = new int[][] { new int[] {2,2,0,0},
                                              new int[] {2,2,0,0},
                                              new int[] {0,0,3,3},
                                              new int[] {0,0,3,3},
                                            };
                Console.WriteLine(HF.PrintMatrix(image) + "\n");
                Dictionary<int, Dictionary<int, int>> com = CBIRfunctions.CalcCoOccurrence(image, 1, 1);
                Console.WriteLine(HF.PrintMatrix(com) + "\n");
            } catch (Exception ex) {
                testresult += "Failed: with Exception " + ex.Message + NL;
            }

            if (checks) { testresult += "Passed" + NL; }
            return testresult + NL;
        }

        public static string TextureTest() {
            string testresult = "Texture Function Tests" + NL;
            bool checks = true;
            decimal tolerance = 0.00005M; //Professor Chen gave numbers rounded to 4 decimal 
                                          //places so assume a tolerance of .5 in the 5th decimal place

            //test texture features on the sample matrix in the assignment
            try {
                int[][] image = new int[][] { new int[] {2,2,0,0},
                                              new int[] {2,2,0,0},
                                              new int[] {0,0,3,3},
                                              new int[] {0,0,3,3},
                                            };
                Console.WriteLine(HF.PrintMatrix(image) + "\n");
                Dictionary<int, Dictionary<int, int>> com = CBIRfunctions.CalcCoOccurrence(image, 1, 1);
                Dictionary<int, Dictionary<int, decimal>> ngtcom = CBIRfunctions.CalcGrayTone(com);

                //check the energy of the gray tone matrix
                decimal energy = CBIRfunctions.CalcEnergy(ngtcom);
                if (Math.Abs(energy - 0.1852M) > tolerance) {
                    //from Min0.1852
                    testresult += "Failed: assignment image expected energy 0.1852 but got " + energy + NL;
                    checks = false;
                }

                //check the entropy of the gray tone matrix
                decimal entropy = CBIRfunctions.CalcEntropy(ngtcom);
                if (Math.Abs(entropy - -2.5033M) > tolerance) {
                    //from Min -2.5033 
                    testresult += "Failed: assignment image expected entropy -2.5033 but got " + entropy + NL;
                    checks = false;
                }

                //check the contrast of the gray tone matrix
                decimal contrast = CBIRfunctions.CalcContrast(ngtcom);
                if (Math.Abs(contrast - 3M) > tolerance) {
                    //from Min  3 
                    testresult += "Failed: assignment image expected contrast 3 but got " + contrast + NL;
                    checks = false;
                }
            } catch (Exception ex) {
                testresult += "Failed: with Exception " + ex.Message + NL;
            }

            //test features against 1.jpg
            try {
                Bitmap i = Properties.Resources._1;
                int[][] image = CBIRfunctions.CalcIntensityMatrix(i);
                Dictionary<int, Dictionary<int, int>> com = CBIRfunctions.CalcCoOccurrence(image, 1, 1);
                Dictionary<int, Dictionary<int, decimal>> ngtcom = CBIRfunctions.CalcGrayTone(com);

                //check the energy of the gray tone matrix
                decimal energy = CBIRfunctions.CalcEnergy(ngtcom);
                if (Math.Abs(energy - 0.0014M) > tolerance) { 
                    //from Min 0.0014
                    testresult += "Failed: 1.jpg expected energy 0.0014 but got " + energy + NL;
                    checks = false;
                }

                //check the entropy of the gray tone matrix
                decimal entropy = CBIRfunctions.CalcEntropy(ngtcom);
                if (Math.Abs(entropy - -11.3853M) > tolerance) { 
                    //from Min -11.3853  maybe she typoed the 6 into a 5
                    testresult += "Failed: 1.jpg expected entropy -11.3853 but got " + entropy + NL;
                    checks = false;
                }

                //check the contrast of the gray tone matrix
                decimal contrast = CBIRfunctions.CalcContrast(ngtcom);
                if (Math.Abs(contrast - 159.0272M) > tolerance) {
                    //from Min  159.0272  slight difference here too
                    testresult += "Failed: 1.jpg expected contrast 159.0272 but got " + contrast + NL;
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
