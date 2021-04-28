using System;
using System.Collections.Generic;
using System.IO;
using Dicom;
using Dicom.Imaging.Codec;
using static Dicom.DicomTag;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DicomConsoleApp
{
    class Program
    {
        private static readonly string myDicomDirectoryToBeSearched = @"\\INBLRH05022WSPR.ad005.onehc.net\AI-Rad-Repository\Image-pool\Stingray-iTest\";
        private static readonly string myUncompressedFilePath = @"D:\SourceCodes\ConsoleApps\Uncompressed\";
        private static readonly string myLogFilePath = @"D:\SourceCodes\ConsoleApps\";

        static void Main(string[] args)
        {
            myDicomFileListInTheDirectory = GetDicomFileListInTheDirectory();
            //FindFileForSingleTag();
            //FindFileForDoubleTag();
            //UncompressDicomFiles();
            RunAllQualityGateFilters();

            Console.ReadLine();
        }

        private static void FindFileForSingleTag()
        {
            ConsoleLog("applicationRunningForSingleTag");
            StartTimer();

            bool requiredFileFound = false;
            List<string> successDicomFiles = new List<string>();
            List<string> failDicomFiles = new List<string>();
            List<string> errorDicomFiles = new List<string>();

            int fileNo = 0;
            foreach (var file in myDicomFileListInTheDirectory)
            {
                fileNo++;
                try
                {
                    DicomDataset ds = DicomFile.Open(file).Dataset;

                    if (ds.TryGetValue<string>(PixelIntensityRelationship, 0, out string value))
                    {
                        if (value == "LIN" || value == "LOG" || value == "DISP")
                        {
                            ConsoleLog("success", fileNo, file);
                            successDicomFiles.Add(file);
                            requiredFileFound = true;
                        }
                        else
                        {
                            ConsoleLog("fail", fileNo, file);
                            failDicomFiles.Add(file);
                        }
                    }
                    else
                    {
                        ConsoleLog("fail", fileNo, file);
                        failDicomFiles.Add(file);
                    }
                }
                catch (Exception e)
                {
                    ConsoleLog("error", fileNo, file);
                    errorDicomFiles.Add(file);
                    continue;
                }
            }

            if (requiredFileFound == false)
            {
                ConsoleLog("noFileFound");
            }

            ConsoleLog("lineSeparator");
            StopTimer();
            successDicomFiles.ForEach(Console.WriteLine);
            WriteToFiles(successDicomFiles, failDicomFiles, errorDicomFiles);
            ConsoleLog("CompletedExecution");
            ConsoleLog("lineSeparator");
        }
        private static void FindFileForDoubleTag()
        {
            ConsoleLog("applicationRunningForDoubleTag");
            StartTimer();

            bool requiredFileFound = false;
            List<string> successDicomFiles = new List<string>();
            List<string> failDicomFiles = new List<string>();
            List<string> errorDicomFiles = new List<string>();

            int fileNo = 0;
            foreach (var file in myDicomFileListInTheDirectory)
            {
                fileNo++;
                try
                {
                    DicomDataset ds = DicomFile.Open(file).Dataset;
                    bool success = false;
                    if (ds.TryGetValue<string>(PatientOrientation, 0, out string value))
                    {
                        if (value.Trim() == @"L\F")
                        {
                            if (ds.TryGetValue<string>(PixelIntensityRelationship, 0, out var value2))
                            {
                                if (value2 == "LIN" || value2 == "LOG" || value2 == "DISP")
                                {
                                    ConsoleLog("success", fileNo, file);
                                    successDicomFiles.Add(file);
                                    requiredFileFound = true;
                                    success = true;
                                }
                            }
                        }
                    }

                    if (!success)
                    {
                        ConsoleLog("fail", fileNo, file);
                        failDicomFiles.Add(file);
                    }
                    else
                    {
                        success = false;
                    }
                }
                catch (Exception e)
                {
                    ConsoleLog("error", fileNo, file);
                    errorDicomFiles.Add(file);
                    continue;
                }
            }

            if (requiredFileFound == false)
            {
                ConsoleLog("noFileFound");
            }

            ConsoleLog("lineSeparator");
            StopTimer();
            successDicomFiles.ForEach(Console.WriteLine);
            WriteToFiles(successDicomFiles, failDicomFiles, errorDicomFiles);
            ConsoleLog("CompletedExecution");
            ConsoleLog("lineSeparator");
        }
        private static void UncompressDicomFiles()
        {
            ConsoleLog("applicationRunningForUncompressDicom");
            StartTimer();

            foreach (var file in myDicomFileListInTheDirectory)
            {
                try
                {
                    var inputFile = DicomFile.Open(file);
                    var outputFile = DicomCodecExtensions.Clone(inputFile, DicomTransferSyntax.ExplicitVRLittleEndian);
                    var outputFileName = file.Split('\\').Last();
                    outputFile.Save(myUncompressedFilePath + outputFileName);
                }
                catch (Exception e)
                {
                    continue;
                }
            }

            StopTimer();
            ConsoleLog("CompletedExecution");
            ConsoleLog("lineSeparator");
        }
        private static List<string> GetDicomFileListInTheDirectory()
        {
            Console.WriteLine($"Getting all dicom files, recursively. Folder: {myDicomDirectoryToBeSearched}");
            StartTimer();
            List<string> dicomFilesFound = new List<string>();

            string[] dcmFiles = Directory.GetFiles(myDicomDirectoryToBeSearched, "*.dcm", SearchOption.AllDirectories);
            string[] imaFiles = Directory.GetFiles(myDicomDirectoryToBeSearched, "*.ima", SearchOption.AllDirectories);
            dicomFilesFound.AddRange(dcmFiles);
            dicomFilesFound.AddRange(imaFiles);

            Console.WriteLine($"Total files Found {dicomFilesFound.Count()}");
            StopTimer();
            ConsoleLog("CompletedExecution");
            ConsoleLog("lineSeparator");

            return dicomFilesFound;
        }

        #region Quality Gate
        private static void RunAllQualityGateFilters()
        {
            ConsoleLog("applicationRunningForQualityGate");
            StartTimer();

            bool requiredFileFound = false;
            List<string> successDicomFiles = new List<string>();
            List<string> failDicomFiles = new List<string>();
            List<string> errorDicomFiles = new List<string>();

            int fileNo = 0;
            foreach (var file in myDicomFileListInTheDirectory)
            {
                fileNo++;
                try
                {
                    DicomDataset ds = DicomFile.Open(file).Dataset;
                    List<string> failedFiltersList = new List<string>();
                    bool isPassedQualityGate = true;

                    if (!ApplyFilter(ds, QualityGateFilters.BitsAllocated))
                    { failedFiltersList.Add(QualityGateFilters.BitsAllocated.ToString()); isPassedQualityGate = false; }

                    if (!ApplyFilter(ds, QualityGateFilters.BitsStored))
                    { failedFiltersList.Add(QualityGateFilters.BitsStored.ToString()); isPassedQualityGate = false; }

                    if (!ApplyFilter(ds, QualityGateFilters.BodyPartExamined))
                    { failedFiltersList.Add(QualityGateFilters.BodyPartExamined.ToString()); isPassedQualityGate = false; }

                    if (!(ApplyFilter(ds, QualityGateFilters.PatientAge) || ApplyFilter(ds, QualityGateFilters.PatientBirthDate)))
                    {
                        failedFiltersList.Add(QualityGateFilters.PatientAge.ToString());
                        failedFiltersList.Add(QualityGateFilters.PatientBirthDate.ToString());
                        isPassedQualityGate = false;
                    }

                    if (!ApplyFilter(ds, QualityGateFilters.PatientOrientation))
                    { failedFiltersList.Add(QualityGateFilters.PatientOrientation.ToString()); isPassedQualityGate = false; }

                    if (!ApplyFilter(ds, QualityGateFilters.PhotometricInterpretation))
                    { failedFiltersList.Add(QualityGateFilters.PhotometricInterpretation.ToString()); isPassedQualityGate = false; }

                    if (!ApplyFilter(ds, QualityGateFilters.PixelIntensityRelationship))
                    { failedFiltersList.Add(QualityGateFilters.PixelIntensityRelationship.ToString()); isPassedQualityGate = false; }

                    if (!ApplyFilter(ds, QualityGateFilters.PixelRepresentation))
                    { failedFiltersList.Add(QualityGateFilters.PixelRepresentation.ToString()); isPassedQualityGate = false; }

                    if (!(ApplyFilter(ds, QualityGateFilters.ImagerPixelSpacing) || ApplyFilter(ds, QualityGateFilters.PixelSpacing)))
                    {
                        failedFiltersList.Add(QualityGateFilters.ImagerPixelSpacing.ToString());
                        failedFiltersList.Add(QualityGateFilters.PixelSpacing.ToString());
                        isPassedQualityGate = false;
                    }

                    if (!ApplyFilter(ds, QualityGateFilters.SamplesPerPixel))
                    { failedFiltersList.Add(QualityGateFilters.SamplesPerPixel.ToString()); isPassedQualityGate = false; }

                    if (isPassedQualityGate)
                    {
                        ConsoleLog("success", fileNo, file);
                        successDicomFiles.Add(file);
                        requiredFileFound = true;
                    }
                    else
                    {
                        ConsoleLog("fail", fileNo, file, string.Join(",", failedFiltersList));
                        failDicomFiles.Add(file);
                    }
                }
                catch (Exception e)
                {
                    ConsoleLog("error", fileNo, file);
                    errorDicomFiles.Add(file);
                    continue;
                }
            }

            if (requiredFileFound == false)
            {
                ConsoleLog("noFileFound");
            }

            ConsoleLog("lineSeparator");
            StopTimer();
            successDicomFiles.ForEach(Console.WriteLine);
            WriteToFiles(successDicomFiles, failDicomFiles, errorDicomFiles);
            ConsoleLog("CompletedExecution");
            ConsoleLog("lineSeparator");
        }
        private static bool ApplyFilter(DicomDataset dicomDataset, QualityGateFilters filterName)
        {
            switch (filterName)
            {
                case QualityGateFilters.BodyPartExamined:
                    if (dicomDataset.TryGetValue<string>(BodyPartExamined, 0, out string bodyPartExaminedValue) && bodyPartExaminedValue != null)
                    {
                        var value = bodyPartExaminedValue.Trim().ToUpper();
                        if (value == "CHEST" || value == "THORAX")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.PatientAge:
                    if (dicomDataset.TryGetValue<string>(DicomTag.PatientAge, 0, out string patientAgeValue) && patientAgeValue != null)
                    {
                        var age = ParseAge(patientAgeValue);
                        if (age >= 22)
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.PatientBirthDate:
                    if (dicomDataset.TryGetValue<string>(DicomTag.PatientBirthDate, 0, out var dob) && dob != null && dicomDataset.TryGetValue<string>(DicomTag.StudyDate, 0, out var studyDate) && studyDate != null)
                    {
                        var age = CalculateAge(studyDate, dob);
                        if (age >= 22)
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.PhotometricInterpretation:
                    if (dicomDataset.TryGetValue<string>(DicomTag.PhotometricInterpretation, 0, out var photometricInterpretationValue) && photometricInterpretationValue != null)
                    {
                        var value = photometricInterpretationValue.Trim().ToUpper();
                        if (value == "MONOCHROME1" || value == "MONOCHROME2")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.BitsAllocated:
                    if (dicomDataset.TryGetValue<string>(DicomTag.BitsAllocated, 0, out var bitsAllocatedValue) && bitsAllocatedValue != null)
                    {
                        var value = bitsAllocatedValue.Trim().ToUpper();
                        if (value == "16")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.SamplesPerPixel:
                    if (dicomDataset.TryGetValue<string>(DicomTag.SamplesPerPixel, 0, out var samplesPerPixelValue) && samplesPerPixelValue != null)
                    {
                        var value = samplesPerPixelValue.Trim().ToUpper();
                        if (value == "1")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.BitsStored:
                    if (dicomDataset.TryGetValue<string>(DicomTag.BitsStored, 0, out var bitsStoredValue) && bitsStoredValue != null)
                    {
                        var value = bitsStoredValue.Trim().ToUpper();
                        if (value == "10" || value == "11" || value == "12" || value == "13" || value == "14" || value == "15" || value == "16")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.PixelIntensityRelationship:
                    if (dicomDataset.TryGetValue<string>(PixelIntensityRelationship, 0, out string pixelIntensityRelationshipValue) && pixelIntensityRelationshipValue != null)
                    {
                        var value = pixelIntensityRelationshipValue.Trim().ToUpper();
                        if (value == "LIN" || value == "LOG" || value == "DISP")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.PixelRepresentation:
                    if (dicomDataset.TryGetValue<string>(PixelRepresentation, 0, out string pixelRepresentationValue) && pixelRepresentationValue != null)
                    {
                        var value = pixelRepresentationValue.Trim().ToUpper();
                        if (value == "0")
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.PatientOrientation:
                    if (dicomDataset.TryGetValues<string>(PatientOrientation, out string[] patientOrientationValue) && patientOrientationValue != null)
                    {
                        var allowedValues = new string[][] { new string[] { "L", "F" }, new string[] { "R", "F" } };
                        return allowedValues.Any(patientOrientationValue.SequenceEqual);
                    }
                    break;
                case QualityGateFilters.PixelSpacing:
                    if (dicomDataset.TryGetValues(DicomTag.PixelSpacing, out double[] pixelSpacingValues) && pixelSpacingValues != null)
                    {
                        if (pixelSpacingValues.Length == 2)
                        {
                            return true;
                        }
                    }
                    break;
                case QualityGateFilters.ImagerPixelSpacing:
                    if (dicomDataset.TryGetValues(DicomTag.ImagerPixelSpacing, out double[] imagerPixelSpacingValues) && imagerPixelSpacingValues != null)
                    {
                        if (imagerPixelSpacingValues.Length == 2)
                        {
                            return true;
                        }
                    }
                    break;
                default:
                    break;
            }
            // view position filter is not required here

            return false;
        }
        private static int ParseAge(string value)
        {
            var group = Regex.Match(value, @"(\d+)Y").Groups;
            return int.TryParse(group[1].Value, out int age) ? age : 0;
        }
        private static int CalculateAge(string studyDate, string dateOfBirth)
        {
            int age = 0;
            System.DateTime.TryParseExact(dateOfBirth, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces,
                           out DateTime birthDate);
            System.DateTime.TryParseExact(studyDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces,
                          out DateTime DateofStudy);
            age = DateofStudy.Year - birthDate.Year;
            if (DateofStudy.DayOfYear < birthDate.DayOfYear)
                age -= 1;

            return age;
        }
        #endregion

        #region Private Functions
        private static void StartTimer()
        {
            myWatch.Start();
        }
        private static void StopTimer()
        {
            myWatch.Stop();
            ConsoleLog("executionTime");
        }
        private static void WriteToSuccessFile(List<string> fileNames)
        {
            using (StreamWriter file = new StreamWriter(myLogFilePath + "SuccessFile.txt", false))
            {
                foreach (string item in fileNames)
                {
                    file.WriteLine(item);
                }
            }
        }
        private static void WriteToFailFile(List<string> fileNames)
        {
            using (StreamWriter file = new StreamWriter(myLogFilePath + "FailFile.txt", false))
            {
                foreach (string item in fileNames)
                {
                    file.WriteLine(item);
                }
            }
        }
        private static void WriteToErrorFile(List<string> fileNames)
        {
            using (StreamWriter file = new StreamWriter(myLogFilePath + "ErrorFile.txt", false))
            {
                foreach (string item in fileNames)
                {
                    file.WriteLine(item);
                }
            }
        }
        private static void WriteToFiles(List<string> successDicomFiles, List<string> failDicomFiles, List<string> errorDicomFiles)
        {
            WriteToSuccessFile(successDicomFiles);
            WriteToFailFile(failDicomFiles);
            WriteToErrorFile(errorDicomFiles);
        }
        private static void ConsoleLog(string logInfo, int? fileNo = null, string fileName = null, string message = null)
        {
            if (logInfo == "success" || logInfo == "fail" || logInfo == "error")
            {
                Console.WriteLine($"{logInfo.ToUpper()}. File No. {fileNo}: {fileName}");
                Console.WriteLine(message);
                Console.WriteLine();
            }
            else if (logInfo == "noFileFound")
            {
                Console.WriteLine("No such files found.");
            }
            else if (logInfo == "CompletedExecution")
            {
                Console.WriteLine("Completed execution");
            }
            else if (logInfo == "lineSeparator")
            {
                Console.WriteLine("--------------------------------------------------");
            }
            else if (logInfo == "applicationRunningForSingleTag")
            {
                Console.WriteLine($"Application Running: {SearchDescription_ForSingleTag}");

            }
            else if (logInfo == "applicationRunningForDoubleTag")
            {
                Console.WriteLine($"Application Running: {SearchDescription_ForDoubleTag}");
            }
            else if (logInfo == "applicationRunningForUncompressDicom")
            {
                Console.WriteLine($"Application Running: {SearchDescription_UncompressDicom}");
            }
            else if (logInfo == "applicationRunningForQualityGate")
            {
                Console.WriteLine($"Application Running: {SearchDescription_ForQualityGate}");
            }
            else if (logInfo == "executionTime")
            {
                Console.WriteLine($"Execution Time: {myWatch.ElapsedMilliseconds} ms");
            }
            else
            {
                Console.WriteLine("**Wrong Log Info**");
            }
        }
        #endregion

        #region Enums
        public enum QualityGateFilters
        {
            BodyPartExamined,
            PatientAge,
            PatientBirthDate,
            PhotometricInterpretation,
            BitsAllocated,
            SamplesPerPixel,
            BitsStored,
            PixelIntensityRelationship,
            PixelRepresentation,
            PatientOrientation,
            PixelSpacing,
            ImagerPixelSpacing
        }
        #endregion

        #region Private Constants/Variables
        private static readonly string SearchDescription_ForSingleTag = "Searching for dicom files having bits allocated 16 bit.";
        private static readonly string SearchDescription_ForDoubleTag = "Searching for Mono1 dicom files having sample per pixel 3.";
        private static readonly string SearchDescription_UncompressDicom = "Uncompressing dicom files.";
        private static readonly string SearchDescription_ForQualityGate = "Running Quality Gate";
        private static Stopwatch myWatch = new Stopwatch();
        private static List<string> myDicomFileListInTheDirectory;
        #endregion
    }
}
