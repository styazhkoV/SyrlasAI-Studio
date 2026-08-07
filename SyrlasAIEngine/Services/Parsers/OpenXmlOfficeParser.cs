using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using SyrlasAIEngine.Models;

namespace SyrlasAIEngine.Services.Parsers
{
    public class OpenXmlOfficeParser : IDocumentParser
    {
        public bool SupportsExtension(string extension)
        {
            string ext = extension.ToLowerInvariant();
            return ext == ".docx" || ext == ".xlsx";
        }

        public Task<IEnumerable<DocumentChunkDto>> ParseAsync(Stream stream, string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".docx")
            {
                return Task.FromResult(ParseWord(stream, fileName));
            }
            else
            {
                return Task.FromResult(ParseExcel(stream, fileName));
            }
        }

        private IEnumerable<DocumentChunkDto> ParseWord(Stream stream, string fileName)
        {
            var chunks = new List<DocumentChunkDto>();
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document.Body;
            if (body == null) return chunks;

            int index = 0;
            var sb = new StringBuilder();

            foreach (var element in body.ChildElements)
            {
                if (element is Paragraph p && !string.IsNullOrWhiteSpace(p.InnerText))
                {
                    sb.AppendLine(p.InnerText);
                    if (sb.Length > 1000)
                    {
                        chunks.Add(CreateChunk(index++, sb.ToString(), fileName, "WORD_PARAGRAPH"));
                        sb.Clear();
                    }
                }
            }

            if (sb.Length > 0)
            {
                chunks.Add(CreateChunk(index++, sb.ToString(), fileName, "WORD_PARAGRAPH"));
            }

            return chunks;
        }

        private IEnumerable<DocumentChunkDto> ParseExcel(Stream stream, string fileName)
        {
            var chunks = new List<DocumentChunkDto>();
            using var doc = SpreadsheetDocument.Open(stream, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return chunks;

            int index = 0;
            foreach (var sheet in workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? Array.Empty<Sheet>())
            {
                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().SystemOrDefault();
                if (sheetData == null) continue;

                var sb = new StringBuilder();
                sb.AppendLine($"### Лист: {sheet.Name}");

                foreach (Row row in sheetData.Elements<Row>())
                {
                    var rowCells = new List<string>();
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        rowCells.Add(GetCellValue(doc, cell));
                    }
                    sb.AppendLine("| " + string.Join(" | ", rowCells) + " |");
                }

                chunks.Add(CreateChunk(index++, sb.ToString(), fileName, "EXCEL_SHEET_MARKDOWN"));
            }

            return chunks;
        }

        private static string GetCellValue(SpreadsheetDocument doc, Cell cell)
        {
            if (cell.CellValue == null) return string.Empty;
            string value = cell.CellValue.Text;
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var stringTable = doc.WorkbookPart?.SharedStringTablePart?.SharedStringTable;
                if (stringTable != null)
                {
                    value = stringTable.ElementAt(int.Parse(value)).InnerText;
                }
            }
            return value;
        }

        private static DocumentChunkDto CreateChunk(int index, string content, string fileName, string type)
        {
            return new DocumentChunkDto
            {
                ChunkIndex = index,
                Content = content,
                MetadataJson = JsonSerializer.Serialize(new { fileName, type }),
                TokenCount = content.Length / 4
            };
        }
    }
}