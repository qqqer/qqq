using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace Appapi.Models
{
    public static class FtpRepository
    {
        public static readonly string ftpUserName = ConfigurationManager.ConnectionStrings["ftpUserName"].ToString();
        public static readonly string ftpPassword = ConfigurationManager.ConnectionStrings["ftpPassword"].ToString();
        public static readonly string ftpServer = ConfigurationManager.ConnectionStrings["ftpServer"].ToString();


        /// 获取根目录下明细(包含文件和文件夹)
        private static string[] GetFilesDetailList(string folderPath)
        {
            try
            {
                StringBuilder result = new StringBuilder();
                FtpWebRequest ftp;
                ftp = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpServer + folderPath));
                ftp.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                ftp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = ftp.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

                string line = reader.ReadLine();

                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                if (result.Length == 0)
                    return null;
                result.Remove(result.ToString().LastIndexOf("\n"), 1);
                reader.Close();
                response.Close();
                return result.ToString().Split('\n');
            }
            catch
            {
                throw;
            }
        }


        /// 获取根目录下所有的文件夹列表(仅文件夹)
        private static string[] GetDirectoryList(string folderPath)
        {
            string[] drectory = GetFilesDetailList(folderPath);

            if (drectory == null)
                return null;

            string m = string.Empty;
            foreach (string str in drectory)
            {
                int dirPos = str.IndexOf("<DIR>");

                if (dirPos > 0) //如果是文件夹
                    m += str.Substring(dirPos + 5).Trim() + "\n";
            }

            return m.Split('\n');
        }


        /// 判断根目录下指定的文件夹是否存在
        public static bool IsFolderExist(string folderPath, string folderName)
        {
            string[] dirList = GetDirectoryList(folderPath);
            if (dirList != null)
            {
                foreach (string str in dirList)
                {
                    if (str.Trim() == folderName.Trim())
                        return true;
                }
            }

            return false;
        }


        public static bool UploadFile(byte[] fileContent, string folderPath, string filename)
        {
            string uri = ftpServer  + folderPath  + filename;
            FtpWebRequest reqFTP;

            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
            reqFTP.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
            reqFTP.KeepAlive = false;
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;
            reqFTP.UseBinary = true;
            reqFTP.ContentLength = fileContent.Length;

            try
            {
                Stream strm = reqFTP.GetRequestStream();
                strm.Write(fileContent, 0, fileContent.Length);
                strm.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }


        public static bool MakeFolder(string folderPath, string folderName)
        {
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpServer + folderPath + folderName));
                reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }


        private static string[] GetFileList(string folderName)
        {
            string[] downloadFiles = null;
            StringBuilder result = new StringBuilder();
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(ftpServer + "/" + folderName));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                WebResponse response = reqFTP.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

                string line = reader.ReadLine();
                while (line != null)
                {

                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }

                if (result.Length == 0)
                    return new string[] { };

                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                reader.Close();
                response.Close();
                downloadFiles = result.ToString().Split('\n');
            }
            catch
            {
                return new string[] { };
            }

            if (downloadFiles == null)
                downloadFiles = new string[] { };

            return downloadFiles;
        }


        /// 删除文件夹（只针对空文件夹）
        private static void RemoveDirectory(string folderName)
        {
            try
            {
                string uri = ftpServer + "/" + folderName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

                reqFTP.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.RemoveDirectory;

                string result = String.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new StreamReader(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// 删除文件
        public static bool DeleteFile(string filePath,string fileName )
        {
            try
            {
                string uri = filePath + fileName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));

                reqFTP.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                reqFTP.KeepAlive = false;
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;

                string result = String.Empty;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                long size = response.ContentLength;
                Stream datastream = response.GetResponseStream();
                StreamReader sr = new StreamReader(datastream);
                result = sr.ReadToEnd();
                sr.Close();
                datastream.Close();
                response.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// 删除文件夹以及其下面的所有内容
        public static void DeleteFolder(string folderName)
        {
            string[] fileList = GetFileList(folderName);//获取folderName下的所有文件列表（仅文件）
                                                        
            if (fileList != null && fileList.Length > 0)//删除folderName里的所有文件
            {
                foreach (string fileName in fileList)
                {
                    DeleteFile(folderName, fileName);
                }
            }

            RemoveDirectory(folderName);//删除当前文件夹
        }
    }
}