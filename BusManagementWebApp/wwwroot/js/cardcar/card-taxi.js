document.addEventListener('DOMContentLoaded', function () {
    const cardRoot = document.querySelector('.taxi-card');
    const table = document.querySelector('.js-taxi-table');
    if (!table || !cardRoot) {
        return;
    }
    //url
    const searchUrl = cardRoot.dataset.searchUrl;
    const exportUrl = cardRoot.dataset.exportUrl;
    const deleteUrl = cardRoot.dataset.deleteUrl;
    const getEmployeeUrl = cardRoot.dataset.getemployeeUrl;
    const getListCardUrl = cardRoot.dataset.listcardUrl;
    const addCardUrl = cardRoot.dataset.saveborrowUrl;
    const listBorrowingUrl = cardRoot.dataset.listborrowingUrl;
    const addUsageUrl = cardRoot.dataset.addcardusageUrl;
    const updateUsageUrl = cardRoot.dataset.updatecardusageUrl;
    const detailUrl = cardRoot.dataset.detaicardusageUrl;
    // thông tin section 
    const sectionMap = {};

    (sections || []).forEach(item => {
        sectionMap[item.CHR_CODE] = item.CHR_NAME;
    });
    //
    const tbody = table.querySelector('.js-taxi-tbody');
    const filters = Array.from(table.querySelectorAll('.js-column-filter'));
    const pageInfo = document.querySelector('.js-taxi-page-info');
    const pagination = document.querySelector('.js-taxi-pagination');
    const searchButton = document.querySelector('.js-btn-search');

    const deleteModalEl = document.getElementById('deleteConfirmModal');
    const deleteModal = deleteModalEl
        ? new bootstrap.Modal(deleteModalEl)
        : null;

    const taxiCardModalEl = document.getElementById('taxiCardModal');
    const taxiCardModal = taxiCardModalEl ? new bootstrap.Modal(taxiCardModalEl) : null;

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
            //pickValue(row, ['chr_code_sec']) ?? '',
            sectionMap[
                pickValue(row, ['chr_code_sec'])
            ] ?? pickValue(row, ['chr_code_sec']) ?? '',
            formatDate(pickValue(row, ['dtm_borrow_time'])),
            pickValue(row, ['nvchr_reason']) ?? '',
            formatDate(pickValue(row, ['dtm_return_time'])),
            pickValue(row, ['nvchr_from_address']) ?? '',
            pickValue(row, ['nvchr_to_address']) ?? '',
            pickValue(row, ['int_number_move']) ?? '',
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

    const handleEditClick = async function (e) {

        const btn = e.currentTarget;
        const id = btn.dataset.id;

        if (!id) {
            return;
        }

        try {
            if (!detailUrl) {
                AppCommon.error("Không tìm thấy URL");
                return;
            }
             
            const response = await fetch(
                `${detailUrl}?id=${encodeURIComponent(id)}`
            );

            if (!response.ok) {
                AppCommon.error('Bug: ' + response.statusText);
                return;
            }

            const data = await response.json();

            if (!data) {
                AppCommon.error("Không tìm thấy dữ liệu");
                return;
            }

            fillDetailModal(data);

            const modal = document.getElementById('taxiUsageDetailModal');

            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();

        } catch (error) {
            AppCommon.error("Bug: " + error.message);
            return;
        }
    };
    const fillDetailModal = (data) => {

        document.getElementById("detailId").value =
            data.ID || 0;

        document.getElementById("detailEmployeeId").value =
            data.CHR_EMPLOYEE_ID || '';

        document.getElementById("detailEmployeeName").value =
            data.NVCHR_EMPLOYEE_NAME || '';

        document.getElementById("detailReason").value =
            data.NVCHR_REASON || '';

        document.getElementById("detailFromAddress").value =
            data.NVCHR_FROM_ADDRESS || '';

        document.getElementById("detailToAddress").value =
            data.NVCHR_TO_ADDRESS || '';

        document.getElementById("detailAmount").value =
            data.INT_AMOUNT || '';

        document.getElementById("detailNote").value =
            data.NVCHR_NOTE || '';

        document.getElementById("detailSectionCost").value =
            data.CHR_SEC_COST || '';

        document.getElementById("detailTranCode").value =
            data.CHR_TRAN_CODE || '';

        document.getElementById("detailNumberMove").value =
            data.INT_NUMBER_MOVE || 1;

        document.getElementById("detailBorrowId").value = data.INT_BORROW_ID || 0;
        document.getElementById("detailCreate").value = data.CHR_USER_CREATE || 'vietdo bad';
        document.getElementById("detailCreateTime").value = data.DTM_CREATE || null;

        if (data.DTM_USE_DATE) {

            const date = new Date(data.DTM_USE_DATE);

            date.setMinutes(
                date.getMinutes() - date.getTimezoneOffset()
            );

            document.getElementById("detailUseDate").value =
                date.toISOString().slice(0, 16);
        }
    };

    const handleDeleteClick = async function (e) {

        const id = this.dataset.id;
        if (!id) {
            AppCommon.error('Không tìm thấy ID để xóa.');
            return;
        }
        const modal = document.getElementById('deleteConfirmModal');

        if (!modal) {
            AppCommon.error('Không tìm thấy popup xác nhận xóa.');
            return;
        }

        const confirmBtn = modal.querySelector('.js-confirm-delete');
        if (confirmBtn) {
            const newConfirmBtn = confirmBtn.cloneNode(true);
            confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

            newConfirmBtn.addEventListener('click', function () {
                performDelete([id]);

                const instance = bootstrap.Modal.getInstance(modal);
                instance?.hide();
            });
        }
        const bsModal = new bootstrap.Modal(modal);
        bsModal.show();
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
                AppCommon.error(errorText || 'Không thể tải dữ liệu.');
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
        taxiCardModal?.show();
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
    document.querySelector('.js-confirm-delete')?.addEventListener('click', function () {
        deleteDataAsync();
    });

    searchButton?.addEventListener('click', function () {
        performSearch();
    });

    document.getElementById('DownloadExcel')?.addEventListener('click', function () {
        exportCurrentData();
    });
    document.querySelectorAll('#taxiCardTabs button[data-bs-toggle="tab"]')
        .forEach(tab => {
            tab.addEventListener('shown.bs.tab', function (e) {

                const target = e.target.getAttribute('data-bs-target');

                if (target === '#tabBorrow') {
                    document.getElementById('saveBorrow').style.display = '';
                    document.getElementById('saveUsage').style.display = 'none';
                }
                else if (target === '#tabUsage') {
                    document.getElementById('saveBorrow').style.display = 'none';
                    document.getElementById('saveUsage').style.display = '';
                }
            });
        });
    // khi enter
    document.getElementById('borrowEmployeeId')?.addEventListener('keydown', async function (e) {
        const employeeCode = document.getElementById('borrowEmployeeId')?.value?.trim();
        if (e.key === 'Enter') {
            const employeeData = await loadEmployeeInfo(employeeCode);
            if (employeeData) {
                document.getElementById('borrowEmployeeName').value = employeeData?.NVCHR_EMPLOYEE_NAME || '';
                document.getElementById('borrowSection').value =
                    sectionMap[employeeData?.CHR_CODE_SEC] ||
                    employeeData?.CHR_CODE_SEC ||
                    '';
            }
        }
    });
    document.getElementById('usageEmployeeId')?.addEventListener('keydown', async function (e) {
        const employeeCode = document.getElementById('usageEmployeeId')?.value?.trim();
        if (e.key === 'Enter') {
            const employeeData = await loadEmployeeInfo(employeeCode);
            if (employeeData) {
                document.getElementById('usageEmployeeName').value = employeeData?.NVCHR_EMPLOYEE_NAME || '';
            }
        }
    });
    // khi blur 
    document.getElementById('borrowEmployeeId')?.addEventListener('blur', async function () {
        const employeeCode = this.value?.trim();

        if (!employeeCode) return;

        const employeeData = await loadEmployeeInfo(employeeCode);

        if (employeeData) {
            document.getElementById('borrowEmployeeName').value =
                employeeData.NVCHR_EMPLOYEE_NAME || '';

            document.getElementById('borrowSection').value =
                employeeData.CHR_CODE_SEC || '';
        }
    });

    document.getElementById('usageEmployeeId')?.addEventListener('blur', async function () {
        const employeeCode = this.value?.trim();

        if (!employeeCode) return;

        const employeeData = await loadEmployeeInfo(employeeCode);

        if (employeeData) {
            document.getElementById('usageEmployeeName').value =
                employeeData.NVCHR_EMPLOYEE_NAME || '';
        }
    });

    document.getElementById('borrowCardId')?.addEventListener('mousedown', function () {
        loadListCardBorrow();
    });

    document.getElementById('usageCardId')?.addEventListener('mousedown', function () {
        loadListCardBorrowing();
    });

    document.getElementById('closeModalAdd')?.addEventListener('click', function () {
        resetModal();
    });
    document.getElementById('saveUpdateUsage')?.addEventListener('click', function () {
        updateCardUsage();
    });

    document.getElementById('saveBorrow')?.addEventListener('click', function () {
        addCardTaxiBorrow();
    });

    document.getElementById('saveUsage')?.addEventListener('click', function () {
        addCardUsage();
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

    const performDelete = async (ids) => {
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
    // POST Add Card Taxi Borrow
    const addCardTaxiBorrow = async () => {
        if (!addCardUrl) {  
            AppCommon.error('Không nhận được API');
            return;
        }
        var borrowEmployeeId = document.getElementById('borrowEmployeeId')?.value?.trim();
        var borrowCardId = document.getElementById('borrowCardId')?.value?.trim();
        var borrowCardCode = document.getElementById('borrowCardId')?.selectedOptions?.[0]?.text;
        var borrowTime = document.getElementById('borrowTime')?.value?.trim();
        var borrowReturnPlan = document.getElementById('returnTime')?.value;

        // Validate required fields
        if (!borrowEmployeeId || !borrowCardId || !borrowTime || !borrowReturnPlan) {
            AppCommon.error('Vui lòng điền đầy đủ thông tin.');
            return;
        }

        var payload = {
            ID: 0,
            INT_CARD_ID: borrowCardId,
            CHR_CARD_CODE: borrowCardCode,
            CHR_EMPLOYEE_ID: borrowEmployeeId,
            DTM_BORROW_TIME: borrowTime,
            DTM_RETURN_TIME: borrowReturnPlan,
            BIT_END: false,
        }

        try {
            const rep = await fetch(addCardUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!rep.ok) {
                AppCommon.error('Bug: ' + rep.statusText);
                return;
            }

            const data = await rep.json();
            AppCommon.success('Thêm thẻ taxi thành công.');

            taxiCardModal?.hide();
            performSearch();
            resetModal();

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }

    };
    // Get infor Employee 
    const loadEmployeeInfo = async (employeeCode) => {
        if (!getEmployeeUrl) {
            AppCommon.error('Không nhận được API');
            return null;
        }

        if (!employeeCode) {
            AppCommon.error('Vui lòng nhập mã nhân viên.');
            return null;
        }

        try {
            const rep = await fetch(getEmployeeUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(employeeCode)
            });
            if (!rep.ok) {
                AppCommon.error('Bug: ' + rep.statusText);
                return null;
            }
            const data = await rep.json();

            return data;

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return null;
        }
    };
    // Lấy dữ liệu thể taxi
    const loadListCardBorrow = async () => {
        if (!getListCardUrl) {
            AppCommon.error('Không nhận được API');
            return;
        }
        const borrowCardSelect = document.getElementById('borrowCardId');

        try {
            const rep = await fetch(getListCardUrl, {
                method: 'GET'
            });
            if (!rep.ok) {
                AppCommon.error('Bug: ' + rep.statusText);
                return;
            }
            const data = await rep.json();

            borrowCardSelect.innerHTML =
            '<option value="">-- Chọn thẻ --</option>';
            data.forEach(item => {
                borrowCardSelect.insertAdjacentHTML(
                    'beforeend',
                    `<option value="${item.ID}">
                    ${item.CHR_CARD_CODE}
                    </option>`
                );
            });
            cardLoaded = true;
            

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }
    };
    // lấy dữ liệu thể taxi đang cho mượn 
    const loadListCardBorrowing = async () => {
        if (!listBorrowingUrl) {
            AppCommon.error('Không nhận được API');
            return;
        }
        const borrowingCardSelect = document.getElementById('usageCardId');

        try {
            const rep = await fetch(listBorrowingUrl, {
                method: 'GET'
            });
            if (!rep.ok) {
                AppCommon.error('Bug: ' + rep.statusText);
                return;
            }
            const data = await rep.json();

            borrowingCardSelect.innerHTML =
            '<option value="">-- Chọn thẻ --</option>';
            data.forEach(item => {
                borrowingCardSelect.insertAdjacentHTML(
                    'beforeend',
                    `<option value="${item.ID}">
                    ${item.CHR_CARD_CODE}
                    </option>`
                );
            });

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }
    };
    // Lưu thông tin sử dụng thẻ taxi
    const addCardUsage = async () => {
        if (!addUsageUrl) {
            AppCommon.error('Không nhận được API');
            return;
        }
        const payInsert = DataInsert();
        if(!payInsert) {
            AppCommon.error('Vui lòng nhập đầy đủ thông tin.');
            return;
        }

        try {
            const rep = await fetch(addUsageUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payInsert)
            });
            if(!rep.ok) {
                AppCommon.error('Bug: ' + rep.statusText);
                return;
            }
            const data = await rep.json();

            AppCommon.success('Thêm thông tin sử dụng thẻ taxi thành công.');
            performSearch();
            resetModal();

            return;

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }
    };

    // Lưu thông tin chỉnh sửa sử dụng thẻ taxi
    const updateCardUsage = async () => {
        if (!updateUsageUrl) {
            AppCommon.error('Không nhận được API');
            return;
        }
        try {
            const payUpdate = DataUpDateUsage();

            const rep = await fetch(updateUsageUrl, {
                method: 'POST',
                headers: {
                   'Content-Type': 'application/json'
                },
                body: JSON.stringify(payUpdate)
            });

            if(!rep.ok) {
                AppCommon.error('Bug: ' + rep.statusText);
                return;
            }
            const data = await rep.json();

            AppCommon.success('Cập nhật thông tin sử dụng thẻ taxi thành công.');
            performSearch();

            return;

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }
    };

    const DataInsert = () => {
        return {
            INT_BORROW_ID: Number(document.getElementById('usageCardId')?.value) || 0,

            CHR_EMPLOYEE_ID: document.getElementById('usageEmployeeId')?.value?.trim() || '',

            NVCHR_EMPLOYEE_NAME: document.getElementById('usageEmployeeName')?.value?.trim() || '',

            DTM_USE_DATE: document.getElementById('useDate')?.value || null,

            NVCHR_REASON: document.getElementById('usageNote')?.value?.trim() || '',

            NVCHR_FROM_ADDRESS: document.getElementById('fromAddress')?.value?.trim() || '',

            NVCHR_TO_ADDRESS: document.getElementById('toAddress')?.value?.trim() || '',

            INT_AMOUNT: Number(document.getElementById('amount')?.value) || 0,

            INT_NUMBER_MOVE: Number(document.getElementById('numberMove')?.value) || 1,

            NVCHR_NOTE: '',

            CHR_SEC_COST: document.getElementById('sectionCodeUsage')?.value?.trim() || null,

            CHR_TRAN_CODE: '',

            BIT_DELETE: false
        };
    };

    const DataUpDateUsage = () => {
        return {
            ID: Number(document.getElementById('detailId')?.value) || 0,
            INT_BORROW_ID: Number(document.getElementById('detailBorrowId')?.value) || 0,
            CHR_EMPLOYEE_ID: document.getElementById('detailEmployeeId')?.value?.trim() || '',
            NVCHR_EMPLOYEE_NAME: document.getElementById('detailEmployeeName')?.value?.trim() || '',
            //CHR_USER_CREATE: document.getElementById('detailCreate')?.value?.trim() || '',
            //DTM_CREATE: document.getElementById('detailCreateTime')?.value || null,
            DTM_USE_DATE: document.getElementById('detailUseDate')?.value || null,
            CHR_SEC_COST: document.getElementById('detailSectionCost')?.value?.trim() || '',
            NVCHR_REASON: document.getElementById('detailReason')?.value?.trim() || '',
            NVCHR_FROM_ADDRESS: document.getElementById('detailFromAddress')?.value?.trim() || '',
            NVCHR_TO_ADDRESS: document.getElementById('detailToAddress')?.value?.trim() || '',
            INT_NUMBER_MOVE: Number(document.getElementById('detailNumberMove')?.value) || 1,
            INT_AMOUNT: Number(document.getElementById('detailAmount')?.value) || 0,
            CHR_TRAN_CODE: document.getElementById('detailTranCode')?.value?.trim() || '',
            NVCHR_NOTE: document.getElementById('detailNote')?.value?.trim() || ''
        };
    };

    // Reset modal khi đóng
    const resetModal = () => {
        // Tab mượn thẻ
        $("#borrowCardId").val("");
        $("#borrowEmployeeId").val("");
        $("#borrowEmployeeName").val("");
        $("#borrowSection").val("");
        $("#borrowTime").val("");
        $("#returnTime").val("");

        // Tab sử dụng thẻ
        $("#usageCardId").val("");
        $("#usageEmployeeId").val("");
        $("#usageEmployeeName").val("");
        $("#usageNote").val("");
        $("#fromAddress").val("");
        $("#toAddress").val("");
        $("#useDate").val("");
        $("#numberMove").val("1");
        $("#amount").val("");
        $("#sectionCodeUsage").val("");

        // Quay về tab đầu tiên
        $('#taxiCardTabs .nav-link:first').tab('show');

        // Hiển thị đúng nút theo tab mặc định
        $("#saveBorrow").show();
        $("#saveUsage").hide();
    };

    performSearch();
});