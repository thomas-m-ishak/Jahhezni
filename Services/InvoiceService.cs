using iTextSharp.text;
using iTextSharp.text.pdf;
using Jahhezni.Models;
using System.IO;

public class InvoiceService
{
    public byte[] GenerateInvoice(Order order, List<OrderItem> items)
    {
        using var ms = new MemoryStream();
        var doc = new Document(PageSize.A4);
        PdfWriter.GetInstance(doc, ms);

        doc.Open();

        doc.Add(new Paragraph("Jahhezni Invoice"));
        doc.Add(new Paragraph($"Order ID: {order.OrderId}"));
        doc.Add(new Paragraph($"Date: {DateTime.UtcNow}"));
        doc.Add(new Paragraph(" "));

        foreach (var item in items)
        {
            doc.Add(new Paragraph(
                $"Product: {item.ProductId} | Qty: {item.Qty} | Price: {item.UnitPrice}"
            ));
        }

        doc.Add(new Paragraph(" "));
        doc.Add(new Paragraph($"Total: {order.OrderTotal} EGP"));

        doc.Close();

        return ms.ToArray();
    }
}