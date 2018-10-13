using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Data;
using System.IO;
using System.Data.SqlClient;

namespace EpicorAPIManager
{

    public class BPMInterface
    {
        private string bpmServerAndPort;
        public string BpmServerAndPort
        {
            get { return bpmServerAndPort; }
            set { bpmServerAndPort = value; }
        }

        private string bpmStrategy;
        public string BpmStrategy
        {
            get { return bpmStrategy; }
            set { bpmStrategy = value; }
        }

        private string boStrategy;
        public string BoStrategy
        {
            get { return boStrategy; }
            set { boStrategy = value; }
        }

        private string userID;
        public string UserID
        {
            get { return userID; }
            set { userID = value; }
        }

        private string flowTitle;
        public string FlowTitle
        {
            get { return flowTitle; }
            set { flowTitle = value; }
        }

        private string bpmStrategyUrl;
        private string boStrategyUrl;




        private string gbinid;   //流程id
        public string Gbinid
        { get { return gbinid; } }

        private string gtaskid; //任务id
        public string Gtaskid
        { get { return gtaskid; } }


        public BPMInterface(object epicorSession)
        {
            try
            {
                string companyId = "hsbs";
                string adapter = "Company";
                string whereClause = "Company = '" + companyId + "'";
                string fields = "BpmServerAndPort_c,BpmStrategy_c,BoStrategy_c,Name";
                //var boReader = WCFServiceSupport.CreateImpl<BOReaderImpl>((Ice.Core.Session)epicorSession, BOReaderImpl.UriPath);

                //DataSet ds = boReader.GetRows("Erp:BO:" + adapter, whereClause, fields);

                //bpmServerAndPort = ds.Tables[0].Rows[0]["BpmServerAndPort_c"].ToString();
                //bpmStrategy = ds.Tables[0].Rows[0]["BpmStrategy_c"].ToString();
                //boStrategy = ds.Tables[0].Rows[0]["BoStrategy_c"].ToString();

                //bpmStrategyUrl = "http://" + bpmServerAndPort + "/services/rs/wftask/" + bpmStrategy + "/";
                //boStrategyUrl = "http://" + bpmServerAndPort + "/services/rs/bo/" + boStrategy + "/";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public BPMInterface(string bpmServerAndPort, string bpmStrategy, string boStrategy)
        {
            try
            {  //bpmServerAndPortn --地址端口,  bpmStrategy--bpm策略, boStrategy--bo策略 
                bpmStrategyUrl = "http://" + bpmServerAndPort + "/services/rs/wftask/" + bpmStrategy + "/";
                boStrategyUrl = "http://" + bpmServerAndPort + "/services/rs/bo/" + boStrategy + "/";
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }




        public string StartFlow(string createUser, string title, string workflowDefUUID, DataTable dtHed, DataTable dtMtl, string nextUser)
        {
            try
            {
                if (dtHed == null)
                {
                    throw new Exception("主表不允许为空");
                }
                if (dtHed.TableName == "")
                {
                    throw new Exception("主表的名称不允许为空");
                }
                if (dtMtl != null && dtMtl.TableName == "")
                {
                    throw new Exception("从表的名称不允许为空");
                }
                userID = createUser;
                flowTitle = title;
                String p = "workflowDefUUID=" + workflowDefUUID + "&userId=" + createUser + "&title=" + flowTitle;
                string url = bpmStrategyUrl.Replace("wftask", "wf") + "createProcessInstance1";
                String pid = HttpPost(url, p);

                url = bpmStrategyUrl + "createProcessTaskInstance";
                p = "ownerId=" + userID + "&processInstanceId=" + pid + "&activityNo=" + 1 + "&participantId=" + userID + "&title=" + flowTitle;
                String sid = HttpPost(url, p);
                sid = sid.Substring(1, sid.Length - 2);
                string url1 = boStrategyUrl + "createBOData";
                string recordData = DataTableToJson(dtHed);
                //String recordData = "{\"ORDERNUM\":\"2\",\"CUSTID\":\"2\",\"CUSTNAME\":2,\"ORDERDATE\":2014-1-1,\"PO\":2,\"REMARK\":2}";  //{\"CUSTID\":\"TEST\",\"NAME\":\"名称\",\"ORDERNUM\":10001,\"CREDITLIMIT\":2}
                string hedTableName = dtHed.TableName;
                string processInstanceId = pid;
                String p1 = "boTableName=" + hedTableName + "&recordData=" + recordData + "&processInstanceId=" + processInstanceId + "&createUser=" + userID;
                String sid1 = HttpPost(url1, p1);

                //从表
                if (dtMtl != null)
                {
                    string mtlTableName = dtMtl.TableName;
                    foreach (DataRow dr in dtMtl.Rows)
                    {
                        recordData = DataRowToJson(dr);
                        //recordData = "{\"ORDERNUM\":\"2\",\"ORDERLINE\":\"2\",\"PARTNUM\":2,\"PARTDESC\":2014-1-1,\"UNITPRICE\":2,\"QUANTITY\":2}";  //{\"CUSTID\":\"TEST\",\"NAME\":\"名称\",\"ORDERNUM\":10001,\"CREDITLIMIT\":2}
                        p1 = "boTableName=" + mtlTableName + "&recordData=" + recordData + "&processInstanceId=" + processInstanceId + "&createUser=" + userID;
                        p1 = p1.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty).Replace("%", " "); //-mai 2016-12-13
                        sid1 = HttpPost(url1, p1);
                    }
                }


                NextStepOne3(pid, sid, nextUser, title, createUser);  //next指定多人并签
                //NextStepOne2("admin", pid, sid, "admin");
                //NextStepOne2("admin", pid, sid, "admin");

                return pid;
                //return "ok";
            }
            catch (Exception ex)
            {
                return "ng";
                throw ex;

            }
        }


        //关闭流程
        public bool closeWF(string user, string pid, string sid)
        {

            try
            {
                //pid就是流程id,对应wf_task.bind_id
                //sid sid是wf_task.id
                //string url = bpmStrategyUrl + "closeProcessInstance";
                string url = bpmStrategyUrl.Replace("wftask", "wf") + "closeProcessInstance/" + user + "/" + pid + "/" + sid;   //流程的方法，不需要指明任务

                //string createUserId = "admin";  //办理人
                //string participantId = "fangwenyan";
                //String p ="closeUserId=" + user + "&processInstanceId=" + pid + "&taskInstanceId=" + sid ;
                //String id = HttpPost(url, p);
                HttpGet(url, "");
                return true;
            }

            catch (Exception ex)
            {
                return false;
            }

        }


        //下步单人办理
        private void NextStepOne(string pid, string sid, string nextUser)
        {   //pid就是流程id,对应wf_task.bind_id
            //sid sid是wf_task.id
            string url = bpmStrategyUrl + "appendOpinionHistory";
            string createUserId = nextUser;  //办理人
            //string createUserId = "admin";  //办理人
            //string participantId = "fangwenyan";
            String p = "processInstanceId=" + pid + "&processTaskInstanceId=" + sid + "&opinionTitle=提交&opinionContent=发起";
            String id = HttpPost(url, p);
            string url1 = bpmStrategyUrl + "getNextParticipants/" + createUserId + "/" + pid + "/" + sid + "";
            String p1 = "";
            string id1 = HttpGet(url1, p1);
            string url2 = bpmStrategyUrl + "getNextStepNo/" + createUserId + "/" + pid + "/" + sid + "";

            String p2 = "";
            string id2 = HttpGet(url2, p2);
            //id2指流程节点，也可以用以下方法查询数据库取得
            //流程ID是wf_task.bind_id,wfid是流程模型ID，wfsid是流程节点模型ID，节点号查询是用bind_id，对应wf_messagedata的id，wf_messagedata的wfs_no就是流程节点号


            if (Convert.ToInt32(id2) != -1 && !"".Equals(id1))
            {
                string[] nextusers = id1.Split(' ');
                string firstnextuser = "";
                string nextuserstep = "";
                int i = 0;
                foreach (string user in nextusers)
                {
                    if (i == 0) firstnextuser = user.Split('<')[0];
                    nextuserstep += (" " + user.Split('<')[0]);
                    i++;
                }


                string url3 = bpmStrategyUrl + "closeProcessTaskInstance/" + createUserId + "/" + pid + "/" + sid + "";
                String p3 = "userId=" + createUserId + "&processInstanceId=" + pid + "&processTaskInstanceId=" + sid;
                HttpGet(url3, p3);
                String p4 = "ownerId=" + createUserId + "&processInstanceId=" + pid + "&activityNo=" + id2 + "&participantId=" + id1 + "&title=" + flowTitle;
                string url4 = bpmStrategyUrl + "createProcessTaskInstance";
                string nexttaskid = HttpPost(url4, p4); //wftask.createProcessTaskInstance(bpmUserId, tProcessInstanceId, Convert.ToInt32(nextno), nextuserstep, "XXX的柴油发放流程");
                nexttaskid = nexttaskid.Substring(1);
                nexttaskid = nexttaskid.Substring(0, nexttaskid.Length - 1);


                if (nexttaskid != null)
                {
                    //MessageBox.Show("流程已经成功推送到下一节点，办理人:" + id1 + ",节点号:" + id2);
                }
            }
            else
            {
                //System.Windows.Forms.MessageBox.Show("流程已经发起，但由于节点2没有设置参与者，因此节点1未办理，请登录BPM手工办理");
            }

        }


        //下步单人办理
        public void NextStepOne2(string ownid, string pid, string sid, string nextUser, string title, DataTable currTask)
        {   //pid就是流程id,对应wf_task.bind_id
            //sid sid是wf_task.id
            //string url = bpmStrategyUrl + "appendOpinionHistory";
            //string createUserId = nextUser;  //办理人
            //string createUserId = "admin";  //办理人
            //string participantId = "fangwenyan";
            //String p = "processInstanceId=" + pid + "&processTaskInstanceId=" + sid + "&opinionTitle=提交&opinionContent=发起";
            //String id = HttpPost(url, p);
            //string url1 = bpmStrategyUrl + "getNextParticipants/" + createUserId + "/" + pid + "/" + sid + "";
            //String p1 = "";
            //string id1 = HttpGet(url1, p1);
            string url2 = bpmStrategyUrl + "getNextStepNo/" + ownid + "/" + pid + "/" + sid + "";

            String p2 = "";
            string id2 = HttpGet(url2, p2);
            //id2指流程节点，也可以用以下方法查询数据库取得
            //流程ID是wf_task.bind_id,wfid是流程模型ID，wfsid是流程节点模型ID，节点号查询是用bind_id，对应wf_messagedata的id，wf_messagedata的wfs_no就是流程节点号


            //if (Convert.ToInt32(id2) != -1 && !"".Equals(id1))
            //{
            //    string[] nextusers = id1.Split(' ');
            //    string firstnextuser = "";
            //    string nextuserstep = "";
            //    int i = 0;
            //    foreach (string user in nextusers)
            //    {
            //        if (i == 0) firstnextuser = user.Split('<')[0];
            //        nextuserstep += (" " + user.Split('<')[0]);
            //        i++;
            //    }

            ////查询所有相同的流程id,然后都关闭

            string url3, p3;
            for (int i = 0; i < currTask.Rows.Count; i++)
            {
                sid = currTask.Rows[i]["id"].ToString().Trim();
                url3 = bpmStrategyUrl + "closeProcessTaskInstance/" + ownid + "/" + pid + "/" + sid + "";
                p3 = "userId=" + ownid + "&processInstanceId=" + pid + "&processTaskInstanceId=" + sid;
                HttpGet(url3, p3);
            }

            String p4 = "ownerId=" + ownid + "&processInstanceId=" + pid + "&activityNo=" + id2 + "&participantId=" + nextUser + "&title=" + title;
            string url4 = bpmStrategyUrl + "createProcessTaskInstance";
            string nexttaskid = HttpPost(url4, p4); //wftask.createProcessTaskInstance(bpmUserId, tProcessInstanceId, Convert.ToInt32(nextno), nextuserstep, "XXX的柴油发放流程");
            nexttaskid = nexttaskid.Substring(1);
            nexttaskid = nexttaskid.Substring(0, nexttaskid.Length - 1);
            if (nexttaskid != null)
            {
                gbinid = pid;
                gtaskid = nexttaskid;
                //MessageBox.Show("流程已经成功推送到下一节点，办理人:" + id1 + ",节点号:" + id2);
            }
            else
            {

            }
            //}
            //else
            //{
            //    //System.Windows.Forms.MessageBox.Show("流程已经发起，但由于节点2没有设置参与者，因此节点1未办理，请登录BPM手工办理");
            //}

        }

        //下步单人办理
        public string NextStepOne2str(string ownid, string pid, string sid, string nextUser, string title, DataTable currTask)
        { 
            string url2 = bpmStrategyUrl + "getNextStepNo/" + ownid + "/" + pid + "/" + sid + "";
            String p2 = "";
            string id2 = HttpGet(url2, p2);
            string url3, p3;
            for (int i = 0; i < currTask.Rows.Count; i++)
            {
                sid = currTask.Rows[i]["id"].ToString().Trim();
                url3 = bpmStrategyUrl + "closeProcessTaskInstance/" + ownid + "/" + pid + "/" + sid + "";
                p3 = "userId=" + ownid + "&processInstanceId=" + pid + "&processTaskInstanceId=" + sid;
                HttpGet(url3, p3);
            }
            String p4 = "ownerId=" + ownid + "&processInstanceId=" + pid + "&activityNo=" + id2 + "&participantId=" + nextUser + "&title=" + title;
            string url4 = bpmStrategyUrl + "createProcessTaskInstance";
            string nexttaskid = HttpPoststr(url4, p4); //wftask.createProcessTaskInstance(bpmUserId, tProcessInstanceId, Convert.ToInt32(nextno), nextuserstep, "XXX的柴油发放流程");
            UpdateAWS("insert into BO_ERPRECORD(UD01,UD10) values('" + nexttaskid + "','收料到转序')");
            if (nexttaskid.Contains("false"))
            {
                
                return nexttaskid;
            }
            else
            {
                nexttaskid = nexttaskid.Substring(1);
                nexttaskid = nexttaskid.Substring(0, nexttaskid.Length - 1);
                gbinid = pid;
                gtaskid = nexttaskid;
                return "";
            }
        }

        public string UpdateAWS(string sqlstr)
        {
            SqlConnection conn = new SqlConnection(System.Configuration.ConfigurationManager.AppSettings["awsConnectionstring"]);
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlstr, conn);
            Object o = cmd.ExecuteNonQuery();
            conn.Close();
            return o == null ? "" : o.ToString();
        }

        //下步多人并签办理       
        public void NextStepOne3(string pid, string sid, string nextUser, string nodeTitle, string ownid)
        {  // 
           //pid就是流程id,对应wf_task.bind_id
           //sid sid是wf_task.id
           //nextUser 下步办理人
           //nodeTitle 流程标题
           //ownid 流程发起人
            string createUserId = nextUser;
            string id1 = nextUser;
            //取下步流程id号
            //id2指流程节点，也可以用以下方法查询数据库取得
            //流程ID是wf_task.bind_id,wfid是流程模型ID，wfsid是流程节点模型ID，节点号查询是用bind_id，对应wf_messagedata的id，wf_messagedata的wfs_no就是流程节点号

            string url2 = bpmStrategyUrl + "getNextStepNo/" + ownid + "/" + pid + "/" + sid + "";
            String p2 = "";
            string id2 = HttpGet(url2, p2);

            flowTitle = nodeTitle;
            //关闭上个流程
            string url3 = bpmStrategyUrl + "closeProcessTaskInstance/" + ownid + "/" + pid + "/" + sid + "";
            String p3 = "userId=" + ownid + "&processInstanceId=" + pid + "&processTaskInstanceId=" + sid;
            HttpGet(url3, p3);


            //如果下步办理人为空，则取bpm中指定的办理人
            if (id1.Trim() == "")
            {

                string url1 = bpmStrategyUrl + "getNextParticipants/" + createUserId + "/" + pid + "/" + sid + "";
                String p1 = "";
                id1 = HttpGet(url1, p1);


                if (Convert.ToInt32(id2) != -1 && !"".Equals(id1))
                {
                    string[] nextusers = id1.Split(' ');
                    string firstnextuser = "";
                    string nextuserstep = "";
                    int i = 0;
                    foreach (string user in nextusers)
                    {
                        if (i == 0) firstnextuser = user.Split('<')[0];
                        nextuserstep += (" " + user.Split('<')[0]);
                        i++;
                    }

                }
            }




            //发起下步流程
            String p4 = "ownerId=" + ownid + "&processInstanceId=" + pid + "&activityNo=" + id2 + "&participantId=" + id1 + "&title=" + flowTitle + "&synType=1"; //并签
            string url4 = bpmStrategyUrl + "createProcessTaskInstance1";
            string nexttaskid = HttpPost(url4, p4); //wftask.createProcessTaskInstance(bpmUserId, tProcessInstanceId, Convert.ToInt32(nextno), nextuserstep, "XXX的柴油发放流程");

            nexttaskid = nexttaskid.Substring(1);
            nexttaskid = nexttaskid.Substring(0, nexttaskid.Length - 1);


            if (nexttaskid != null)
            {
                gtaskid = nexttaskid;  ////返回流程id到全局变量
                                       //MessageBox.Show("流程已经成功推送到下一节点，办理人:" + id1 + ",节点号:" + id2);
            }
            //}
            //else
            //{
            //    //System.Windows.Forms.MessageBox.Show("流程已经发起，但由于节点2没有设置参与者，因此节点1未办理，请登录BPM手工办理");
            //}

        }



        public string HttpPost(string Url, string postDataStr)
        {
            try
            {
                Encoding encode = System.Text.Encoding.GetEncoding("gb2312");
                byte[] arrB = encode.GetBytes(postDataStr);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded;charset=gb2312";
                request.ContentLength = arrB.Length;
                //request.CookieContainer = cookie;
                Stream myRequestStream = request.GetRequestStream();
                StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
                myStreamWriter.Write(postDataStr);
                myStreamWriter.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, encode);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public string HttpPoststr(string Url, string postDataStr)
        {
            try
            {
                Encoding encode = System.Text.Encoding.GetEncoding("gb2312");
                byte[] arrB = encode.GetBytes(postDataStr);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded;charset=gb2312";
                request.ContentLength = arrB.Length;
                //request.CookieContainer = cookie;
                Stream myRequestStream = request.GetRequestStream();
                StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
                myStreamWriter.Write(postDataStr);
                myStreamWriter.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, encode);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            catch (Exception e)
            {
                return "false."+e.Message.ToString();
                //throw e;
            }
        }

        public static string DataTableToJson(DataTable dt)
        {
            string json = string.Empty;
            for (int j = 0; j < dt.Rows.Count; j++)
            {
                json += "{";
                for (int k = 0; k < dt.Columns.Count; k++)
                {
                    json += "\"" + dt.Columns[k].ColumnName + "\":'" + dt.Rows[j][k].ToString() + "'";
                    if (k != dt.Columns.Count - 1)
                        json += ",";
                }
                json += "}";
                if (j != dt.Rows.Count - 1)
                    json += ",";
            }

            return json;
        }

        public static string DataRowToJson(DataRow dr)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            foreach (DataColumn column in dr.Table.Columns)
            {
                builder.Append("\"");
                builder.Append(column.ColumnName);
                builder.Append("\":\"");
                if (((dr[column] != null) && (dr[column] != DBNull.Value)) && (dr[column].ToString() != ""))
                {
                    builder.Append(dr[column]);
                }
                else
                {
                    builder.Append("");
                }
                builder.Append("\",");
            }
            string returnStr = builder.ToString().Substring(0, builder.Length - 2);
            returnStr += "\"}";
            return returnStr;
        }

        public static string HttpGet(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

    }
}
