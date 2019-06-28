using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace OA_WebService
{
    public class Func
    {
        private Hashtable GetParametersFromXML(string XMLParameters)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(XMLParameters);
            XmlNode DataNode = xml.SelectSingleNode("paras");

            Hashtable Parameters = new Hashtable();
            foreach (XmlNode node in DataNode.ChildNodes)
            {
                Parameters.Add(node.Name, node.InnerText);
            }
            return Parameters;
        }
    }
}