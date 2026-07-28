let auditoriaPagina = 1;
const AUDITORIA_TAMANO = 50;

function initAuditoria() {
    onView('auditoria', loadAuditoria);

    document.getElementById('btn-prev-page').addEventListener('click', () => {
        if (auditoriaPagina > 1) {
            auditoriaPagina--;
            loadAuditoria();
        }
    });

    document.getElementById('btn-next-page').addEventListener('click', () => {
        auditoriaPagina++;
        loadAuditoria();
    });
}

async function loadAuditoria() {
    const tbody = document.getElementById('auditoria-body');
    tbody.innerHTML = '<tr><td colspan="8" class="loading">Cargando registros de auditoría...</td></tr>';

    try {
        const resp = await apiGet(`/auditoria?pagina=${auditoriaPagina}&tamano=${AUDITORIA_TAMANO}`);
        const logs = resp.items || resp;

        if (!logs || logs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" style="text-align:center;color:var(--gray-500)">No hay registros de auditoría.</td></tr>';
            document.getElementById('btn-prev-page').disabled = true;
            document.getElementById('btn-next-page').disabled = true;
            document.getElementById('page-info').textContent = 'Página 1';
            return;
        }

        tbody.innerHTML = logs.map(l => {
            const accion = (l.accion || '').toLowerCase();
            return `
            <tr>
                <td>${l.idLog ?? ''}</td>
                <td>${l.nombreUsuario ?? ''}</td>
                <td>${l.emailUsuario ?? ''}</td>
                <td><span class="status-badge status-${accion}">${l.accion ?? ''}</span></td>
                <td>${l.modulo ?? ''}</td>
                <td>${l.idRegistro ?? '-'}</td>
                <td>${l.detalle ?? '-'}</td>
                <td>${formatearFechaHora(l.fechaRegistro)}</td>
            </tr>`;
        }).join('');

        document.getElementById('btn-prev-page').disabled = auditoriaPagina <= 1;
        document.getElementById('btn-next-page').disabled = logs.length < AUDITORIA_TAMANO;
        document.getElementById('page-info').textContent = `Página ${auditoriaPagina}`;
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="8" style="color:var(--red);text-align:center">${err.message}</td></tr>`;
    }
}

function formatearFechaHora(fechaStr) {
    if (!fechaStr) return '';
    const d = new Date(fechaStr);
    if (isNaN(d.getTime())) return fechaStr;
    return d.toLocaleString('es-DO');
}
