(function () {
    'use strict';

    const cardRoot = document.querySelector('.using-card');
    const apiBusNumber = cardRoot.dataset.getbusUrl;
    const apiBusStop = cardRoot.dataset.getbusstopUrl;
    const apiUpdateCard = cardRoot.dataset.getupdatecardUrl;

    function showWarning(message) {
        if (window.AppCommon && AppCommon.warning) AppCommon.warning(message);
        else alert(message);
    }
    function showSuccess(message) {
        if (window.AppCommon && AppCommon.success) AppCommon.success(message);
        else alert(message);
    }
    function showError(message) {
        if (window.AppCommon && AppCommon.error) AppCommon.error(message);
        else alert(message);
    }
    function showLoading() { if (window.showLoading) window.showLoading(); }
    function hideLoading() { if (window.hideLoading) window.hideLoading(); }
    async function confirmAction(title, message) {
        if (window.AppCommon && typeof AppCommon.confirm === 'function')
            return await AppCommon.confirm({ title, message });
        return confirm(message);
    }

    function getAntiForgeryToken(form) {
        const input = form.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    const chkAll = document.getElementById('chkAll');
    const btnBulkDelete = document.getElementById('btnBulkDelete');
    const cardChecks = Array.from(document.querySelectorAll('.js-card-check'));

    function updateBulkDeleteState() {
        if (!btnBulkDelete) return;
        const checkedCount = document.querySelectorAll('.js-card-check:checked').length;
        const hasSelection = checkedCount > 0;
        btnBulkDelete.disabled = !hasSelection;
        btnBulkDelete.classList.toggle('d-none', !hasSelection);
        if (chkAll) {
            const total = cardChecks.length;
            chkAll.checked = total > 0 && checkedCount === total;
            chkAll.indeterminate = checkedCount > 0 && checkedCount < total;
        }
    }

    if (chkAll) {
        chkAll.addEventListener('change', function () {
            cardChecks.forEach(cb => cb.checked = chkAll.checked);
            updateBulkDeleteState();
        });
    }
    cardChecks.forEach(cb => cb.addEventListener('change', updateBulkDeleteState));
    updateBulkDeleteState();

    // Xóa nhiều (Bulk Delete)
    if (btnBulkDelete) {
        btnBulkDelete.addEventListener('click', async function () {
            const form = document.getElementById('bulkDeleteForm');
            if (!form) return;

            const checked = document.querySelectorAll('.js-card-check:checked');
            if (!checked.length) {
                showWarning('Vui lòng chọn ít nhất một thẻ để xóa.');
                return;
            }

            const title = this.dataset.confirmTitle || 'Xác nhận xóa';
            const message = this.dataset.confirmMessage || 'Bạn có chắc chắn muốn xóa các thẻ đã chọn?';
            const ok = await confirmAction(title, message);
            if (!ok) return;

            const ids = Array.from(checked).map(cb => parseInt(cb.value, 10));
            const token = getAntiForgeryToken(form);

            showLoading();
            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token,
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify(ids)
                });

                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(errorText || 'Xóa thất bại.');
                }

                const result = await response.json();
                if (result.success === false) {
                    throw new Error(result.message || 'Xóa thất bại.');
                }

                showSuccess(result.message || 'Xóa thành công.');
                setTimeout(() => location.reload(), 500);
            } catch (error) {
                showError(error.message || 'Đã xảy ra lỗi khi xóa.');
            } finally {
                hideLoading();
            }
        });
    }

    // Xóa đơn (dùng fetch với JSON)
    document.addEventListener('click', async function (e) {
        const btn = e.target.closest('.js-action-button');
        if (!btn) return;

        const formId = btn.dataset.formId;
        if (!formId) return;
        const form = document.getElementById(formId);
        if (!form) return;

        const title = btn.dataset.confirmTitle || 'Xác nhận';
        const message = btn.dataset.confirmMessage || 'Bạn có chắc chắn?';
        const ok = await confirmAction(title, message);
        if (!ok) return;

        // Lấy cardId từ input hidden trong form
        const cardIdInput = form.querySelector('input[name="id"]');
        if (!cardIdInput || !cardIdInput.value) {
            showError('Không tìm thấy ID thẻ.');
            return;
        }
        const cardId = parseInt(cardIdInput.value, 10);
        const token = getAntiForgeryToken(form);

        showLoading();
        try {
            const response = await fetch(form.action, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify(cardId)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Xóa thất bại.');
            }

            const result = await response.json();
            if (result.success === false) {
                throw new Error(result.message || 'Xóa thất bại.');
            }

            showSuccess(result.message || 'Xóa thành công.');
            setTimeout(() => location.reload(), 500);
        } catch (error) {
            showError(error.message || 'Đã xảy ra lỗi khi xóa.');
        } finally {
            hideLoading();
        }
    });

    // Nút Làm mới
    const resetBtn = document.querySelector('.js-btn-reset');
    if (resetBtn) {
        resetBtn.addEventListener('click', function () {
            const form = document.querySelector('form.js-search-form');
            if (!form) return;
            const inputs = form.querySelectorAll('input[type="text"], select');
            inputs.forEach(function (input) {
                if (input.tagName === 'SELECT') input.selectedIndex = 0;
                else input.value = '';
            });
            form.submit();
        });
    }

    // Modal – load danh sách xe và điểm dừng
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('show.bs.modal', async function () {
            const busSelect = modal.querySelector('.bus-number-select');
            const stopSelect = modal.querySelector('.stop-point-select');
            const selectedBusId = busSelect.dataset.selected;
            const selectedStopId = stopSelect.dataset.selected;

            try {
                const busRep = await fetch(apiBusNumber);
                const buses = await busRep.json();
                busSelect.innerHTML = '<option value="">-- Chọn xe --</option>';
                buses.forEach(bus => {
                    busSelect.insertAdjacentHTML(
                        'beforeend',
                        `<option value="${bus.ID}" ${bus.ID == selectedBusId ? 'selected' : ''}>${bus.CHR_BUS_CODE}</option>`
                    );
                });

                const stopRep = await fetch(apiBusStop);
                const stops = await stopRep.json();
                stopSelect.innerHTML = '<option value="">-- Chọn điểm đón/trả --</option>';
                stops.forEach(stop => {
                    stopSelect.insertAdjacentHTML(
                        'beforeend',
                        `<option value="${stop.ID}" ${stop.ID == selectedStopId ? 'selected' : ''}>${stop.NVCHR_STOP_NAME}</option>`
                    );
                });
            } catch (error) {
                showError('Không thể tải danh sách xe / điểm dừng.');
            }
        });
    });

    // Cập nhật thẻ trong modal
    document.querySelectorAll('form.js-post-form').forEach(function (form) {
        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const busSelect = form.querySelector('.bus-number-select');
            const stopSelect = form.querySelector('.stop-point-select');
            const idInput = form.querySelector('input[name="id"]');

            const id = idInput?.value;
            const busId = busSelect.value;
            const stopId = stopSelect.value;
            const busCode = busSelect.options[busSelect.selectedIndex]?.text || '';

            if (!busId) {
                showWarning('Vui lòng chọn xe.');
                return;
            }
            if (!stopId) {
                showWarning('Vui lòng chọn điểm đón/trả.');
                return;
            }

            try {
                const formData = formUpdateCard(id, busId, stopId, busCode);
                const response = await fetch(apiUpdateCard, {
                    method: 'POST',
                    body: JSON.stringify(formData),
                    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) {
                    const errorText = await response.text();
                    showError(errorText || 'Cập nhật thất bại.');
                    return;
                }

                const result = await response.json();
                if (result.success === false) {
                    showError(result.message || 'Cập nhật thất bại.');
                    return;
                }

                showSuccess(result.message || 'Cập nhật thành công.');
                const modalEl = form.closest('.modal');
                const modalInstance = bootstrap.Modal.getInstance(modalEl);
                if (modalInstance) modalInstance.hide();

                setTimeout(() => location.reload(), 500);
            } catch (error) {
                showError(error.message || 'Đã xảy ra lỗi khi cập nhật.');
                return;
            } finally {
                hideLoading();
            }
        });
    });

    const formUpdateCard = (id, busID, stopID, busCode) => {
        return {
            ID: Number(id),

            CHR_EMPLOYEE_ID: null,
            CHR_CARD_CODE: null,

            INT_CARD_TYPE: null,
            NVCHR_CARD_TYPE: null,

            INT_CARD_SCOPE: null,
            NVCHR_CARD_SCOPE: null,

            CHR_BUS_CODE: busCode,

            INT_BUS_ID: Number(busID),
            INT_BUS_STOP_ID: Number(stopID),

            DTM_ISSUE_DATE: null,
            DTM_EXPRIRATION_DATE: null,

            BIT_IS_ACTIVE: null
        };
    };

})();