using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using System.Globalization;
using MaxisMyOSSCharts.Models;
using System.Data.Common;
using System.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Common;

namespace MaxisMyOSSCharts.Controllers
{
    public class ChartDataApplicationController: ApiController
    {
        protected Database dataBase = DatabaseFactory.CreateDatabase();
        protected DbTransaction transaction = null;
        private DataSet ExecuteProc(object[] parameters, string procName, string dataTableName, bool UseTransaction)
        {
            DataSet dataSet = new DataSet();

            DbConnection connection = dataBase.CreateConnection();
            try
            {
                connection.Open();
                if (UseTransaction)
                    transaction = connection.BeginTransaction();
                if (parameters == null)
                {
                    if (UseTransaction)
                        dataBase.LoadDataSet(dataBase.GetStoredProcCommand(procName), dataSet, dataTableName, transaction);
                    else
                        dataBase.LoadDataSet(dataBase.GetStoredProcCommand(procName), dataSet, dataTableName);
                }
                else
                {
                    if (UseTransaction)
                        dataBase.LoadDataSet(dataBase.GetStoredProcCommand(procName, parameters), dataSet, dataTableName, transaction);
                    else
                        dataBase.LoadDataSet(dataBase.GetStoredProcCommand(procName, parameters), dataSet, dataTableName);
                }
                if (UseTransaction)
                    transaction.Commit();
            }
            catch (Exception ex)
            {
                //Utility.Log.LogEvent(ex, "Database Execution Error" + procName);
                dataSet = null;
                if (UseTransaction)
                    transaction.Rollback();
            }
            finally
            {
                connection.Close();
            }
            return dataSet;
        }

