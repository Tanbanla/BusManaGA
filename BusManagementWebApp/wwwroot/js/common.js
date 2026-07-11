/*
  Common helpers for toast notifications and confirmation modals
  Requires Bootstrap 5 JS and CSS present on the page.
*/
(function (window) {
    const TYPE_META = {
        success: { title: 'Thành công', icon: 'success', bgColor: '#71dd37', fgColor: '#ffffff' },
        danger: { title: 'Lỗi', icon: 'error', bgColor: '#ff3e1d', fgColor: '#ffffff' },
        error: { title: 'Lỗi', icon: 'error', bgColor: '#ff3e1d', fgColor: '#ffffff' },
        warning: { title: 'Cảnh báo', icon: 'warning', bgColor: '#ffab00', fgColor: '#111827' },
        info: { title: 'Thông báo', icon: 'info', bgColor: '#03c3ec', fgColor: '#0f172a' },
        primary: { title: 'Thông báo', icon: 'info', bgColor: '#4e7adf', fgColor: '#ffffff' },
        secondary: { title: 'Thông báo', icon: 'info', bgColor: '#74788d', fgColor: '#ffffff' }
    };

    function ensureToastContainer() {
        let ctn = document.getElementById('toastContainer');
        if (!ctn) {
            ctn = document.createElement('div');
            ctn.id = 'toastContainer';
            ctn.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            ctn.style.zIndex = 1080;
            document.body.appendChild(ctn);
        }
        return ctn;
    }

    function buildToast(message, type, title) {
        const meta = TYPE_META[type] || TYPE_META.primary;
        const toastEl = document.createElement('div');
        toastEl.className = 'bs-toast toast fade border-0 shadow-lg rounded-3';
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        const safeTitle = title || meta.title;
        const safeMsg = typeof message === 'string' ? message : JSON.stringify(message);

        toastEl.style.background = `linear-gradient(180deg, ${meta.bgColor} 0%, ${shade(meta.bgColor, -8)} 100%)`;
        toastEl.style.color = meta.fgColor;
        toastEl.style.padding = '0.75rem';

        const btnCloseClass = isLightColor(meta.fgColor) ? 'btn-close' : 'btn-close btn-close-white';

        toastEl.innerHTML = `
      <div class="toast-header bg-transparent border-0" style="color:${meta.fgColor}">
        <i class="fas fa-bell me-2 fs-5" style="color:${meta.fgColor}"></i>
        <strong class="me-auto">${safeTitle}</strong>
        <small>Vừa xong</small>
        <button type="button" class="${btnCloseClass}" data-bs-dismiss="toast" aria-label="Close"></button>
      </div>
      <div class="toast-body">${safeMsg}</div>
    `;
        return toastEl;
    }

    // SweetAlert2 toast hehe
    function toast(message, type = 'primary', opts = {}) {
        const meta = TYPE_META[type] || TYPE_META.primary;
        const delay = typeof opts.delay === 'number' ? opts.delay : 3000;

        if (window.Swal && typeof window.Swal.mixin === 'function') {
            const toastSwal = Swal.mixin({
                //toast: true,
                position: 'bottom-end',
                showConfirmButton: false,
                timer: delay,
                customClass: {
                    confirmButton: "btn btn-label-info btn-wide mx-1",
                    denyButton: "btn btn-label-secondary btn-wide mx-1",
                    cancelButton: "btn btn-label-danger btn-wide mx-1"
                },
                buttonsStyling: !1,
                timerProgressBar: true,
                didOpen: (t) => {
                    t.addEventListener('mouseenter', Swal.stopTimer);
                    t.addEventListener('mouseleave', Swal.resumeTimer);
                }
            });
            const title = opts.title || meta.title;
            // icon uses SweetAlert2 keywords: 'success', 'error', 'warning', 'info', 'question'
            toastSwal.fire({ icon: meta.icon, title: typeof message === 'string' ? message : JSON.stringify(message) });

            return toastSwal;
        }

        const container = ensureToastContainer();
        const el = buildToast(message, type, opts.title);
        container.appendChild(el);
        const inst = new bootstrap.Toast(el, { delay });
        inst.show();
        return inst;
    }
    function toastTopRight(message, type = 'primary', opts = {}) {
        const meta = TYPE_META[type] || TYPE_META.primary;
        const delay = typeof opts.delay === 'number' ? opts.delay : 3000;

        if (window.Swal && typeof window.Swal.mixin === 'function') {
            const toastSwal = Swal.mixin({
                //toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: delay,
                customClass: {
                    confirmButton: "btn btn-label-info btn-wide mx-1",
                    denyButton: "btn btn-label-secondary btn-wide mx-1",
                    cancelButton: "btn btn-label-danger btn-wide mx-1"
                },
                buttonsStyling: !1,
                timerProgressBar: true,
                didOpen: (t) => {
                    t.addEventListener('mouseenter', Swal.stopTimer);
                    t.addEventListener('mouseleave', Swal.resumeTimer);
                }
            });
            const title = opts.title || meta.title;
            // icon uses SweetAlert2 keywords: 'success', 'error', 'warning', 'info', 'question'
            toastSwal.fire({ icon: meta.icon, title: typeof message === 'string' ? message : JSON.stringify(message) });

            return toastSwal;
        }

        const container = ensureToastContainer();
        const el = buildToast(message, type, opts.title);
        container.appendChild(el);
        const inst = new bootstrap.Toast(el, { delay });
        inst.show();
        return inst;
    }
    function success(message, opts) { return toast(message, 'success', opts); }
    function successTopRight(message, opts) { return toastTopRight(message, 'success', opts); }
    function error(message, opts) { return toast(message, 'danger', opts); }
    function info(message, opts) { return toast(message, 'info', opts); }
    function warning(message, opts) { return toast(message, 'warning', opts); }

    function confirm(options) {
        const {
            message = 'Bạn có chắc chắn?',
            title = 'Xác nhận',
            okText = 'Đồng ý',
            cancelText = 'Hủy',
            okVariant = 'primary',
            backdrop = 'static',
            keyboard = false
        } = options || {};

        return new Promise((resolve) => {
            // Build modal
            const wrapper = document.createElement('div');
            wrapper.innerHTML = `
        <div class="modal fade dragable-modal" tabindex="-1" aria-hidden="true">
          <div class="modal-dialog">
            <div class="modal-content">
              <div class="modal-header">
                <h5 class="modal-title"><i class="fas fa-question-circle text-${okVariant} me-2"></i>${title}</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
              </div>
              <div class="modal-body">
                <p class="mb-0">${message}</p>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">${cancelText}</button>
                <button type="button" class="btn btn-${okVariant}" data-role="ok">${okText}</button>
              </div>
            </div>
          </div>
        </div>`;
            const modalEl = wrapper.firstElementChild;
            document.body.appendChild(modalEl);
            const modal = new bootstrap.Modal(modalEl, { backdrop, keyboard });

            const handleOk = () => {
                resolve(true);
                modal.hide();
            };
            const handleCancel = () => {
                resolve(false);
            };
            modalEl.addEventListener('click', (e) => {
                if ((e.target instanceof Element) && e.target.getAttribute('data-role') === 'ok') {
                    handleOk();
                }
            });
            modalEl.addEventListener('hidden.bs.modal', () => {
                modal.dispose();
                modalEl.parentElement?.removeChild(modalEl);
            });
            const dismissBtn = modalEl.querySelector('[data-bs-dismiss="modal"]');
            if (dismissBtn) {
                dismissBtn.addEventListener('click', handleCancel);
            }
            modal.show();
        });
    }

    // Utilities
    function hexToRgb(hex) {
        const normalized = hex.replace('#', '');
        const bigint = parseInt(normalized.length === 3 ? normalized.split('').map(c => c + c).join('') : normalized, 16);
        const r = (bigint >> 16) & 255, g = (bigint >> 8) & 255, b = bigint & 255;
        return { r, g, b };
    }
    function shade(hex, percent) {
        const { r, g, b } = hexToRgb(hex);
        const t = percent < 0 ? 0 : 255;
        const p = Math.abs(percent) / 100;
        const R = Math.round((t - r) * p) + r;
        const G = Math.round((t - g) * p) + g;
        const B = Math.round((t - b) * p) + b;
        return `#${((1 << 24) + (R << 16) + (G << 8) + B).toString(16).slice(1)}`;
    }
    function isLightColor(hex) {
        const { r, g, b } = hexToRgb(hex);
        const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return luminance > 0.7; // if foreground is light, use dark close button
    }

    function init() { ensureToastContainer(); }

    window.AppCommon = {
        init,
        ensureToastContainer,
        toast,
        success,
        successTopRight,
        error,
        info,
        warning,
        confirm
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else { init(); }

})(window);
