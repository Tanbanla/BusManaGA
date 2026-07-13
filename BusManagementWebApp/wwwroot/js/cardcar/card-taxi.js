document.addEventListener('DOMContentLoaded', function () {
    const cardRoot = document.querySelector('.taxi-card');
    const table = document.querySelector('.js-taxi-table');
    if (!table || !cardRoot) {
        return;
    }

    const searchUrl = cardRoot.dataset.searchUrl;
    const exportUrl = cardRoot.dataset.exportUrl;
    const deleteUrl = cardRoot.dataset.deleteUrl;

    const tbody = table.querySelector('.js-taxi-tbody');
    const filters = Array.from(table.querySelectorAll('.js-column-filter'));
    const pageInfo = document.querySelector('.js-taxi-page-info');
    const pagination = document.querySelector('.js-taxi-pagination');
    const searchButton = document.querySelector('.js-btn-search');

    const deleteModalEl = document.getElementById('deleteConfirmModal');
    const deleteModal = deleteModalEl
        ? new bootstrap.Modal(deleteModalEl)
        : null;

    const pageSize = 8;
    let currentPage = 1;
    let allRows = [];

    const normalize = (value) => (value || '').toString().trim().toLowerCase();
    const toNumber = (value) => {
        if (value === null || value === undefined || value === '') {
            return null;
        }

        const parsed = Number(value);
        return Number.isNaN(parsed) ? null : parsed;
    };

    const pickValue = (row, keys) => {
        if (!row || !keys || !keys.length) {
            return null;
        }

        for (const key of keys) {
            const value = row[key.toLowerCase()];
            if (value !== undefined && value !== null && value !== '') {
                return value;
            }
        }

        return null;
    };

    const formatDate = (value) => {
        if (!value) {
            return '';
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return date.toLocaleDateString('vi-VN');
    };

    const formatNumber = (value) => {
        if (value === null || value === undefined || value === '') {
            return '';
        }

        const parsed = Number(value);
        if (Number.isNaN(parsed)) {
            return value;
        }

        return parsed.toLocaleString('vi-VN');
    };

    const escapeHtml = (value) => {
        const text = (value || '').toString();
        return text
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    };

    const mapRowToCells = (row, stt) => {
        const detailId = pickValue(row, ['id', 'int_card_usage_id', 'int_card_id']) ?? '';
        const isDisabled = !detailId;
        const actionButtons = `
            <div class="d-flex gap-1 justify-content-center">
                <button class="btn btn-sm btn-primary btn-edit-row" 
                        data-id="${escapeHtml(detailId)}"
                        ${isDisabled ? 'disabled' : ''}
                        title="Sửa">
                    <i class="fa-solid fa-pen"></i>
                </button>
                <button class="btn btn-sm btn-danger btn-delete-row" 
                        data-id="${escapeHtml(detailId)}"
                        ${isDisabled ? 'disabled' : ''}
                        title="Xóa">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </div>
        `;
        return [
            String(stt),
            actionButtons,
            pickValue(row, ['nvchr_brand']) ?? '',
            pickValue(row, ['chr_card_code']) ?? '',
            pickValue(row, ['chr_employee_id']) ?? '',
            pickValue(row, ['nvchr_employee_name']) ?? '',
            pickValue(row, ['chr_code_sec']) ?? '',
            formatDate(pickValue(row, ['dtm_borrow_time'])),
            pickValue(row, ['nvchr_note']) ?? '',
            formatDate(pickValue(row, ['dtm_return_time'])),
            pickValue(row, ['nvchr_from_address']) ?? '',
            pickValue(row, ['nvchr_to_address']) ?? '',
            pickValue(row, ['int_card_type']) ?? '',
            formatNumber(pickValue(row, ['int_amount']))
        ];
    };

    const collectSearchPayload = () => {
        return {
            employeeCode: document.getElementById('employeeCode')?.value?.trim() || null,
            employeeName: document.getElementById('employeeName')?.value?.trim() || null,
            sectionCode: document.getElementById('sectionCode')?.value?.trim() || null,
            taxiCardNumber: document.getElementById('taxiCardNumber')?.value?.trim() || null,
            taxiCompany: document.getElementById('taxiCompany')?.value?.trim() || null,
            addressTo: document.getElementById('addressTo')?.value?.trim() || null,
            addressFrom: document.getElementById('addressFrom')?.value?.trim() || null,
            dateBorow: document.getElementById('dateBorow')?.value || null,
            dateReturn: document.getElementById('dateReturn')?.value || null,
            number: toNumber(document.getElementById('number')?.value),
            costSum: document.getElementById('costSum')?.value?.trim() || null,
            pageIndex: 1,
            pageSize: 1000
        };
    };
    const getSelectIds = () => {
        return Array.from(document.querySelectorAll('.js-row-check:checked'))
            .map(x => x.dataset.id)
            .filter(x => x);
    };
    const getFilteredRows = () => {
        const filterValues = filters.map((filter) => normalize(filter.value));
        const rowsWithCells = allRows.map((row, index) => ({
            row,
            cells: mapRowToCells(row, index + 1)
        }));

        return rowsWithCells.filter((entry) => {
            const cells = entry.cells;

            for (let i = 0; i < filterValues.length; i++) {
                const keyword = filterValues[i];
                if (!keyword) {
                    continue;
                }

                const cellText = normalize(cells[i]);
                if (!cellText.includes(keyword)) {
                    return false;
                }
            }

            return true;
        });
    };

    const renderBody = (pageRows, startIndex) => {
        if (!tbody) {
            return;
        }

        if (!pageRows.length) {
            tbody.innerHTML = '<tr><td colspan="15" class="text-center text-muted py-3">Không có dữ liệu.</td></tr>';
            return;
        }

        const html = pageRows.map((entry, index) => {
            const cells = mapRowToCells(entry.row, startIndex + index);
            const id = pickValue(entry.row, ['id', 'int_card_usage_id', 'int_card_id']) ?? '';
            const checkbox = '<td class="text-center"><input type="checkbox" class="js-row-check" data-id="' + escapeHtml(id) + '" /></td>';
            const tds = cells.map((cell, cellIndex) => {

                if (cellIndex === 1) {
                    return '<td class="text-center">' + cell + '</td>';
                }
                return '<td>' + escapeHtml(cell) + '</td>';
            }).join('');
            return '<tr data-row-id="' + escapeHtml(id) + '">' + checkbox + tds + '</tr>';
        }).join('');

        tbody.innerHTML = html;

        attachRowButtonEvents();
    };
    // Hàm attach event listeners cho các button trong row
    const attachRowButtonEvents = () => {
        // Event cho button Sửa
        tbody.querySelectorAll('.btn-edit-row').forEach(btn => {
            btn.removeEventListener('click', handleEditClick);
            btn.addEventListener('click', handleEditClick);
        });

        // Event cho button Xóa
        tbody.querySelectorAll('.btn-delete-row').forEach(btn => {
            btn.removeEventListener('click', handleDeleteClick);
            btn.addEventListener('click', handleDeleteClick);
        });
    };

    const handleEditClick = function (e) {


    };

    const handleDeleteClick = function (e) {

        
    };

    const renderPagination = (totalPages) => {
        if (!pagination) {
            return;
        }

        pagination.innerHTML = '';

        const addItem = (label, page, disabled, active) => {
            const li = document.createElement('li');
            li.className = 'page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '');

            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'page-link';
            btn.textContent = label;
            btn.disabled = disabled;

            btn.addEventListener('click', function () {
                if (disabled || page === currentPage) {
                    return;
                }

                currentPage = page;
                render();
            });

            li.appendChild(btn);
            pagination.appendChild(li);
        };

        addItem('«', currentPage - 1, currentPage <= 1, false);
        for (let i = 1; i <= totalPages; i++) {
            addItem(String(i), i, false, i === currentPage);
        }
        addItem('»', currentPage + 1, currentPage >= totalPages, false);
    };

    const render = () => {
        const filteredRows = getFilteredRows();
        const totalCount = filteredRows.length;
        const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

        if (currentPage > totalPages) {
            currentPage = totalPages;
        }

        const startIndex = totalCount === 0 ? 0 : ((currentPage - 1) * pageSize + 1);
        const endIndex = totalCount === 0 ? 0 : Math.min(currentPage * pageSize, totalCount);
        const pageRows = filteredRows.slice((currentPage - 1) * pageSize, currentPage * pageSize);
        renderBody(pageRows, startIndex);

        if (pageInfo) {
            pageInfo.textContent = 'Hiển thị ' + startIndex + ' - ' + endIndex + ' / ' + totalCount;
        }

        renderPagination(totalPages);
    };

    const performSearch = async () => {
        if (!searchUrl) {
            return;
        }

        try {
            const payload = collectSearchPayload();
            const response = await fetch(searchUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Không thể tải dữ liệu.');
            }

            const data = await response.json();
            allRows = Array.isArray(data)
                ? data.map((item) => {
                    const normalized = {};
                    Object.keys(item || {}).forEach((key) => {
                        normalized[key.toLowerCase()] = item[key];
                    });
                    return normalized;
                })
                : [];

            currentPage = 1;
            render();
        }
        catch (error) {
            console.error(error);
            if (tbody) {
                tbody.innerHTML = '<tr><td colspan="15" class="text-center text-danger py-3">Lỗi tải dữ liệu: ' + escapeHtml(error?.message || 'Unknown error') + '</td></tr>';
            }
            if (pageInfo) {
                pageInfo.textContent = 'Hiển thị 0 - 0 / 0';
            }
            if (pagination) {
                pagination.innerHTML = '';
            }
        }
    };

    const exportCurrentData = async () => {
        if (!exportUrl) {
            return;
        }
        try {
            const payload = collectSearchPayload();
            const response = await fetch(exportUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Không thể tải dữ liệu.');
            }

            const blob = await response.blob();

            let fileName = `FileTaxiCard_${new Date().toISOString()}.xlsx`;

            const cd = response.headers.get('content-disposition');
            if (cd) {
                const m = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(cd);
                if (m && m[1]) fileName = m[1].replace(/['"]/g, '').trim();
            }

            // Tạo link download
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');

            a.href = url;
            a.download = fileName;
            a.setAttribute('data-no-loader', 'true');
            document.body.appendChild(a);

            a.click();

            a.remove();
            window.URL.revokeObjectURL(url);

            AppCommon.success('Xuất File thành công!');

        } catch (err) {
            console.log('Bug: ' + err);
            AppCommon.error('Bug: ' + err?.message || 'Unknown error');
            return;
        }
    };

    document.querySelector('.js-btn-add')?.addEventListener('click', function () {
        window.AppCommon && AppCommon.info ? AppCommon.info('Chức năng thêm đang được cập nhật.') : alert('Chức năng thêm đang được cập nhật.');
    });

    document.querySelector('.js-btn-update')?.addEventListener('click', function () {
        window.AppCommon && AppCommon.info ? AppCommon.info('Chức năng cập nhật đang được cập nhật.') : alert('Chức năng cập nhật đang được cập nhật.');
    });
    // sự kiện xóa
    document.querySelector('.js-btn-delete')?.addEventListener('click', function () {
        const ids = getSelectIds();
        if (!ids.length) {
            AppCommon.error('Vui lòng chọn ít nhất một bản ghi để xóa.');

            return;
        }

        deleteModal?.show();

    });

    const deleteDataAsync = async () => {
        const ids = getSelectIds();

        if (!ids.length) {
            AppCommon.error('Vui lòng chọn ít nhất một bản ghi để xóa.');

            return;
        }
        if (!deleteUrl) {
            AppCommon.error('Không nhận được API');
            return;
        }

        try {
            const response = await fetch(deleteUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(ids)
            });

            if (!response.ok) {
                AppCommon.error('Bug: ' + response.message);
                console.log('Bug: ' + response.message);
                return;
            }

            deleteModal?.hide();
            AppCommon.success('Xóa dữ liệu thành công.');
            performSearch();


        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            console.log('Bug: ' + err.message);
            return;
        }
    };

    document.querySelector('.js-confirm-delete')?.addEventListener('click', function () {
        deleteDataAsync();
    });


    searchButton?.addEventListener('click', function () {
        performSearch();
    });

    document.getElementById('DownloadExcel')?.addEventListener('click', function () {
        exportCurrentData();
    });

    const chkAll = document.getElementById('chkAllRows') || document.querySelector('.js-chk-all');

    function updateChkAllState() {
        if (!chkAll || !tbody) return;
        const visibleChecks = Array.from(tbody.querySelectorAll('.js-row-check'));
        if (!visibleChecks.length) {
            chkAll.checked = false;
            chkAll.indeterminate = false;
            return;
        }

        const checkedCount = visibleChecks.filter(c => c.checked).length;
        chkAll.checked = checkedCount === visibleChecks.length;
        chkAll.indeterminate = checkedCount > 0 && checkedCount < visibleChecks.length;
    }

    if (chkAll) {
        chkAll.addEventListener('change', function () {
            const checked = chkAll.checked;
            tbody.querySelectorAll('.js-row-check').forEach(function (c) { c.checked = checked; });
        });
    }

    // Delegate change events from checkboxes in tbody to update header checkbox state
    tbody.addEventListener('change', function (e) {
        if (e.target && e.target.matches && e.target.matches('.js-row-check')) {
            updateChkAllState();
        }
    });

    filters.forEach((filter) => {
        filter.addEventListener('input', function () {
            currentPage = 1;
            render();
        });
    });

    performSearch();
});