        [HttpPost]
        public DataSet CaseResolutionData(PassingParam passingParam)
        {
            DataSet dsToReturn = new DataSet();
            try
            {
                Object[] parametes = new Object[] 
                    {
                        passingParam.Param1,
                        passingParam.Param2.Length >0 ?int.Parse(passingParam.Param2):0
                    };
                DataSet dsData = ExecuteProc(parametes, "P_ChartData_MyOSS_Cases_APP", "CaseData", true);

                if (dsData != null && dsData.Tables.Count > 0 && dsData.Tables[2].Rows[0]["Result"].ToString() == "Y")
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("WeekDesc");
                    dt.Columns.Add("Level1");
                    dt.Columns.Add("Level2");
                    dt.Columns.Add("Level3");
                    dt.Columns.Add("Level4");
                    dt.Columns.Add("YTDPerc");
                    DataRow dr = dt.NewRow();
                    dr["WeekDesc"] = "["; dr["Level1"] = "["; dr["Level2"] = "["; dr["Level3"] = "["; dr["Level4"] = "["; dr["YTDPerc"] = "[";

                    DataView dvWeeks = new DataView(dsData.Tables[0]);
                    DataTable dtWeeks = dvWeeks.ToTable(true, "WeekNo", "APPname");
                    DataTable dtTableData = new DataTable();
                    DataView dvData = new DataView(dsData.Tables[0]);
                    DataTable dtDistinctRows = dvData.ToTable(true, "RowNo", "RowDesc");

                    DataTable dtTblHeader = new DataTable();
                    dtTblHeader.Columns.Add("title");
                    dtTblHeader.Columns.Add("data");
                    DataRow drTblHeader = dtTblHeader.NewRow();
                    drTblHeader["title"] = "";
                    drTblHeader["data"] = "RowDesc";
                    dtTblHeader.Rows.Add(drTblHeader);
                    dtTblHeader.TableName = "TableHeader";

                    dtTableData.Columns.Add("RowDesc");
                    foreach (DataRow currRow in dtDistinctRows.Rows)
                    {
                        DataRow drTblRow = dtTableData.NewRow();
                        drTblRow["RowDesc"] = currRow["RowDesc"].ToString().Trim();
                        dtTableData.Rows.Add(drTblRow);
                    }
                    int lpCtr = 1;
                    foreach (DataRow currRow in dtWeeks.Rows)
                    {
                        //"<a class=edit-ProjectDocument data-url="+entry.ID+" href=#>"+entry.ProjectName+"</a>"
                       // dr["WeekDesc"] += "<a class=edit-ProjectDocument data-url=" + "'" + currRow["WeekDesc"].ToString().Trim() + "'" + "," + " href=#>" + "'" + currRow["WeekDesc"].ToString().Trim() + "'" + "," + "</a>";
                        //dr["WeekDesc"] += "'" + currRow["WeekDesc"].ToString().Trim() + "'" + ",";
                        dr["WeekDesc"] += "'" + currRow["APPname"].ToString().Trim() + "'" + ",";
                        dtTableData.Columns.Add("Week" + lpCtr.ToString());
                        drTblHeader = dtTblHeader.NewRow();
                        //drTblHeader["title"] = currRow["WeekDesc"].ToString().Trim();
                        drTblHeader["title"] = currRow["APPname"].ToString().Trim();
                        //drTblHeader["title"] = "<a class=edit-ProjectDocument data-url=" + currRow["WeekDesc"].ToString().Trim() + " href=#>" + currRow["WeekDesc"].ToString().Trim() + "</a>";
                        drTblHeader["data"] = "Week" + lpCtr.ToString();
                        dtTblHeader.Rows.Add(drTblHeader);
                        lpCtr++;
                    }
                    foreach (DataRow currRow in dsData.Tables[0].Rows)
                    {
                        if (currRow["RowNo"].ToString().Trim() == "1")
                            dr["Level1"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "2")
                            dr["Level2"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "3")
                            dr["Level3"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "4")
                            dr["Level4"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "10")
                            dr["YTDPerc"] += currRow["KPIValue"].ToString().Trim() + ",";
                    }
                    dr["WeekDesc"] = dr["WeekDesc"].ToString().TrimEnd(',') + "]";
                    dr["Level1"] = dr["Level1"].ToString().TrimEnd(',') + "]";
                    dr["Level2"] = dr["Level2"].ToString().TrimEnd(',') + "]";
                    dr["Level3"] = dr["Level3"].ToString().TrimEnd(',') + "]";
                    dr["Level4"] = dr["Level4"].ToString().TrimEnd(',') + "]";
                    dr["YTDPerc"] = dr["YTDPerc"].ToString().TrimEnd(',') + "]";
                    dt.Rows.Add(dr);
                    dt.TableName = "ChartData";
                    dsToReturn.Tables.Add(dt);


                    dsToReturn.Tables.Add(dtTblHeader);

                    for (int rowCtr = 0; rowCtr < dtDistinctRows.Rows.Count; rowCtr++)
                    {
                        for (int weekCtr = 0; weekCtr < dtWeeks.Rows.Count; weekCtr++)
                        {
                            DataRow[] arrDr = dsData.Tables[0].Select("RowNo = '" + dtDistinctRows.Rows[rowCtr][0].ToString() + "' AND WeekNo = '" + dtWeeks.Rows[weekCtr]["WeekNo"].ToString() + "'");
                            dtTableData.Rows[rowCtr][weekCtr + 1] = ((arrDr != null && arrDr.Length > 0) ? arrDr[0][4].ToString().Trim() : "0") + (rowCtr == 9 ? "%" : "");
                        }
                    }
                    dtTableData.Rows.InsertAt(dtTableData.NewRow(), 6);
                    dtTableData.TableName = "TableData";
                    dsToReturn.Tables.Add(dtTableData);

                    dsData.Tables[1].TableName = "NextPrevData";
                    dsToReturn.Tables.Add(dsData.Tables[1].Copy());
                }
                else
                {
                    dsToReturn = null;
                }
            }
            catch (Exception ex)
            {
                dsToReturn = null;
            }
            return dsToReturn;
        }

