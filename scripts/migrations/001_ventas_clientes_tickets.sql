CREATE TABLE IF NOT EXISTS clientes (
    id_cliente       INT AUTO_INCREMENT PRIMARY KEY,
    nombre           VARCHAR(200) NOT NULL,
    numero_documento VARCHAR(50)  NOT NULL,
    email            VARCHAR(200),
    telefono         VARCHAR(50),
    fecha_registro   DATETIME NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_clientes_documento UNIQUE (numero_documento)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS ventas (
    id_venta          INT AUTO_INCREMENT PRIMARY KEY,
    id_evento         INT NOT NULL,
    id_cliente        INT NOT NULL,
    id_staff          INT NOT NULL,
    subtotal          DECIMAL(12,2) NOT NULL,
    total             DECIMAL(12,2) NOT NULL,
    estado            VARCHAR(30)   NOT NULL DEFAULT 'completada',
    notas             TEXT,
    fecha_venta       DATETIME NOT NULL DEFAULT NOW(),
    fecha_cancelacion DATETIME,
    CONSTRAINT fk_ventas_cliente FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS venta_asientos (
    id_venta          INT NOT NULL,
    id_evento_asiento INT NOT NULL,
    PRIMARY KEY (id_venta, id_evento_asiento),
    CONSTRAINT fk_va_venta FOREIGN KEY (id_venta) REFERENCES ventas(id_venta)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS tickets (
    id_ticket           INT AUTO_INCREMENT PRIMARY KEY,
    id_venta            INT NOT NULL,
    id_evento           INT NOT NULL,
    id_cliente          INT NOT NULL,
    id_evento_asiento   INT NOT NULL,
    codigo_asiento      VARCHAR(50),
    zona                VARCHAR(100),
    codigo_unico        VARCHAR(150) NOT NULL,
    qr_token            VARCHAR(200) NOT NULL,
    qr_imagen_base64    MEDIUMTEXT,
    precio_pagado       DECIMAL(12,2) NOT NULL DEFAULT 0,
    estado_ticket       VARCHAR(30)   NOT NULL DEFAULT 'activo',
    fecha_emision       DATETIME NOT NULL DEFAULT NOW(),
    fecha_validacion    DATETIME,
    id_staff_validacion INT,
    CONSTRAINT uq_tickets_codigo UNIQUE (codigo_unico),
    CONSTRAINT uq_tickets_qr    UNIQUE (qr_token),
    CONSTRAINT fk_tickets_venta  FOREIGN KEY (id_venta)   REFERENCES ventas(id_venta),
    CONSTRAINT fk_tickets_cliente FOREIGN KEY (id_cliente) REFERENCES clientes(id_cliente)
) ENGINE=InnoDB;

CREATE INDEX IF NOT EXISTS idx_ventas_cliente   ON ventas(id_cliente);
CREATE INDEX IF NOT EXISTS idx_ventas_evento    ON ventas(id_evento);
CREATE INDEX IF NOT EXISTS idx_tickets_venta    ON tickets(id_venta);
CREATE INDEX IF NOT EXISTS idx_tickets_cliente  ON tickets(id_cliente);
CREATE INDEX IF NOT EXISTS idx_tickets_qr_token ON tickets(qr_token);