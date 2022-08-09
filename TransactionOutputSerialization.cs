using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AtaccamaTransactions
{
    class TransactionOutputSerialization
    {
        #region GlobalVariables
        //Global variables
        private static string[] logFileLines;
        private static readonly List<FrontendTransaction> frontendTransactions = new List<FrontendTransaction>();
        #endregion

        #region Constants
        //Access variables for hardcoded values
        private const string provideFilePathMsg = "Provide input log file path";
        private const string fileNotFoundAtMsg = "Input file not found at";
        private const string fileReadSuccessMsg = "Log file read successfuly";
        private const string fileEmptyMsg = "Log file is empty";
        private const string transactionPrefix = "Notify: Transaction";
        private const string transactionEndFlag = "ended";
        private const string transactionThinkTimeFlag = "Think Time";
        private const string notFoundDefaultString = "Not Found";
        private static Tuple<string, string> requestTypeNeeded = new Tuple<string, string>("POST", "graphql");
        private const string fileName = "Transactions.json";
        #endregion

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(provideFilePathMsg);
                return;
            }
            string logFilePath = args[0];
            if (!File.Exists(logFilePath))
            {
                Console.WriteLine(fileNotFoundAtMsg + logFilePath);
                return;
            }

            //Reads log output file at the provided path
            ReadInputFile(logFilePath);

            //Identifies valid transactions and defines them into object class
            CollectValidTransactions();

            //Finds and adds valid steps within transactions and defines them into object class
            AttributeStepsToTransactions();

            //Finds and adds valid requests within steps and defines them into object class
            AttributeRequestsToSteps();

            //Takes whole object structure (transactions - steps - requests), and export to .json file at assembly folder 
            ExportTransactionsToFile();
        }

        /// <summary>
        /// Reads imported log file, separating lines into string array
        /// </summary>
        /// <param name="filePath">Path to imported file</param>
        private static void ReadInputFile(string filePath)
        {
            logFileLines = File.ReadAllLines(filePath);
            if (logFileLines.Length > 1 && logFileLines != null)
            {
                Console.WriteLine(fileReadSuccessMsg);
            }
            else
            {
                Console.WriteLine(fileEmptyMsg);
                return;
            }
        }

        #region TransactionsCollection
        /// <summary>
        /// Runs through log file lines, identifies starting and ending transaction lines, before filling transaction properties
        /// </summary>
        private static void CollectValidTransactions()
        {
            if (logFileLines != null)
            {
                for (int l = 0; l < logFileLines.Length; l++)
                {
                    if (logFileLines[l].StartsWith(transactionPrefix))
                    {
                        int startTransactionIndex = l;
                        int endTransactionIndex = l;
                        string transactionName = ExtractTransactionName(logFileLines[l]);
                        for (int m = startTransactionIndex; m < logFileLines.Length; m++)
                        {
                            if (logFileLines[m].StartsWith(transactionPrefix) && logFileLines[m].Contains(transactionEndFlag) && logFileLines[m].Contains(transactionName))
                            {
                                endTransactionIndex = m;
                                break;
                            }
                        }

                        FillTransactionBasicProperties(startTransactionIndex, endTransactionIndex, transactionName);
                    }
                }
            }
        }

        /// <summary>
        /// Checks between start (included) and end (excluded) line whether there are lines containing 2 needed strings (representing request types)
        /// </summary>
        /// <param name="startLineIndex">Starting line in log</param>
        /// <param name="endLineIndex">Ending line in log</param>
        /// <returns></returns>
        private static bool ContainsRequestType(int startLineIndex, int endLineIndex)
        {
            for (int i = startLineIndex; i < endLineIndex; i++)
            {
                if (logFileLines[i].Contains(requestTypeNeeded.Item1) && logFileLines[i].Contains(requestTypeNeeded.Item2))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Initializes transaction object class and fills its basic properties (not its child steps)
        /// </summary>
        /// <param name="startTransactionIndex">Position of line in log where transaction starts</param>
        /// <param name="endTransactionIndex">Position of line in log where transaction ends</param>
        /// <param name="transactionName">Name of the transaction</param>
        private static void FillTransactionBasicProperties(int startTransactionIndex, int endTransactionIndex, string transactionName)
        {
            if (startTransactionIndex < endTransactionIndex && ContainsRequestType(startTransactionIndex + 1, endTransactionIndex - 1))
            {
                var transactionTimeValues = ExtractTransactionTimeValues(logFileLines[endTransactionIndex]);
                frontendTransactions.Add(
                    _ = new FrontendTransaction
                    {
                        Name = transactionName,
                        Duration = transactionTimeValues[0],
                        ThinkTime = transactionTimeValues[1],
                        WastedTime = transactionTimeValues[2],
                        CompletionState = ExtractTransactionCompletionState(logFileLines[endTransactionIndex]),
                        StartMessageID = ExtractTransactionMessageID(logFileLines[startTransactionIndex]),
                        EndMessageID = ExtractTransactionMessageID(logFileLines[endTransactionIndex]),
                        StartLineIndex = startTransactionIndex,
                        EndLineIndex = endTransactionIndex
                    }
                );
            }
        }

        /// <summary>
        /// Finds part of line containing transaction duration, (optionally think time), and wasted time and converts them to int
        /// </summary>
        /// <param name="line">String with ended transaction summary</param>
        /// <returns>Int array representing transaction time values in miliseconds</returns>
        public static int[] ExtractTransactionTimeValues(string line)
        {
            Regex regex = new Regex(@"[^\(]+(?=\))");
            var regexMatch = regex.Match(line).Value;

            string duration = regexMatch.Split(':')[1].Split(' ')[1].Trim();
            string thinkTime = "";
            string wastedTime = "";
            if (line.Contains(transactionThinkTimeFlag))
            {
                thinkTime = regexMatch.Split(':')[2].Split(' ')[1].Trim();
                wastedTime = regexMatch.Split(':')[3].Split(' ')[1].Trim();
            }
            else
            {
                wastedTime = regexMatch.Split(':')[2].Split(' ')[1].Trim();
            }

            int[] timeValues = {
                ConvertTransactionTimeToMs(duration),
                ConvertTransactionTimeToMs(thinkTime),
                ConvertTransactionTimeToMs(wastedTime)
            };
            return timeValues;
        }

        /// <summary>
        /// Converts log file time format (in seconds, e.g. 1.2340), into int (in miliseconds)
        /// </summary>
        /// <param name="time">String holding number value with a single floating point</param>
        /// <returns>Int result of multiplying input number by 1000 (miliseconds)</returns>
        private static int ConvertTransactionTimeToMs(string time)
        {
            int convertedTime = 0;
            if (time != null && time != "")
            {
                float floatTime = float.Parse(time, CultureInfo.InvariantCulture) * 1000;
                convertedTime = (int)floatTime;
            }
            return convertedTime;
        }

        //Returns transaction fail or success state (or not found default) found in a string from transaction end line
        private static string ExtractTransactionCompletionState(string line)
        {
            string passState = GetMatchAtSplitByChar(line, '"', 3);
            if (passState != null)
            {
                return passState;
            }
            return notFoundDefaultString;
        }

        /// <summary>
        /// General helper function for getting one part of string split by a char
        /// </summary>
        /// <param name="line">String with a line to be split</param>
        /// <param name="ch">Char used to split the string</param>
        /// <param name="position">Index position of substring to be returned</param>
        /// <returns>Substring of original line, without whitespaces on sides</returns>
        private static string GetMatchAtSplitByChar(string line, char ch, int position)
        {
            return line.Split(ch)[position].Trim();
        }

        //Returns transaction name found in a string from transaction start/end line
        private static string ExtractTransactionName(string line)
        {
            return GetMatchAtSplitByChar(line, '"', 1);
        }

        //Returns transaction message ID found in a string from transaction start/end line
        private static string ExtractTransactionMessageID(string line)
        {
            Regex regex = new Regex(@"[^\[]+(?=\])");
            return regex.Match(line).Value.Split(':')[1].Trim();
        }
        #endregion

        #region StepsCollection
        private static void AttributeStepsToTransactions()
        {
            if (frontendTransactions != null)
            {
                foreach (var transaction in frontendTransactions)
                {
                    for (int l = transaction.StartLineIndex; l < transaction.EndLineIndex; l++)
                    {
                        if (logFileLines[l].Contains("Step") && logFileLines[l].Contains("started"))
                        {
                            string stepName = ExtractStepName(logFileLines[l]);
                            string stepIdIndex = ExtractStepIdIndex(logFileLines[l]);
                            int startStepTime = TimeInMsFromLineTimeStamp(logFileLines[l]);
                            int endStepTime = startStepTime;
                            string completionState = notFoundDefaultString;
                            int startLineIndex = l;
                            int endLineIndex = l;
                            for (int m = l + 1; m < transaction.EndLineIndex; m++)
                            {
                                if (logFileLines[m].Contains(stepIdIndex) && logFileLines[m].Contains("completed"))
                                {
                                    endStepTime = TimeInMsFromLineTimeStamp(logFileLines[m]);
                                    completionState = ExtractStepCompletionState(logFileLines[m]);
                                    endLineIndex = m;
                                    break;
                                }
                            }

                            transaction.Steps.Add(
                                _ = new Step
                                {
                                    Name = stepName,
                                    IdIndex = stepIdIndex,
                                    Duration = endStepTime - startStepTime,
                                    StartTime = startStepTime,
                                    EndTime = endStepTime,
                                    CompletionState = completionState,
                                    StartLineIndex = startLineIndex,
                                    EndLineIndex = endLineIndex
                                }
                            );
                        }
                        //TODO - Add condition for standalone Step without "started" phase
                    }
                }
            }
        }

        private static string ExtractStepName(string line)
        {
            string state = "started";
            string splitLine = GetMatchAtSplitByChar(line, ':', 2);
            return splitLine.Substring(0, splitLine.IndexOf(state) - 1).Trim();
        }

        private static string ExtractStepIdIndex(string line)
        {
            return GetMatchAtSplitByChar(line, ':', 1).Remove(0, 5).Trim();
        }

        private static int TimeInMsFromLineTimeStamp(string line)
        {
            return int.Parse(GetMatchAtSplitByChar(line, ':', 0).Remove(0, 2).Remove(8, 2));
        }

        private static string ExtractStepCompletionState(string line)
        {
            if (line.Contains("successfully"))
            {
                return "Success";
            }
            return "Fail";
        }
        #endregion

        #region RequestsCollection
        private static void AttributeRequestsToSteps()
        {
            if (frontendTransactions != null)
            {
                foreach (var transaction in frontendTransactions)
                {
                    if (AnyStepInTransaction(transaction))
                    {
                        foreach (var step in transaction.Steps)
                        {
                            for (int l = step.StartLineIndex; l < step.EndLineIndex; l++)
                            {
                                if (ContainsRequestType(l + 1, l + 2))
                                {
                                    string[] requestHeaderLines = FindRequestHeaderLines(l);
                                    int internalID = ExtractRequestInternalID(requestHeaderLines[0]);
                                    string[] requestBodyLines = FindRequestBodyLines(l + 3, step.EndLineIndex, internalID, out int linesPassed);
                                    string[] requestResponseLines = FindRequestResponseLines(l + 5 + linesPassed, step.EndLineIndex, internalID);

                                    //To make sure we only record requests fully executed within the step
                                    if (requestHeaderLines != null && requestBodyLines != null && requestResponseLines != null)
                                    {
                                        int startRequestTime = TimeInMsFromLineTimeStamp(requestHeaderLines[0]);
                                        int endRequestTime = TimeInMsFromLineTimeStamp(requestResponseLines[0]);

                                        step.Requests.Add(
                                            _ = new Request
                                            {
                                                InternalID = internalID,
                                                Duration = endRequestTime - startRequestTime,
                                                URL = ExtractRequestURL(requestHeaderLines[0]),
                                                RequestHeaders = new RequestHeaders
                                                {
                                                    Time = startRequestTime,
                                                    BytesSize = ExtractRequestBytesSize(requestHeaderLines[0]),
                                                    UserAgent = ExtractUserAgent(requestHeaderLines[1])
                                                },
                                                RequestBody = new RequestBody 
                                                {
                                                    Time = startRequestTime,
                                                    BytesSize = ExtractRequestBytesSize(requestBodyLines[0]),
                                                    OperationName = ExtractGraphQLOperationName(requestBodyLines[1])
                                                },
                                                ResponseHeaders = new ResponseHeaders 
                                                {
                                                    Time = endRequestTime,
                                                    BytesSize = ExtractRequestBytesSize(requestResponseLines[0]),
                                                    ContentLength = ExtractContentLength(requestResponseLines[1])
                                                }
                                            }
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool AnyStepInTransaction(FrontendTransaction transaction)
        {
            if (transaction.Steps.Count > 0)
            {
                return true;
            }
            return false;
        }

        private static string[] FindRequestHeaderLines(int l)
        {
            string requestHeaderLine = logFileLines[l];
            //At this point we know it is not at l or l+1, hence l + 2
            string requestUserAgentLine = FindRequestHeaderLine(l + 2, "User-Agent");
            return new string[] { requestHeaderLine, requestUserAgentLine };
        }

        private static string FindRequestHeaderLine(int lineIndex, string headerName)
        {
            while (!logFileLines[lineIndex].Contains("t=0"))
            {
                if (logFileLines[lineIndex].Contains(headerName))
                {
                    return logFileLines[lineIndex];
                }
                lineIndex++;
            }
            return null;
        }

        private static int ExtractRequestInternalID(string line)
        {
            Regex regex = new Regex(@"[^\=]+(?=\))");
            return int.Parse(regex.Matches(line)[1].Value);
        }

        private static string[] FindRequestBodyLines(int startLine, int endLineIndex, int internalID, out int linesPassed)
        {
            linesPassed = 0;
            for (int l = startLine; l < endLineIndex; l++)
            {
                if (logFileLines[l].Contains("internal ID=" + internalID.ToString()))
                {
                    return new string[] { logFileLines[l], logFileLines[l+1] };
                }
                linesPassed++;
            }
            return null;
        }

        private static string[] FindRequestResponseLines(int startLine, int endLineIndex, int internalID)
        {
            for (int l = startLine; l < endLineIndex; l++)
            {
                if (logFileLines[l].Contains("internal ID=" + internalID.ToString()))
                {
                    return new string[] { logFileLines[l], FindRequestHeaderLine(l + 1, "content-length") };
                }
            }
            return null;
        }

        private static string ExtractRequestURL(string line)
        {
            return GetMatchAtSplitByChar(line, '"', 1);
        }

        private static int ExtractRequestBytesSize(string line)
        {
            string splitLine = GetMatchAtSplitByChar(line, '(', 1);
            return int.Parse(splitLine.Remove(splitLine.Length - 5));
        }

        private static string ExtractUserAgent(string line)
        {
            return GetMatchAtSplitByChar(line, ':', 1);
        }

        private static string ExtractGraphQLOperationName(string line)
        {
            return GetMatchAtSplitByChar(line, '"', 3);
        }

        //Gets content-length value by parsing string from header line to int
        private static int ExtractContentLength(string line)
        {
            if (line == null)
            {
                return 0;
            }
            return int.Parse(GetMatchAtSplitByChar(line, ':', 1));
        }
        #endregion

        #region FileExport
        /// <summary>
        /// Transactions records are serialized into json format and written into a .json file
        /// </summary>
        private static void ExportTransactionsToFile()
        {
            string json = SerializeTransactionsToJson();
            if (json != null)
            {
                File.WriteAllText(fileName, json);
                Console.WriteLine("Transactions has been exported to: " + fileName);
            }
            else
            {
                Console.WriteLine("Transactions failed to export");
            }
        }

        /// <summary>
        /// Converts transactions records into json formatted string, using indented option for better readability in file
        /// </summary>
        /// <returns>String formatted as indented json</returns>
        private static string SerializeTransactionsToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(frontendTransactions, options);
            return json;
        }
        #endregion
    }
}