        [HttpPost]
        public DataSet CaseResolutionApplicationData(PassingParam passingParam)
        {
            DataSet dsToReturn = new DataSet();
            try
            {
                Object[] parametes = new Object[] 
                    {
                        passingParam.Param1,
                        passingParam.Param2.Length >0 ?int.Parse(passingParam.Param2):0
                    };
                DataSet dsData = ExecuteProc(parametes, "P_ChartData_MyOSS_Cases_APP", "CaseData", true);

                if (dsData != null && dsData.Tables.Count > 0 && dsData.Tables[2].Rows[0]["Result"].ToString() == "Y")
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("WeekDesc");
                    dt.Columns.Add("Level1");
                    dt.Columns.Add("Level2");
                    dt.Columns.Add("Level3");
                    dt.Columns.Add("Level4");
                    dt.Columns.Add("YTDPerc");
                    DataRow dr = dt.NewRow();
                    dr["WeekDesc"] = "["; dr["Level1"] = "["; dr["Level2"] = "["; dr["Level3"] = "["; dr["Level4"] = "["; dr["YTDPerc"] = "[";

                    DataView dvWeeks = new DataView(dsData.Tables[0]);
                    DataTable dtWeeks = dvWeeks.ToTable(true, "WeekNo", "APPname");
                    DataTable dtTableData = new DataTable();
                    DataView dvData = new DataView(dsData.Tables[0]);
                    DataTable dtDistinctRows = dvData.ToTable(true, "RowNo", "RowDesc");

                    DataTable dtTblHeader = new DataTable();
                    dtTblHeader.Columns.Add("title");
                    dtTblHeader.Columns.Add("data");
                    DataRow drTblHeader = dtTblHeader.NewRow();
                    drTblHeader["title"] = "";
                    drTblHeader["data"] = "RowDesc";
                    dtTblHeader.Rows.Add(drTblHeader);
                    dtTblHeader.TableName = "TableHeader";

                    dtTableData.Columns.Add("RowDesc");
                    foreach (DataRow currRow in dtDistinctRows.Rows)
                    {
                        DataRow drTblRow = dtTableData.NewRow();
                        drTblRow["RowDesc"] = currRow["RowDesc"].ToString().Trim();
                        dtTableData.Rows.Add(drTblRow);
                    }
                    int lpCtr = 1;
                    foreach (DataRow currRow in dtWeeks.Rows)
                    {
                        //"<a class=edit-ProjectDocument data-url="+entry.ID+" href=#>"+entry.ProjectName+"</a>"
                        // dr["WeekDesc"] += "<a class=edit-ProjectDocument data-url=" + "'" + currRow["WeekDesc"].ToString().Trim() + "'" + "," + " href=#>" + "'" + currRow["WeekDesc"].ToString().Trim() + "'" + "," + "</a>";
                        //dr["WeekDesc"] += "'" + currRow["WeekDesc"].ToString().Trim() + "'" + ",";
                        dr["WeekDesc"] += "'" + currRow["APPname"].ToString().Trim() + "'" + ",";
                        dtTableData.Columns.Add("Week" + lpCtr.ToString());
                        drTblHeader = dtTblHeader.NewRow();
                        //drTblHeader["title"] = currRow["WeekDesc"].ToString().Trim();
                        drTblHeader["title"] = currRow["APPname"].ToString().Trim();
                        //drTblHeader["title"] = "<a class=edit-ProjectDocument data-url=" + currRow["WeekDesc"].ToString().Trim() + " href=#>" + currRow["WeekDesc"].ToString().Trim() + "</a>";
                        drTblHeader["data"] = "Week" + lpCtr.ToString();
                        dtTblHeader.Rows.Add(drTblHeader);
                        lpCtr++;
                    }
                    foreach (DataRow currRow in dsData.Tables[0].Rows)
                    {
                        if (currRow["RowNo"].ToString().Trim() == "1")
                            dr["Level1"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "2")
                            dr["Level2"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "3")
                            dr["Level3"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "4")
                            dr["Level4"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "10")
                            dr["YTDPerc"] += currRow["KPIValue"].ToString().Trim() + ",";
                    }
                    dr["WeekDesc"] = dr["WeekDesc"].ToString().TrimEnd(',') + "]";
                    dr["Level1"] = dr["Level1"].ToString().TrimEnd(',') + "]";
                    dr["Level2"] = dr["Level2"].ToString().TrimEnd(',') + "]";
                    dr["Level3"] = dr["Level3"].ToString().TrimEnd(',') + "]";
                    dr["Level4"] = dr["Level4"].ToString().TrimEnd(',') + "]";
                    dr["YTDPerc"] = dr["YTDPerc"].ToString().TrimEnd(',') + "]";
                    dt.Rows.Add(dr);
                    dt.TableName = "ChartData";
                    dsToReturn.Tables.Add(dt);


                    dsToReturn.Tables.Add(dtTblHeader);

                    for (int rowCtr = 0; rowCtr < dtDistinctRows.Rows.Count; rowCtr++)
                    {
                        for (int weekCtr = 0; weekCtr < dtWeeks.Rows.Count; weekCtr++)
                        {
                            DataRow[] arrDr = dsData.Tables[0].Select("RowNo = '" + dtDistinctRows.Rows[rowCtr][0].ToString() + "' AND WeekNo = '" + dtWeeks.Rows[weekCtr]["WeekNo"].ToString() + "'");
                            dtTableData.Rows[rowCtr][weekCtr + 1] = ((arrDr != null && arrDr.Length > 0) ? arrDr[0][4].ToString().Trim() : "0") + (rowCtr == 9 ? "%" : "");
                        }
                    }
                    dtTableData.Rows.InsertAt(dtTableData.NewRow(), 6);
                    dtTableData.TableName = "TableData";
                    dsToReturn.Tables.Add(dtTableData);

                    dsData.Tables[1].TableName = "NextPrevData";
                    dsToReturn.Tables.Add(dsData.Tables[1].Copy());
                }
                else
                {
                    dsToReturn = null;
                }
            }
            catch (Exception ex)
            {
                dsToReturn = null;
            }
            return dsToReturn;
        }

        [HttpPost]
        public DataSet SmileyData()
        {
            return ExecuteProc(null, "P_ChartData_Smily", "SmileyData", true);
        }
        [HttpPost]
        public DataSet UCRData(PassingParam passingParam)
        {
            DataSet dsToReturn = new DataSet();
            try
            {
                Object[] parametes = new Object[] 
                    {
                        passingParam.Param1,
                        passingParam.Param2.Length >0 ?int.Parse(passingParam.Param2):0
                    };
                DataSet dsData = ExecuteProc(parametes, "P_ChartData_MyOSS_UCRMetrics", "UCRData", true);

                if (dsData != null && dsData.Tables.Count > 0 && dsData.Tables[2].Rows[0]["Result"].ToString() == "Y")
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("WeekDesc");
                    dt.Columns.Add("Met");
                    dt.Columns.Add("NotMet");
                    dt.Columns.Add("NewUCR");
                    dt.Columns.Add("PendingUCR");
                    dt.Columns.Add("TotalUCR");
                    dt.Columns.Add("PendingOSS");
                    dt.Columns.Add("YTD");
                    DataRow dr = dt.NewRow();
                    dr["WeekDesc"] = "[";
                    dr["Met"] = "[";
                    dr["NotMet"] = "[";
                    dr["NewUCR"] = "[";
                    dr["PendingUCR"] = "[";
                    dr["TotalUCR"] = "[";
                    dr["PendingOSS"] = "[";
                    dr["YTD"] = "[";

                    DataView dvWeeks = new DataView(dsData.Tables[0]);
                    DataTable dtWeeks = dvWeeks.ToTable(true, "WeekNo", "WeekDesc");
                    DataTable dtTableData = new DataTable();
                    DataView dvData = new DataView(dsData.Tables[0]);
                    DataTable dtDistinctRows = dvData.ToTable(true, "RowNo", "RowDesc");

                    DataTable dtTblHeader = new DataTable();
                    dtTblHeader.Columns.Add("title");
                    dtTblHeader.Columns.Add("data");
                    DataRow drTblHeader = dtTblHeader.NewRow();
                    drTblHeader["title"] = "";
                    drTblHeader["data"] = "RowDesc";
                    dtTblHeader.Rows.Add(drTblHeader);
                    dtTblHeader.TableName = "TableHeader";

                    dtTableData.Columns.Add("RowDesc");
                    foreach (DataRow currRow in dtDistinctRows.Rows)
                    {
                        if (Convert.ToInt16(currRow["RowNo"].ToString()) >= 7)
                        {
                            DataRow drTblRow = dtTableData.NewRow();
                            drTblRow["RowDesc"] = currRow["RowDesc"].ToString().Trim();
                            dtTableData.Rows.Add(drTblRow);
                        }
                    }
                    int lpCtr = 1;
                    foreach (DataRow currRow in dtWeeks.Rows)
                    {
                        dr["WeekDesc"] += "'" + currRow["WeekDesc"].ToString().Trim() + "'" + ",";
                        dtTableData.Columns.Add("Week" + lpCtr.ToString());
                        drTblHeader = dtTblHeader.NewRow();
                        drTblHeader["title"] = currRow["WeekDesc"].ToString().Trim();
                        drTblHeader["data"] = "Week" + lpCtr.ToString();
                        dtTblHeader.Rows.Add(drTblHeader);
                        lpCtr++;
                    }
                    foreach (DataRow currRow in dsData.Tables[0].Rows)
                    {
                        if (currRow["RowNo"].ToString().Trim() == "1")
                            dr["Met"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "2")
                            dr["NotMet"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "3")
                            dr["NewUCR"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "4")
                            dr["PendingUCR"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "5")
                            dr["TotalUCR"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "6")
                            dr["PendingOSS"] += currRow["KPIValue"].ToString().Trim() + ",";
                        if (currRow["RowNo"].ToString().Trim() == "10")
                            dr["YTD"] += currRow["KPIValue"].ToString().Trim() + ",";
                    }
                    dr["WeekDesc"] = dr["WeekDesc"].ToString().TrimEnd(',') + "]";
                    dr["Met"] = dr["Met"].ToString().TrimEnd(',') + "]";
                    dr["NotMet"] = dr["NotMet"].ToString().TrimEnd(',') + "]";
                    dr["NewUCR"] = dr["NewUCR"].ToString().TrimEnd(',') + "]";
                    dr["PendingUCR"] = dr["PendingUCR"].ToString().TrimEnd(',') + "]";
                    dr["TotalUCR"] = dr["TotalUCR"].ToString().TrimEnd(',') + "]";
                    dr["PendingOSS"] = dr["PendingOSS"].ToString().TrimEnd(',') + "]";
                    dr["YTD"] = dr["YTD"].ToString().TrimEnd(',') + "]";
                    dt.Rows.Add(dr);
                    dt.TableName = "ChartData";
                    dsToReturn.Tables.Add(dt);

                    dsToReturn.Tables.Add(dtTblHeader);

                    lpCtr = 0;
                    for (int rowCtr = 6; rowCtr <= 10; rowCtr++)
                    {
                        for (int weekCtr = 0; weekCtr < dtWeeks.Rows.Count; weekCtr++)
                        {
                            DataRow[] arrDr = dsData.Tables[0].Select("RowNo = '" + dtDistinctRows.Rows[rowCtr][0].ToString() + "' AND WeekNo = '" + dtWeeks.Rows[weekCtr]["WeekNo"].ToString() + "'");
                            dtTableData.Rows[lpCtr][weekCtr + 1] = ((arrDr != null && arrDr.Length > 0) ? arrDr[0][4].ToString().Trim() : "0") + (rowCtr == 9 ? "%" : "");
                        }
                        lpCtr++;
                    }
                    dtTableData.TableName = "TableData";
                    dsToReturn.Tables.Add(dtTableData);

                    dsData.Tables[1].TableName = "NextPrevData";
                    dsToReturn.Tables.Add(dsData.Tables[1].Copy());
                }
                else
                {
                    dsToReturn = null;
                }
            }
            catch (Exception ex)
            {
                dsToReturn = null;
            }
            return dsToReturn;
        }
    }
}
