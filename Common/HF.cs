using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System;

using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;

namespace CBIR
{
    //HF == HelperFunctions
    static class HF
    {
        public static void Serialize(string filename, ISerializable objectToSerialize)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            BinaryFormatter bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, objectToSerialize);
            stream.Close();
        }

        public static ISerializable DeSerialize(string filename)
        {
            ISerializable objectToSerialize;
            Stream stream = File.Open(filename, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            objectToSerialize = (ISerializable)bFormatter.Deserialize(stream);
            stream.Close();
            return objectToSerialize;
        }

        static public Image BytesToImage(byte[] byteArray, ImageFormat formatOfImage)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(byteArray))
                {
                    Image img = Image.FromStream(ms);
                    //img.Save(ms, formatOfImage); //this line is causing an exception
                    return img;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        static public Bitmap BytesToBitmap(byte[] byteArray)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(byteArray))
                {
                    Bitmap img = (Bitmap)Image.FromStream(ms);
                    return img;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        static public Bitmap reScaleImage(System.Drawing.Image iImage, int lnWidth, int lnHeight, string sScale)
        {
            /// Taken from http://west-wind.com/weblog/posts/283.aspx
            /// returns bitmap or null
            Bitmap bmpOut = null;
            try
            {
                decimal lnRatio;
                int lnNewWidth = 0;
                int lnNewHeight = 0;
                sScale = sScale.ToLower();

                switch (sScale.ToLower())
                {
                    case "noscale":
                        {
                            lnNewHeight = lnHeight;
                            lnNewWidth = lnWidth;
                            break;
                        }
                    case "height":
                        {
                            lnRatio = (decimal)lnHeight / iImage.Height;
                            lnNewHeight = lnHeight;
                            decimal lnTemp = iImage.Width * lnRatio;
                            lnNewWidth = (int)lnTemp;
                            break;
                        }
                    case "width":
                        {
                            lnRatio = (decimal)lnWidth / iImage.Width;
                            lnNewWidth = lnWidth;
                            decimal lnTemp = iImage.Height * lnRatio;
                            lnNewHeight = (int)lnTemp;
                            break;
                        }
                    default:
                        {
                            if (iImage.Width > iImage.Height)
                            {
                                lnRatio = (decimal)lnWidth / iImage.Width;
                                lnNewWidth = lnWidth;
                                decimal lnTemp = iImage.Height * lnRatio;
                                lnNewHeight = (int)lnTemp;
                            }
                            else
                            {
                                lnRatio = (decimal)lnHeight / iImage.Height;
                                lnNewHeight = lnHeight;
                                decimal lnTemp = iImage.Width * lnRatio;
                                lnNewWidth = (int)lnTemp;
                            }
                            break;
                        }
                }
                // System.Drawing.Image imgOut = loBMP.GetThumbnailImage(lnNewWidth,lnNewHeight,null,IntPtr.Zero);
                // *** This code creates cleaner (though bigger) thumbnails and properly
                // *** handles GIF files better by generating a white background for
                // *** transparent images (as opposed to black)

                bmpOut = new Bitmap(lnNewWidth, lnNewHeight);
                Graphics g = Graphics.FromImage(bmpOut);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                //g.FillRectangle(Brushes.White, 0, 0, lnNewWidth, lnNewHeight);
                g.DrawImage(iImage, 0, 0, lnNewWidth, lnNewHeight);
            }
            catch (Exception)
            {
                //if bmpOut is null here you probably ran out of memory so
                //let's at least give the original image back
                return (Bitmap)iImage;
            }

            if (bmpOut == null) { return (Bitmap)iImage; }
            iImage.Dispose(); //cleanup after ourselves
            return bmpOut;
        }
    }
}
