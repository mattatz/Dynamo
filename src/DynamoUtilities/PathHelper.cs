﻿using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Xml;
using Newtonsoft.Json;

namespace DynamoUtilities
{
    public class PathHelper
    {
        private static readonly string sizeUnits = " KB";
        private const long KbConversionConstant = 1024;

        // This return an exception if any operation failed and the folder
        // wasn't created. It's the responsibility of the caller of this function
        // to check whether the folder creation is successful or not.
        public static Exception CreateFolderIfNotExist(string folderPath)
        {
            try
            {
                // When network path is access denied, the Directory.Exits however still 
                // return true.
                // EnumerateDirectories operation is additional check
                // to catch exception for network path.
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
                Directory.EnumerateDirectories(folderPath);
            }
            catch (IOException ex) { return ex; }
            catch (ArgumentException ex) { return ex; }
            catch (UnauthorizedAccessException ex) { return ex; }

            return null;
        }

        /// <summary>
        /// Checks if the file path string is valid and file exist.
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns></returns>
        public static bool IsValidPath(string filePath)
        {
            return (!string.IsNullOrEmpty(filePath) && (File.Exists(filePath)));
        }

        /// <summary>
        /// Check if user has readonly privilege to the folder path.
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns></returns>
        public static bool IsReadOnlyPath(string filePath)
        {
            if (IsValidPath(filePath))
            {
                FileInfo Finfo = new FileInfo(filePath);
                // We mark the path read only when
                // 1. file read-only
                // 2. user does not have write access to the folder
                return Finfo.IsReadOnly || !HasWritePermissionOnDir(Finfo.Directory.ToString());
            }
            else
                return false;
        }

