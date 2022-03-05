using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DicomConsoleApps
{
    class DicomImageDrawing
    {
        public DicomImageDrawing()
        {
            string imageFile = @"D:\sc.jpg";
            string newFilePath = @"D:\sc1.dcm";
            string oldDicomFile = @"D:\sc.dcm";
            ImportImage(imageFile, newFilePath, oldDicomFile);
        }
        

        public bool ImportImage(string imageFile, string newFilePath, string oldDicomFile)
        {
            Bitmap bitmap = new Bitmap(imageFile);
            bitmap = DrawOnBitmap(bitmap);

            int rows, columns;
            byte[] pixels = GetPixels(bitmap, out rows, out columns);
            MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);
            DicomDataset dataset = new DicomDataset();
            var dicomfile = DicomFile.Open(oldDicomFile);
            dataset = dicomfile.Dataset.Clone();

            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)rows);
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)columns);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);

            DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            pixelData.SamplesPerPixel = 3;
            pixelData.HighBit = 7;
            pixelData.PhotometricInterpretation = PhotometricInterpretation.Rgb;
            pixelData.PixelRepresentation = 0;
            pixelData.PlanarConfiguration = 0;
            pixelData.Height = (ushort)rows;
            pixelData.Width = (ushort)columns;
            pixelData.AddFrame(buffer);

            dicomfile = new DicomFile(dataset);
            dicomfile.Save(newFilePath);
            return true;
        }

        private byte[] GetPixels(Bitmap bitmap, out int rows, out int columns)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                rows = bitmap.Height;
                columns = bitmap.Width;
                return stream.ToArray();
            }
        }

        public Bitmap DrawOnBitmap(Bitmap bmp)
        {
            RectangleF rectf = new RectangleF(1480, 1330, 500, 500);

            Graphics g = Graphics.FromImage(bmp);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawString("Lesion - 1", new Font("Tahoma", 30), Brushes.Black, rectf);

            g.Flush();

            return bmp;
        }
    }
}
