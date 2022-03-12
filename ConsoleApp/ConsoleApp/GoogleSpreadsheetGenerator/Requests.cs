using System;
using System.Collections.Generic;
using System.Threading;
using Google.Apis.Sheets.v4.Data;
using Google;
using System.Linq;

namespace ConsoleApp
{
    partial class GoogleSpreadsheetGenerator
    {
        private List<Request> spreadsheetRequests = new List<Request>();
        private List<ValueRange> insertedData = new List<ValueRange>();

        private void SendBatchUpdateValuesRequest()
        {
            var batchUpdateValuesRequest = new BatchUpdateValuesRequest
            {
                ValueInputOption = "USER_ENTERED",
                Data = insertedData
            };

            // https://developers.google.com/webmaster-tools/search-console-api-original/v3/errors?hl=ru
            var batchUpdateRequest = service.Spreadsheets.Values.BatchUpdate(batchUpdateValuesRequest, spreadsheetId);
            while (true)
            {
                BatchUpdateValuesResponse response;
                try
                {
                    response = batchUpdateRequest.Execute();
                    return;
                }
                catch (GoogleApiException e) when (e.Error.Errors.FirstOrDefault()?.Reason== "limitExceeded")
                {
                    Thread.Sleep(10000);
                }
                catch (GoogleApiException e)
                {
                    foreach (var error in e.Error.Errors)
                    {
                        Console.WriteLine($"Reason: {error.Reason}. Message: {error.Message}");
                    }
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }

        private void SendBatchUpdateSpreadsheetRequest(Request request = null)
        {
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = request == null 
                    ? spreadsheetRequests
                    : new List<Request> { request }
            };

            var batchUpdateRequest = service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetId);
            while (true)
            {
                try
                {
                    var response = batchUpdateRequest.Execute(); // async
                    return;
                }
                catch (GoogleApiException e) when (e.Error.Errors.FirstOrDefault().Reason == "limitExceeded")
                {
                    Thread.Sleep(10000);
                }
                catch (GoogleApiException e)
                {
                    foreach (var error in e.Error.Errors)
                    {
                        Console.WriteLine($"Reason: {error.Reason}. Message: {error.Message}");
                    }
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
            }
        }

        private void InsertValue(object value, char column, int row)
            => InsertValues($"{sheet}!{column}{row}", new List<object> { value }, Dimension.Row);

        void InsertValues(string range, List<object> values, string dimension)
        {
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> { values },
                MajorDimension = dimension,
                Range = range
            };
            insertedData.Add(valueRange);
        }

        private void InsertFormula(string formula, char column, int row, bool sendNow = false)
            => InsertRepeatedFormula(formula, column, row, column, row, sendNow);

        private void InsertRepeatedFormula(string formula, char startColumn, int startRow, char endColumn, int endRow, bool sendNow = false)
        {
            var request = new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Cell = new CellData
                    {
                        UserEnteredValue = new ExtendedValue { FormulaValue = formula }
                    },
                    Range = new GridRange
                    {
                        StartColumnIndex = Column.ToInt(startColumn) - 1,
                        StartRowIndex = startRow - 1,
                        EndColumnIndex = Column.ToInt(endColumn),
                        EndRowIndex = endRow
                    },
                    Fields = "UserEnteredValue"
                }
            };

            if (sendNow)
            {
                SendBatchUpdateSpreadsheetRequest(request);
                return;
            }
            spreadsheetRequests.Add(request);
        }

        private void FormatCells(char startColumn, int startRow, char endColumn, int endRow, (CellFormat CellFormat, string Fields) format, bool sendNow = false)
        {
            var request = new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Cell = new CellData
                    {
                        UserEnteredFormat = format.CellFormat
                    },
                    Range = new GridRange
                    {
                        StartColumnIndex = Column.ToInt(startColumn) - 1,
                        StartRowIndex = startRow - 1,
                        EndColumnIndex = Column.ToInt(endColumn),
                        EndRowIndex = endRow,
                    },
                    Fields = format.Fields
                }
            };

            if (sendNow)
            {
                SendBatchUpdateSpreadsheetRequest(request);
                return;
            }
            spreadsheetRequests.Add(request);
        }

        private void FormatCell(char column, int row, (CellFormat CellFormat, string Fields) format, bool sendNow = false)
            => FormatCells(column, row, column, row, format, sendNow);

        private void MergeCells(char startColumn, int startRow, char endColumn, int endRow, string mergeType, bool sendNow = false)
        {
            var request = new Request
            {
                MergeCells = new MergeCellsRequest
                {
                    MergeType = mergeType,
                    Range = new GridRange
                    {
                        StartColumnIndex = Column.ToInt(startColumn) - 1,
                        StartRowIndex = startRow - 1,
                        EndColumnIndex = Column.ToInt(endColumn),
                        EndRowIndex = endRow
                    }
                }
            };

            if (sendNow)
            {
                SendBatchUpdateSpreadsheetRequest(request);
                return;
            }
            spreadsheetRequests.Add(request);
        }

        private void AutoResizeCells(string dimension, int startIndex, int endIndex, bool sendNow = false)
        {
            var request = new Request
            {
                AutoResizeDimensions = new AutoResizeDimensionsRequest
                {
                    Dimensions = new DimensionRange { Dimension = dimension, StartIndex = startIndex, EndIndex = endIndex }
                }
            };

            if (sendNow)
            {
                SendBatchUpdateSpreadsheetRequest(request);
                return;
            }
            spreadsheetRequests.Add(request);
        }

        private void ResizeCells(string dimension, int start, int end, int pixelSize, bool sendNow = false)
        {
            var request = new Request
            {
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    Range = new DimensionRange
                    {
                        Dimension = dimension,
                        StartIndex = start,
                        EndIndex = end + 1
                    },
                    Properties = new DimensionProperties
                    {
                        PixelSize = pixelSize
                    },
                    Fields = "pixelSize"
                }
            };

            if (sendNow)
            {
                SendBatchUpdateSpreadsheetRequest(request);
                return;
            }
            spreadsheetRequests.Add(request);
        }

        private void AddConditionalFormatRuleBasedOnCustomFormula(List<GridRange> ranges, string formula, CellFormat format, bool sendNow = false)
        {
            var request = new Request
            {
                AddConditionalFormatRule = new AddConditionalFormatRuleRequest
                {
                    Rule = new ConditionalFormatRule
                    {
                        BooleanRule = new BooleanRule
                        {
                            Condition = new BooleanCondition
                            {
                                Type = "CUSTOM_FORMULA",
                                Values = new List<ConditionValue>
                                {
                                    new ConditionValue { UserEnteredValue = formula}
                                }
                            },
                            Format = format
                        },
                        Ranges = ranges
                    },
                }
            };

            if (sendNow)
            {
                SendBatchUpdateSpreadsheetRequest(request);
                return;
            }
            spreadsheetRequests.Add(request);
        }
    }
}
