(function () {
    'use strict';

    // Các hàm hỗ trợ
    function showWarning(message) {
        if (window.AppCommon && AppCommon.warning) AppCommon.warning(message);
        else alert(message);
    }
    function showLoading() { if (window.showLoading) window.showLoading(); }
    function hideLoading() { if (window.hideLoading) window.hideLoading(); }
    async function confirmAction(title, message) {
        if (window.AppCommon && typeof AppCommon.confirm === 'function')
            return await AppCommon.confirm({ title, message });
        return confirm(message);
    }
    function ensureAntiForgery(form) {
        if (!form) return;
        if (form.querySelector('input[name="__RequestVerificationToken"]')) return;
        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!token) return;
        var clone = document.createElement('input');
        clone.type = 'hidden';
        clone.name = '__RequestVerificationToken';
        clone.value = token.value;
        form.appendChild(clone);
    }

    // Chọn tất cả
    var chkAll = document.getElementById('chkAll');
    var btnBulkDelete = document.getElementById('btnBulkDelete');
    var cardChecks = Array.from(document.querySelectorAll('.js-card-check'));

    function updateBulkDeleteState() {
        if (!btnBulkDelete) return;
        var checkedCount = document.querySelectorAll('.js-card-check:checked').length;
        var hasSelection = checkedCount > 0;
        btnBulkDelete.disabled = !hasSelection;
        btnBulkDelete.classList.toggle('d-none', !hasSelection);
        if (chkAll) {
            var total = cardChecks.length;
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

    // Xóa nhiều
    btnBulkDelete.addEventListener('click', async function (e) {
        var form = document.getElementById('bulkDeleteForm');
        if (!form) return;
        var checked = document.querySelectorAll('.js-card-check:checked');
        if (!checked.length) {
            showWarning('Vui lòng chọn ít nhất một thẻ để xóa.');
            return;
        }
        var title = this.dataset.confirmTitle || 'Xác nhận xóa';
        var message = this.dataset.confirmMessage || 'Bạn có chắc chắn muốn xóa các thẻ đã chọn?';
        var ok = await confirmAction(title, message);
        if (!ok) return;
        ensureAntiForgery(form);
        showLoading();
        form.submit();
    });

    // Xóa đơn (dùng js-action-button)
    document.addEventListener('click', async function (e) {
        var btn = e.target.closest('.js-action-button');
        if (!btn) return;
        var formId = btn.dataset.formId;
        if (!formId) return;
        var form = document.getElementById(formId);
        if (!form) return;
        // Nếu có data-require-selection, kiểm tra (không áp dụng cho xóa đơn)
        var title = btn.dataset.confirmTitle || 'Xác nhận';
        var message = btn.dataset.confirmMessage || 'Bạn có chắc chắn?';
        var ok = await confirmAction(title, message);
        if (!ok) return;
        ensureAntiForgery(form);
        showLoading();
        form.submit();
    });

    // Nút Làm mới
    var resetBtn = document.querySelector('.js-btn-reset');
    if (resetBtn) {
        resetBtn.addEventListener('click', function () {
            var form = document.querySelector('form.js-search-form');
            if (!form) return;
            var inputs = form.querySelectorAll('input[type="text"], select');
            inputs.forEach(function (input) {
                if (input.tagName === 'SELECT') input.selectedIndex = 0;
                else input.value = '';
            });
            form.submit();
        });
    }

    // Xử lý form submit (cho modal chỉnh sửa)
    document.querySelectorAll('form.js-post-form').forEach(function (form) {
        form.addEventListener('submit', function () {
            ensureAntiForgery(form);
            showLoading();
        });
    });
})();