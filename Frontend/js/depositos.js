let depositoEditandoId = null;
let depositoContratoPreseleccionado = null;
let contratosDepositoCache = [];

function initDepositos() {
    onView('depositos', loadDepositos);
    onView('deposito-form', initDepositoFormView);
    document.getElementById('btn-nuevo-deposito').addEventListener('click', () => { depositoEditandoId = null; depositoContratoPreseleccionado = null; navigateTo('deposito-form'); });
    document.getElementById('btn-volver-depositos').addEventListener('click', () => navigateTo('depositos'));
    document.getElementById('deposito-form').addEventListener('submit', guardarDeposito);
    document.getElementById('deposito-contrato').addEventListener('change', completarDepositoDesdeContrato);
    document.getElementById('deposito-recibido').addEventListener('input', sugerirEstadoDeposito);
}

async function loadDepositos() {
    const tbody = document.getElementById('depositos-body');
    tbody.innerHTML = '<tr><td colspan="9" class="loading">Cargando depósitos...</td></tr>';
    try {
        const depositos = await apiGet('/depositos');
        if (!depositos.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="empty-state">No hay depósitos registrados.</td></tr>';
            return;
        }
        tbody.innerHTML = depositos.map(d => `<tr>
            <td>${d.idDeposito}</td><td><strong>${escapeHtml(d.codigoContrato)}</strong><br><span class="muted">${escapeHtml(d.inquilino)}</span></td><td>${escapeHtml(d.propiedad)}</td>
            <td>${dinero(d.montoRequerido)}</td><td>${dinero(d.montoRecibido)}</td><td><strong>${dinero(d.montoPendiente)}</strong></td><td>${formatearFecha(d.fechaRecepcion)}</td>
            <td><span class="status-badge ${statusClass(d.estado)}">${escapeHtml(d.estado)}</span></td><td class="acciones">${hasPermission('DEPOSITOS.GESTIONAR') ? `<button class="btn btn-sm btn-secondary" onclick="editarDeposito(${d.idDeposito})">Editar</button><button class="btn btn-sm btn-danger" onclick="eliminarDeposito(${d.idDeposito})">Desactivar</button>` : '<span class="muted">Solo lectura</span>'}</td>
        </tr>`).join('');
    } catch (error) { tbody.innerHTML = `<tr><td colspan="9" class="table-error">${escapeHtml(error.message)}</td></tr>`; }
}

function abrirDepositoDesdeContrato(idContrato) {
    depositoEditandoId = null;
    depositoContratoPreseleccionado = idContrato;
    navigateTo('deposito-form');
}

async function initDepositoFormView() {
    const form = document.getElementById('deposito-form');
    form.reset();
    document.getElementById('deposito-id').value = depositoEditandoId || '';
    document.getElementById('deposito-recibido').value = '0';
    document.getElementById('deposito-estado').value = 'Pendiente';
    document.getElementById('deposito-error').classList.add('hidden');
    const heading = document.querySelector('#view-deposito-form .page-header h1');
    try {
        contratosDepositoCache = await apiGet('/contratos');
        document.getElementById('deposito-contrato').innerHTML = '<option value="">Seleccione un contrato...</option>' + contratosDepositoCache.map(c => `<option value="${c.idContrato}">${escapeHtml(c.codigoContrato)} · ${escapeHtml(c.razonSocial)} · ${escapeHtml(c.codigoPropiedad || '')}</option>`).join('');
        if (depositoEditandoId) {
            const d = await apiGet(`/depositos/${depositoEditandoId}`);
            document.getElementById('deposito-contrato').value = d.idContrato;
            document.getElementById('deposito-contrato').disabled = true;
            document.getElementById('deposito-requerido').value = d.montoRequerido;
            document.getElementById('deposito-recibido').value = d.montoRecibido;
            document.getElementById('deposito-fecha-recepcion').value = d.fechaRecepcion || '';
            document.getElementById('deposito-fecha-devolucion').value = d.fechaDevolucion || '';
            document.getElementById('deposito-estado').value = d.estado;
            document.getElementById('deposito-metodo').value = d.metodoPago || '';
            document.getElementById('deposito-referencia').value = d.referencia || '';
            document.getElementById('deposito-observaciones').value = d.observaciones || '';
            heading.textContent = `Editar depósito #${d.idDeposito}`;
            document.getElementById('btn-enviar-deposito').textContent = 'Actualizar depósito';
        } else {
            document.getElementById('deposito-contrato').disabled = false;
            if (depositoContratoPreseleccionado) {
                document.getElementById('deposito-contrato').value = depositoContratoPreseleccionado;
                completarDepositoDesdeContrato();
                depositoContratoPreseleccionado = null;
            }
            heading.textContent = 'Registrar depósito de garantía';
            document.getElementById('btn-enviar-deposito').textContent = 'Guardar depósito';
        }
    } catch (error) {
        const div = document.getElementById('deposito-error'); div.textContent = error.message; div.classList.remove('hidden');
    }
}

function completarDepositoDesdeContrato() {
    if (depositoEditandoId) return;
    const contrato = contratosDepositoCache.find(c => c.idContrato === Number(document.getElementById('deposito-contrato').value));
    if (contrato) document.getElementById('deposito-requerido').value = contrato.depositoRequerido || 0;
    sugerirEstadoDeposito();
}
function sugerirEstadoDeposito() {
    const requerido = Number(document.getElementById('deposito-requerido').value || 0);
    const recibido = Number(document.getElementById('deposito-recibido').value || 0);
    const select = document.getElementById('deposito-estado');
    if (select.value === 'Aplicado' || select.value === 'Devuelto') return;
    select.value = recibido <= 0 ? 'Pendiente' : recibido < requerido ? 'Parcial' : 'Recibido';
    if (recibido > 0 && !document.getElementById('deposito-fecha-recepcion').value) document.getElementById('deposito-fecha-recepcion').value = todayIso();
}

async function guardarDeposito(event) {
    event.preventDefault();
    const error = document.getElementById('deposito-error');
    const button = document.getElementById('btn-enviar-deposito');
    const payload = {
        idContrato: Number(document.getElementById('deposito-contrato').value),
        montoRequerido: Number(document.getElementById('deposito-requerido').value || 0),
        montoRecibido: Number(document.getElementById('deposito-recibido').value || 0),
        fechaRecepcion: document.getElementById('deposito-fecha-recepcion').value || null,
        fechaDevolucion: document.getElementById('deposito-fecha-devolucion').value || null,
        estado: document.getElementById('deposito-estado').value,
        metodoPago: document.getElementById('deposito-metodo').value || null,
        referencia: document.getElementById('deposito-referencia').value.trim() || null,
        observaciones: document.getElementById('deposito-observaciones').value.trim() || null
    };
    error.classList.add('hidden'); button.disabled = true;
    try {
        if (depositoEditandoId) await apiPut(`/depositos/${depositoEditandoId}`, payload);
        else await apiPost('/depositos', payload);
        depositoEditandoId = null;
        document.getElementById('deposito-contrato').disabled = false;
        navigateTo('depositos');
    } catch (err) { error.textContent = err.message; error.classList.remove('hidden'); }
    finally { button.disabled = false; }
}
async function editarDeposito(id) { depositoEditandoId = id; navigateTo('deposito-form'); }
async function eliminarDeposito(id) {
    if (!confirm('¿Desactivar este registro de depósito?')) return;
    try { await apiDelete(`/depositos/${id}`); await loadDepositos(); }
    catch (error) { alert(error.message); }
}
