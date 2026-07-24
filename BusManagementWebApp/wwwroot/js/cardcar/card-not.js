document.addEventListener('DOMContentLoaded', function () {

    const root = document.querySelector('.notcard-card');

    // url 
    const deleteUrl = root?.dataset.deletecardnotsUrl;
    const printUrl = root?.dataset.printcardUrl;

    //

    const chkAll = document.getElementById('chkAll');

    const btnBulkPrint = document.getElementById('btnBulkPrint');
    const btnBulkDelete = document.getElementById('btnBulkDelete');

    const printForm = document.getElementById('bulkPrintForm');


    async function confirmAction(title, message) {
        if (window.AppCommon && typeof AppCommon.confirm === 'function')
            return await AppCommon.confirm({ title, message });
        return confirm(message);
    }
    function getCheckedItems() {
        return Array.from(
            document.querySelectorAll('.js-print-check:checked')
        );
    }

    function updateButtons() {

        const checkedCount = getCheckedItems().length;

        btnBulkPrint.disabled = checkedCount === 0;
        btnBulkDelete.disabled = checkedCount === 0;

        if (chkAll) {

            const allChecks = document.querySelectorAll('.js-print-check');

            chkAll.checked =
                allChecks.length > 0 &&
                checkedCount === allChecks.length;

            chkAll.indeterminate =
                checkedCount > 0 &&
                checkedCount < allChecks.length;
        }
    }

    // chọn tất cả
    chkAll?.addEventListener('change', function () {

        document
            .querySelectorAll('.js-print-check')
            .forEach(chk => {
                chk.checked = this.checked;
            });

        updateButtons();
    });

    // chọn từng dòng
    document.addEventListener('change', function (e) {

        if (e.target.classList.contains('js-print-check')) {
            updateButtons();
        }
    });

    // In nhiều
    btnBulkPrint?.addEventListener('click', async function () {

        const selected = getCheckedItems();
        if (!selected.length) {
            AppCommon.error('Vui lòng chọn ít nhất một nhân viên.');
            return;
        }

        if (!printUrl) {
            AppCommon.error('URL xóa không hợp lệ.');
            return;
        }
        try {
            const payload = selected.map(chk => Number(chk.value));
            const rep = await fetch(printUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!rep.ok) {
                AppCommon.error('Lỗi khi in.');
                return;
            }

            const result = await rep.json();
            if (!result.Success) {
                AppCommon.error(result.Message || 'Lỗi khi in.');
                return;
            }

            AppCommon.success('In thành công.');

            location.reload();

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }

    });

    // Xóa nhiều
    btnBulkDelete?.addEventListener('click', async function () {

        const selected = getCheckedItems();

        if (!selected.length) {
            AppCommon.error('Vui lòng chọn ít nhất một nhân viên.');
            return;
        }
        const title = btnBulkDelete.dataset.confirmTitle || 'Xác nhận';
        const message = btnBulkDelete.dataset.confirmMessage || 'Bạn có chắc chắn muốn xóa các bản ghi đã chọn?';
        const ok = await confirmAction(title, message);
        if (!ok) return;

        if (!deleteUrl) {
            AppCommon.error('URL xóa không hợp lệ.');
            return;
        }

        try {
            const payload = selected.map(chk => Number(chk.value));
            const rep = await fetch(deleteUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            if (!rep.ok) {
                AppCommon.error('Lỗi khi xóa bản ghi.');
                return;
            }

            const result = await rep.json();
            if (!result.Success) {
                AppCommon.error(result.Message || 'Lỗi khi xóa bản ghi.');
                return;
            }

            AppCommon.success('Xóa bản ghi thành công.');

            location.reload();

        } catch (err) {
            AppCommon.error('Bug: ' + err.message);
            return;
        }
    });

    // In đơn lẻ
    document.querySelectorAll('.js-print-single')
        .forEach(btn => {

            btn.addEventListener('click', async function () {

                const id = Number(this.dataset.employeeId);

                if (!id) {
                    AppCommon.error('Không tìm thấy nhân viên.');
                    return;
                }


                if (!printUrl) {
                    AppCommon.error('URL in không hợp lệ.');
                    return;
                }

                try {

                    const payload = [id];

                    const rep = await fetch(printUrl, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload)
                    });

                    if (!rep.ok) {
                        AppCommon.error('Lỗi khi in.');
                        return;
                    }

                    const result = await rep.json();

                    if (!result.Success) {
                        AppCommon.error(
                            result.Message || 'Lỗi khi in.'
                        );
                        return;
                    }

                    AppCommon.success('In thành công.');

                    location.reload();

                } catch (err) {

                    AppCommon.error(
                        'Bug: ' + err.message
                    );
                }
            });
        });

    updateButtons();
});