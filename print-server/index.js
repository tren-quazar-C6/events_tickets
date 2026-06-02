const express = require('express');
const cors = require('cors');
const { exec } = require('child_process');
const fs = require('fs');

const app = express();
app.use(cors());
app.use(express.json({ limit: '2mb' }));

const PRINTER = process.env.PRINTER_NAME || 'Printer_USB_Printer_Port';

app.post('/print/ticket', async (req, res) => {
    const { eventName, eventDate, venue, customerName, documentNumber,
        section, seatNumber, ticketCode, qrToken } = req.body;

    const line = '================================';
    const qrData = String(qrToken || ticketCode || '').trim();
    const esc = [];

    esc.push(Buffer.from([0x1b, 0x40])); // init
    esc.push(Buffer.from([0x1b, 0x4d, 0x01])); // font B (smaller)
    esc.push(Buffer.from([0x1b, 0x61, 0x01])); // center
    esc.push(Buffer.from(clean(center('ENTRADA / TICKET') + '\n')));
    esc.push(Buffer.from(clean(line + '\n')));
    esc.push(Buffer.from([0x1b, 0x61, 0x00])); // left
    esc.push(Buffer.from(clean('\n')));
    esc.push(Buffer.from(clean(`Evento: ${trimTo(eventName, 24)}\n`)));
    esc.push(Buffer.from(clean(`Fecha:  ${trimTo(eventDate, 24)}\n`)));
    esc.push(Buffer.from(clean(`Lugar:  ${trimTo(venue || '', 24)}\n`)));
    esc.push(Buffer.from(clean('\n' + line + '\n')));
    esc.push(Buffer.from(clean(`Cliente: ${trimTo(customerName, 22)}\n`)));
    esc.push(Buffer.from(clean(`Seccion: ${trimTo(section, 22)}\n`)));
    esc.push(Buffer.from(clean(`Asiento: ${trimTo(seatNumber, 22)}\n\n`)));
    esc.push(Buffer.from([0x1b, 0x61, 0x01])); // center
    esc.push(Buffer.from(clean(`*** ${trimTo(ticketCode, 28)} ***\n\n`)));

    if (qrData.length > 0) {
        // ESC/POS QR (model 2, module size 5, EC level M)
        esc.push(Buffer.from([0x1d, 0x28, 0x6b, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00])); // model
        esc.push(Buffer.from([0x1d, 0x28, 0x6b, 0x03, 0x00, 0x31, 0x43, 0x05])); // size
        esc.push(Buffer.from([0x1d, 0x28, 0x6b, 0x03, 0x00, 0x31, 0x45, 0x31])); // EC M

        const qrBytes = Buffer.from(qrData, 'utf8');
        const qrLen = qrBytes.length + 3;
        const pL = qrLen & 0xff;
        const pH = (qrLen >> 8) & 0xff;
        esc.push(Buffer.from([0x1d, 0x28, 0x6b, pL, pH, 0x31, 0x50, 0x30])); // store
        esc.push(qrBytes);
        esc.push(Buffer.from([0x1d, 0x28, 0x6b, 0x03, 0x00, 0x31, 0x51, 0x30])); // print
        esc.push(Buffer.from(clean('\n')));
    }

    esc.push(Buffer.from(clean('Conserve este ticket\npara el ingreso al evento\n\n\n')));

    const tmpFile = `/tmp/ticket_${Date.now()}.bin`;
    fs.writeFileSync(tmpFile, Buffer.concat(esc));

    exec(`lp -d "${PRINTER}" -o raw "${tmpFile}"`, (err, stdout, stderr) => {
        fs.unlink(tmpFile, () => {});
        if (err) {
            return res.status(500).json({ error: err.message, stderr, stdout });
        }
        res.json({ ok: true, stdout });
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

function clean(value) {
    return String(value ?? '')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .replace(/[^\x20-\x7E\n]/g, '')
        .replace(/\r/g, '');
}

function trimTo(value, max) {
    const txt = clean(value);
    return txt.length <= max ? txt : txt.slice(0, max - 3) + '...';
}

app.listen(9100, () => console.log('Print server running on :9100'));