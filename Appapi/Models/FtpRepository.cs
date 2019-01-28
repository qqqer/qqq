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
    public static class FTPRepository
    {
        public static readonly string ftpUserName = ConfigurationManager.ConnectionStrings["ftpUserName"].ToString();
        public static readonly string ftpPassword = ConfigurationManager.ConnectionStrings["ftpPassword"].ToString();
        public static readonly string ftpServer = ConfigurationManager.ConnectionStrings["ftpServer"].ToString();


        /// 获取根目录下明细(包含文件和文件夹)
        private static string[] GetFilesDetailList(string Path)
        {
            try
            {
                StringBuilder result = new StringBuilder();
                FtpWebRequest ftp;
                ftp = (FtpWebRequest)WebRequest.Create(new Uri(ftpServer + Path));
                ftp.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                ftp.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = ftp.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);

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


        /// 获取指定路径下所有的文件夹列表(仅文件夹)
        private static string[] GetDirectoryList(string Path)
        {
            string[] drectory = GetFilesDetailList(Path);

            if (drectory == null)
                return null;

            string m = string.Empty;
            foreach (string str in drectory)
            {

                //if (str.Contains("<DIR>"))
                //    dirPos = str.IndexOf("<DIR>");
                //else
                //    dirPos =

                int dirPos = str.IndexOf("<DIR>");

                if (dirPos > 0) //如果是文件夹
                    m += str.Substring(dirPos + 5).Trim() + "\n";
                else//bug
                    m += str.Split(' ').Last() + "\n";
            }

            return m.Split('\n');
        }


        /// 判断指定路径下指定的文件夹是否存在
        public static bool IsFolderExist(string Path, string folderName)
        {
            string[] dirList = GetDirectoryList(Path);
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


        public static bool IsFileExist(string FolderURL, string FileName)
        {
            string[] fileList = GetFileList(FolderURL, "*.*");
            foreach (string str in fileList)
            {
                if (str.Trim() == FileName.Trim())
                {
                    return true;
                }
            }
            return false;
        }


        public static bool UploadFile(byte[] fileContent, string Path, string filename)
        {
            string uri = ftpServer + Path + filename;
            FtpWebRequest reqFTP;

            reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(uri));
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


        public static bool MakeFolder(string Path, string folderName)
        {
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(ftpServer + Path + folderName));
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
                throw;
            }
        }


        private static string[] GetFileList(string FolderURL, string mask)
        {
            string[] downloadFiles = null;
            StringBuilder result = new StringBuilder();
            FtpWebRequest reqFTP;

            reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(FolderURL));
            reqFTP.UseBinary = true;
            reqFTP.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
            reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
            WebResponse response = reqFTP.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);

            string line = reader.ReadLine();
            while (line != null)
            {
                if (mask.Trim() != string.Empty && mask.Trim() != "*.*")
                {

                    string mask_ = mask.Substring(0, mask.IndexOf("*"));
                    if (line.Substring(0, mask_.Length) == mask_)
                    {
                        result.Append(line);
                        result.Append("\n");
                    }
                }
                else
                {
                    result.Append(line);
                    result.Append("\n");
                }
                line = reader.ReadLine();
            }

            if (result.Length == 0)
                return new string[] { };

            result.Remove(result.ToString().LastIndexOf('\n'), 1);
            reader.Close();
            response.Close();
            downloadFiles = result.ToString().Split('\n');


            if (downloadFiles == null)
                downloadFiles = new string[] { };

            return downloadFiles;
        }



        /// 删除文件
        public static bool DeleteFile(string FolderURL, string fileName)
        {
            try
            {
                if(!IsFileExist(FolderURL, fileName)) return true;


                string uri = FolderURL + fileName;
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)WebRequest.Create(new Uri(uri));

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
    }
}