        /// <summary>
        /// Returns whether current user has write access to the folder path.
        /// </summary>
        /// <param name="folderPath">Folder path</param>
        /// <returns></returns>
        public static bool HasWritePermissionOnDir(string folderPath)
        {
            try
            {
                var writeAllow = false;
                var writeDeny = false;
                var accessControlList = Directory.GetAccessControl(folderPath);
                if (accessControlList == null)
                    return false;
                var accessRules = accessControlList.GetAccessRules(true, true,
                                            typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                    return false;

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    // When current rule does not contain setting related to WRITE, skip.
                    if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                        continue;

                    if (rule.AccessControlType == AccessControlType.Allow)
                        writeAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        writeDeny = true;
                }

                return writeAllow && !writeDeny;
            }
            catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns whether current user has read access to the folder path.
        /// </summary>
        /// <param name="folderPath">Folder path</param>
        /// <returns></returns>
        internal static bool HasReadPermissionOnDir(string folderPath)
        {
            try
            {
                var readAllow = false;
                var readDeny = false;
                var accessControlList = Directory.GetAccessControl(folderPath);
                if (accessControlList == null)
                    return false;

                var accessRules = accessControlList.GetAccessRules(true, true,
                                            typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                    return false;

                var curentUser = WindowsIdentity.GetCurrent();
                foreach (FileSystemAccessRule rule in accessRules)
                {
                    // When current rule does not contain setting related to Read, skip.
                    if ((FileSystemRights.Read & rule.FileSystemRights) != FileSystemRights.Read)
                        continue;

                    if (!curentUser.Groups.Contains(rule.IdentityReference))
                        continue;
                    
                    if (rule.AccessControlType == AccessControlType.Allow)
                        readAllow = true;
                    else if (rule.AccessControlType == AccessControlType.Deny)
                        readDeny = true;
                }

                return readAllow && !readDeny;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// This is a utility method for checking if given path contains valid XML document.
        /// </summary>
        /// <param name="path">path to the target xml file</param>
        /// <param name="xmlDoc">System.Xml.XmlDocument representation of target xml file</param>
        /// <returns>Return true if file is Json, false if file is not Json, exception as out param</returns>
        public static bool isValidXML(string path, out XmlDocument xmlDoc, out Exception ex)
        {
            // Based on https://msdn.microsoft.com/en-us/library/875kz807(v=vs.110).aspx
            // Exception thrown indicate invalid XML document path or due to other failure
            try
            {
                xmlDoc = new XmlDocument();
                xmlDoc.Load(path);
                ex = null;
                return true;
            }
            catch(Exception e)
            {
                xmlDoc = null;
                ex = e;
                return false;
            }
        }

        /// <summary>
        /// This is a utility method for checking if a given string represents a valid Json document.
        /// </summary>
        /// <param name="fileContents"> string contents of target json file</param>
        /// <returns>Return true if fileContents is Json, false if file is not Json, exception as out param</returns>
        public static bool isFileContentsValidJson(string fileContents, out Exception ex)
        {
            ex = null;
            if (string.IsNullOrEmpty(fileContents))
            {
                ex = new JsonReaderException();
                return false;
            }
            
            try
            {
                fileContents = fileContents.Trim();
                if ((fileContents.StartsWith("{") && fileContents.EndsWith("}")) || //For object
                    (fileContents.StartsWith("[") && fileContents.EndsWith("]"))) //For array
                {
                    var obj = Newtonsoft.Json.Linq.JToken.Parse(fileContents);
                    return true;
                }
                else 
                {
                    ex = new JsonReaderException();
                }
            }
            catch(Exception e)
            {
                ex = e;
            }
            
            return false;
        }

        /// <summary>
        /// This is a utility method for checking if given path contains valid Json document.
        /// </summary>
        /// <param name="path">path to the target json file</param>
        /// <param name="fileContents"> string contents of target json file</param>
        /// <returns>Return true if file is Json, false if file is not Json, exception as out param</returns>
        public static bool isValidJson(string path, out string fileContents, out Exception ex)
        {
            fileContents = "";
            try
            {
                fileContents = File.ReadAllText(path);
                return isFileContentsValidJson(fileContents, out ex);
            }
            catch (Exception e) //some other exception
            {
                Console.WriteLine(e.ToString());
                ex = e;
                return false;
            }
        }

        // Check if the file name contains any special non-printable chatacters.
        public static bool IsFileNameInValid(string fileName)
        {
            // Some other extra characters that are to be checked. 
            char[] invalidCharactersFileName = { '#', '%', '&', '.', ' ' };

            if (fileName.Any(f => Path.GetInvalidFileNameChars().Contains(f)) || fileName.IndexOfAny(invalidCharactersFileName) > -1)
                return true;

            return false;
        }

        /// <summary>
        /// This is a utility method for generating a default name to the snapshot image. 
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Returns a default name(along with the timestamp) for the workspace image</returns>
        public static String GetScreenCaptureNameFromPath(String filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            String timeStamp = string.Format("{0:yyyy-MM-dd_hh-mm-ss}", DateTime.Now);
            String snapshotName = fileInfo.Name.Replace(fileInfo.Extension, "_") + timeStamp;
            return snapshotName;
        }

        /// <summary>
        /// Computes the file size from the path.
        /// </summary>
        /// <param name="path">File path</param>
        public static string GetFileSize(string path)
        {
            if (path != null)
            {
                var fileInfo = new FileInfo(path);
                long size = fileInfo.Length / KbConversionConstant;
                return size.ToString() + sizeUnits;
            }

            return null;
        }

        /// <summary>
        /// Checks if the file exists at the specified path and computes size.
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Returns a boolean if the file exists at the path and also returns its size</returns>
        public static void FileInfoAtPath(string path, out bool fileExists, out string size)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                fileExists = true;
                var fileLength = fileInfo.Length / KbConversionConstant;
                size = fileLength.ToString() + sizeUnits;
            }
            catch
            {
                fileExists = false;
                size = string.Empty;
            }
        }

        internal static Char[] SpecialAndInvalidCharacters()
        {
            // Excluding white spaces and uncommon characters, only keeping the displayed in the Windows alert
            return System.IO.Path.GetInvalidFileNameChars().Where(x => !char.IsWhiteSpace(x) && (int)x > 31).ToArray();
        }
    }
}
