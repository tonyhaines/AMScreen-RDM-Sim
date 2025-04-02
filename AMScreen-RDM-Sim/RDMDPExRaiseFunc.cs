using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using Messaging;

namespace AMScreenRDM
{
    public static class ImmediateDataProcessing
    {
        //***********************************************************************************
        //NAME: ProcessExceptionRaise                                                       *
        //                                                                                  *
        //INPUT:        1. Sign Serial Number                                               *
        //              2. Path and filename of file to process                             *
        //PROCESSING:   1. Insert the exception into the database                           *
        //              2. Email the exception details if required                          *
        //OUTPUT:       1. None                                                             *
        //***********************************************************************************
        public static void ProcessExceptionRaise(string strSignSerialNumber, string strPathAndFilename)
        {
            const string EXCEPTION_PARTS_SEPARATOR = "|";
            const string ADDITIONAL_DATA_PARTS_SEPARATOR = ";";
            const string ADDITIONAL_DATA_NAME_AND_VALUE_SEPARATOR = ":";

            const int ARRAY_POS_CODE = 0;
            const int ARRAY_POS_RAISE_TIMESTAMP = 1;
            const int ARRAY_POS_RAISE_VALUE = 2;
            const int ARRAY_POS_ADDITIONAL_DATA = 3;
            const int EXPECTED_NUM_DATA_LINE_PARTS = 4;

            const int ARRAY_POS_ADDITIONAL_DATA_NAME_TAG = 0;
            const int ARRAY_POS_ADDITIONAL_DATA_VALUE = 1;

            // we use this for decimal.TryParse
            decimal decHelper = 0;

            decimal? decValue = null;
            decimal? decMin = null;
            decimal? decMax = null;

            Stopwatch objStopWatch = new Stopwatch();
            
            // MDR: instead of replacing tags in the description with values, we save the values directly and leave the description alone
            try
            {
                //start the stopwatch for use in determining how long the file took to process
                objStopWatch.Start();

                //split the line into an array (NOTE: not removing empty entries on the split because sometimes the additional data can be empty but we still need to access the array pos to determine that)
                string[] arrDataLineParts = File.ReadAllText(strPathAndFilename).Split(new string[] { EXCEPTION_PARTS_SEPARATOR }, StringSplitOptions.None);

                //if we don't have the expected number of array entries then move the file to the error directory and exit
                if (arrDataLineParts.Length != EXPECTED_NUM_DATA_LINE_PARTS)
                {
                    //log the problem and move the file to the error directory
                    string strMessage = string.Format("Sign {0} attempted to raise an exception using incorrectly formatted data ({1}).", strSignSerialNumber, Path.GetFileName(strPathAndFilename));
                    CommonProcessing.LogAndHandleFileError(strPathAndFilename, "ImmediateDataProcessing-ProcessExceptionRaise", strMessage);

                    //exit
                    return;
                }

                //clear parameters before using in case a previous usage failed (as the database object is used globally or passed around)
                m_objMsSqlServer.ClearParameters();

                //get the details for the given exception code so we can build up the data for the database and the email to be sent out
                m_objMsSqlServer.AddParameter("@vcSignSerialNumber", SQLDataType.SQLVarChar, 20, strSignSerialNumber);
                m_objMsSqlServer.AddParameter("@vcExceptionCode", SQLDataType.SQLVarChar, 50, arrDataLineParts[ARRAY_POS_CODE]);
                DataTable mdtExceptionCodeDetails = m_objMsSqlServer.RunSPDataTable("spExceptionCode_DetailsGet");

                //act according to whether we got back the details or not
                if (mdtExceptionCodeDetails.Rows.Count > 0)
                {
                    //get the first and only row
                    DataRow objRow = mdtExceptionCodeDetails.Rows[0];

                    //get the exception description template and replace with value if it applies
                    string strExceptionDescription = objRow["vcDescriptionTemplate"].ToString().Replace(Constants.EXCEPTION_DESCRIPTION_TEMPLATE_VALUE_TAG, arrDataLineParts[ARRAY_POS_RAISE_VALUE]);

                    if (decimal.TryParse(arrDataLineParts[ARRAY_POS_RAISE_VALUE], out decHelper))
                    {
                        // assign to value                        
                        decValue = Convert.ToDecimal(decHelper.ToString("0.#####"));

                        // Cap max decimal value to prevent overflow in edge case HWManager reported value exceeds datatype
                        if(decValue > 99999.99999m) 
                        {
                            decValue = 99999.99999m;
                        }
                    }

                    //if we have additional data then split it and loop through it replacing appropriate parts of the exception description template
                    if (arrDataLineParts[ARRAY_POS_ADDITIONAL_DATA] != string.Empty)
                    {
                        //split the additional data into an array
                        string[] arrAdditionalDataParts = arrDataLineParts[ARRAY_POS_ADDITIONAL_DATA].Split(new string[] { ADDITIONAL_DATA_PARTS_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

                        //loop through the additional data replacing appropriate parts of the exception description template
                        foreach (string strAdditionalDataPart in arrAdditionalDataParts)
                        {
                            //get an array with name and value in
                            string[] arrNameAndValue = strAdditionalDataPart.Split(new string[] { ADDITIONAL_DATA_NAME_AND_VALUE_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

                            switch (arrNameAndValue[0])
                            {
                                case "<MIN>":
                                    if (decimal.TryParse(arrNameAndValue[1], out decHelper))
                                    {
                                        decMin = Convert.ToDecimal(decHelper.ToString("0.#####"));
                                    }
                                    break;
                                case "<MAX>":
                                    if (decimal.TryParse(arrNameAndValue[1], out decHelper))
                                    {
                                        decMax = Convert.ToDecimal(decHelper.ToString("0.#####"));
                                    }
                                    break;
                            }

                            //replace any named tags in the exception description with the value
                            strExceptionDescription = strExceptionDescription.Replace(arrNameAndValue[ARRAY_POS_ADDITIONAL_DATA_NAME_TAG], arrNameAndValue[ARRAY_POS_ADDITIONAL_DATA_VALUE]);
                        }
                    }

                    // checks the raise value against the constraints setting and returns the next larger/smaller constraint value if it exceeds the constraints
                    decimal dcRaiseValue = Utilities.CheckExceptionConstraints(arrDataLineParts[ARRAY_POS_CODE], arrDataLineParts[ARRAY_POS_RAISE_VALUE]);

                    // MDR: Changed to use the new function which sorts out the milliseconds
                    DateTime dtRaiseTime = Utilities.ParseDateWithMillisecondsIfAvailable(arrDataLineParts[ARRAY_POS_RAISE_TIMESTAMP]);
                    //DateTime.ParseExact(arrDataLineParts[ARRAY_POS_RAISE_TIMESTAMP], Constants.SIGN_DATA_TIMESTAMP_FORMAT, null);

                    //insert the exception into the live exceptions table
                    m_objMsSqlServer.AddParameter("@vcSignSerialNumber", SQLDataType.SQLVarChar, 20, strSignSerialNumber);
                    m_objMsSqlServer.AddParameter("@inExceptionCodeID", SQLDataType.SQLInt, int.Parse(objRow["inExceptionCodeID"].ToString()));
                    m_objMsSqlServer.AddParameter("@inExceptionCategoryID", SQLDataType.SQLInt, int.Parse(objRow["inExceptionCategoryID"].ToString()));
                    m_objMsSqlServer.AddParameter("@inExceptionTypeID", SQLDataType.SQLInt, int.Parse(objRow["inExceptionTypeID"].ToString()));
                    //  m_objMsSqlServer.AddParameter("@dtRaisedLocal", SQLDataType.SQLDateTime, DateTime.ParseExact(arrDataLineParts[ARRAY_POS_RAISE_TIMESTAMP], Constants.SIGN_DATA_TIMESTAMP_FORMAT, null).ToString(Constants.MY_SQL_DATE_TIME_FORMAT));
                    // MDR: Changed to use an actual datetime value rather than a string representation
                    m_objMsSqlServer.AddParameter("@dtRaisedLocal", SQLDataType.SQLDateTime2, dtRaiseTime);
                    m_objMsSqlServer.AddParameter("@dcRaiseValue", SQLDataType.SQLDecimal, dcRaiseValue);
                    m_objMsSqlServer.AddParameter("@dcValue", SQLDataType.SQLDecimal, decValue);
                    m_objMsSqlServer.AddParameter("@dcMinValue", SQLDataType.SQLDecimal, decMin);
                    m_objMsSqlServer.AddParameter("@dcMaxValue", SQLDataType.SQLDecimal, decMax);

                    // m_objMsSqlServer.AddParameter("@vcDescription", SQLDataType.SQLVarChar, 750, strExceptionDescription);
                    m_objMsSqlServer.AddParameter("@dtInserted", SQLDataType.SQLDateTime2, DateTime.UtcNow);

                    //try to insert the alarm but catch any errors that occur (mainly in case a sign sends the same alarm more than once and we have a key violation)
                    try
                    {
                        m_objMsSqlServer.RunSPNonQuery("spSignExceptionLive_Raise");
                    }
                    catch (Exception objDatabaseException)
                    {
                        string strMessage = string.Empty;

                        //see if its a unique key violation error or not and build the error message accordingly
                        if (objDatabaseException.Message.IndexOf(Constants.EXCEPTION_UNQIUE_KEY_VIOLATION_ERROR) > -1)
                        {
                            strMessage = string.Format("Sign {0} raised a duplicate exception using code of \"{1}\".", strSignSerialNumber, arrDataLineParts[ARRAY_POS_CODE]);
                        }
                        else
                        {
                            strMessage = string.Format("There has been an error attempting to process exception raise file {0} from sign {1} *** ErrSource={2} *** ErrDesc={3}", Path.GetFileName(strPathAndFilename), strSignSerialNumber, objDatabaseException.Source, objDatabaseException.Message);
                        }

                        //log and handle the file error and then exit
                        CommonProcessing.LogAndHandleFileError(strPathAndFilename, "ImmediateDataProcessing-ProcessExceptionRaise", strMessage);
                        return;
                    }

                    //only email about exceptions if configured to do so (e.g. may not want to on dev systems)
                    if (Globals.blnEmailExceptions && objRow["vcSignState"].ToString() != "Disabled")
                    {
                        // build email subject
                        string strTicketSubject = "Ticket Subject: " + string.Format("[{0}:{1}:{2}:{3}] Site: {4}, Sign: {5}, CMS ID: {6}", (int)objRow["inNetworkOwner"], (int)objRow["inLandlord"], (int)objRow["inSite"], (int)objRow["inSign"], objRow["vcSiteCode"].ToString(), strSignSerialNumber, objRow["vcThirdPartyCmsID"].ToString());

                        //build the exception email
                        string strEmailMessage = string.Format("{0}\r\n\r\nCMS ID: {1}\r\nSign: {2}\r\nSite Details: {3}, {4}.\r\nLandlord: {5}\r\nNetwork Owner: {6}\r\n\r\nType: {7}\r\nCategory: {8}\r\nName: {9}\r\n\r\nRaised: {10}\r\n\r\n{11}", strTicketSubject, objRow["vcThirdPartyCmsID"].ToString(), strSignSerialNumber, objRow["vcSiteAddressLine1"].ToString(), objRow["vcSiteAddressPostcode"].ToString(), objRow["vcLandlordName"].ToString(), objRow["vcNetworkOwnerName"].ToString(), objRow["vcType"].ToString(), objRow["vcCategory"].ToString(), objRow["vcName"].ToString(), dtRaiseTime.ToString(Constants.DISPLAY_DATE_FORMAT), strExceptionDescription);

                        //always send exception alarm emails
                        if ((int)objRow["inExceptionTypeID"] == Constants.EXCEPTION_TYPE_ALARM)
                        {
                            //build the exception subject
                            string strEmailSubject = string.Format("RDM EXCEPTION RAISE: {0} alarm for sign {1}", objRow["vcName"].ToString(), strSignSerialNumber);
                                                          "Subject: RDM EXCEPTION CLEAR: {Billboard cabinet 18 PSU#2 voltage alarm} for sign 2081900058"

                            //send the exception email
                            Utilities.SendEmail(Globals.strExceptionEmailAddress, strEmailSubject, strEmailMessage);

                            // call to ticketing system here
                        }

                        //only send exception warning emails if required
                        if (((int)objRow["inExceptionTypeID"] == Constants.EXCEPTION_TYPE_WARNING) && (Globals.blnEmailExceptionWarnings))
                        {
                            //build the exception subject
                            string strEmailSubject = string.Format("RDM EXCEPTION RAISE: {0} warning for sign {1}", objRow["vcName"].ToString(), strSignSerialNumber);

                            //send the exception email
                            Utilities.SendEmail(Globals.strExceptionEmailAddress, strEmailSubject, strEmailMessage);
                        }

                        // Determine notification type
                        string notificationType = (int)objRow["inExceptionTypeID"] == Constants.EXCEPTION_TYPE_ALARM ? "alarm" : "warning";

                        // Format to JSON
                        EmailMessageFormatter formatter = new EmailMessageFormatter();
                        string jsonEmailData = formatter.FormatToJson(
                            "RAISE",
                            (int)objRow["inNetworkOwner"],
                            (int)objRow["inLandlord"],
                            (int)objRow["inSite"],
                            (int)objRow["inSign"],
                            objRow["vcSiteCode"].ToString(),
                            objRow["vcThirdPartyCmsID"].ToString(),
                            strSignSerialNumber,
                            objRow["vcSiteAddressLine1"].ToString(),
                            objRow["vcSiteAddressPostcode"].ToString(),
                            objRow["vcLandlordName"].ToString(),
                            objRow["vcNetworkOwnerName"].ToString(),
                            objRow["vcType"].ToString(),
                            objRow["vcCategory"].ToString(),
                            objRow["vcName"].ToString(),
                            dtRaiseTime.ToString(Constants.DISPLAY_DATE_FORMAT),
                            strExceptionDescription,
                            (int)objRow["inExceptionTypeID"],
                            notificationType);

                        // Send the JSON string as a message
                        RabbitMQSender sender = new RabbitMQSender("10.137.0.53", "ticketingQ", "ticketingExchange", 5672);
                        sender.SendMessage(jsonEmailData);
                    }

                    //no errors so put the file in the processed directory
                    CommonProcessing.MoveFileToProcessedDirectory(strPathAndFilename);
                }
                else
                {
                    //exception code doesn't seem to exist so email an error and put the file in the errors directory
                    string strMessage = string.Format("Sign {0} raised an exception using an unknown exception code of \"{1}\" ({2}).", strSignSerialNumber, arrDataLineParts[ARRAY_POS_CODE], Path.GetFileName(strPathAndFilename));
                    CommonProcessing.LogAndHandleFileError(strPathAndFilename, "ImmediateDataProcessing-ProcessExceptionRaise", strMessage);
                }

                objStopWatch.Stop();
                
                //record how long it took to process the file
                //objStopWatch.Stop();
                using (StreamWriter objStreamWriter = new StreamWriter(Globals.strLoggingPath + "\\ExceptionRaiseFileProcessingTimes_" + DateTime.Now.ToString("dd-MMM-yyyy") + ".txt", true))
                {
                    objStreamWriter.WriteLine(DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + "," + Path.GetFileName(strPathAndFilename) + "," + objStopWatch.ElapsedMilliseconds);
                }
            }
            catch (Exception objException)
            {
                //create the log entry
                string strMessage = string.Format("There has been an error attempting to process the given exception raise file {0} *** ErrSource={1} *** ErrDesc={2}", strPathAndFilename, objException.Source, objException.Message);

                //add the log entry
                CommonProcessing.LogAndHandleFileError(strPathAndFilename, "ImmediateDataProcessing-ProcessExceptionRaise", strMessage);
            }
        }
    }
}