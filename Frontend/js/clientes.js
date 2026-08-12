let entidadReturnView = 'clientes';


function initClientes() {
    onView('clientes', () => loadEntidades('clientes', 'Cliente'));
    onView('propietarios', () => loadEntidades('propietarios', 'Propietario'));
    onView('proveedores', () => loadEntidades('proveedores', 'Proveedor'));

    document
        .getElementById('btn-nuevo-cliente')
        .addEventListener('click', () => abrirFormularioEntidad('Cliente'));

    document
        .getElementById('btn-nuevo-propietario')
        .addEventListener('click', () => abrirFormularioEntidad('Propietario'));

    document
        .getElementById('btn-nuevo-proveedor')
        .addEventListener('click', () => abrirFormularioEntidad('Proveedor'));

    document
        .getElementById('btn-volver-entidades')
        .addEventListener('click', () => navigateTo(entidadReturnView));

    document
        .getElementById('entidad-form')
        .addEventListener('submit', guardarEntidad);

    const documento = document.getElementById('entidad-rnc');

    documento.addEventListener('input', () => {
        const tipo =
            document.getElementById('entidad-tipo').value || 'Cliente';

        documento.value =
            formatearDocumentoDominicano(documento.value, tipo);
    });
}


/* =========================================================
   VISTAS Y ETIQUETAS
========================================================= */

function viewForEntityType(tipo) {
    if (tipo === 'Propietario') return 'propietarios';
    if (tipo === 'Proveedor') return 'proveedores';

    return 'clientes';
}


function singularLabel(tipo) {
    if (tipo === 'Cliente') return 'inquilino';

    return tipo.toLowerCase();
}


/* =========================================================
   CÓDIGO VISUAL PROFESIONAL

   El ID REAL de PostgreSQL NO se modifica.

   Ejemplos:
   Propietario ID 3 -> PROP-0003
   Cliente ID 5     -> INQ-0005
   Proveedor ID 8   -> PROV-0008
========================================================= */

function formatEntityCode(id, tipo) {
    const numero = String(id).padStart(4, '0');

    switch (tipo) {
        case 'Propietario':
            return `PROP-${numero}`;

        case 'Cliente':
            return `INQ-${numero}`;

        case 'Proveedor':
            return `PROV-${numero}`;

        default:
            return `ENT-${numero}`;
    }
}


/* =========================================================
   CARGAR ENTIDADES
========================================================= */

