using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Appapi.Models
{
    static class PrintRepository
    {
        private static string Print(DataRow dt, string path, string printer, int qty)
        {
            BarTender.Application btApp = null;
            BarTender.Format btFormat;

            try
            {
                // 模板初始化
                btApp = new BarTender.Application();
                // 产品标签 模板中无此参数则报错
                btFormat = btApp.Formats.Open(path, false, "");
                btFormat.PrintSetup.IdenticalCopiesOfLabel = qty; //打印份数
                                                                  //btFormat.PrintSetup.NumberSerializedLabels = 1;//条码递增+1
                #region 
                btFormat.SetNamedSubStringValue("text1", (dt["text1"] == null) ? "" : dt["text1"].ToString());
                btFormat.SetNamedSubStringValue("text2", (dt["text2"] == null) ? "" : dt["text2"].ToString());
                btFormat.SetNamedSubStringValue("text3", (dt["text3"] == null) ? "" : dt["text3"].ToString());
                btFormat.SetNamedSubStringValue("text4", dt["text4"].ToString());
                btFormat.SetNamedSubStringValue("text5", dt["text5"].ToString());
                btFormat.SetNamedSubStringValue("text6", dt["text6"].ToString());
                btFormat.SetNamedSubStringValue("text7", dt["text7"].ToString());
                btFormat.SetNamedSubStringValue("text8", dt["text8"].ToString());
                btFormat.SetNamedSubStringValue("text9", dt["text9"].ToString());
                btFormat.SetNamedSubStringValue("text10", dt["text10"].ToString());
                btFormat.SetNamedSubStringValue("text11", dt["text11"].ToString());
                btFormat.SetNamedSubStringValue("text12", dt["text12"].ToString());
                btFormat.SetNamedSubStringValue("text13", dt["text13"].ToString());
                btFormat.SetNamedSubStringValue("text14", dt["text14"].ToString());
                btFormat.SetNamedSubStringValue("text15", dt["text15"].ToString());
                btFormat.SetNamedSubStringValue("text16", dt["text16"].ToString());
                btFormat.SetNamedSubStringValue("text17", dt["text17"].ToString());
                btFormat.SetNamedSubStringValue("text18", dt["text18"].ToString());
                btFormat.SetNamedSubStringValue("text19", dt["text19"].ToString());
                btFormat.SetNamedSubStringValue("text20", dt["text20"].ToString());
                btFormat.SetNamedSubStringValue("text21", dt["text21"].ToString());
                btFormat.SetNamedSubStringValue("text22", dt["text22"].ToString());
                btFormat.SetNamedSubStringValue("text23", dt["text23"].ToString());
                btFormat.SetNamedSubStringValue("text24", dt["text24"].ToString());
                btFormat.SetNamedSubStringValue("text25", dt["text25"].ToString());
                btFormat.SetNamedSubStringValue("text26", dt["text26"].ToString());
                btFormat.SetNamedSubStringValue("text27", dt["text27"].ToString());
                btFormat.SetNamedSubStringValue("text28", dt["text28"].ToString());
                btFormat.SetNamedSubStringValue("text29", dt["text29"].ToString());
                btFormat.SetNamedSubStringValue("text30", dt["text30"].ToString());

                #endregion
                btFormat.PrintSetup.Printer = printer;
                btFormat.PrintOut(false, false);
                return "Success!";
            }
            catch (Exception ex)
            {
                return "打印失败，请联系管理员." + ex.ToString();
                throw;
            }
            finally
            {
                if (btApp != null)
                {
                    btApp.Quit(BarTender.BtSaveOptions.btDoNotSaveChanges);
                    btApp = null;
                }
            }
        }

        private static string PrintRow(DataRow dt, string fileName, string printer, int qty)
        {
            string message = Print(dt, fileName, printer, qty);

            if (message == "Success!")
            {
                return "print success";
            }
            else
            {
                return message;
            }
        }

        private static string PrintDT(DataTable dt, string fileName, string printer, int qty)
        {
            //if (dt.Columns.Count != 35)
            //{
            //    return false;
            //}
            string info;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                info = PrintRow(dt.Rows[i], fileName, printer, qty);
                if (info != "print success")
                {
                    return info;
                }
            }

            KillProcess("bartend");
            return "ok";
        }

        private static void KillProcess(string processName)
        {
            System.Diagnostics.Process myproc = new System.Diagnostics.Process();
            foreach (Process thisproc in Process.GetProcessesByName(processName))
            {
                if (!thisproc.CloseMainWindow())
                {
                    thisproc.Kill();
                    GC.Collect();
                }
                Process[] prcs = Process.GetProcesses();
                foreach (Process p in prcs)
                {
                    if (p.ProcessName.Equals("程序名"))
                    {
                        p.Kill();
                    }
                }
            }
        }

        public static string PrintQR(string btwPath, string printer, int printQty, string jsonStr)
        {

            try
            {
                JObject ja = (JObject)JsonConvert.DeserializeObject(jsonStr);
                int jsonQty = ja.Count;
                #region define var
                string text1 = "";
                string text2 = "";
                string text3 = "";
                string text4 = "";
                string text5 = "";
                string text6 = "";
                string text7 = "";
                string text8 = "";
                string text9 = "";
                string text10 = "";
                string text11 = "";
                string text12 = "";
                string text13 = "";
                string text14 = "";
                string text15 = "";
                string text16 = "";
                string text17 = "";
                string text18 = "";
                string text19 = "";
                string text20 = "";
                string text21 = "";
                string text22 = "";
                string text23 = "";
                string text24 = "";
                string text25 = "";
                string text26 = "";
                string text27 = "";
                string text28 = "";
                string text29 = "";
                string text30 = "";
                try
                {
                    text1 = ja["text1"].ToString().Trim();
                    text2 = ja["text2"].ToString().Trim();
                    text3 = ja["text3"].ToString().Trim();
                    text4 = ja["text4"].ToString().Trim();
                    text5 = ja["text5"].ToString().Trim();
                    text6 = ja["text6"].ToString().Trim();
                    text7 = ja["text7"].ToString().Trim();
                    text8 = ja["text8"].ToString().Trim();
                    text9 = ja["text9"].ToString().Trim();
                    text10 = ja["text10"].ToString().Trim();
                    text11 = ja["text11"].ToString().Trim();
                    text12 = ja["text12"].ToString().Trim();
                    text13 = ja["text13"].ToString().Trim();
                    text14 = ja["text14"].ToString().Trim();
                    text15 = ja["text15"].ToString().Trim();
                    text16 = ja["text16"].ToString().Trim();
                    text17 = ja["text17"].ToString().Trim();
                    text18 = ja["text18"].ToString().Trim();
                    text19 = ja["text19"].ToString().Trim();
                    text20 = ja["text20"].ToString().Trim();
                    text21 = ja["text21"].ToString().Trim();
                    text22 = ja["text22"].ToString().Trim();
                    text23 = ja["text23"].ToString().Trim();
                    text24 = ja["text24"].ToString().Trim();
                    text25 = ja["text25"].ToString().Trim();
                    text26 = ja["text26"].ToString().Trim();
                    text27 = ja["text27"].ToString().Trim();
                    text28 = ja["text28"].ToString().Trim();
                    text29 = ja["text29"].ToString().Trim();
                    text30 = ja["text30"].ToString().Trim();
                }
                catch
                {
                    { return "0|打印出错,请检查jsonstr的内容是否正确。"; }
                }
                #endregion



                if (printQty == 0) { return "0|打印出错," + "打印份数不正确"; }
                if (printer == "") { return "0|打印出错," + "打印机不正确"; }
                if (btwPath.Trim() == "") { return "0|打印出错," + "打印模板btw路径不正确"; }


                //检查打印机名称
                bool printerExit = false;
                PrintDocument print = new PrintDocument();
                foreach (string sPrint in PrinterSettings.InstalledPrinters)//获取所有打印机名称
                {

                    if (sPrint == printer)
                    { printerExit = true; }
                }

                if (printerExit == false) { return "0|打印出错,打印机:" + printer + "不存在"; }


                //printer = @"/PRN=" + '"' + "Gprinter  GP-3150T" + '"' + " /p/x";
                // dos命令方式打印
                //runDos(@"C:\Program Files (x86)\Seagull\BarTender Suite\bartend.exe", @"c:\test.btw " + printer);



                //dll方式打印
                //printer = @"Gprinter  GP-3150T";



                #region add drColumn
                DataTable dt2 = new DataTable("Dt2");
                dt2.Columns.Add("text1");
                dt2.Columns.Add("text2");
                dt2.Columns.Add("text3");
                dt2.Columns.Add("text4");
                dt2.Columns.Add("text5");
                dt2.Columns.Add("text6");
                dt2.Columns.Add("text7");
                dt2.Columns.Add("text8");
                dt2.Columns.Add("text9");
                dt2.Columns.Add("text10");
                dt2.Columns.Add("text11");
                dt2.Columns.Add("text12");
                dt2.Columns.Add("text13");
                dt2.Columns.Add("text14");
                dt2.Columns.Add("text15");
                dt2.Columns.Add("text16");
                dt2.Columns.Add("text17");
                dt2.Columns.Add("text18");
                dt2.Columns.Add("text19");
                dt2.Columns.Add("text20");
                dt2.Columns.Add("text21");
                dt2.Columns.Add("text22");
                dt2.Columns.Add("text23");
                dt2.Columns.Add("text24");
                dt2.Columns.Add("text25");
                dt2.Columns.Add("text26");
                dt2.Columns.Add("text27");
                dt2.Columns.Add("text28");
                dt2.Columns.Add("text29");
                dt2.Columns.Add("text30");
                #endregion

                #region add dr
                DataRow dr = dt2.NewRow();
                //以后增加替换%的功能
                //dr["text1"] = partnum;
                //dr["text2"] = partdesc;
                //dr["text3"] = jobnum;
                ////dr["text4"] = "part123%法兰一二三abc%part123%F1111%F1111%0%1";
                //dr["text4"] = partnum + "%" + partdesc + "%" + jobnum + "%" + jobnum + asseq.ToString() + "%" + "";
                //dr["text5"] = "";
                dr["text1"] = text1;
                dr["text2"] = text2;
                dr["text3"] = text3;
                dr["text4"] = text4;
                dr["text5"] = text5;
                dr["text6"] = text6;
                dr["text7"] = text7;
                dr["text8"] = text8;
                dr["text9"] = text9;
                dr["text10"] = text10;
                dr["text11"] = text11;
                dr["text12"] = text12;
                dr["text13"] = text13;
                dr["text14"] = text14;
                dr["text15"] = text15;
                dr["text16"] = text16;
                dr["text17"] = text17;
                dr["text18"] = text18;
                dr["text19"] = text19;
                dr["text20"] = text20;
                dr["text21"] = text21;
                dr["text22"] = text22;
                dr["text23"] = text23;
                dr["text24"] = text24;
                dr["text25"] = text25;
                dr["text26"] = text26;
                dr["text27"] = text27;
                dr["text28"] = text28;
                dr["text29"] = text29;
                dr["text30"] = text30;
                dt2.Rows.Add(dr);
                #endregion

                string info = PrintDT(dt2, btwPath.Trim(), printer, printQty);
                if (info == "ok")
                {
                    return "1|处理成功";
                }
                else
                {
                    return "0|打印出错:" + info;
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
