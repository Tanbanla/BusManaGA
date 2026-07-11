document.addEventListener('DOMContentLoaded', function () {
    const cardRoot = document.querySelector('.taxi-card');
    const table = document.querySelector('.js-taxi-table');
    if (!table || !cardRoot) {
        return;
    }

    const searchUrl = cardRoot.dataset.searchUrl;
    const tbody = table.querySelector('.js-taxi-tbody');
    const filters = Array.from(table.querySelectorAll('.js-column-filter'));
    const pageInfo = document.querySelector('.js-taxi-page-info');
    const pagination = document.querySelector('.js-taxi-pagination');
    const searchButton = document.querySelector('.js-btn-search');
    const exportButton = document.querySelector('.js-btn-export');

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

        return [
            String(stt),
            detailId ? ('#' + detailId) : '',
            pickValue(row, ['nvchr_brand', 'taxicompany']) ?? '',
            pickValue(row, ['nvchr_card_number', 'taxicardnumber']) ?? '',
            pickValue(row, ['nvchr_employee_code', 'employeecode', 'chr_employee_id']) ?? '',
            pickValue(row, ['nvchr_employee_name', 'employeename']) ?? '',
            pickValue(row, ['nvchr_section_code', 'sectioncode']) ?? '',
            formatDate(pickValue(row, ['dtm_borrow_time', 'dateborow'])),
            pickValue(row, ['nvchr_purpose', 'purpose']) ?? '',
            formatDate(pickValue(row, ['dtm_return_time', 'datereturn'])),
            pickValue(row, ['nvchr_from_address', 'addressfrom']) ?? '',
            pickValue(row, ['nvchr_to_address', 'addressto']) ?? '',
            pickValue(row, ['int_card_type', 'number']) ?? '',
            formatNumber(pickValue(row, ['int_amount', 'costsum']))
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
            const tds = cells.map((cell) => '<td>' + escapeHtml(cell) + '</td>').join('');
            return '<tr>' + checkbox + tds + '</tr>';
        }).join('');

        tbody.innerHTML = html;
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

    const exportCurrentData = () => {
        const filteredRows = getFilteredRows();
        if (!filteredRows.length) {
            return;
        }

        const headers = ['STT', 'Chi tiết', 'Hãng', 'Số thẻ taxi', 'MNV', 'Họ và tên', 'Phòng ban', 'Ngày mượn', 'Mục đích', 'Ngày trả', 'Địa chỉ đi', 'Địa chỉ đến', 'Số chiều', 'Số tiền'];
        const lines = [headers];

        filteredRows.forEach((entry, index) => {
            lines.push(mapRowToCells(entry.row, index + 1));
        });

        const csvContent = lines
            .map((line) => line.map((value) => '"' + (value ?? '').toString().replaceAll('"', '""') + '"').join(','))
            .join('\n');

        const blob = new Blob(['\ufeff' + csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = 'card-taxi.csv';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    };

    document.querySelector('.js-btn-add')?.addEventListener('click', function () {
        window.AppCommon && AppCommon.info ? AppCommon.info('Chức năng thêm đang được cập nhật.') : alert('Chức năng thêm đang được cập nhật.');
    });

    document.querySelector('.js-btn-update')?.addEventListener('click', function () {
        window.AppCommon && AppCommon.info ? AppCommon.info('Chức năng cập nhật đang được cập nhật.') : alert('Chức năng cập nhật đang được cập nhật.');
    });

    document.querySelector('.js-btn-delete')?.addEventListener('click', function () {
        window.AppCommon && AppCommon.info ? AppCommon.info('Chức năng xóa đang được cập nhật.') : alert('Chức năng xóa đang được cập nhật.');
    });

    searchButton?.addEventListener('click', function () {
        performSearch();
    });

    exportButton?.addEventListener('click', function () {
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