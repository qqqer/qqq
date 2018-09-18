using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CommonClass
{
    public class XmlHelper1
    {
        public bool WriteXml(string userId,string passWord,string autoLogin)
        {
            XmlDocument xmlDoc = new XmlDocument();
            //创建Xml声明部分，即<?xml version="1.0" encoding="utf-8" ?>
            XmlDeclaration xmldecl = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);

            //创建根节点
            XmlNode rootNode = xmlDoc.CreateElement("configuration");

            
            //创建 configuration 子节点
            XmlNode UserSettingsNode = xmlDoc.CreateElement("UserSettings");
            //创建一个UserID节点及属性
            XmlNode UserIDNode = xmlDoc.CreateElement("UserID");
            XmlAttribute UserIDAttribute = xmlDoc.CreateAttribute("Value");
            UserIDAttribute.Value = userId;
            //xml节点附件属性
            UserIDNode.Attributes.Append(UserIDAttribute);

            //创建一个Password节点及属性
            XmlNode PasswordNode = xmlDoc.CreateElement("Password");
            XmlAttribute PasswordAttribute = xmlDoc.CreateAttribute("Value");
            PasswordAttribute.Value = passWord;
            //xml节点附件属性
            PasswordNode.Attributes.Append(PasswordAttribute);

            //创建一个AutoLogin节点及属性
            XmlNode AutoLoginNode = xmlDoc.CreateElement("AutoLogin");
            XmlAttribute AutoLoginAttribute = xmlDoc.CreateAttribute("Value");
            AutoLoginAttribute.Value = autoLogin;
            //xml节点附件属性
            AutoLoginNode.Attributes.Append(AutoLoginAttribute);


            //创建 configuration 子节点
            XmlNode appSettingsNode = xmlDoc.CreateElement("appSettings");


            //创建courses子节点
            //XmlNode coursesNode = xmlDoc.CreateElement("courses");
            //XmlNode courseNode1 = xmlDoc.CreateElement("course");
            //XmlAttribute courseNameAttr = xmlDoc.CreateAttribute("Value");
            //courseNameAttr.Value = "语文";
            //courseNode1.Attributes.Append(courseNameAttr);
            //XmlNode teacherCommentNode = xmlDoc.CreateElement("teacherComment");
            ////创建Cdata块
            //XmlCDataSection cdata = xmlDoc.CreateCDataSection("<font color=\"red\">这是语文老师的批注</font>");
            //teacherCommentNode.AppendChild(cdata);
            //courseNode1.AppendChild(teacherCommentNode);
            //coursesNode.AppendChild(courseNode1);
            //附加子节点
            UserSettingsNode.AppendChild(UserIDNode);
            UserSettingsNode.AppendChild(PasswordNode);
            UserSettingsNode.AppendChild(AutoLoginNode);

            rootNode.AppendChild(UserSettingsNode);
            rootNode.AppendChild(appSettingsNode);
            //附加根节点
            xmlDoc.AppendChild(rootNode);

            XmlElement root = xmlDoc.DocumentElement;
            xmlDoc.InsertBefore(xmldecl, root);

            //保存Xml文档
            xmlDoc.Save(AppDomain.CurrentDomain.BaseDirectory + @"EAT.sysconfig");

            Console.WriteLine("已保存Xml文档");
            return true;
        }
        public void ReadXml()
        {
            string xmlFilePath = AppDomain.CurrentDomain.BaseDirectory + @"EAT.sysconfig";
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlFilePath);

            //List<Users> users = new List<Users>();

            XmlNodeList xmlNoteListUserID = doc.GetElementsByTagName("UserID");
            foreach (XmlElement item in xmlNoteListUserID)
            {
               Users.UserId = item.GetAttribute("Value");
            }
            XmlNodeList xmlNoteListPassword = doc.GetElementsByTagName("Password");
            foreach (XmlElement item in xmlNoteListPassword)
            {
                Users.Password = item.GetAttribute("Value");
            }
            XmlNodeList xmlNoteListAutoLogin = doc.GetElementsByTagName("AutoLogin");
            //foreach (XmlElement item in xmlNoteListAutoLogin)
            //{
            //    Users.AutoLogin = bool.Parse(item.GetAttribute("Value"));
            //}


            //使用xpath表达式选择文档中所有的student子节点
            //XmlNodeList studentNodeList = doc.SelectNodes("/configuration/UserSettings");
            //if (studentNodeList != null)
            //{
                
            //    foreach (XmlNode UserIDNode in studentNodeList)
            //    {
            //        //通过Attributes获得属性名字为name的属性
            //        string userId = UserIDNode.Attributes["Value"].Value;
            //        Console.WriteLine("UserIDNode:" + userId);

            //        //通过SelectSingleNode方法获得当前节点下的courses子节点
            //        XmlNode coursesNode = UserIDNode.SelectSingleNode("courses");

            //        //通过ChildNodes属性获得courseNode的所有一级子节点
            //        XmlNodeList courseNodeList = coursesNode.ChildNodes;
            //        if (courseNodeList != null)
            //        {
            //            foreach (XmlNode courseNode in courseNodeList)
            //            {
            //                Console.Write("\t");
            //                Console.Write(courseNode.Attributes["name"].Value);
            //                Console.Write("老师评语");
            //                //通过FirstNode属性可以获得课程节点的第一个子节点，LastNode可以获得最后一个子节点
            //                XmlNode teacherCommentNode = courseNode.FirstChild;
            //                //读取CData节点
            //                XmlCDataSection cdata = (XmlCDataSection)teacherCommentNode.FirstChild;
            //                Console.WriteLine(cdata.InnerText.Trim());
            //            }
            //        }
            //    }
            //}
        }
    }
}