async function loadEntidades(viewId, tipo) {
    const tbody = document.getElementById(`${viewId}-body`);

    tbody.innerHTML = `
        <tr>
            <td colspan="5" class="loading">
                Cargando...
            </td>
        </tr>
    `;

    try {
        const entidades = await apiGet(
            `/entidades${queryString({
                tipo,
                incluirInactivos: true
            })}`
        );

        if (!entidades.length) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="empty-state">
                        No hay registros.
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = entidades
            .map(entidad => {
                const codigo =
                    formatEntityCode(entidad.idEntidad, tipo);

                const documento =
                    formatearDocumentoDominicano(
                        entidad.rncCedula,
                        tipo
                    );

                const estadoTexto =
                    entidad.activo ? 'Activo' : 'Inactivo';

                const estadoClase =
                    entidad.activo
                        ? 'status-activo'
                        : 'status-cancelado';

                const acciones =
                    hasPermission('ENTIDADES.GESTIONAR')
                        ? `
                            <button
                                class="btn btn-sm btn-secondary"
                                onclick="abrirFormularioEntidad(
                                    '${tipo}',
                                    ${entidad.idEntidad}
                                )"
                            >
                                Editar
                            </button>

                            <button
                                class="btn btn-sm ${
                                    entidad.activo
                                        ? 'btn-danger'
                                        : 'btn-success'
                                }"
                                onclick="cambiarEstadoEntidad(
                                    ${entidad.idEntidad},
                                    ${!entidad.activo},
                                    '${tipo}'
                                )"
                            >
                                ${
                                    entidad.activo
                                        ? 'Desactivar'
                                        : 'Activar'
                                }
                            </button>
                        `
                        : `
                            <span class="muted">
                                Solo lectura
                            </span>
                        `;

                return `
                    <tr>
                        <td>
                            <strong class="entity-code">
                                ${escapeHtml(codigo)}
                            </strong>
                        </td>

                        <td>
                            ${escapeHtml(documento)}
                        </td>

                        <td>
                            ${escapeHtml(entidad.razonSocial)}
                        </td>

                        <td>
                            <span class="status-badge ${estadoClase}">
                                ${estadoTexto}
                            </span>
                        </td>

                        <td class="acciones">
                            ${acciones}
                        </td>
                    </tr>
                `;
            })
            .join('');
    } catch (error) {
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="table-error">
                    ${escapeHtml(error.message)}
                </td>
            </tr>
        `;
    }
}


/* =========================================================
   CONFIGURACIÓN DEL DOCUMENTO
========================================================= */

function configurarCampoDocumento(tipo) {
    const input =
        document.getElementById('entidad-rnc');

    const label =
        document.getElementById('entidad-documento-label');

    const ayuda =
        document.getElementById('entidad-documento-ayuda');

    if (tipo === 'Cliente') {
        label.textContent =
            'Cédula del inquilino *';

        input.placeholder =
            '000-0000000-0';

        if (ayuda) {
            ayuda.textContent = '';
        }
    } else {
        label.textContent =
            'Cédula o RNC *';

        input.placeholder =
            '000-0000000-0 o 000-00000-0';

        if (ayuda) {
            ayuda.textContent = '';
        }
    }
}


/* =========================================================
   FORMATO DE CÉDULA / RNC
========================================================= */

function soloDigitosDocumento(valor) {
    return String(valor || '')
        .replace(/\D/g, '')
        .slice(0, 11);
}


function formatearCedulaParcial(digitos) {
    const d = digitos.slice(0, 11);

    if (d.length <= 3) {
        return d;
    }

    if (d.length <= 10) {
        return `${d.slice(0, 3)}-${d.slice(3)}`;
    }

    return `${d.slice(0, 3)}-${d.slice(3, 10)}-${d.slice(10)}`;
}


function formatearRncParcial(digitos) {
    const d = digitos.slice(0, 9);

    if (d.length <= 3) {
        return d;
    }

    if (d.length <= 8) {
        return `${d.slice(0, 3)}-${d.slice(3)}`;
    }

    return `${d.slice(0, 3)}-${d.slice(3, 8)}-${d.slice(8)}`;
}


function formatearDocumentoDominicano(valor, tipo) {
    const digitos =
        soloDigitosDocumento(valor);

    if (
        tipo === 'Cliente' ||
        digitos.length > 9
    ) {
        return formatearCedulaParcial(digitos);
    }

    return formatearRncParcial(digitos);
}


/* =========================================================
   VALIDACIÓN CÉDULA / RNC
========================================================= */

function validarDocumentoDominicano(valor, tipo) {
    const digitos =
        soloDigitosDocumento(valor);

    if (
        tipo === 'Cliente' &&
        digitos.length !== 11
    ) {
        return {
            valido: false,
            mensaje:
                'La cédula del inquilino debe tener exactamente 11 números.'
        };
    }

    if (
        tipo !== 'Cliente' &&
        digitos.length !== 9 &&
        digitos.length !== 11
    ) {
        return {
            valido: false,
            mensaje:
                'Introduzca una cédula de 11 números o un RNC de 9 números.'
        };
    }

    return {
        valido: true,

        documento:
            digitos.length === 11
                ? formatearCedulaParcial(digitos)
                : formatearRncParcial(digitos)
    };
}


/* =========================================================
   ABRIR FORMULARIO
========================================================= */

async function abrirFormularioEntidad(
    tipo,
    id = null
) {
    entidadReturnView =
        viewForEntityType(tipo);

    document
        .getElementById('entidad-form')
        .reset();

    document
        .getElementById('entidad-id')
        .value = id || '';

    document
        .getElementById('entidad-tipo')
        .value = tipo;

    document
        .getElementById('entidad-error')
        .classList.add('hidden');

    document
        .getElementById('entidad-form-title')
        .textContent =
            `${id ? 'Editar' : 'Nuevo'} ${singularLabel(tipo)}`;

    document
        .getElementById('btn-enviar-entidad')
        .textContent =
            id ? 'Actualizar' : 'Guardar';

    configurarCampoDocumento(tipo);

    if (id) {
        try {
            const entidad =
                await apiGet(`/entidades/${id}`);

            document
                .getElementById('entidad-rnc')
                .value =
                    formatearDocumentoDominicano(
                        entidad.rncCedula,
                        tipo
                    );

            document
                .getElementById('entidad-razon')
                .value =
                    entidad.razonSocial;
        } catch (error) {
            alert(error.message);
            return;
        }
    }

    navigateTo('entidad-form');
}


/* =========================================================
   GUARDAR ENTIDAD
========================================================= */

async function guardarEntidad(event) {
    event.preventDefault();

    const error =
        document.getElementById('entidad-error');

    const button =
        document.getElementById('btn-enviar-entidad');

    const id =
        document.getElementById('entidad-id').value;

    const tipo =
        document.getElementById('entidad-tipo').value;

    const validacion =
        validarDocumentoDominicano(
            document.getElementById('entidad-rnc').value,
            tipo
        );

    error.classList.add('hidden');

    if (!validacion.valido) {
        error.textContent =
            validacion.mensaje;

        error.classList.remove('hidden');

        document
            .getElementById('entidad-rnc')
            .focus();

        return;
    }

    const payload = {
        tipo,

        rncCedula:
            validacion.documento,

        razonSocial:
            document
                .getElementById('entidad-razon')
                .value
                .trim()
    };

    button.disabled = true;

    try {
        if (id) {
            await apiPut(
                `/entidades/${id}`,
                payload
            );
        } else {
            await apiPost(
                '/entidades',
                payload
            );
        }

        navigateTo(
            viewForEntityType(tipo)
        );
    } catch (err) {
        error.textContent =
            err.message;

        error.classList.remove('hidden');
    } finally {
        button.disabled = false;

        button.textContent =
            id ? 'Actualizar' : 'Guardar';
    }
}


/* =========================================================
   ACTIVAR / DESACTIVAR
========================================================= */

async function cambiarEstadoEntidad(
    id,
    activo,
    tipo
) {
    const accion =
        activo ? 'activar' : 'desactivar';

    if (
        !confirm(
            `¿Desea ${accion} este registro?`
        )
    ) {
        return;
    }

    try {
        await apiPatch(
            `/entidades/${id}/estado`,
            activo
        );

        await loadEntidades(
            viewForEntityType(tipo),
            tipo
        );
    } catch (error) {
        alert(error.message);
    }
}