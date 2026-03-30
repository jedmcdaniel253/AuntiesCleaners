window.pdfInterop = {
    generateBillingReportPdf: function (report) {
        if (!window.jspdf || !window.jspdf.jsPDF) {
            throw new Error('jsPDF library not loaded. Check your internet connection and try again.');
        }
        const { jsPDF } = window.jspdf;
        const doc = new jsPDF();
        let y = 20;
        const pageHeight = 280;
        const leftMargin = 14;
        const rightMargin = 196;

        function checkPageBreak(needed) {
            if (y + needed > pageHeight) {
                doc.addPage();
                y = 20;
            }
        }

        // Title
        doc.setFontSize(18);
        doc.text("Auntie's Cleaners — Billing Report", leftMargin, y);
        y += 10;

        doc.setFontSize(11);
        doc.text("Period: " + report.dateFrom + " — " + report.dateTo, leftMargin, y);
        y += 12;

        // Sections
        for (const section of report.sections) {
            checkPageBreak(20);
            doc.setFontSize(14);
            doc.setFont(undefined, "bold");
            doc.text(section.name, leftMargin, y);
            y += 8;

            doc.setFontSize(10);
            doc.setFont(undefined, "normal");

            for (const item of section.lineItems) {
                checkPageBreak(8);
                doc.text(item.description, leftMargin, y);
                doc.text("$" + item.amount.toFixed(2), rightMargin, y, { align: "right" });
                y += 6;
            }

            checkPageBreak(10);
            doc.setFont(undefined, "bold");
            doc.text("Subtotal:", leftMargin + 4, y);
            doc.text("$" + section.subtotal.toFixed(2), rightMargin, y, { align: "right" });
            doc.setFont(undefined, "normal");
            y += 10;
        }

        // Grand Total
        checkPageBreak(16);
        doc.setDrawColor(0);
        doc.line(leftMargin, y, rightMargin, y);
        y += 8;
        doc.setFontSize(14);
        doc.setFont(undefined, "bold");
        doc.text("Grand Total:", leftMargin, y);
        doc.text("$" + report.grandTotal.toFixed(2), rightMargin, y, { align: "right" });

        // Return as base64
        return doc.output("datauristring").split(",")[1];
    }
};
