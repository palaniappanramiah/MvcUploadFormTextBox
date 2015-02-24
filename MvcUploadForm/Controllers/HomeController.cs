using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using Ionic.Zip;
using MvcUploadForm.Models;

namespace MvcUploadForm.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return View();
        }

        public ActionResult AddRepeatGroup()
        {
            return PartialView("_RepeatPartial");
        }

        public ActionResult Create()
        {
            return View();
        }

        // For multiple file select
        [HttpPost]
        public ActionResult Create(List<HttpPostedFileBase> file, List<String> password)
        {                  
            string message, timeStamp = "", defaultFileName = "", fileName = "", rawFileName, rawFileNameType = null, textFileNameType = null, fileSavePath, line, zipPath, extractPath, textFileName, extractedFileFormat = "Not a valid format";
            string[] localFiles, localExtractedFiles;
            int uploadedCount = 0, emptyFilesCount = 0, fileNumber = 0, format = 0, lineCounter, numFiles = file.Count;
            bool fileExists, extractedFileExists;
            DateTime startTime, endTime;
            TimeSpan elapsedTime;

            for (int filesCount = 0; filesCount < numFiles; filesCount++)
            {
                fileExists = false;
                lineCounter = 0;
                fileNumber = filesCount + 1;

                var uploadedFileLength = 0;
                var uploadedFile = file[filesCount];

                message = "<font color='Red'>";

                if (uploadedFile != null)
                {
                    uploadedFileLength = file[filesCount].ContentLength;

                    fileName = file[filesCount].FileName;
                    rawFileName = fileName.Remove(fileName.Length - 4);
                    rawFileNameType = fileName.Substring(fileName.Length - 4);

                    localFiles = Directory.GetFiles(Server.MapPath("~/Files/"), "*.*")
                                         .Select(path => Path.GetFileName(path)).ToArray();

                    for (int fileNamesCount = 0; fileNamesCount < localFiles.Length; fileNamesCount++)
                    {
                        if (fileName.Equals(localFiles[fileNamesCount]))
                            fileExists = true;
                    }

                    if (fileExists)
                    {
                        message = message + "<br/><br/>File " + fileNumber + ": " + fileName
                                          + " already exists!";
                        timeStamp = DateTime.UtcNow.ToString("MM-dd-yyyy_HH-mm-ss", CultureInfo.InvariantCulture);
                        defaultFileName = fileName;
                        fileName = rawFileName + "_" + timeStamp + rawFileNameType;
                        fileSavePath = Server.MapPath("~/Files/" + Path.GetFileName(fileName));
                    }
                    else
                        fileSavePath = Server.MapPath("~/Files/" + Path.GetFileName(fileName));
                    if (!(uploadedFile != null && uploadedFileLength > 0))
                    {
                        emptyFilesCount++;
                        if (emptyFilesCount == numFiles)
                            message = "<br/>Please select atleast a file.<br/>";
                    }

                    else if (uploadedFile.ContentType == "image/gif")
                        message = message + "<br/><br/>File " + fileNumber + ": " + fileName
                                          + " is not uploaded as it is a GIF Format, which is not supported!<br/>";
                    else
                    {
                        try
                        {
                            startTime = DateTime.Now;
                            uploadedFile.SaveAs(fileSavePath);
                            endTime = DateTime.Now;
                            elapsedTime = endTime - startTime;

                            if (fileExists)
                                message = message +
                                                  "</font><font color='Blue'> So, The filename has been changed to " +
                                                  fileName + " and uploaded.";
                            else
                                message = message + "<br/><br/></font><font color='Blue'>File " + fileNumber +
                                                  ": " + fileName + " is uploaded.";
                            message = message + "<br/>Elapsed Time: " + elapsedTime
                                              + " Milliseconds.<br/>Uploaded Date and Time: " + endTime
                                              + ".<br/>Uploaded IP address1: " +
                                              (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] ??
                                               Request.ServerVariables["REMOTE_ADDR"])
                                              + "</font><font color='Red'>";
                            uploadedCount++;
                        }
                        catch (Exception ex)
                        {
                            message = message + "ERROR:" + ex.Message.ToString() + "<br/>";
                        }

                        if (uploadedFile.ContentType == "application/zip" || uploadedFile.ContentType == "application/x-zip" ||
                            uploadedFile.ContentType == "application/x-zip-compressed" ||
                            uploadedFile.ContentType == "application/octet - stream")
                        {

                            extractedFileExists = false;

                            zipPath = Server.MapPath("~/Files/") + fileName;
                            
                            extractPath = Server.MapPath("~/ExtractedFiles/");

                            localExtractedFiles = Directory.GetFiles(extractPath, "*.*")
                                .Select(path => Path.GetFileName(path)).ToArray();

                            

                            try
                            {
                                using (ZipFile zip = ZipFile.Read(zipPath))
                                {
                                    foreach (ZipEntry e in zip)
                                    {
                                        textFileNameType = e.FileName.Substring(e.FileName.Length - 4);
                                    }
                                    textFileName = rawFileName + textFileNameType;
                                    for (int textFileNameCount = 0; textFileNameCount < localExtractedFiles.Length; textFileNameCount++)
                                    {
                                        if (textFileName.Equals(localExtractedFiles[textFileNameCount]))
                                            extractedFileExists = true;
                                    }
                                    ZipEntry entry = zip[textFileName];
                                    if (extractedFileExists)
                                    {
                                        message = message + "<br/><br/>The file " + textFileName +
                                                          " already exists.";
                                        extractPath = extractPath + rawFileName + "_" + timeStamp + "\\";
                                        message = message +
                                                          "</font><font color='Blue'> So, the extracted file has been uploaded under a new folder " +
                                                          rawFileName + "_" + timeStamp + ".<br/>";
                                    }
                                    else
                                        message = message +
                                                          "</font><font color='Blue'><br/><br/>The file has been extracted from the zip file.<br/>";
                                    if (entry != null && entry.UsesEncryption)
                                        entry.ExtractWithPassword(extractPath, password[filesCount]);
                                    else
                                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                                }
                            

                            using (StreamReader sr = new StreamReader(extractPath + textFileName))
                            {
                                lineCounter = System.IO.File.ReadLines(extractPath + textFileName).Count();
                                Regex regex = new Regex(@"[A-Za-z0-9\s]*,[A-Za-z0-9\s]*,[A-Za-z0-9\s]*");
                                
                                format = 0; // Not a valid format
                                
                                while ((line = sr.ReadLine()) != null)
                                {
                                    if (!(line.Contains(",")) && line.Length == 55 && format != 2)
                                        format = 1;
                                    else if (regex.IsMatch(line) && format != 1)
                                        format = 2;
                                    else
                                    {
                                        format = 0;
                                        goto outer;
                                    }
                                }
                            outer:
                                if (format.Equals(1))
                                    extractedFileFormat = "Format 1";
                                else if (format.Equals(2))
                                    extractedFileFormat = "Format 2";
                                else
                                    extractedFileFormat = "Not a valid format";

                                message = message + "Format of the extracted file " +
                                                  textFileName + ": " + extractedFileFormat;
                                message = message + "<br/>No. of lines in this extracted file: " +
                                                  lineCounter + "</font><font color='Red'><br/>";
                            }
                            }
                            catch
                            {
                                System.IO.File.Delete(fileSavePath);
                                uploadedCount--;
                                message = "<br/><font color='Red'>The password you entered for file inside the " +
                                                  defaultFileName +
                                                  " does not match with the file's password.</font><br/>";
                            }
                        }
                    }
                }
                ViewBag.Message = ViewBag.Message + message;
            }

            if (uploadedCount > 0)
                ViewBag.Message = ViewBag.Message + "<br/><br/></font><font color='Green'>Total number of uploaded files: " + uploadedCount + "</font>";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult Upload()
        {
            ViewBag.Message = "Upload the File.<br/>";

            return View();
        }

        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase[] files)
        {
            return View();
        }
    }
}