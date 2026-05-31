const express = require('express');
const cors = require('cors');
const { exec } = require('child_process');
const fs = require('fs');
const QRCode = require('qrcode');

const app = express();
app.use(cors());
app.use(express.json({ limit: '2mb' }));

const PRINTER = process.env.PRINTER_NAME || 'Printer_USB_Printer_Port';

app.post('/print/ticket', async (req, res) => {
    const { eventName, eventDate, venue, customerName, documentNumber,
        section, seatNumber, ticketCode, qrToken } = req.body;

    const line = '================================';

    let qrAscii = '';
    if (qrToken) {
        try {
            qrAscii = await QRCode.toString(qrToken, { type: 'terminal', small: true });
        } catch (_) {}
    }

    const content = [
        line,
        center('  ENTRADA / TICKET  '),
        line,
        '',
        wrap('Evento:  ' + eventName),
        'Fecha:   ' + eventDate,
        'Lugar:   ' + (venue || ''),
        '',
        line,
        'Cliente: ' + customerName,
        'Doc:     ' + documentNumber,
        '',
        'Sección: ' + section,
        'Asiento: ' + seatNumber,
        '',
        center('*** ' + ticketCode + ' ***'),
        line,
        '',
        qrAscii,
        '',
        center('Conserve este ticket'),
        center('para el ingreso al evento'),
        '',
        '\n\n\n'
    ].join('\n');

    const tmpFile = `/tmp/ticket_${Date.now()}.txt`;
    fs.writeFileSync(tmpFile, content);

    exec(`lp -d "${PRINTER}" "${tmpFile}"`, (err) => {
        fs.unlink(tmpFile, () => {});
        if (err) return res.status(500).json({ error: err.message });
        res.json({ ok: true });
    });
});

app.get('/health', (_req, res) => res.json({ ok: true }));

function center(text, width = 32) {
    const pad = Math.max(0, Math.floor((width - text.length) / 2));
    return ' '.repeat(pad) + text;
}

function wrap(text, width = 32) {
    return text.length <= width ? text : text.slice(0, width - 3) + '...';
}

app.listen(9100, () => console.log('Print server running on :9100'));