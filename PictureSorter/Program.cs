using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace PictureSorter
{
    class Program
    {
        private static int fileMoved = 0;
        private static int duplicateFileFounds = 0;
        private static int totalFileMoved = 0;
        private static int totalVideosMoved = 0;
        private static int duplicateVideosFileFounds = 0;

        static void Main(string[] args)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // read all the file in the directory

            // output to a directory with hierarchy year-month and copy the file to that directory
            string inputDirectoryPath = @"G:\Moving\";

            string outputDirectoryPath = @"D:\SortedPicturesVersion3\";
            string outputDirectoryPathBasedOnCreateDate = @"D:\SortedPicturesVersion3\CreateDate\";
            string outputDirectoryPathForVideos = @"D:\SortedPicturesVersion3\Videos\";

            DirectoryInfo topDirectory = new DirectoryInfo(inputDirectoryPath);

            Stack<DirectoryInfo> processDirectories = new Stack<DirectoryInfo>();

            processDirectories.Push(topDirectory);

            while (processDirectories.Count() > 0)
            {
                var stackTop = processDirectories.Pop();

                foreach (DirectoryInfo currentDir in stackTop.EnumerateDirectories())
                {
                    processDirectories.Push(currentDir);
                }
                ProcessDirectoryForImages(outputDirectoryPath, outputDirectoryPathBasedOnCreateDate, stackTop);

                ProcessDirectoryForvideos(outputDirectoryPathForVideos, stackTop);
            }

            // the code that you want to measure comes here
            watch.Stop();

            //delete empty directories
            DeleteEmptyDirectories(topDirectory);

            Console.WriteLine($"Duplicate files found: {duplicateFileFounds}");
            Console.WriteLine($"Duplicate videos files found: {duplicateVideosFileFounds}");
            Console.WriteLine($"Total files moved: {totalFileMoved}");
            Console.WriteLine($"Total videos moved: {totalVideosMoved}");
            Console.WriteLine($"Time taken to process {watch.Elapsed.TotalMinutes}");
            Console.ReadLine();

        }

        private static void DeleteEmptyDirectories(DirectoryInfo topDirectory)
        {
            foreach (DirectoryInfo currentDir in topDirectory.EnumerateDirectories())
            {
                DeleteEmptyDirectories(currentDir);
            }
            if (topDirectory.EnumerateFiles().Any() == false && topDirectory.EnumerateDirectories().Any() == false)
            {
                topDirectory.Delete();
            }
        }

        private static void ProcessDirectoryForvideos(string outputDirectoryPath, DirectoryInfo currentDir)
        {
            List<string> videosExtensions = new List<string> { ".MOV", ".MP4", ".AVI", ".3GP", ".MPG", ".DAT" };
            Regex r = new Regex(":");

            foreach (FileInfo fileInfo in currentDir.GetFiles())
            {
                String fileFullPathName = fileInfo.FullName;

                if (!videosExtensions.Contains(fileInfo.Extension.ToUpper()))
                {
                    continue;
                }

                var pictureDateTaken = fileInfo.CreationTime;

                int year = pictureDateTaken.Year;
                string month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(pictureDateTaken.Month);

                string pictureOutputDirectoryPath = Path.Combine(outputDirectoryPath, year.ToString(), month.ToString());
                string outputFileNameWithoutExtension = Path.Combine(pictureOutputDirectoryPath, pictureDateTaken.ToString("yyy-MM-dd_HH-mm-ss_fff"));


                Directory.CreateDirectory(pictureOutputDirectoryPath);

                string outputFileName = outputFileNameWithoutExtension + fileInfo.Extension;

                int count = 0;
                while (File.Exists(outputFileName))
                {
                    FileInfo existingFileName = new FileInfo(outputFileName);

                    if (existingFileName.Length != fileInfo.Length)
                    {
                        outputFileNameWithoutExtension += "-Dup" + count++;
                        outputFileName = outputFileNameWithoutExtension + fileInfo.Extension;
                        continue;
                    }

                    duplicateVideosFileFounds++;
                    Console.WriteLine($" File deleted: {fileFullPathName}   : Duplicate file deleted {duplicateVideosFileFounds}");

                    fileInfo.Delete();
                    continue;
                }
                totalVideosMoved++;
                Console.WriteLine($" File moved: {fileFullPathName}   : Total file moved {totalVideosMoved}");

                fileInfo.MoveTo(outputFileName);
                fileMoved++;
            }
        }

        private static void ProcessDirectoryForImages(string outputDirectoryPath, string outputDirectoryPathBasedOnCreateDate, DirectoryInfo currentDir)
        {
            List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
            Regex r = new Regex(":");

            foreach (FileInfo fileInfo in currentDir.GetFiles())
            {
                bool fUsingCreationTime = false;
                String fileFullPathName = fileInfo.FullName;

                if (!ImageExtensions.Contains(fileInfo.Extension.ToUpper()))
                {
                    continue;
                }

                // Create image.
                Image currentImage = Image.FromFile(fileFullPathName);

                DateTime pictureDateTaken;
                try
                {
                    PropertyItem datePropItem = currentImage.GetPropertyItem(36867);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(datePropItem.Value), "-", 2);
                    pictureDateTaken = DateTime.Parse(dateTaken);

                }
                catch (ArgumentException e)
                {
                    fUsingCreationTime = true;
                    // use the file create to move the file but move the file to different folder
                    pictureDateTaken = fileInfo.CreationTime;
                }
                catch (FormatException e)
                {
                    fUsingCreationTime = true;
                    // use the file create to move the file but move the file to different folder
                    pictureDateTaken = fileInfo.CreationTime;
                }
                catch (PathTooLongException e)
                {
                    currentImage.Dispose();
                    continue;
                }

                //if (fUsingCreationTime)
                //{
                //    currentImage.Dispose();
                //    continue;
                //}

                int year = pictureDateTaken.Year;
                string month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(pictureDateTaken.Month);

                string pictureOutputDirectoryPath;

                if (fUsingCreationTime)
                {
                    pictureOutputDirectoryPath = Path.Combine(outputDirectoryPathBasedOnCreateDate, year.ToString(), month.ToString());
                }
                else
                {
                    pictureOutputDirectoryPath = Path.Combine(outputDirectoryPath, year.ToString(), month.ToString());
                }

                string outputFileNameWithoutExtension = Path.Combine(pictureOutputDirectoryPath, pictureDateTaken.ToString("yyy-MM-dd_HH-mm-ss_fff"));

                currentImage.Dispose();
                Directory.CreateDirectory(pictureOutputDirectoryPath);

                string outputFileName = outputFileNameWithoutExtension + fileInfo.Extension;

                int count = 0;
                bool fFileDeleted = false;
                while (File.Exists(outputFileName))
                {
                    if (fUsingCreationTime)
                    {
                        FileInfo existingFileName = new FileInfo(outputFileName);

                        if (existingFileName.Length != fileInfo.Length)
                        {
                            outputFileNameWithoutExtension += "-Dup" + count++;
                            outputFileName = outputFileNameWithoutExtension + fileInfo.Extension;
                            continue;
                        }
                        else
                        {
                            duplicateFileFounds++;
                            Console.WriteLine($" File deleted: {fileFullPathName}   : Duplicate file deleted {duplicateFileFounds}");

                            fFileDeleted = true;
                            fileInfo.Delete();
                            break;
                        }
                    }
                    else
                    {
                        duplicateFileFounds++;
                        Console.WriteLine($" File deleted: {fileFullPathName}   : Duplicate file deleted {duplicateFileFounds}");

                        fileInfo.Delete();

                        fFileDeleted = true;
                        break;

                    }
                }
                if (!fFileDeleted)
                {
                    totalFileMoved++;
                    Console.WriteLine($" File moved: {fileFullPathName}   : Total file moved {totalFileMoved}");

                    fileInfo.MoveTo(outputFileName);
                    fileMoved++;
                }
            }
        }
    }
}