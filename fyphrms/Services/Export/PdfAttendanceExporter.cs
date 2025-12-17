using iTextSharp.text;
using iTextSharp.text.pdf;
using fyphrms.Models;
using System.Globalization;

namespace fyphrms.Services.Export
{
    public static class PdfAttendanceExporter
    {
        public static byte[] GenerateAttendancePdf(DateTime selectedDate, List<Attendance> attendanceList)
        {
            using (var stream = new MemoryStream())
            {
                // PDF Document setup
                Document document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                Paragraph title = new Paragraph($"Attendance Report - {selectedDate:dd MMM yyyy}", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                title.SpacingAfter = 20;
                document.Add(title);

                // Table Setup
                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 25f, 25f, 25f, 25f });

                // Header Styling
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                AddCell(table, "Employee Name", headerFont, true);
                AddCell(table, "Check In Time", headerFont, true);
                AddCell(table, "Check Out Time", headerFont, true);
                AddCell(table, "Status", headerFont, true);
                
                

                // Rows
                foreach (var a in attendanceList)
                {
                    var fullName = a.Employee.FirstName + " " + a.Employee.LastName;
                    AddCell(table, fullName);

                    AddCell(table, a.CheckInTime.HasValue
                        ? a.CheckInTime.Value.ToString(@"hh\:mm")
                        : "-");

                    AddCell(table, a.CheckOutTime.HasValue
                        ? a.CheckOutTime.Value.ToString(@"hh\:mm")
                        : "-");

                    string status = (!a.CheckInTime.HasValue && !a.CheckOutTime.HasValue)
                                    ? "Absent"
                                    : "Present";

                    AddCell(table, status);
                }

                document.Add(table);
                document.Close();

                return stream.ToArray();
            }
        }

        private static void AddCell(PdfPTable table, string text, Font? font = null, bool isHeader = false)
        {
            font ??= FontFactory.GetFont(FontFactory.HELVETICA, 11);

            PdfPCell cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 8,
                HorizontalAlignment = Element.ALIGN_LEFT,
                BackgroundColor = isHeader ? new BaseColor(230, 230, 230) : BaseColor.White
            };

            table.AddCell(cell);
        }
    }
}
